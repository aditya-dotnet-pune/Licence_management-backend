using System.ComponentModel.DataAnnotations;

namespace LicenseManagerAPI.Models
{
    public class RenewalTask
    {
        [Key]
        public int TaskId { get; set; }
        public int LicenseId { get; set; }
        public required string Status { get; set; } // Pending, Approved
        public string? AssignedTo { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public decimal CostEstimate { get; set; }
    }
}
