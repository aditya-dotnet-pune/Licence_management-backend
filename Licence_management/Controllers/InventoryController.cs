using LicenseManagerAPI.Data;
using LicenseManagerAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; // Needed for RBAC if applied later

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
            _context.Devices.Add(device);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetDevices), new { id = device.DeviceId }, device);
        }

        [HttpPut("devices/{id}")]
        public async Task<IActionResult> UpdateDevice(int id, Device device)
        {
            if (id != device.DeviceId) return BadRequest();
            _context.Entry(device).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("devices/{id}")]
        public async Task<IActionResult> DeleteDevice(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null) return NotFound();
            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // --- Installation Endpoints ---

        [HttpPost("devices/{deviceId}/installations")]
        public async Task<IActionResult> AddInstallation(int deviceId, InstalledSoftware install)
        {
            if (deviceId != install.DeviceId) return BadRequest("Device ID mismatch");

            var device = await _context.Devices.FindAsync(deviceId);
            if (device == null) return NotFound("Device not found");

            _context.InstalledSoftware.Add(install);
            await _context.SaveChangesAsync();
            return Ok(install);
        }

        // --- Import Endpoints ---

        [HttpPost("import")]
        public async Task<IActionResult> ImportCsv(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

            using (var stream = file.OpenReadStream())
            using (var reader = new StreamReader(stream))
            {
                await reader.ReadLineAsync(); // Skip header

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var values = line.Split(',');
                    if (values.Length >= 4)
                    {
                        var lic = new SoftwareLicense
                        {
                            ProductName = values[0].Trim(),
                            Vendor = values[1].Trim(),
                            LicenseType = values[2].Trim().ToLower().Replace(" ", "_").Replace("-", "_"),
                            TotalEntitlements = int.TryParse(values[3], out int count) ? count : 0,
                            Cost = (values.Length > 4 && decimal.TryParse(values[4], out decimal cost)) ? cost : 0
                        };
                        _context.SoftwareLicenses.Add(lic);
                    }
                }
                await _context.SaveChangesAsync();
            }
            return Ok(new { message = "CSV Imported Successfully" });
        }
    }
}