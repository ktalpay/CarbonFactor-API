using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CarbonOps.Api.Tests;

public sealed class StructuredLoggingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string CurrentApiKey = "test-import-api-key";
    private const string CurrentApiKeyHash = "f9ffdcc248b716dfc2dbee5492ecbabab672b7d19522859ad3b0a0fd49a86fd0";
    private const string PreviousApiKeyHash = "dcf2d8f1700d7f49fae4215241cbf24c0fb633cc3783e49e24a754891552e91d";
    private const string WrongApiKey = "wrong-import-api-key";
    private const string WrongApiKeyHash = "ff51c126165fcd11e442ff49e3a50c16b1adcdb8753bed9acd77bc7b125de163";
    private const string PlaintextDevKey = "dev-import-key-not-for-production";
    private const string TenantId = "tenant-dev-001";
    private const string ImportScope = "carbon_factors:import";
    private readonly WebApplicationFactory<Program> sourceFactory;

    public StructuredLoggingTests(WebApplicationFactory<Program> factory)
    {
        sourceFactory = factory;
    }

    [Fact]
    public void StartupWritesSafeStructuredConfigurationSummary()
    {
        var loggerProvider = new CapturingLoggerProvider();
        using var factory = CreateFactory(
            loggerProvider,
            CreateApiKeyConfiguration(
                previousKeyHashes: [PreviousApiKeyHash],
                revokedKeyHashes: [WrongApiKeyHash],
                plaintextKey: PlaintextDevKey));

        using var _ = factory.CreateClient();

        var logEntry = Assert.Single(loggerProvider.Entries.Where(entry =>
            entry.Message.StartsWith("CarbonOps API startup configuration loaded", StringComparison.Ordinal)));
        Assert.Equal(LogLevel.Information, logEntry.LogLevel);
        Assert.Equal("CarbonOps.Api.Startup", logEntry.Category);
        Assert.Equal("in_memory", logEntry.GetProperty<string>("persistence_provider"));
        Assert.True(logEntry.GetProperty<bool>("api_key_hash_configured"));
        Assert.Equal(1, logEntry.GetProperty<int>("previous_key_hash_count"));
        Assert.Equal(1, logEntry.GetProperty<int>("revoked_key_hash_count"));
        Assert.True(logEntry.GetProperty<bool>("tenant_configured"));
        Assert.Equal(1, logEntry.GetProperty<int>("import_scope_count"));
        AssertNoSensitiveValues(loggerProvider);
    }

    [Fact]
    public async Task AuthFailureWritesWarningWithSafeStructuredFields()
    {
        var loggerProvider = new CapturingLoggerProvider();
        using var factory = CreateFactory(
            loggerProvider,
            CreateApiKeyConfiguration(
                previousKeyHashes: [PreviousApiKeyHash],
                plaintextKey: PlaintextDevKey));
        using var client = factory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), WrongApiKey);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var logEntry = Assert.Single(loggerProvider.Entries.Where(entry =>
            entry.Message.StartsWith("CarbonOps import authorization failed", StringComparison.Ordinal)));
        Assert.Equal(LogLevel.Warning, logEntry.LogLevel);
        Assert.Equal("CarbonOps.Api.Import", logEntry.Category);
        Assert.Equal("/carbon-factors/import", logEntry.GetProperty<string>("endpoint"));
        Assert.Equal("invalid_api_key", logEntry.GetProperty<string>("auth_failure_reason"));
        Assert.Equal("api_key", logEntry.GetProperty<string>("authentication_scheme"));
        AssertNoSensitiveValues(loggerProvider);
    }

    [Fact]
    public async Task AcceptedImportWritesInformationWithSafeStructuredFields()
    {
        var loggerProvider = new CapturingLoggerProvider();
        using var factory = CreateFactory(loggerProvider, CreateApiKeyConfiguration(previousKeyHashes: [PreviousApiKeyHash]));
        using var client = factory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), CurrentApiKey);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var logEntry = Assert.Single(loggerProvider.Entries.Where(entry =>
            entry.Message.StartsWith("CarbonOps import request accepted", StringComparison.Ordinal)));
        Assert.Equal(LogLevel.Information, logEntry.LogLevel);
        Assert.Equal("CarbonOps.Api.Import", logEntry.Category);
        Assert.Equal("/carbon-factors/import", logEntry.GetProperty<string>("endpoint"));
        Assert.Equal("api_key", logEntry.GetProperty<string>("authentication_scheme"));
        Assert.Equal(TenantId, logEntry.GetProperty<string>("tenant_id"));
        Assert.Equal("batch-1", logEntry.GetProperty<string>("batch_id"));
        Assert.Equal("accepted", logEntry.GetProperty<string>("validation_status"));
        Assert.Equal(1, logEntry.GetProperty<int>("accepted_records"));
        Assert.Equal(0, logEntry.GetProperty<int>("rejected_records"));
        Assert.Equal(0, logEntry.GetProperty<int>("error_count"));
        Assert.Equal(0, logEntry.GetProperty<int>("warning_count"));
        Assert.False(logEntry.GetProperty<bool>("persisted"));
        Assert.Equal("not_started", logEntry.GetProperty<string>("import_execution"));
        AssertNoSensitiveValues(loggerProvider);
    }

    [Fact]
    public async Task ImportValidationFailureWritesInformationWithoutRequestBodyOrSecrets()
    {
        var loggerProvider = new CapturingLoggerProvider();
        using var factory = CreateFactory(loggerProvider, CreateApiKeyConfiguration());
        using var client = factory.CreateClient();
        var importRequest = CreateValidImportRequest();
        importRequest["factors"] = Array.Empty<object>();
        using var request = CreateImportRequestMessage(importRequest, CurrentApiKey);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var logEntry = Assert.Single(loggerProvider.Entries.Where(entry =>
            entry.Message.StartsWith("CarbonOps import validation failed", StringComparison.Ordinal)));
        Assert.Equal(LogLevel.Information, logEntry.LogLevel);
        Assert.Equal("CarbonOps.Api.Import", logEntry.Category);
        Assert.Equal("/carbon-factors/import", logEntry.GetProperty<string>("endpoint"));
        Assert.Equal("factors must contain at least one item", logEntry.GetProperty<string>("validation_failure_reason"));
        AssertNoSensitiveValues(loggerProvider);
        Assert.DoesNotContain("external_factor_id", logEntry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("source_provider", logEntry.Message, StringComparison.Ordinal);
    }

    private WebApplicationFactory<Program> CreateFactory(
        CapturingLoggerProvider loggerProvider,
        Dictionary<string, string?> configurationValues)
    {
        return sourceFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Trace);
                logging.AddProvider(loggerProvider);
            });
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(configurationValues);
            });
        });
    }

    private static Dictionary<string, string?> CreateApiKeyConfiguration(
        string? currentKeyHash = CurrentApiKeyHash,
        string? tenantId = TenantId,
        IReadOnlyList<string?>? scopes = null,
        string? plaintextKey = null,
        IReadOnlyList<string?>? previousKeyHashes = null,
        IReadOnlyList<string?>? revokedKeyHashes = null)
    {
        var configuration = new Dictionary<string, string?>();

        if (currentKeyHash is not null)
        {
            configuration["Security:ApiKey:ImportEndpointKeyHash"] = currentKeyHash;
        }

        if (tenantId is not null)
        {
            configuration["Security:ApiKey:ImportTenantId"] = tenantId;
        }

        AddConfigurationArray(configuration, "Security:ApiKey:ImportEndpointScopes", scopes ?? [ImportScope]);

        if (plaintextKey is not null)
        {
            configuration["Security:ApiKey:ImportEndpointKey"] = plaintextKey;
        }

        AddConfigurationArray(configuration, "Security:ApiKey:ImportEndpointPreviousKeyHashes", previousKeyHashes);
        AddConfigurationArray(configuration, "Security:ApiKey:RevokedKeyHashes", revokedKeyHashes);

        return configuration;
    }

    private static void AddConfigurationArray(
        Dictionary<string, string?> configuration,
        string keyPrefix,
        IReadOnlyList<string?>? values)
    {
        if (values is null)
        {
            return;
        }

        for (var index = 0; index < values.Count; index++)
        {
            configuration[$"{keyPrefix}:{index}"] = values[index];
        }
    }

    private static HttpRequestMessage CreateImportRequestMessage(object request, string apiKey)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/carbon-factors/import")
        {
            Content = JsonContent.Create(request)
        };
        requestMessage.Headers.Add("X-Api-Key", apiKey);

        return requestMessage;
    }

    private static Dictionary<string, object> CreateValidImportRequest()
    {
        return new Dictionary<string, object>
        {
            ["contract_version"] = "1.0",
            ["batch_id"] = "batch-1",
            ["source"] = new Dictionary<string, object>
            {
                ["source_system"] = "parser",
                ["source_family"] = "electricity",
                ["source_provider"] = "synthetic",
                ["publication"] = "pub",
                ["publication_version"] = "v1"
            },
            ["factors"] = new object[]
            {
                new Dictionary<string, object>
                {
                    ["external_factor_id"] = "ext-1",
                    ["source_family"] = "electricity",
                    ["source_provider"] = "synthetic",
                    ["category"] = "electricity",
                    ["activity"] = "grid",
                    ["factor_value"] = 0.1m,
                    ["factor_unit"] = "kg"
                }
            }
        };
    }

    private static void AssertNoSensitiveValues(CapturingLoggerProvider loggerProvider)
    {
        var serializedEntries = string.Join(
            Environment.NewLine,
            loggerProvider.Entries.Select(entry => entry.ToString()));

        Assert.DoesNotContain(CurrentApiKey, serializedEntries, StringComparison.Ordinal);
        Assert.DoesNotContain(CurrentApiKeyHash, serializedEntries, StringComparison.Ordinal);
        Assert.DoesNotContain(PreviousApiKeyHash, serializedEntries, StringComparison.Ordinal);
        Assert.DoesNotContain(WrongApiKey, serializedEntries, StringComparison.Ordinal);
        Assert.DoesNotContain(WrongApiKeyHash, serializedEntries, StringComparison.Ordinal);
        Assert.DoesNotContain(PlaintextDevKey, serializedEntries, StringComparison.Ordinal);
        Assert.DoesNotContain(ImportScope, serializedEntries, StringComparison.Ordinal);
        Assert.DoesNotContain("X-Api-Key", serializedEntries, StringComparison.Ordinal);
    }

}
