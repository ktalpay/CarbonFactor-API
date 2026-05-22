namespace CarbonOps.Infrastructure.Tests;

public sealed class PostgreSqlSchemaBootstrapperTests
{
    private const string SecretConnectionString = "Host=db.example.internal;Username=carbonops;Password=should-not-leak";

    [Fact]
    public async Task ValidateOnlyWithMissingSchemaFailsWithoutExecutingScripts()
    {
        var executor = new CapturingSchemaExecutor();
        var bootstrapper = CreateBootstrapper(
            new QueueSchemaInspector(new PostgreSqlSchemaInspectionResult(["table carbonops.carbon_factors"])),
            executor);

        var exception = await Assert.ThrowsAsync<PostgreSqlSchemaBootstrapException>(() =>
            bootstrapper.BootstrapAsync(PostgreSqlSchemaBootstrapMode.ValidateOnly));

        Assert.Contains("PostgreSQL schema validation failed", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, executor.ExecuteCallCount);
    }

    [Fact]
    public async Task ValidateOnlyWithSchemaPresentSucceedsWithoutExecutingScripts()
    {
        var executor = new CapturingSchemaExecutor();
        var bootstrapper = CreateBootstrapper(new QueueSchemaInspector(PostgreSqlSchemaInspectionResult.Valid), executor);

        var result = await bootstrapper.BootstrapAsync(PostgreSqlSchemaBootstrapMode.ValidateOnly);

        Assert.Equal(PostgreSqlSchemaBootstrapMode.ValidateOnly, result.Mode);
        Assert.Equal(1, result.ScriptsPlanned);
        Assert.Equal(0, result.ScriptsExecuted);
        Assert.Equal("valid", result.ValidationStatus);
        Assert.Equal(0, executor.ExecuteCallCount);
    }

    [Fact]
    public async Task CreateMissingExecutesSafeScriptsWhenSchemaIsMissing()
    {
        var executor = new CapturingSchemaExecutor();
        var bootstrapper = CreateBootstrapper(
            new QueueSchemaInspector(
                new PostgreSqlSchemaInspectionResult(["schema carbonops"]),
                PostgreSqlSchemaInspectionResult.Valid),
            executor);

        var result = await bootstrapper.BootstrapAsync(PostgreSqlSchemaBootstrapMode.CreateMissing);

        Assert.Equal(PostgreSqlSchemaBootstrapMode.CreateMissing, result.Mode);
        Assert.Equal(1, result.ScriptsPlanned);
        Assert.Equal(1, result.ScriptsExecuted);
        Assert.Equal("created_missing", result.ValidationStatus);
        Assert.Equal(1, executor.ExecuteCallCount);
        Assert.Equal("001_safe.sql", Assert.Single(executor.ExecutedScripts).Name);
    }

    [Fact]
    public async Task CreateMissingIsIdempotentWhenSchemaExistsAfterFirstBootstrap()
    {
        var executor = new CapturingSchemaExecutor();
        var bootstrapper = CreateBootstrapper(
            new QueueSchemaInspector(
                new PostgreSqlSchemaInspectionResult(["schema carbonops"]),
                PostgreSqlSchemaInspectionResult.Valid,
                PostgreSqlSchemaInspectionResult.Valid),
            executor);

        await bootstrapper.BootstrapAsync(PostgreSqlSchemaBootstrapMode.CreateMissing);
        var secondResult = await bootstrapper.BootstrapAsync(PostgreSqlSchemaBootstrapMode.CreateMissing);

        Assert.Equal(1, executor.ExecuteCallCount);
        Assert.Equal(0, secondResult.ScriptsExecuted);
        Assert.Equal("valid", secondResult.ValidationStatus);
    }

    [Fact]
    public async Task UnsafeSqlIsRejectedBeforeExecution()
    {
        var executor = new CapturingSchemaExecutor();
        var bootstrapper = new PostgreSqlSchemaBootstrapper(
            new PostgreSqlSchemaBootstrapPlanner(new UnsafeSchemaScriptCatalog(), new PostgreSqlSchemaSafetyValidator()),
            new QueueSchemaInspector(new PostgreSqlSchemaInspectionResult(["table carbonops.carbon_factors"])),
            executor);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            bootstrapper.BootstrapAsync(PostgreSqlSchemaBootstrapMode.CreateMissing));

        Assert.Equal(0, executor.ExecuteCallCount);
    }

    [Fact]
    public async Task ExecutorFailureDoesNotLeakConnectionStringOrCredentials()
    {
        var bootstrapper = new PostgreSqlSchemaBootstrapper(
            new StaticSchemaBootstrapPlanner(),
            new QueueSchemaInspector(new PostgreSqlSchemaInspectionResult(["schema carbonops"])),
            new ThrowingSchemaExecutor(SecretConnectionString));

        var exception = await Assert.ThrowsAsync<PostgreSqlSchemaBootstrapException>(() =>
            bootstrapper.BootstrapAsync(PostgreSqlSchemaBootstrapMode.CreateMissing));

        Assert.DoesNotContain("db.example.internal", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("carbonops", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("should-not-leak", exception.Message, StringComparison.Ordinal);
        Assert.Null(exception.InnerException);
    }

    private static PostgreSqlSchemaBootstrapper CreateBootstrapper(
        IPostgreSqlSchemaInspector inspector,
        IPostgreSqlSchemaExecutor executor)
    {
        return new PostgreSqlSchemaBootstrapper(
            new StaticSchemaBootstrapPlanner(),
            inspector,
            executor);
    }

    private sealed class StaticSchemaBootstrapPlanner : IPostgreSqlSchemaBootstrapPlanner
    {
        public IReadOnlyList<PostgreSqlSchemaScript> CreatePlan()
        {
            return
            [
                new PostgreSqlSchemaScript(
                    "001_safe.sql",
                    "Database/postgresql/001_safe.sql",
                    "create schema if not exists carbonops;")
            ];
        }
    }

    private sealed class UnsafeSchemaScriptCatalog : IPostgreSqlSchemaScriptCatalog
    {
        public IReadOnlyList<PostgreSqlSchemaScript> GetScripts()
        {
            return
            [
                new PostgreSqlSchemaScript(
                    "999_unsafe.sql",
                    "Database/postgresql/999_unsafe.sql",
                    "drop table carbonops.carbon_factors;")
            ];
        }
    }

    private sealed class QueueSchemaInspector(params PostgreSqlSchemaInspectionResult[] results) : IPostgreSqlSchemaInspector
    {
        private readonly Queue<PostgreSqlSchemaInspectionResult> results = new(results);

        public Task<PostgreSqlSchemaInspectionResult> InspectAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(results.Count > 1 ? results.Dequeue() : results.Peek());
        }
    }

    private sealed class CapturingSchemaExecutor : IPostgreSqlSchemaExecutor
    {
        private readonly List<PostgreSqlSchemaScript> executedScripts = [];

        public int ExecuteCallCount { get; private set; }

        public IReadOnlyList<PostgreSqlSchemaScript> ExecutedScripts => executedScripts;

        public Task ExecuteAsync(IReadOnlyList<PostgreSqlSchemaScript> scripts, CancellationToken cancellationToken = default)
        {
            ExecuteCallCount++;
            executedScripts.AddRange(scripts);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingSchemaExecutor(string failureMessage) : IPostgreSqlSchemaExecutor
    {
        public Task ExecuteAsync(IReadOnlyList<PostgreSqlSchemaScript> scripts, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(failureMessage);
        }
    }
}
