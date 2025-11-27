using LicenseManagerAPI.Data;
using LicenseManagerAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagerAPI.Services
{
    public class ComplianceEngine
    {
        private readonly LicenseContext _context;

        public ComplianceEngine(LicenseContext context)
        {
            _context = context;
        }

        public async Task<List<ComplianceReportDTO>> GenerateReportAsync()
        {
            var report = new List<ComplianceReportDTO>();

            // 1. Fetch Data
            var licenses = await _context.SoftwareLicenses.ToListAsync();
            var devices = await _context.Devices.Include(d => d.Installations).ToListAsync();

            foreach (var license in licenses)
            {
                int usedCount = 0;

                // --- ENTITLEMENT MATCHING LOGIC ---
                if (license.LicenseType.Equals("Per-Device", StringComparison.OrdinalIgnoreCase))
                {
                    usedCount = devices.Count(d => d.Installations.Any(i => IsMatch(i.ProductName, license.ProductName)));
                }
                else if (license.LicenseType.Equals("Per-User", StringComparison.OrdinalIgnoreCase))
                {
                    var uniqueUsers = devices
                        .Where(d => !string.IsNullOrEmpty(d.OwnerUserId) &&
                                    d.Installations.Any(i => IsMatch(i.ProductName, license.ProductName)))
                        .Select(d => d.OwnerUserId)
                        .Distinct()
                        .ToList();
                    usedCount = uniqueUsers.Count;
                }
                else // Concurrent / Subscription / Default
                {
                    usedCount = devices
                        .SelectMany(d => d.Installations)
                        .Count(i => IsMatch(i.ProductName, license.ProductName));
                }

                // --- COMPLIANCE SCORING (Updated) ---

                var gap = license.TotalEntitlements - usedCount;
                string status = "Compliant";

                // Logic for: compliant, under-licensed, over-licensed, unused

                if (gap < 0)
                {
                    // Usage > Entitlements = Illegal/Risk
                    status = "Under-licensed";

                    // Trigger Alert Logic
                    await LogComplianceEvent(license.LicenseId, "Under-Licensed", "High",
                        $"Compliance Violation: {license.ProductName} is used by {usedCount} entities but only {license.TotalEntitlements} licenses are owned.");
                }
                else if (usedCount == 0 && license.TotalEntitlements > 0)
                {
                    // No usage at all
                    status = "Unused";
                }
                else if (gap > 0)
                {
                    // Entitlements > Usage (but usage > 0) = Waste
                    status = "Over-licensed";
                }
                else
                {
                    // Gap == 0
                    status = "Compliant";
                }

                report.Add(new ComplianceReportDTO
                {
                    ProductName = license.ProductName,
                    LicenseType = license.LicenseType,
                    TotalEntitlements = license.TotalEntitlements,
                    UsedLicenses = usedCount,
                    Status = status,
                    Gap = gap
                });
            }

            return report;
        }

        private bool IsMatch(string installedName, string licenseName)
        {
            if (string.IsNullOrEmpty(installedName) || string.IsNullOrEmpty(licenseName)) return false;
            return installedName.Trim().Equals(licenseName.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private async Task LogComplianceEvent(int licenseId, string type, string severity, string details)
        {
            bool exists = await _context.ComplianceEvents.AnyAsync(e =>
                e.LicenseId == licenseId &&
                e.EventType == type &&
                e.DetectedAt > DateTime.Today);

            if (!exists)
            {
                _context.ComplianceEvents.Add(new ComplianceEvent
                {
                    LicenseId = licenseId,
                    EventType = type,
                    Severity = severity,
                    Details = details,
                    DetectedAt = DateTime.Now,
                    IsResolved = false
                });
                await _context.SaveChangesAsync();
            }
        }
    }
}