using LicenseManagerAPI.Data;
using LicenseManagerAPI.Models;
using LicenseManagerAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public async Task<ActionResult<IEnumerable<ComplianceReportDTO>>> GetComplianceReport()
        {
            var report = await _engine.GenerateReportAsync();
            return Ok(report);
        }

        [HttpGet("alerts")]
        public async Task<ActionResult<IEnumerable<ComplianceEvent>>> GetAlerts()
        {
            return await _context.ComplianceEvents
                .OrderByDescending(e => e.DetectedAt)
                .Take(50)
                .ToListAsync();
        }

        [HttpGet("renewals")]
        public async Task<ActionResult<IEnumerable<RenewalTask>>> GetRenewals()
        {
            return await _context.RenewalTasks.ToListAsync();
        }

        [HttpPost("renewals")]
        public async Task<ActionResult<RenewalTask>> CreateRenewal(RenewalTask task)
        {
            try
            {
                // Validate License Exists
                if (!await _context.SoftwareLicenses.AnyAsync(l => l.LicenseId == task.LicenseId))
                {
                    return BadRequest($"License ID {task.LicenseId} does not exist.");
                }

                _context.RenewalTasks.Add(task);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetRenewals), new { id = task.TaskId }, task);
            }
            catch (Exception ex)
            {
                // This will return the ACTUAL error details to your browser console
                return StatusCode(500, new { message = "Server Error", error = ex.Message, inner = ex.InnerException?.Message });
            }
        }
    }
}