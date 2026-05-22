using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CarbonOps.Api.Tests;

public sealed class AuditEventTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string CorrelationHeaderName = "X-Correlation-Id";
    private const string CurrentApiKey = "test-import-api-key";
    private const string CurrentApiKeyHash = "f9ffdcc248b716dfc2dbee5492ecbabab672b7d19522859ad3b0a0fd49a86fd0";
    private const string PreviousApiKeyHash = "dcf2d8f1700d7f49fae4215241cbf24c0fb633cc3783e49e24a754891552e91d";
    private const string RevokedConfiguredHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string WrongApiKey = "wrong-import-api-key";
    private const string WrongApiKeyHash = "ff51c126165fcd11e442ff49e3a50c16b1adcdb8753bed9acd77bc7b125de163";
    private const string PlaintextDevKey = "dev-import-key-not-for-production";
    private const string TenantId = "tenant-dev-001";
    private const string ImportScope = "carbon_factors:import";
    private readonly WebApplicationFactory<Program> sourceFactory;

    public AuditEventTests(WebApplicationFactory<Program> factory)
    {
        sourceFactory = factory;
    }

    [Fact]
    public async Task AuthFailureEmitsSafeAuditEventWithCorrelationId()
    {
        const string CorrelationId = "ops-028-auth-failure";

        var loggerProvider = new CapturingLoggerProvider();
        using var factory = CreateFactory(loggerProvider);
        using var client = factory.CreateClient();
        using var request = CreateImportRequest(CreateValidImportRequest(), WrongApiKey, CorrelationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(CorrelationId, ReadCorrelationId(response));

        var auditEvent = AssertSingleAuditEvent(loggerProvider, "import.authorization_failed");
        Assert.Equal(LogLevel.Warning, auditEvent.LogLevel);
        AssertCommonAuditFields(auditEvent, "import.authorization_failed", "Warning", "failure", CorrelationId);
        Assert.Equal("api_key", auditEvent.GetProperty<string>("authentication_scheme"));
        Assert.Equal("invalid_api_key", auditEvent.GetProperty<string>("reason_code"));
        Assert.Null(Assert.Contains("tenant_id", auditEvent.Properties));
        AssertNoSensitiveValues(loggerProvider);
    }

    [Fact]
    public async Task ValidationFailureEmitsSafeAuditEventWithCorrelationId()
    {
        const string CorrelationId = "ops-028-validation-failure";

        var loggerProvider = new CapturingLoggerProvider();
        using var factory = CreateFactory(loggerProvider);
        using var client = factory.CreateClient();
        var importRequest = CreateValidImportRequest();
        importRequest["factors"] = Array.Empty<object>();
        using var request = CreateImportRequest(importRequest, CurrentApiKey, CorrelationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(CorrelationId, ReadCorrelationId(response));

        var auditEvent = AssertSingleAuditEvent(loggerProvider, "import.validation_failed");
        Assert.Equal(LogLevel.Information, auditEvent.LogLevel);
        AssertCommonAuditFields(auditEvent, "import.validation_failed", "Information", "failure", CorrelationId);
        Assert.Equal("api_key", auditEvent.GetProperty<string>("authentication_scheme"));
        Assert.Equal(TenantId, auditEvent.GetProperty<string>("tenant_id"));
        Assert.Equal("invalid_query", auditEvent.GetProperty<string>("reason_code"));
        AssertNoSensitiveValues(loggerProvider);
    }

    [Fact]
    public async Task AcceptedImportEmitsSafeAuditEventWithImportBoundaryFields()
    {
        const string CorrelationId = "ops-028-accepted-import";

        var loggerProvider = new CapturingLoggerProvider();
        using var factory = CreateFactory(loggerProvider);
        using var client = factory.CreateClient();
        using var request = CreateImportRequest(CreateValidImportRequest(), CurrentApiKey, CorrelationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(CorrelationId, ReadCorrelationId(response));

        var auditEvent = AssertSingleAuditEvent(loggerProvider, "import.accepted");
        Assert.Equal(LogLevel.Information, auditEvent.LogLevel);
        AssertCommonAuditFields(auditEvent, "import.accepted", "Information", "success", CorrelationId);
        Assert.Equal("api_key", auditEvent.GetProperty<string>("authentication_scheme"));
        Assert.Equal(TenantId, auditEvent.GetProperty<string>("tenant_id"));
        Assert.Equal("batch-1", auditEvent.GetProperty<string>("batch_id"));
        Assert.Equal("accepted", auditEvent.GetProperty<string>("validation_status"));
        Assert.Equal(1, auditEvent.GetProperty<int>("accepted_records"));
        Assert.Equal(0, auditEvent.GetProperty<int>("rejected_records"));
        Assert.Equal(0, auditEvent.GetProperty<int>("error_count"));
        Assert.Equal(0, auditEvent.GetProperty<int>("warning_count"));
        Assert.False(auditEvent.GetProperty<bool>("persisted"));
        Assert.Equal("not_started", auditEvent.GetProperty<string>("import_execution"));
        AssertNoSensitiveValues(loggerProvider);
    }

    private WebApplicationFactory<Program> CreateFactory(CapturingLoggerProvider loggerProvider)
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
                configuration.AddInMemoryCollection(CreateApiKeyConfiguration());
            });
        });
    }

    private static Dictionary<string, string?> CreateApiKeyConfiguration()
    {
        return new Dictionary<string, string?>
        {
            ["Security:ApiKey:ImportEndpointKeyHash"] = CurrentApiKeyHash,
            ["Security:ApiKey:ImportTenantId"] = TenantId,
            ["Security:ApiKey:ImportEndpointScopes:0"] = ImportScope,
            ["Security:ApiKey:ImportEndpointPreviousKeyHashes:0"] = PreviousApiKeyHash,
            ["Security:ApiKey:RevokedKeyHashes:0"] = RevokedConfiguredHash,
            ["Security:ApiKey:ImportEndpointKey"] = PlaintextDevKey
        };
    }

    private static HttpRequestMessage CreateImportRequest(
        object importRequest,
        string apiKey,
        string correlationId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/carbon-factors/import")
        {
            Content = JsonContent.Create(importRequest)
        };
        request.Headers.Add("X-Api-Key", apiKey);
        request.Headers.TryAddWithoutValidation(CorrelationHeaderName, correlationId);

        return request;
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

    private static CapturedLogEntry AssertSingleAuditEvent(
        CapturingLoggerProvider loggerProvider,
        string eventType)
    {
        return Assert.Single(loggerProvider.Entries.Where(entry =>
            entry.Category == "CarbonOps.Api.Audit"
            && entry.Properties.TryGetValue("event_type", out var value)
            && string.Equals(value as string, eventType, StringComparison.Ordinal)));
    }

    private static void AssertCommonAuditFields(
        CapturedLogEntry auditEvent,
        string eventType,
        string severity,
        string outcome,
        string correlationId)
    {
        Assert.StartsWith("CarbonOps audit event", auditEvent.Message, StringComparison.Ordinal);
        Assert.Equal("/carbon-factors/import", auditEvent.GetProperty<string>("endpoint"));
        Assert.Equal(eventType, auditEvent.GetProperty<string>("event_type"));
        Assert.Equal(severity, auditEvent.GetProperty<string>("severity"));
        Assert.Equal(outcome, auditEvent.GetProperty<string>("outcome"));
        Assert.Equal(correlationId, auditEvent.GetProperty<string>("correlation_id"));
        Assert.True(Guid.TryParse(auditEvent.GetProperty<string>("event_id"), out _));
        Assert.True(auditEvent.GetProperty<DateTimeOffset>("occurred_at_utc") > DateTimeOffset.UnixEpoch);
    }

    private static string ReadCorrelationId(HttpResponseMessage response)
    {
        var values = response.Headers.GetValues(CorrelationHeaderName);
        return Assert.Single(values);
    }

    private static void AssertNoSensitiveValues(CapturingLoggerProvider loggerProvider)
    {
        var serializedEntries = string.Join(
            Environment.NewLine,
            loggerProvider.Entries.Select(entry => entry.ToString()));

        Assert.DoesNotContain(CurrentApiKey, serializedEntries, StringComparison.Ordinal);
        Assert.DoesNotContain(CurrentApiKeyHash, serializedEntries, StringComparison.Ordinal);
        Assert.DoesNotContain(PreviousApiKeyHash, serializedEntries, StringComparison.Ordinal);
        Assert.DoesNotContain(RevokedConfiguredHash, serializedEntries, StringComparison.Ordinal);
        Assert.DoesNotContain(WrongApiKey, serializedEntries, StringComparison.Ordinal);
        Assert.DoesNotContain(WrongApiKeyHash, serializedEntries, StringComparison.Ordinal);
        Assert.DoesNotContain(PlaintextDevKey, serializedEntries, StringComparison.Ordinal);
        Assert.DoesNotContain(ImportScope, serializedEntries, StringComparison.Ordinal);
        Assert.DoesNotContain("X-Api-Key", serializedEntries, StringComparison.Ordinal);
        Assert.DoesNotContain("external_factor_id", serializedEntries, StringComparison.Ordinal);
        Assert.DoesNotContain("source_provider", serializedEntries, StringComparison.Ordinal);
        Assert.DoesNotContain("synthetic", serializedEntries, StringComparison.Ordinal);
        Assert.DoesNotContain("ext-1", serializedEntries, StringComparison.Ordinal);
    }
}
