using LicenseManagerAPI.Data;
using LicenseManagerAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagerAPI.Services
{
    public class ComplianceEngine
    {
        private readonly LicenseContext _context;

        public ComplianceEngine(LicenseContext context) { _context = context; }

        public async Task<List<ComplianceReportDTO>> GenerateReportAsync()
        {
            var report = new List<ComplianceReportDTO>();
            var licenses = await _context.SoftwareLicenses.ToListAsync();
            var devices = await _context.Devices.Include(d => d.Installations).ToListAsync();

            foreach (var license in licenses)
            {
                int usedCount = 0;
                // Normalize schema: "per_user", "per_device", "concurrent", "subscription"
                string type = license.LicenseType.ToLower().Trim().Replace("-", "_");

                if (type.Contains("per_device"))
                {
                    usedCount = devices.Count(d => d.Installations.Any(i => IsMatch(i.ProductName, license.ProductName)));
                }
                else if (type.Contains("per_user"))
                {
                    usedCount = devices
                        .Where(d => !string.IsNullOrEmpty(d.OwnerUserId) && d.Installations.Any(i => IsMatch(i.ProductName, license.ProductName)))
                        .Select(d => d.OwnerUserId).Distinct().Count();
                }
                else
                {
                    // Concurrent / Subscription / Default
                    usedCount = devices.SelectMany(d => d.Installations)
                                       .Count(i => IsMatch(i.ProductName, license.ProductName));
                }

                // --- SCORING LOGIC ---
                var gap = license.TotalEntitlements - usedCount;
                string status = "Compliant";

                if (gap < 0) status = "Over-licensed"; // Risk (Usage > Owned)
                else if (usedCount == 0 && license.TotalEntitlements > 0) status = "Unused"; // Waste
                else if (gap > 0) status = "Under-licensed"; // Surplus/Available

                report.Add(new ComplianceReportDTO
                {
                    ProductName = license.ProductName,
                    LicenseType = license.LicenseType,
                    TotalEntitlements = license.TotalEntitlements,
                    UsedLicenses = usedCount,
                    Status = status,
                    Gap = gap
                });

                // Auto-log critical events
                if (gap < 0) await LogEvent(license.LicenseId, "overuse", "high", $"Usage {usedCount} exceeds {license.TotalEntitlements}");
            }
            return report;
        }

        // FIX: Fuzzy matching to prevent "Always Unused"
        // Matches if "Visual Studio" is in "Visual Studio 2022" or vice versa
        private bool IsMatch(string installed, string license)
        {
            if (string.IsNullOrEmpty(installed) || string.IsNullOrEmpty(license)) return false;
            var s1 = installed.Trim().ToLower();
            var s2 = license.Trim().ToLower();
            return s1 == s2 || s1.Contains(s2) || s2.Contains(s1);
        }

        private async Task LogEvent(int licenseId, string type, string severity, string details)
        {
            if (!await _context.ComplianceEvents.AnyAsync(e => e.LicenseId == licenseId && e.Type == type && e.DetectedAt > DateTime.Today))
            {
                _context.ComplianceEvents.Add(new ComplianceEvent
                {
                    LicenseId = licenseId,
                    Type = type,
                    Severity = severity,
                    Details = details,
                    Resolved = false,
                    DetectedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }
        }
    }
}