using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CarbonOps.Api.Tests;

public sealed class RateLimitingTests : IClassFixture<WebApplicationFactory<Program>>
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

    public RateLimitingTests(WebApplicationFactory<Program> factory)
    {
        sourceFactory = factory;
    }

    [Fact]
    public async Task ReadEndpointReturnsTooManyRequestsWhenReadLimitIsExceeded()
    {
        using var factory = CreateFactory(readPermitLimit: 1, importPermitLimit: 10);
        using var client = factory.CreateClient();

        var firstResponse = await client.GetAsync("/carbon-factors");
        var secondResponse = await client.GetAsync("/carbon-factors");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        await AssertRateLimitedEnvelope(secondResponse);
    }

    [Fact]
    public async Task ImportEndpointReturnsTooManyRequestsWhenImportLimitIsExceeded()
    {
        using var factory = CreateFactory(readPermitLimit: 10, importPermitLimit: 1);
        using var client = factory.CreateClient();

        using var firstRequest = CreateImportRequest(CreateValidImportRequest(), CurrentApiKey);
        var firstResponse = await client.SendAsync(firstRequest);
        using var secondRequest = CreateImportRequest(CreateValidImportRequest(), CurrentApiKey);
        var secondResponse = await client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.Accepted, firstResponse.StatusCode);
        await AssertRateLimitedEnvelope(secondResponse);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    [InlineData("/version")]
    public async Task OperationalEndpointsAreNotRateLimited(string path)
    {
        using var factory = CreateFactory(readPermitLimit: 1, importPermitLimit: 1);
        using var client = factory.CreateClient();

        var firstResponse = await client.GetAsync(path);
        var secondResponse = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
    }

    [Fact]
    public async Task TooManyRequestsEchoesValidIncomingCorrelationId()
    {
        const string CorrelationId = "ops-029-rate-limited";

        using var factory = CreateFactory(readPermitLimit: 1, importPermitLimit: 10);
        using var client = factory.CreateClient();

        var firstResponse = await client.GetAsync("/carbon-factors");
        using var secondRequest = CreateRequest(HttpMethod.Get, "/carbon-factors", CorrelationId);
        var secondResponse = await client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        await AssertRateLimitedEnvelope(secondResponse);
        Assert.Equal(CorrelationId, ReadCorrelationId(secondResponse));
    }

    [Fact]
    public async Task TooManyRequestsReturnsGeneratedCorrelationIdWhenMissing()
    {
        using var factory = CreateFactory(readPermitLimit: 1, importPermitLimit: 10);
        using var client = factory.CreateClient();

        var firstResponse = await client.GetAsync("/carbon-factors");
        var secondResponse = await client.GetAsync("/carbon-factors");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        await AssertRateLimitedEnvelope(secondResponse);
        AssertGeneratedCorrelationId(ReadCorrelationId(secondResponse));
    }

    [Fact]
    public async Task TooManyRequestsBodyAndLogsDoNotLeakSecrets()
    {
        var loggerProvider = new CapturingLoggerProvider();
        using var factory = CreateFactory(readPermitLimit: 10, importPermitLimit: 1, loggerProvider);
        using var client = factory.CreateClient();

        using var firstRequest = CreateImportRequest(CreateValidImportRequest(), CurrentApiKey);
        var firstResponse = await client.SendAsync(firstRequest);
        using var secondRequest = CreateImportRequest(CreateValidImportRequest(), CurrentApiKey);
        var secondResponse = await client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.Accepted, firstResponse.StatusCode);
        await AssertRateLimitedEnvelope(secondResponse);
        AssertNoSensitiveValues(await secondResponse.Content.ReadAsStringAsync());
        AssertNoSensitiveValues(
            string.Join(Environment.NewLine, loggerProvider.Entries.Select(entry => entry.ToString())));
    }

    [Fact]
    public async Task ReadAndImportPoliciesAreIndependent()
    {
        using var factory = CreateFactory(readPermitLimit: 1, importPermitLimit: 1);
        using var client = factory.CreateClient();

        var firstReadResponse = await client.GetAsync("/carbon-factors");
        var secondReadResponse = await client.GetAsync("/carbon-factors");
        using var importRequest = CreateImportRequest(CreateValidImportRequest(), CurrentApiKey);
        var importResponse = await client.SendAsync(importRequest);

        Assert.Equal(HttpStatusCode.OK, firstReadResponse.StatusCode);
        await AssertRateLimitedEnvelope(secondReadResponse);
        Assert.Equal(HttpStatusCode.Accepted, importResponse.StatusCode);
    }

    [Fact]
    public async Task AuthFailureStillReturnsUnauthorizedWhenImportLimitIsAvailable()
    {
        using var factory = CreateFactory(readPermitLimit: 10, importPermitLimit: 2);
        using var client = factory.CreateClient();
        using var request = CreateImportRequest(CreateValidImportRequest(), WrongApiKey);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ImportRateLimitRejectsBeforeAuthHandlerWhenLimitIsExceeded()
    {
        var loggerProvider = new CapturingLoggerProvider();
        using var factory = CreateFactory(readPermitLimit: 10, importPermitLimit: 1, loggerProvider);
        using var client = factory.CreateClient();

        using var firstRequest = CreateImportRequest(CreateValidImportRequest(), CurrentApiKey);
        var firstResponse = await client.SendAsync(firstRequest);
        using var secondRequest = CreateImportRequest(CreateValidImportRequest(), WrongApiKey);
        var secondResponse = await client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.Accepted, firstResponse.StatusCode);
        await AssertRateLimitedEnvelope(secondResponse);
        Assert.DoesNotContain(loggerProvider.Entries, entry =>
            entry.Message.StartsWith("CarbonOps import authorization failed", StringComparison.Ordinal));
        Assert.DoesNotContain(loggerProvider.Entries, entry =>
            entry.Properties.TryGetValue("event_type", out var value)
            && string.Equals(value as string, "import.authorization_failed", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RateLimitRejectionWritesSafeStructuredLogAndAuditEvent()
    {
        const string CorrelationId = "ops-029-log-audit";

        var loggerProvider = new CapturingLoggerProvider();
        using var factory = CreateFactory(readPermitLimit: 1, importPermitLimit: 10, loggerProvider);
        using var client = factory.CreateClient();

        var firstResponse = await client.GetAsync("/carbon-factors");
        using var secondRequest = CreateRequest(HttpMethod.Get, "/carbon-factors", CorrelationId);
        var secondResponse = await client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        await AssertRateLimitedEnvelope(secondResponse);

        var rejectionLog = Assert.Single(loggerProvider.Entries.Where(entry =>
            entry.Message.StartsWith("CarbonOps rate limit rejected", StringComparison.Ordinal)));
        Assert.Equal(LogLevel.Warning, rejectionLog.LogLevel);
        Assert.Equal("CarbonOps.Api.RateLimiting", rejectionLog.Category);
        Assert.Equal("/carbon-factors", rejectionLog.GetProperty<string>("endpoint"));
        Assert.Equal("carbonops-read", rejectionLog.GetProperty<string>("rate_limit_policy"));
        Assert.Equal("rate_limit_exceeded", rejectionLog.GetProperty<string>("reason_code"));
        Assert.Equal(CorrelationId, rejectionLog.GetProperty<string>("correlation_id"));

        var auditEvent = Assert.Single(loggerProvider.Entries.Where(entry =>
            entry.Category == "CarbonOps.Api.Audit"
            && entry.Properties.TryGetValue("event_type", out var value)
            && string.Equals(value as string, "rate_limit.rejected", StringComparison.Ordinal)));
        Assert.Equal(LogLevel.Warning, auditEvent.LogLevel);
        Assert.Equal("/carbon-factors", auditEvent.GetProperty<string>("endpoint"));
        Assert.Equal("rate_limit_exceeded", auditEvent.GetProperty<string>("reason_code"));
        Assert.Equal(CorrelationId, auditEvent.GetProperty<string>("correlation_id"));
        AssertNoSensitiveValues(
            string.Join(Environment.NewLine, loggerProvider.Entries.Select(entry => entry.ToString())));
    }

    private WebApplicationFactory<Program> CreateFactory(
        int readPermitLimit,
        int importPermitLimit,
        CapturingLoggerProvider? loggerProvider = null)
    {
        return sourceFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            if (loggerProvider is not null)
            {
                builder.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Trace);
                    logging.AddProvider(loggerProvider);
                });
            }

            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(CreateConfiguration(readPermitLimit, importPermitLimit));
            });
        });
    }

    private static Dictionary<string, string?> CreateConfiguration(
        int readPermitLimit,
        int importPermitLimit)
    {
        return new Dictionary<string, string?>
        {
            ["Security:ApiKey:ImportEndpointKeyHash"] = CurrentApiKeyHash,
            ["Security:ApiKey:ImportTenantId"] = TenantId,
            ["Security:ApiKey:ImportEndpointScopes:0"] = ImportScope,
            ["Security:ApiKey:ImportEndpointPreviousKeyHashes:0"] = PreviousApiKeyHash,
            ["Security:ApiKey:RevokedKeyHashes:0"] = RevokedConfiguredHash,
            ["Security:ApiKey:ImportEndpointKey"] = PlaintextDevKey,
            ["RateLimiting:Import:PermitLimit"] = importPermitLimit.ToString(),
            ["RateLimiting:Import:WindowSeconds"] = "60",
            ["RateLimiting:Import:QueueLimit"] = "0",
            ["RateLimiting:Read:PermitLimit"] = readPermitLimit.ToString(),
            ["RateLimiting:Read:WindowSeconds"] = "60",
            ["RateLimiting:Read:QueueLimit"] = "0"
        };
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string path, string? correlationId = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (correlationId is not null)
        {
            request.Headers.TryAddWithoutValidation(CorrelationHeaderName, correlationId);
        }

        return request;
    }

    private static HttpRequestMessage CreateImportRequest(
        object importRequest,
        string apiKey,
        string? correlationId = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/carbon-factors/import")
        {
            Content = JsonContent.Create(importRequest)
        };
        request.Headers.Add("X-Api-Key", apiKey);
        if (correlationId is not null)
        {
            request.Headers.TryAddWithoutValidation(CorrelationHeaderName, correlationId);
        }

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

    private static async Task AssertRateLimitedEnvelope(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        Assert.True(response.Headers.Contains("Retry-After"));

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("rate_limited", payload.GetProperty("code").GetString());
        Assert.Equal("Too many requests", payload.GetProperty("message").GetString());
        Assert.Equal("rate limit exceeded", payload.GetProperty("details").GetProperty("reason").GetString());
    }

    private static string ReadCorrelationId(HttpResponseMessage response)
    {
        var values = response.Headers.GetValues(CorrelationHeaderName);
        return Assert.Single(values);
    }

    private static void AssertGeneratedCorrelationId(string correlationId)
    {
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
        Assert.Equal(32, correlationId.Length);
        Assert.True(Guid.TryParseExact(correlationId, "N", out _));
    }

    private static void AssertNoSensitiveValues(string value)
    {
        Assert.DoesNotContain(CurrentApiKey, value, StringComparison.Ordinal);
        Assert.DoesNotContain(CurrentApiKeyHash, value, StringComparison.Ordinal);
        Assert.DoesNotContain(PreviousApiKeyHash, value, StringComparison.Ordinal);
        Assert.DoesNotContain(RevokedConfiguredHash, value, StringComparison.Ordinal);
        Assert.DoesNotContain(WrongApiKey, value, StringComparison.Ordinal);
        Assert.DoesNotContain(WrongApiKeyHash, value, StringComparison.Ordinal);
        Assert.DoesNotContain(PlaintextDevKey, value, StringComparison.Ordinal);
        Assert.DoesNotContain(ImportScope, value, StringComparison.Ordinal);
        Assert.DoesNotContain("X-Api-Key", value, StringComparison.Ordinal);
        Assert.DoesNotContain("external_factor_id", value, StringComparison.Ordinal);
        Assert.DoesNotContain("source_provider", value, StringComparison.Ordinal);
        Assert.DoesNotContain("synthetic", value, StringComparison.Ordinal);
        Assert.DoesNotContain("ext-1", value, StringComparison.Ordinal);
    }
}
