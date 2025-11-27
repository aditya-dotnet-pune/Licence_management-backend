using LicenseManagerAPI.Data;
using LicenseManagerAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly LicenseContext _context;

        public InventoryController(LicenseContext context)
        {
            _context = context;
        }

        // --- License Endpoints ---

        [HttpGet("licenses")]
        public async Task<ActionResult<IEnumerable<SoftwareLicense>>> GetLicenses()
        {
            return await _context.SoftwareLicenses.ToListAsync();
        }

        [HttpPost("licenses")]
        public async Task<ActionResult<SoftwareLicense>> CreateLicense(SoftwareLicense license)
        {
            _context.SoftwareLicenses.Add(license);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetLicenses), new { id = license.LicenseId }, license);
        }

        [HttpDelete("licenses/{id}")]
        public async Task<IActionResult> DeleteLicense(int id)
        {
            var license = await _context.SoftwareLicenses.FindAsync(id);
            if (license == null) return NotFound();

            _context.SoftwareLicenses.Remove(license);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // --- Device Endpoints ---

        [HttpGet("devices")]
        public async Task<ActionResult<IEnumerable<Device>>> GetDevices()
        {
            return await _context.Devices.Include(d => d.Installations).ToListAsync();
        }

        [HttpPost("devices")]
        public async Task<ActionResult<Device>> OnboardDevice(Device device)
        {
            // If installations are sent with the device, EF Core handles them automatically
            _context.Devices.Add(device);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetDevices), new { id = device.DeviceId }, device);
        }
    }
}
