using System.ComponentModel.DataAnnotations;

namespace LicenseManagerAPI.Models
{
    public class CostAllocation
    {
        [Key]
        public int AllocationId { get; set; }
        public int LicenseId { get; set; }
        public required string DepartmentId { get; set; } // e.g. "Engineering"
        public required string AllocationMethod { get; set; } // "fixed|usage_based"
        public decimal AllocatedAmount { get; set; }
        public string Currency { get; set; } = "USD";
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }
}