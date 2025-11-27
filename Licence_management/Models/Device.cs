using System.ComponentModel.DataAnnotations;

namespace LicenseManagerAPI.Models
{
    public class Device
    {
        [Key]
        public int DeviceId { get; set; }
        public required string Hostname { get; set; }
        public string? OwnerUserId { get; set; }
        public required string DeviceType { get; set; } // Laptop, Server
        public string? OS { get; set; }
        public DateTime LastCheckIn { get; set; } = DateTime.Now;

        // Navigation property for EF Core (Optional for basic CRUD)
        public List<InstalledSoftware> Installations { get; set; } = new();
    }
}


