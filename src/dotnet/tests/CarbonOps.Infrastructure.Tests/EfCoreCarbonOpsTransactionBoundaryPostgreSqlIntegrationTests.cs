using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CarbonOps.Infrastructure.Tests;

public sealed class EfCoreCarbonOpsTransactionBoundaryPostgreSqlIntegrationTests
{
    private const string TestDsnEnvVar = "CARBONOPS_POSTGRESQL_TEST_DSN";

    [Fact]
    public async Task ExecuteAsync_CommitsInsertedRows_WhenOperationSucceeds()
    {
        var dataSource = CreateOptInDataSourceOrNoOp();
        if (dataSource is null)
        {
            return;
        }

        await using var setupConnection = await dataSource.OpenConnectionAsync();
        ApplyBaselineSchema(setupConnection);

        var factorId = $"it-txn-commit-{Guid.NewGuid():N}";

        await using (var dbContext = CreateDbContext(dataSource))
        {
            var boundary = new EfCoreCarbonOpsTransactionBoundary(dbContext);
            await boundary.ExecuteAsync(async cancellationToken =>
            {
                await dbContext.CarbonFactors.AddAsync(
                    new Domain.CarbonFactor(
                        factorId,
                        "integration-test",
                        "electricity",
                        "grid electricity",
                        0.222m,
                        "kgCO2e/unit",
                        "US-EAST",
                        2025,
                        "txn commit"),
                    cancellationToken);
            });
        }

        await using var verificationContext = CreateDbContext(dataSource);
        var saved = await verificationContext.CarbonFactors
            .AsNoTracking()
            .SingleOrDefaultAsync(f => f.Id == factorId);

        Assert.NotNull(saved);
    }

    [Fact]
    public async Task ExecuteAsync_RollsBackInsertedRows_WhenOperationThrows()
    {
        var dataSource = CreateOptInDataSourceOrNoOp();
        if (dataSource is null)
        {
            return;
        }

        await using var setupConnection = await dataSource.OpenConnectionAsync();
        ApplyBaselineSchema(setupConnection);

        var factorId = $"it-txn-rollback-{Guid.NewGuid():N}";

        await using (var dbContext = CreateDbContext(dataSource))
        {
            var boundary = new EfCoreCarbonOpsTransactionBoundary(dbContext);

            var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                boundary.ExecuteAsync(async cancellationToken =>
                {
                    await dbContext.CarbonFactors.AddAsync(
                        new Domain.CarbonFactor(
                            factorId,
                            "integration-test",
                            "transport",
                            "rail",
                            0.333m,
                            "kgCO2e/unit",
                            "US",
                            2024,
                            "txn rollback"),
                        cancellationToken);

                    throw new InvalidOperationException("expected-failure");
                }));

            Assert.Equal("expected-failure", error.Message);
        }

        await using var verificationContext = CreateDbContext(dataSource);
        var saved = await verificationContext.CarbonFactors
            .AsNoTracking()
            .SingleOrDefaultAsync(f => f.Id == factorId);

        Assert.Null(saved);
    }

    private static NpgsqlDataSource? CreateOptInDataSourceOrNoOp()
    {
        var dsn = Environment.GetEnvironmentVariable(TestDsnEnvVar);
        if (string.IsNullOrWhiteSpace(dsn))
        {
            return null;
        }

        var builder = new NpgsqlConnectionStringBuilder(dsn);
        if (builder.Database is null || !builder.Database.Contains("test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{TestDsnEnvVar} database name must include 'test' for safety.");
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
