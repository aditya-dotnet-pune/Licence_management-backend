using LicenseManagerAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagerAPI.Data
{
    public class LicenseContext : DbContext
    {
        public LicenseContext(DbContextOptions<LicenseContext> options) : base(options) { }

        public DbSet<SoftwareLicense> SoftwareLicenses { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<InstalledSoftware> InstalledSoftware { get; set; }
        public DbSet<ComplianceEvent> ComplianceEvents { get; set; }
        public DbSet<RenewalTask> RenewalTasks { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<CostAllocation> CostAllocations { get; set; }
    }
}