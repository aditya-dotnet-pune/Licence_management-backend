namespace LicenseManagerAPI.Models
{
    public class ComplianceReportDTO
    {
        public string ProductName { get; set; } = "";
        public string LicenseType { get; set; } = "";
        public int TotalEntitlements { get; set; }
        public int UsedLicenses { get; set; }
        public string Status { get; set; } = ""; // "Compliant", "Over-Licensed", "Under-Utilized"
        public int Gap { get; set; } // Positive = Available, Negative = Shortage
    }
}

