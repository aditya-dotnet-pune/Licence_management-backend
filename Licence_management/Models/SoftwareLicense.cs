using System.ComponentModel.DataAnnotations;

namespace LicenseManagerAPI.Models
{
    public class SoftwareLicense
    {
        [Key]
        public int LicenseId { get; set; } // Keeping int for DB performance, mapped to string in JSON if needed
        public required string ProductName { get; set; }
        public required string Vendor { get; set; }
        // Schema: "per_user|per_device|concurrent|subscription"
        public required string LicenseType { get; set; }
        public int TotalEntitlements { get; set; }
        public decimal Cost { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime PurchaseDate { get; set; } = DateTime.Now;
    }
}