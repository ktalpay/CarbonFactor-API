using CarbonOps.Domain;
using Microsoft.EntityFrameworkCore;

namespace CarbonOps.Infrastructure;

public sealed class CarbonOpsDbContext(DbContextOptions<CarbonOpsDbContext> options) : DbContext(options)
{
    public DbSet<CarbonFactor> CarbonFactors => Set<CarbonFactor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CarbonOpsDbContext).Assembly);
    }
}
