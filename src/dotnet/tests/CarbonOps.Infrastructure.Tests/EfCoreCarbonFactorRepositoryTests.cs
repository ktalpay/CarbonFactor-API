using CarbonOps.Domain;
using Microsoft.EntityFrameworkCore;

namespace CarbonOps.Infrastructure.Tests;

public sealed class EfCoreCarbonFactorRepositoryTests
{
    [Fact]
    public void ListCarbonFactorsReturnsAllFactors()
    {
        using var dbContext = CreateDbContext();
        dbContext.CarbonFactors.AddRange(
            new CarbonFactor("f-003", "synthetic", "electricity", "onsite solar", 0.05m, "kgCO2e/kWh", "TR", 2023, "low-carbon sample"),
            new CarbonFactor("f-001", "synthetic", "electricity", "grid electricity", 0.42m, "kgCO2e/kWh", "US-WEST", 2024, "grid sample"));
        dbContext.SaveChanges();

        var repository = new EfCoreCarbonFactorRepository(dbContext);

        var factors = repository.ListCarbonFactors();

        Assert.Equal(2, factors.Count);
        Assert.Contains(factors, factor => factor.Id == "f-001");
        Assert.Contains(factors, factor => factor.Id == "f-003");
    }

    [Fact]
    public void GetCarbonFactorByIdReturnsMatchingFactor()
    {
        using var dbContext = CreateDbContext();
        dbContext.CarbonFactors.Add(new CarbonFactor("f-002", "synthetic", "transport", "passenger vehicle", 0.19m, "kgCO2e/km", "US", 2024, "vehicle sample"));
        dbContext.SaveChanges();

        var repository = new EfCoreCarbonFactorRepository(dbContext);

        var factor = repository.GetCarbonFactorById("f-002");

        Assert.NotNull(factor);
        Assert.Equal("transport", factor.Category);
        Assert.Equal("US", factor.Region);
    }

    private static CarbonOpsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarbonOpsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new CarbonOpsDbContext(options);
    }
}
