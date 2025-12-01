using System.ComponentModel.DataAnnotations;

namespace LicenseManagerAPI.Models
{
    public class ComplianceEvent
    {
        [Key]
        public int EventId { get; set; }
        public int LicenseId { get; set; } // Foreign Key
        public required string Type { get; set; } // "expiry|overuse|unused"
        public required string Severity { get; set; } // "low|medium|high"
        public DateTime DetectedAt { get; set; } = DateTime.Now;
        public string? Details { get; set; }
        public bool Resolved { get; set; }
        public string? ResolvedBy { get; set; } // userId
        public string? ResolutionNotes { get; set; }
    }
}