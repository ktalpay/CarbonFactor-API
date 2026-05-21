using Microsoft.Extensions.DependencyInjection;

namespace CarbonOps.Infrastructure.Tests;

public sealed class PostgreSqlSchemaBootstrapPlannerTests
{
    [Fact]
    public void CreatePlanReturnsScriptsInManifestOrder()
    {
        var planner = CreatePlanner();

        var plan = planner.CreatePlan();

        Assert.NotEmpty(plan);
        Assert.Equal("001_carbon_factor_schema.sql", plan[0].Name);
        Assert.Contains("create schema if not exists carbonops", plan[0].Sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BaselineScriptIsDiscoverableFromCatalog()
    {
        var catalog = new PostgreSqlSchemaScriptCatalog(GetInfrastructureProjectRoot());

        var scripts = catalog.GetScripts();

        var baseline = Assert.Single(scripts.Where(script => script.Name == "001_carbon_factor_schema.sql"));
        Assert.Equal("Database/postgresql/001_carbon_factor_schema.sql", baseline.RelativePath);
    }

    [Fact]
    public void SchemaStrategyIsRegisteredButNotExecutedByDependencyInjection()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddCarbonFactorServices();

        using var serviceProvider = serviceCollection.BuildServiceProvider();

        var planner = serviceProvider.GetRequiredService<IPostgreSqlSchemaBootstrapPlanner>();
        Assert.IsType<PostgreSqlSchemaBootstrapPlanner>(planner);
    }

    private static IPostgreSqlSchemaBootstrapPlanner CreatePlanner()
    {
        var rootPath = GetInfrastructureProjectRoot();
        return new PostgreSqlSchemaBootstrapPlanner(
            new PostgreSqlSchemaScriptCatalog(rootPath),
            new PostgreSqlSchemaSafetyValidator());
    }

    private static string GetInfrastructureProjectRoot()
    {
        var testBasePath = AppContext.BaseDirectory;
        return Path.GetFullPath(Path.Combine(testBasePath, "../../../../src/CarbonOps.Infrastructure"));
    }
}
