namespace CarbonOps.Infrastructure;

public sealed class PostgreSqlSchemaBootstrapper(
    IPostgreSqlSchemaBootstrapPlanner planner,
    IPostgreSqlSchemaInspector inspector,
    IPostgreSqlSchemaExecutor executor) : IPostgreSqlSchemaBootstrapper
{
    public async Task<PostgreSqlSchemaBootstrapResult> BootstrapAsync(
        PostgreSqlSchemaBootstrapMode mode,
        CancellationToken cancellationToken = default)
    {
        var scripts = planner.CreatePlan();
        var initialInspection = await InspectSafelyAsync(cancellationToken);

        if (initialInspection.IsValid)
        {
            return new PostgreSqlSchemaBootstrapResult(mode, scripts.Count, 0, "valid");
        }

        if (mode == PostgreSqlSchemaBootstrapMode.ValidateOnly)
        {
            throw new PostgreSqlSchemaBootstrapException(
                "PostgreSQL schema validation failed: required schema objects are missing.");
        }

        await ExecuteSafelyAsync(scripts, cancellationToken);

        var followUpInspection = await InspectSafelyAsync(cancellationToken);
        if (!followUpInspection.IsValid)
        {
            throw new PostgreSqlSchemaBootstrapException(
                "PostgreSQL schema bootstrap failed: required schema objects are missing after bootstrap.");
        }

        return new PostgreSqlSchemaBootstrapResult(mode, scripts.Count, scripts.Count, "created_missing");
    }

    private async Task<PostgreSqlSchemaInspectionResult> InspectSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await inspector.InspectAsync(cancellationToken);
        }
        catch (PostgreSqlSchemaBootstrapException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new PostgreSqlSchemaBootstrapException(
                "PostgreSQL schema bootstrap failed: check database connectivity and bootstrap configuration.");
        }
    }

    private async Task ExecuteSafelyAsync(
        IReadOnlyList<PostgreSqlSchemaScript> scripts,
        CancellationToken cancellationToken)
    {
        try
        {
            await executor.ExecuteAsync(scripts, cancellationToken);
        }
        catch (PostgreSqlSchemaBootstrapException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new PostgreSqlSchemaBootstrapException(
                "PostgreSQL schema bootstrap failed: required schema objects could not be created.");
        }
    }
}
