using CarbonOps.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit.Sdk;

namespace CarbonOps.Infrastructure.Tests;

public sealed class EfCoreCarbonFactorRepositoryPostgreSqlIntegrationTests
{
    private const string TestDsnEnvVar = "CARBONOPS_POSTGRESQL_TEST_DSN";

    [Fact]
    public void BaselineSchemaCanBeAppliedAndRepositoryCanReadFromPostgreSql()
    {
        var dataSource = CreateOptInDataSourceOrSkip();

        using var connection = dataSource.OpenConnection();
        ApplyBaselineSchema(connection);

        var testFactorId = $"it-list-{Guid.NewGuid():N}";
        InsertCarbonFactorRow(connection, testFactorId, "electricity", "grid electricity", "US-EAST", 2025);

        using var dbContext = CreateDbContext(dataSource);
        var repository = new EfCoreCarbonFactorRepository(dbContext);

        var factors = repository.ListCarbonFactors();

        Assert.Contains(factors, factor => factor.Id == testFactorId);
    }

    [Fact]
    public void GetCarbonFactorByIdReturnsMatchingRecordAndMissingReturnsNull()
    {
        var dataSource = CreateOptInDataSourceOrSkip();

        using var connection = dataSource.OpenConnection();
        ApplyBaselineSchema(connection);

        var existingFactorId = $"it-get-{Guid.NewGuid():N}";
        InsertCarbonFactorRow(connection, existingFactorId, "transport", "passenger rail", "US", 2024);

        using var dbContext = CreateDbContext(dataSource);
        var repository = new EfCoreCarbonFactorRepository(dbContext);

        var existing = repository.GetCarbonFactorById(existingFactorId);
        var missing = repository.GetCarbonFactorById($"missing-{Guid.NewGuid():N}");

        Assert.NotNull(existing);
        Assert.Equal(existingFactorId, existing.Id);
        Assert.Equal("transport", existing.Category);
        Assert.Null(missing);
    }

    private static NpgsqlDataSource CreateOptInDataSourceOrSkip()
    {
        var dsn = Environment.GetEnvironmentVariable(TestDsnEnvVar);
        if (string.IsNullOrWhiteSpace(dsn))
        {
            throw new SkipException($"PostgreSQL integration tests require {TestDsnEnvVar}.");
        }

        var builder = new NpgsqlConnectionStringBuilder(dsn);
        if (builder.Database is null || !builder.Database.Contains("test", StringComparison.OrdinalIgnoreCase))
        {
            throw new SkipException($"{TestDsnEnvVar} database name must include 'test' for safety.");
        }

        return NpgsqlDataSource.Create(builder.ConnectionString);
    }

    private static CarbonOpsDbContext CreateDbContext(NpgsqlDataSource dataSource)
    {
        var options = new DbContextOptionsBuilder<CarbonOpsDbContext>()
            .UseNpgsql(dataSource)
            .Options;

        return new CarbonOpsDbContext(options);
    }

    private static void ApplyBaselineSchema(NpgsqlConnection connection)
    {
        var planner = new PostgreSqlSchemaBootstrapPlanner(
            new PostgreSqlSchemaScriptCatalog(GetInfrastructureProjectPath()),
            new PostgreSqlSchemaSafetyValidator());

        var scripts = planner.CreatePlan();
        foreach (var script in scripts)
        {
            using var command = connection.CreateCommand();
            command.CommandText = script.Sql;
            command.ExecuteNonQuery();
        }
    }

    private static void InsertCarbonFactorRow(
        NpgsqlConnection connection,
        string factorId,
        string category,
        string activity,
        string region,
        int year)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO carbonops.carbon_factors
            (factor_id, source_provider, category, activity, region, factor_year, factor_value, factor_unit, notes)
            VALUES (@factorId, @sourceProvider, @category, @activity, @region, @factorYear, @factorValue, @factorUnit, @notes)
            ON CONFLICT (factor_id) DO NOTHING;
            """;

        command.Parameters.AddWithValue("factorId", factorId);
        command.Parameters.AddWithValue("sourceProvider", "integration-test");
        command.Parameters.AddWithValue("category", category);
        command.Parameters.AddWithValue("activity", activity);
        command.Parameters.AddWithValue("region", region);
        command.Parameters.AddWithValue("factorYear", year);
        command.Parameters.AddWithValue("factorValue", 0.12345678m);
        command.Parameters.AddWithValue("factorUnit", "kgCO2e/unit");
        command.Parameters.AddWithValue("notes", "integration-test-row");

        command.ExecuteNonQuery();
    }

    private static string GetInfrastructureProjectPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "src", "CarbonOps.Infrastructure");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate src/CarbonOps.Infrastructure for schema script catalog.");
    }
}
