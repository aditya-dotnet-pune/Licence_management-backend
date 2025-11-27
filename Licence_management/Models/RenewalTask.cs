using System.ComponentModel.DataAnnotations;

namespace LicenseManagerAPI.Models
{
    public class RenewalTask
    {
        [Key]
        public int TaskId { get; set; }

        public int LicenseId { get; set; }

        public required string Status { get; set; } // Pending, QuoteRequested, Approved

        public string? AssignedTo { get; set; }

        // Change to nullable DateTime? to prevent SQL crashes on empty dates
        public DateTime? DueDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public decimal CostEstimate { get; set; }

        // NEW FIELD
        public string? QuoteReference { get; set; }
    }
}