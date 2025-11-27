using System.ComponentModel.DataAnnotations;

namespace LicenseManagerAPI.Models
{
    public class SoftwareLicense
    {
        [Key]
        public int LicenseId { get; set; }
        public required string ProductName { get; set; }
        public required string Vendor { get; set; }
        public required string LicenseType { get; set; } // Per-User, Per-Device, etc.
        public int TotalEntitlements { get; set; }
        public decimal Cost { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime PurchaseDate { get; set; } = DateTime.Now;
    }
}
