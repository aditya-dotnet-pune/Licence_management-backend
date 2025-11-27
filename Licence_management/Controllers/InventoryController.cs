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

        // ==========================================
        // LICENSE ENDPOINTS
        // ==========================================

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

        // NEW: Update License (PUT)
        [HttpPut("licenses/{id}")]
        public async Task<IActionResult> UpdateLicense(int id, SoftwareLicense license)
        {
            if (id != license.LicenseId) return BadRequest();

            _context.Entry(license).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.SoftwareLicenses.Any(e => e.LicenseId == id)) return NotFound();
                else throw;
            }

            return NoContent();
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

        // ==========================================
        // DEVICE ENDPOINTS
        // ==========================================

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

        // NEW: Update Device (PUT)
        [HttpPut("devices/{id}")]
        public async Task<IActionResult> UpdateDevice(int id, Device device)
        {
            if (id != device.DeviceId) return BadRequest();

            // We don't want to accidentally wipe out installations when updating the device info
            // So we attach and only modify the main properties
            var existingDevice = await _context.Devices.FindAsync(id);
            if (existingDevice == null) return NotFound();

            existingDevice.Hostname = device.Hostname;
            existingDevice.DeviceType = device.DeviceType;
            existingDevice.OS = device.OS;
            existingDevice.OwnerUserId = device.OwnerUserId;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // NEW: Delete Device (DELETE)
        [HttpDelete("devices/{id}")]
        public async Task<IActionResult> DeleteDevice(int id)
        {
            var device = await _context.Devices.Include(d => d.Installations).FirstOrDefaultAsync(d => d.DeviceId == id);
            if (device == null) return NotFound();

            // Optional: Delete associated installations first (Cascade Delete)
            if (device.Installations != null && device.Installations.Any())
            {
                _context.InstalledSoftware.RemoveRange(device.Installations);
            }

            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("devices/{deviceId}/installations")]
        public async Task<ActionResult<InstalledSoftware>> AddInstallation(int deviceId, InstalledSoftware install)
        {
            var device = await _context.Devices.FindAsync(deviceId);
            if (device == null) return NotFound("Device not found");

            install.DeviceId = deviceId;
            install.Device = null;

            _context.InstalledSoftware.Add(install);
            await _context.SaveChangesAsync();

            return Ok(install);
        }

        // ==========================================
        // IMPORT ENDPOINT
        // ==========================================

        [HttpPost("import")]
        public async Task<IActionResult> ImportLicenses(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

            int count = 0;
            try
            {
                using (var stream = new StreamReader(file.OpenReadStream()))
                {
                    string? line;
                    while ((line = await stream.ReadLineAsync()) != null)
                    {
                        var values = line.Split(',');
                        if (values.Length < 5) continue;
                        if (values[0].Trim().Equals("ProductName", StringComparison.OrdinalIgnoreCase)) continue;

                        try
                        {
                            var license = new SoftwareLicense
                            {
                                ProductName = values[0].Trim(),
                                Vendor = values[1].Trim(),
                                LicenseType = values[2].Trim(),
                                TotalEntitlements = int.Parse(values[3]),
                                Cost = decimal.Parse(values[4]),
                                ExpiryDate = values.Length > 5 && DateTime.TryParse(values[5], out var date)
                                    ? date : DateTime.Now.AddYears(1)
                            };
                            _context.SoftwareLicenses.Add(license);
                            count++;
                        }
                        catch { continue; }
                    }
                }
                await _context.SaveChangesAsync();
                return Ok(new { message = $"Successfully imported {count} licenses." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}