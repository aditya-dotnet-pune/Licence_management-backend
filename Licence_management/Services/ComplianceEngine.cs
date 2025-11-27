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

            // 1. Fetch all licenses and installations
            var licenses = await _context.SoftwareLicenses.ToListAsync();
            var devices = await _context.Devices.Include(d => d.Installations).ToListAsync();

            foreach (var license in licenses)
            {
                int usedCount = 0;

                // 2. Logic: Entitlement Matching
                if (license.LicenseType == "Per-Device" || license.LicenseType == "Concurrent")
                {
                    // Count total installations across all devices
                    usedCount = devices.SelectMany(d => d.Installations)
                                       .Count(i => i.ProductName.Equals(license.ProductName, StringComparison.OrdinalIgnoreCase));
                }
                else if (license.LicenseType == "Per-User")
                {
                    // Count distinct users who have this software installed on ANY of their devices
                    // (Note: Requires Device to have an OwnerUserId)
                    var userInstallations = devices
                        .Where(d => !string.IsNullOrEmpty(d.OwnerUserId) &&
                                    d.Installations.Any(i => i.ProductName.Equals(license.ProductName, StringComparison.OrdinalIgnoreCase)))
                        .Select(d => d.OwnerUserId)
                        .Distinct()
                        .Count();

                    usedCount = userInstallations;
                }
                else // Subscription
                {
                    // Simple count of installs for now
                    usedCount = devices.SelectMany(d => d.Installations)
                                      .Count(i => i.ProductName.Equals(license.ProductName, StringComparison.OrdinalIgnoreCase));
                }

                // 3. Logic: Compliance Scoring
                var gap = license.TotalEntitlements - usedCount;
                string status = "Compliant";

                if (gap < 0) status = "Over-Licensed"; // CRITICAL: Using more than owned
                else if (gap > 5) status = "Under-Utilized"; // Warning: Wasting money

                report.Add(new ComplianceReportDTO
                {
                    ProductName = license.ProductName,
                    LicenseType = license.LicenseType,
                    TotalEntitlements = license.TotalEntitlements,
                    UsedLicenses = usedCount,
                    Status = status,
                    Gap = gap
                });

                // 4. Auto-Generate Alerts (Side effect logic)
                if (status == "Over-Licensed")
                {
                    await LogComplianceEvent(license.LicenseId, "OverUsage", "High",
                        $"Detected {usedCount} installs but only {license.TotalEntitlements} licenses owned.");
                }
            }

            return report;
        }

        private async Task LogComplianceEvent(int licenseId, string type, string severity, string details)
        {
            // Avoid duplicate alerts for the same day
            bool exists = await _context.ComplianceEvents.AnyAsync(e =>
                e.LicenseId == licenseId &&
                e.EventType == type &&
                e.DetectedAt > DateTime.Now.AddDays(-1));

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
