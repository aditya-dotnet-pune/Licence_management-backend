using LicenseManagerAPI.Data;
using LicenseManagerAPI.Models;
using LicenseManagerAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; // Added for RBAC

namespace LicenseManagerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComplianceController : ControllerBase
    {
        private readonly ComplianceEngine _engine;
        private readonly LicenseContext _context;

        public ComplianceController(ComplianceEngine engine, LicenseContext context)
        {
            _engine = engine;
            _context = context;
        }

        [HttpGet("report")]
        [Authorize(Roles = "IT Admin,Finance,Auditor,Viewer")] // Everyone can view
        public async Task<ActionResult<IEnumerable<ComplianceReportDTO>>> GetComplianceReport()
        {
            var report = await _engine.GenerateReportAsync();
            return Ok(report);
        }

        [HttpGet("alerts")]
        [Authorize(Roles = "IT Admin,Auditor")] // Only Admin and Auditor
        public async Task<ActionResult<IEnumerable<ComplianceEvent>>> GetAlerts()
        {
            return await _context.ComplianceEvents.OrderByDescending(e => e.DetectedAt).Take(50).ToListAsync();
        }

        [HttpGet("renewals")]
        [Authorize(Roles = "IT Admin,Finance")]
        public async Task<ActionResult<IEnumerable<RenewalTask>>> GetRenewals()
        {
            return await _context.RenewalTasks.ToListAsync();
        }

        [HttpPost("renewals")]
        [Authorize(Roles = "IT Admin,Finance")] // Only Admin/Finance can create tasks
        public async Task<ActionResult<RenewalTask>> CreateRenewal(RenewalTask task)
        {
            try
            {
                if (!await _context.SoftwareLicenses.AnyAsync(l => l.LicenseId == task.LicenseId))
                    return BadRequest($"License ID {task.LicenseId} not found.");

                _context.RenewalTasks.Add(task);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetRenewals), new { id = task.TaskId }, task);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
