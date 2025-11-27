using System.ComponentModel.DataAnnotations;

namespace LicenseManagerAPI.Models
{
    public class ComplianceEvent
    {
        [Key]
        public int EventId { get; set; }
        public int LicenseId { get; set; }
        public required string EventType { get; set; } // Expiry, OverUsage
        public required string Severity { get; set; } // High, Medium, Low
        public DateTime DetectedAt { get; set; } = DateTime.Now;
        public string? Details { get; set; }
        public bool IsResolved { get; set; }
        public string? ResolutionNotes { get; set; }
    }
}
