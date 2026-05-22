using CarbonOps.Api;
using CarbonOps.Infrastructure;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddOptions<ApiKeyAuthenticationOptions>()
    .Bind(builder.Configuration.GetSection(ApiKeyAuthenticationOptions.SectionName));
builder.Services.AddCarbonFactorServices(builder.Configuration);
var app = builder.Build();

LogStartupConfiguration(app);

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

public partial class Program
{
}
