using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CarbonOps.Infrastructure.Tests;

public sealed class EfCoreCarbonOpsTransactionBoundaryPostgreSqlIntegrationTests
{
    private const string TestDsnEnvVar = "CARBONOPS_POSTGRESQL_TEST_DSN";
    private static readonly SemaphoreSlim SchemaBootstrapLock = new(1, 1);
    private static bool _baselineSchemaApplied;

    [Fact]
    public async Task ExecuteAsync_CommitsInsertedRows_WhenOperationSucceeds()
    {
        var dataSource = CreateOptInDataSourceOrNoOp();
        if (dataSource is null)
        {
            return;
        }

        await EnsureBaselineSchemaAppliedAsync(dataSource);

        var uniqueSuffix = Guid.NewGuid().ToString("N");
        var factorId = $"it-txn-commit-{uniqueSuffix}";
        var activity = $"grid electricity {uniqueSuffix}";
        var region = $"US-EAST-{uniqueSuffix[..8]}";

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
                        activity,
                        0.222m,
                        "kgCO2e/unit",
                        region,
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

        await EnsureBaselineSchemaAppliedAsync(dataSource);

        var uniqueSuffix = Guid.NewGuid().ToString("N");
        var factorId = $"it-txn-rollback-{uniqueSuffix}";
        var activity = $"rail {uniqueSuffix}";
        var region = $"US-{uniqueSuffix[..8]}";

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
                            activity,
                            0.333m,
                            "kgCO2e/unit",
                            region,
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

    private static async Task EnsureBaselineSchemaAppliedAsync(NpgsqlDataSource dataSource)
    {
        await SchemaBootstrapLock.WaitAsync();
        try
        {
            if (_baselineSchemaApplied)
            {
                return;
            }

            await using var setupConnection = await dataSource.OpenConnectionAsync();
            ApplyBaselineSchema(setupConnection);
            _baselineSchemaApplied = true;
        }
        finally
        {
            SchemaBootstrapLock.Release();
        }
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
