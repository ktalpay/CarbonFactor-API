using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CarbonOps.Api.Tests;

public sealed class CorrelationIdMiddlewareTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string HeaderName = "X-Correlation-Id";
    private const string CurrentApiKey = "test-import-api-key";
    private const string CurrentApiKeyHash = "f9ffdcc248b716dfc2dbee5492ecbabab672b7d19522859ad3b0a0fd49a86fd0";
    private const string PreviousApiKeyHash = "dcf2d8f1700d7f49fae4215241cbf24c0fb633cc3783e49e24a754891552e91d";
    private const string WrongApiKey = "wrong-import-api-key";
    private const string WrongApiKeyHash = "ff51c126165fcd11e442ff49e3a50c16b1adcdb8753bed9acd77bc7b125de163";
    private const string PlaintextDevKey = "dev-import-key-not-for-production";
    private const string TenantId = "tenant-dev-001";
    private const string ImportScope = "carbon_factors:import";
    private readonly WebApplicationFactory<Program> sourceFactory;

    public CorrelationIdMiddlewareTests(WebApplicationFactory<Program> factory)
    {
        sourceFactory = factory;
    }

    [Fact]
    public async Task RequestWithoutCorrelationIdReturnsGeneratedResponseHeader()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/carbon-factors");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertGeneratedCorrelationId(ReadCorrelationId(response));
    }

    [Fact]
    public async Task RequestWithValidCorrelationIdEchoesSameResponseHeader()
    {
        const string CorrelationId = "ops-027.valid:ABC_123-xyz";

        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var request = CreateRequest(HttpMethod.Get, "/carbon-factors", CorrelationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(CorrelationId, ReadCorrelationId(response));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid/correlation")]
    public async Task RequestWithBlankWhitespaceOrInvalidCorrelationIdGeneratesNewResponseHeader(string invalidCorrelationId)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var request = CreateRequest(HttpMethod.Get, "/carbon-factors", invalidCorrelationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseCorrelationId = ReadCorrelationId(response);
        AssertGeneratedCorrelationId(responseCorrelationId);
        Assert.NotEqual(invalidCorrelationId, responseCorrelationId);
    }

    [Fact]
    public async Task RequestWithTooLongCorrelationIdGeneratesNewResponseHeader()
    {
        var tooLongCorrelationId = new string('a', 129);
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var request = CreateRequest(HttpMethod.Get, "/carbon-factors", tooLongCorrelationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseCorrelationId = ReadCorrelationId(response);
        AssertGeneratedCorrelationId(responseCorrelationId);
        Assert.NotEqual(tooLongCorrelationId, responseCorrelationId);
    }

    [Fact]
    public async Task RequestWithDuplicateCorrelationIdHeadersGeneratesNewResponseHeader()
    {
        const string FirstCorrelationId = "ops-027-first";
        const string SecondCorrelationId = "ops-027-second";

        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/carbon-factors");
        request.Headers.TryAddWithoutValidation(HeaderName, [FirstCorrelationId, SecondCorrelationId]);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseCorrelationId = ReadCorrelationId(response);
        AssertGeneratedCorrelationId(responseCorrelationId);
        Assert.NotEqual(FirstCorrelationId, responseCorrelationId);
        Assert.NotEqual(SecondCorrelationId, responseCorrelationId);
    }

    [Fact]
    public async Task AuthFailureResponseIncludesCorrelationId()
    {
        const string CorrelationId = "ops-027-auth-failure";

        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var request = CreateImportRequest(CreateValidImportRequest(), WrongApiKey, CorrelationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(CorrelationId, ReadCorrelationId(response));
    }

    [Fact]
    public async Task ValidationFailureResponseIncludesCorrelationId()
    {
        const string CorrelationId = "ops-027-validation-failure";

        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var importRequest = CreateValidImportRequest();
        importRequest["factors"] = Array.Empty<object>();
        using var request = CreateImportRequest(importRequest, CurrentApiKey, CorrelationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(CorrelationId, ReadCorrelationId(response));
    }

    [Fact]
    public async Task SuccessfulImportResponseIncludesCorrelationId()
    {
        const string CorrelationId = "ops-027-import-success";

        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var request = CreateImportRequest(CreateValidImportRequest(), CurrentApiKey, CorrelationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(CorrelationId, ReadCorrelationId(response));
    }

    [Theory]
    [InlineData("/carbon-factors")]
    [InlineData("/carbon-factors/search?category=electricity")]
    [InlineData("/carbon-factors/f-001")]
    public async Task PublicReadEndpointsIncludeCorrelationId(string path)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertGeneratedCorrelationId(ReadCorrelationId(response));
    }

    [Fact]
    public async Task AuthFailureLogIncludesGeneratedCorrelationScopeWithoutInvalidRawValue()
    {
        const string InvalidCorrelationId = "invalid/raw-correlation";

        var loggerProvider = new CapturingLoggerProvider();
        using var factory = CreateFactory(loggerProvider);
        using var client = factory.CreateClient();
        using var request = CreateImportRequest(CreateValidImportRequest(), WrongApiKey, InvalidCorrelationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var responseCorrelationId = ReadCorrelationId(response);
        AssertGeneratedCorrelationId(responseCorrelationId);
        Assert.NotEqual(InvalidCorrelationId, responseCorrelationId);

        var logEntry = Assert.Single(loggerProvider.Entries.Where(entry =>
            entry.Message.StartsWith("CarbonOps import authorization failed", StringComparison.Ordinal)));
        Assert.Equal(responseCorrelationId, logEntry.GetProperty<string>("correlation_id"));
        AssertLogsDoNotContain(loggerProvider, InvalidCorrelationId);
    }

    [Fact]
    public async Task AcceptedImportLogIncludesCorrelationScope()
    {
        const string CorrelationId = "ops-027-accepted-import";

        var loggerProvider = new CapturingLoggerProvider();
        using var factory = CreateFactory(loggerProvider);
        using var client = factory.CreateClient();
        using var request = CreateImportRequest(CreateValidImportRequest(), CurrentApiKey, CorrelationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var logEntry = Assert.Single(loggerProvider.Entries.Where(entry =>
            entry.Message.StartsWith("CarbonOps import request accepted", StringComparison.Ordinal)));
        Assert.Equal(CorrelationId, logEntry.GetProperty<string>("correlation_id"));
        AssertLogsDoNotContain(
            loggerProvider,
            CurrentApiKey,
            CurrentApiKeyHash,
            PreviousApiKeyHash,
            WrongApiKeyHash,
            PlaintextDevKey,
            ImportScope);
    }

    private WebApplicationFactory<Program> CreateFactory(CapturingLoggerProvider? loggerProvider = null)
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
            ["Security:ApiKey:RevokedKeyHashes:0"] = WrongApiKeyHash,
            ["Security:ApiKey:ImportEndpointKey"] = PlaintextDevKey
        };
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string path, string correlationId)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(HeaderName, correlationId);

        return request;
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
        request.Headers.TryAddWithoutValidation(HeaderName, correlationId);

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

    private static string ReadCorrelationId(HttpResponseMessage response)
    {
        var values = response.Headers.GetValues(HeaderName);
        return Assert.Single(values);
    }

    private static void AssertGeneratedCorrelationId(string correlationId)
    {
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
        Assert.Equal(32, correlationId.Length);
        Assert.True(Guid.TryParseExact(correlationId, "N", out _));
    }

    private static void AssertLogsDoNotContain(CapturingLoggerProvider loggerProvider, params string[] values)
    {
        var serializedEntries = string.Join(
            Environment.NewLine,
            loggerProvider.Entries.Select(entry => entry.ToString()));

        foreach (var value in values)
        {
            Assert.DoesNotContain(value, serializedEntries, StringComparison.Ordinal);
        }
    }
}
