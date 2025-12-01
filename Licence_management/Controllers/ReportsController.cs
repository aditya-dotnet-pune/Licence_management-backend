using LicenseManagerAPI.Data;
using LicenseManagerAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; // Required for RBAC

namespace LicenseManagerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly LicenseContext _context;

        public ReportsController(LicenseContext context)
        {
            _context = context;
        }

        // GET: api/reports/allocations
        // Generates a cost allocation report based on current inventory.
        // Protected Endpoint: Accessible only by Finance, Admin, or Auditor roles.
        [HttpGet("allocations")]
        [Authorize(Roles = "Finance,IT Admin,Auditor")]
        public async Task<ActionResult<IEnumerable<CostAllocation>>> GetCostAllocations()
        {
            var licenses = await _context.SoftwareLicenses.ToListAsync();
            var allocations = new List<CostAllocation>();

            // Mock Departments for cost distribution logic
            // In a real scenario, this would come from a Departments table or User Directory
            var departments = new[] { "Engineering", "Sales", "HR", "IT Ops" };

            // Counter for generating temporary IDs for the report view
            int tempIdCounter = 1;

            foreach (var lic in licenses)
            {
                if (departments.Length == 0) continue;

                // Logic: Evenly distribute the license cost across departments
                // Note: Divide by departments.Length, handling potential division by zero is implicitly handled by the check above.
                var share = lic.Cost / departments.Length;

                foreach (var dept in departments)
                {
                    allocations.Add(new CostAllocation
                    {
                        AllocationId = tempIdCounter++, // Temporary ID for the frontend key
                        LicenseId = lic.LicenseId, // Fixed: Removed .ToString() to match 'int' type in Model
                        DepartmentId = dept,
                        AllocationMethod = "fixed", // Defaulting to 'fixed' split for this demo
                        AllocatedAmount = Math.Round(share, 2),
                        Currency = "USD",
                        PeriodStart = new DateTime(DateTime.Now.Year, 1, 1),
                        PeriodEnd = new DateTime(DateTime.Now.Year, 12, 31)
                    });
                }
            }

            return Ok(allocations);
        }
    }
}