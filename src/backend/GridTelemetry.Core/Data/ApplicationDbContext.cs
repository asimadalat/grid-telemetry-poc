using GridTelemetry.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace GridTelemetry.Core.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<SubstationMetric> SubstationMetrics => Set<SubstationMetric>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder
            .Entity<SubstationMetric>()
            .HasIndex(metric => metric.SubstationCode);

        modelBuilder
            .Entity<SubstationMetric>()
            .HasIndex(metric => metric.Timestamp);
    }
}