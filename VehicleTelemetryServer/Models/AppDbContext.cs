using Microsoft.EntityFrameworkCore;

namespace VehicleTelemetryServer.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<RawTelemetryData> RawTelemetry { get; set; }
        public DbSet<PossibleFailureData> PossibleFailures { get; set; }
    }
}
