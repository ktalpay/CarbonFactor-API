using CarbonOps.Application.Factors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarbonOps.Infrastructure.Tests;

public sealed class InMemoryCarbonFactorRepositoryTests
{
    [Fact]
    public void ListCarbonFactorsReturnsDeterministicSeedData()
    {
        var repository = new InMemoryCarbonFactorRepository();

        var factors = repository.ListCarbonFactors().ToList();

        Assert.Equal(["f-001", "f-002", "f-003"], factors.Select(factor => factor.Id));
        Assert.Equal("synthetic", factors[0].Source);
        Assert.Equal("grid electricity", factors[0].Activity);
        Assert.Equal(0.42m, factors[0].FactorValue);
        Assert.Equal("kgCO2e/kWh", factors[0].FactorUnit);
        Assert.Equal("US-WEST", factors[0].Region);
        Assert.Equal(2024, factors[0].Year);
        Assert.Equal("grid sample", factors[0].Notes);
    }

    [Fact]
    public void GetCarbonFactorByIdReturnsMatchingSeededFactor()
    {
        var repository = new InMemoryCarbonFactorRepository();

        var factor = repository.GetCarbonFactorById("f-002");

        Assert.NotNull(factor);
        Assert.Equal("transport", factor.Category);
        Assert.Equal("passenger vehicle", factor.Activity);
        Assert.Equal("US", factor.Region);
    }

    [Fact]
    public void GetCarbonFactorByIdReturnsNullWhenFactorDoesNotExist()
    {
        var repository = new InMemoryCarbonFactorRepository();

        var factor = repository.GetCarbonFactorById("missing-factor");

        Assert.Null(factor);
    }

    [Fact]
    public void AddCarbonFactorServicesRegistersRepositoryAndUseCases()
    {
        var services = new ServiceCollection();
        services.AddCarbonFactorServices();

        using var provider = services.BuildServiceProvider();

        var repository = provider.GetRequiredService<ICarbonFactorRepository>();
        var useCases = provider.GetRequiredService<CarbonFactorUseCases>();
        var response = useCases.ListCarbonFactors();

        Assert.IsType<InMemoryCarbonFactorRepository>(repository);
        Assert.Equal(3, response.Total);
        Assert.Equal(["f-001", "f-002", "f-003"], response.Factors.Select(factor => factor.Id));
    }

    [Fact]
    public void AddCarbonFactorServicesRegistersDbContextWhenPostgreSqlEnabled()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:UsePostgreSql"] = "true",
                ["Persistence:PostgreSql:ConnectionString"] = "Host=localhost;Database=carbonops;Username=test;Password=test"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddCarbonFactorServices(config);

        using var provider = services.BuildServiceProvider();
        var dbContextOptions = provider.GetRequiredService<DbContextOptions<CarbonOpsDbContext>>();

        Assert.NotNull(dbContextOptions);
    }
}
