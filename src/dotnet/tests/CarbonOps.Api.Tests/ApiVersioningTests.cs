using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CarbonOps.Api.Tests;

public sealed class ApiVersioningTests : IClassFixture<WebApplicationFactory<Program>>
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

    public ApiVersioningTests(WebApplicationFactory<Program> factory)
    {
        sourceFactory = factory;
    }

    [Fact]
    public async Task VersionedListCarbonFactorsMatchesLegacyResponseBody()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var legacyResponse = await client.GetAsync("/carbon-factors");
        var versionedResponse = await client.GetAsync("/v1/carbon-factors");

        legacyResponse.EnsureSuccessStatusCode();
        versionedResponse.EnsureSuccessStatusCode();
        Assert.Equal(
            await ReadNormalizedJsonAsync(legacyResponse),
            await ReadNormalizedJsonAsync(versionedResponse));
    }

    [Fact]
    public async Task VersionedSearchCarbonFactorsSupportsExistingFilters()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/v1/carbon-factors/search?category=electricity&activity=grid%20electricity&region=US-WEST&year=2024");

        response.EnsureSuccessStatusCode();
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(1, payload.GetProperty("total").GetInt32());
        Assert.Equal("f-001", payload.GetProperty("factors")[0].GetProperty("id").GetString());
    }

    [Fact]
    public async Task VersionedGetCarbonFactorByIdReturnsMatchingFactor()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/v1/carbon-factors/f-002");

        response.EnsureSuccessStatusCode();
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("f-002", payload.GetProperty("factor").GetProperty("id").GetString());
    }

    [Theory]
    [InlineData("/carbon-factors")]
    [InlineData("/carbon-factors/search")]
    [InlineData("/carbon-factors/f-001")]
    public async Task LegacyReadRoutesRemainAvailable(string path)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VersionedImportRequiresApiKey()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/v1/carbon-factors/import", CreateValidImportRequest());

        await AssertUnauthorizedEnvelope(response, "missing API key");
    }

    [Fact]
    public async Task VersionedImportRejectsInvalidApiKey()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var request = CreateImportRequest("/v1/carbon-factors/import", CreateValidImportRequest(), WrongApiKey);

        var response = await client.SendAsync(request);

        await AssertUnauthorizedEnvelope(response, "invalid API key");
    }

    [Fact]
    public async Task VersionedImportReturnsAcceptedResponseMatchingLegacyBody()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var legacyRequest = CreateImportRequest("/carbon-factors/import", CreateValidImportRequest(), CurrentApiKey);
        var legacyResponse = await client.SendAsync(legacyRequest);
        using var versionedRequest = CreateImportRequest("/v1/carbon-factors/import", CreateValidImportRequest(), CurrentApiKey);
        var versionedResponse = await client.SendAsync(versionedRequest);

        Assert.Equal(HttpStatusCode.Accepted, legacyResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, versionedResponse.StatusCode);
        Assert.Equal(
            await ReadNormalizedJsonAsync(legacyResponse),
            await ReadNormalizedJsonAsync(versionedResponse));
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    [InlineData("/version")]
    public async Task OperationalEndpointsRemainUnversionedAndAvailable(string path)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VersionedHealthEndpointIsNotAdded()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/v1/health");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task VersionedResponseIncludesCorrelationId()
    {
        const string CorrelationId = "ops-030-versioned-read";

        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var request = CreateRequest(HttpMethod.Get, "/v1/carbon-factors", CorrelationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(CorrelationId, ReadCorrelationId(response));
    }

    [Fact]
    public async Task VersionedImportRateLimitReturnsCorrelationIdAndEnvelope()
    {
        const string CorrelationId = "ops-030-versioned-rate-limit";

        using var factory = CreateFactory(importPermitLimit: 1);
        using var client = factory.CreateClient();

        using var firstRequest = CreateImportRequest("/v1/carbon-factors/import", CreateValidImportRequest(), CurrentApiKey);
        var firstResponse = await client.SendAsync(firstRequest);
        using var secondRequest = CreateImportRequest(
            "/v1/carbon-factors/import",
            CreateValidImportRequest(),
            CurrentApiKey,
            CorrelationId);
        var secondResponse = await client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.Accepted, firstResponse.StatusCode);
        await AssertRateLimitedEnvelope(secondResponse);
        Assert.Equal(CorrelationId, ReadCorrelationId(secondResponse));
    }

    [Fact]
    public async Task VersionedImportLogsAndAuditUseVersionedEndpointWithoutSecretLeakage()
    {
        const string CorrelationId = "ops-030-versioned-import";

        var loggerProvider = new CapturingLoggerProvider();
        using var factory = CreateFactory(loggerProvider: loggerProvider);
        using var client = factory.CreateClient();
        using var request = CreateImportRequest(
            "/v1/carbon-factors/import",
            CreateValidImportRequest(),
            CurrentApiKey,
            CorrelationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var acceptedLog = Assert.Single(loggerProvider.Entries.Where(entry =>
            entry.Message.StartsWith("CarbonOps import request accepted", StringComparison.Ordinal)));
        Assert.Equal("/v1/carbon-factors/import", acceptedLog.GetProperty<string>("endpoint"));
        Assert.Equal(CorrelationId, acceptedLog.GetProperty<string>("correlation_id"));

        var auditEvent = Assert.Single(loggerProvider.Entries.Where(entry =>
            entry.Category == "CarbonOps.Api.Audit"
            && entry.Properties.TryGetValue("event_type", out var value)
            && string.Equals(value as string, "import.accepted", StringComparison.Ordinal)));
        Assert.Equal("/v1/carbon-factors/import", auditEvent.GetProperty<string>("endpoint"));
        Assert.Equal(CorrelationId, auditEvent.GetProperty<string>("correlation_id"));

        AssertNoSensitiveValues(
            string.Join(Environment.NewLine, loggerProvider.Entries.Select(entry => entry.ToString())));
    }

    private WebApplicationFactory<Program> CreateFactory(
        int readPermitLimit = 1000,
        int importPermitLimit = 1000,
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

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string path,
        string? correlationId = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (correlationId is not null)
        {
            request.Headers.TryAddWithoutValidation(CorrelationHeaderName, correlationId);
        }

        return request;
    }

    private static HttpRequestMessage CreateImportRequest(
        string path,
        object importRequest,
        string apiKey,
        string? correlationId = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
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

    private static async Task<string> ReadNormalizedJsonAsync(HttpResponseMessage response)
    {
        return JsonSerializer.Serialize(
            JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement);
    }

    private static async Task AssertUnauthorizedEnvelope(
        HttpResponseMessage response,
        string expectedReason)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("unauthorized", payload.GetProperty("code").GetString());
        Assert.Equal("Unauthorized", payload.GetProperty("message").GetString());
        Assert.Equal(expectedReason, payload.GetProperty("details").GetProperty("reason").GetString());
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
