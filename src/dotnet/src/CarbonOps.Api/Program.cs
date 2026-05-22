using CarbonOps.Api;
using CarbonOps.Infrastructure;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddOptions<ApiKeyAuthenticationOptions>()
    .Bind(builder.Configuration.GetSection(ApiKeyAuthenticationOptions.SectionName));
builder.Services.AddSingleton<IAuditEventSink, LoggingAuditEventSink>();
builder.Services.AddSingleton<CarbonOpsProductionConfigurationValidator>();
builder.Services.AddCarbonOpsRateLimiting(builder.Configuration);
builder.Services.AddCarbonFactorServices(builder.Configuration);
var app = builder.Build();

ValidateProductionConfiguration(app);
await RunPostgreSqlBootstrapAsync(app);
LogStartupConfiguration(app);

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseRouting();
app.UseRateLimiter();
app.UseMiddleware<ApiErrorMappingMiddleware>();

app.MapOperationalEndpoints();
app.MapCarbonFactorEndpoints();

app.Run();

static void LogStartupConfiguration(WebApplication app)
{
    var apiKeyOptions = app.Services.GetRequiredService<IOptions<ApiKeyAuthenticationOptions>>().Value;
    var configuration = app.Services.GetRequiredService<IConfiguration>();
    var logger = app.Services
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("CarbonOps.Api.Startup");

    logger.LogInformation(
        "CarbonOps API startup configuration loaded {persistence_provider} {api_key_hash_configured} {previous_key_hash_count} {revoked_key_hash_count} {tenant_configured} {import_scope_count}",
        configuration.GetValue<bool>("Persistence:UsePostgreSql") ? "postgresql" : "in_memory",
        !string.IsNullOrWhiteSpace(apiKeyOptions.ImportEndpointKeyHash),
        apiKeyOptions.ImportEndpointPreviousKeyHashes?.Length ?? 0,
        apiKeyOptions.RevokedKeyHashes?.Length ?? 0,
        !string.IsNullOrWhiteSpace(apiKeyOptions.ImportTenantId),
        apiKeyOptions.ImportEndpointScopes?.Length ?? 0);
}

static async Task RunPostgreSqlBootstrapAsync(WebApplication app)
{
    var configuration = app.Services.GetRequiredService<IConfiguration>();
    if (!configuration.GetValue<bool>("Persistence:UsePostgreSql"))
    {
        return;
    }

    var options = configuration
        .GetSection(PostgreSqlPersistenceOptions.SectionName)
        .Get<PostgreSqlPersistenceOptions>() ?? new PostgreSqlPersistenceOptions();
    var mode = PostgreSqlSchemaBootstrapModeParser.Parse(options.BootstrapMode);
    var logger = app.Services
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("CarbonOps.Api.PostgreSqlBootstrap");

    if (!options.BootstrapOnStartup)
    {
        logger.LogInformation(
            "CarbonOps PostgreSQL schema bootstrap skipped {bootstrap_enabled} {bootstrap_mode}",
            false,
            mode.ToString());
        return;
    }

    try
    {
        using var scope = app.Services.CreateScope();
        var bootstrapper = scope.ServiceProvider.GetRequiredService<IPostgreSqlSchemaBootstrapper>();
        var result = await bootstrapper.BootstrapAsync(mode, app.Lifetime.ApplicationStopping);

        logger.LogInformation(
            "CarbonOps PostgreSQL schema bootstrap completed {bootstrap_enabled} {bootstrap_mode} {schema_scripts_planned} {schema_scripts_executed} {schema_validation_status}",
            true,
            result.Mode.ToString(),
            result.ScriptsPlanned,
            result.ScriptsExecuted,
            result.ValidationStatus);
    }
    catch (Exception exception)
    {
        logger.LogError(
            exception,
            "CarbonOps PostgreSQL schema bootstrap failed {bootstrap_enabled} {bootstrap_mode} {schema_validation_status}",
            true,
            mode.ToString(),
            "failure");
        throw;
    }
}

static void ValidateProductionConfiguration(WebApplication app)
{
    if (!app.Environment.IsProduction())
    {
        return;
    }

    app.Services
        .GetRequiredService<CarbonOpsProductionConfigurationValidator>()
        .Validate();
}

public partial class Program
{
}
