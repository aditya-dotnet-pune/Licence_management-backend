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

        // GET: api/compliance/report
        // returns the calculated compliance status for the dashboard
        [HttpGet("report")]
        public async Task<ActionResult<IEnumerable<ComplianceReportDTO>>> GetComplianceReport()
        {
            var report = await _engine.GenerateReportAsync();
            return Ok(report);
        }

        // GET: api/compliance/alerts
        // Returns the audit log of alerts
        [HttpGet("alerts")]
        public async Task<ActionResult<IEnumerable<ComplianceEvent>>> GetAlerts()
        {
            return await _context.ComplianceEvents
                .OrderByDescending(e => e.DetectedAt)
                .Take(50)
                .ToListAsync();
        }

        // GET: api/compliance/renewals
        [HttpGet("renewals")]
        public async Task<ActionResult<IEnumerable<RenewalTask>>> GetRenewals()
        {
            return await _context.RenewalTasks.ToListAsync();
        }

        // POST: api/compliance/renewals
        [HttpPost("renewals")]
        public async Task<ActionResult<RenewalTask>> CreateRenewal(RenewalTask task)
        {
            _context.RenewalTasks.Add(task);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetRenewals), new { id = task.TaskId }, task);
        }
    }
}