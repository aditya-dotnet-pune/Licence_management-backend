namespace LicenseManagerAPI.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    public class InstalledSoftware
    {
        [Key]
        public int InstallId { get; set; }
        public int DeviceId { get; set; }
        public required string ProductName { get; set; }
        public string? Version { get; set; }
        public DateTime InstallDate { get; set; } = DateTime.Now;

        [JsonIgnore] // Prevent cyclic reference loops
        public Device? Device { get; set; }
    }
}
