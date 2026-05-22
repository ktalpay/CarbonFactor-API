using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CarbonOps.Api.Tests;

public sealed class ResponseFixtureComparisonTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestApiKey = "test-import-api-key";
    private const string TestApiKeyHash = "f9ffdcc248b716dfc2dbee5492ecbabab672b7d19522859ad3b0a0fd49a86fd0";
    private const string TestTenantId = "tenant-dev-001";
    private const string ImportScope = "carbon_factors:import";

    private readonly WebApplicationFactory<Program> sourceFactory;

    public ResponseFixtureComparisonTests(WebApplicationFactory<Program> factory)
    {
        sourceFactory = factory;
    }

    [Theory]
    [InlineData("/v1/carbon-factors", "carbon-factors-list.v1.json")]
    [InlineData("/v1/carbon-factors/search?category=electricity&activity=grid%20electricity&region=US-WEST&year=2024", "carbon-factors-search-electricity.v1.json")]
    [InlineData("/v1/carbon-factors/f-001", "carbon-factor-f001.v1.json")]
    [InlineData("/health", "health.json")]
    [InlineData("/health/live", "health-live.json")]
    [InlineData("/health/ready", "health-ready.json")]
    [InlineData("/version", "version.json")]
    public async Task GetResponseMatchesFixture(string path, string fixtureName)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        response.EnsureSuccessStatusCode();
        await AssertResponseBodyMatchesFixture(response, fixtureName);
    }

    [Fact]
    public async Task ImportAcceptedResponseMatchesFixtureAndPreservesInvariants()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var request = CreateImportRequest("/v1/carbon-factors/import", CreateValidImportRequest(), TestApiKey);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await AssertResponseBodyMatchesFixture(response, "import-accepted.v1.json");

        var payload = await ReadJsonAsync(response);
        Assert.False(payload.GetProperty("persisted").GetBoolean());
        Assert.Equal("not_started", payload.GetProperty("import_execution").GetString());
        Assert.Equal("api_key", payload.GetProperty("audit").GetProperty("authentication_scheme").GetString());
        Assert.Equal(TestTenantId, payload.GetProperty("audit").GetProperty("tenant_id").GetString());
    }

    [Fact]
    public async Task ImportMissingApiKeyUnauthorizedResponseMatchesFixture()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/v1/carbon-factors/import", CreateValidImportRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertResponseBodyMatchesFixture(response, "import-unauthorized-missing-api-key.v1.json");
    }

    [Fact]
    public async Task InvalidQueryResponseMatchesFixture()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/v1/carbon-factors/search?year=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertResponseBodyMatchesFixture(response, "search-invalid-year.v1.json");
    }

    [Fact]
    public async Task NotFoundResponseMatchesFixture()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/v1/carbon-factors/missing-factor");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertResponseBodyMatchesFixture(response, "factor-not-found.v1.json");
    }

    [Fact]
    public async Task RateLimitedResponseMatchesFixtureAndIncludesRetryAfter()
    {
        using var factory = CreateFactory(readPermitLimit: 1);
        using var client = factory.CreateClient();

        var firstResponse = await client.GetAsync("/v1/carbon-factors");
        var secondResponse = await client.GetAsync("/v1/carbon-factors");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, secondResponse.StatusCode);
        Assert.True(secondResponse.Headers.Contains("Retry-After"));
        await AssertResponseBodyMatchesFixture(secondResponse, "rate-limited.v1.json");
    }

    [Fact]
    public async Task LegacyReadRouteMatchesVersionedFixture()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var legacyResponse = await client.GetAsync("/carbon-factors");
        var versionedResponse = await client.GetAsync("/v1/carbon-factors");

        legacyResponse.EnsureSuccessStatusCode();
        versionedResponse.EnsureSuccessStatusCode();
        Assert.Equal(await NormalizeJsonAsync(versionedResponse), await NormalizeJsonAsync(legacyResponse));
        await AssertResponseBodyMatchesFixture(legacyResponse, "carbon-factors-list.v1.json");
    }

    private WebApplicationFactory<Program> CreateFactory(
        int readPermitLimit = 1000,
        int importPermitLimit = 1000)
    {
        return sourceFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
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
            ["Security:ApiKey:ImportEndpointKeyHash"] = TestApiKeyHash,
            ["Security:ApiKey:ImportTenantId"] = TestTenantId,
            ["Security:ApiKey:ImportEndpointScopes:0"] = ImportScope,
            ["RateLimiting:Import:PermitLimit"] = importPermitLimit.ToString(),
            ["RateLimiting:Import:WindowSeconds"] = "60",
            ["RateLimiting:Import:QueueLimit"] = "0",
            ["RateLimiting:Read:PermitLimit"] = readPermitLimit.ToString(),
            ["RateLimiting:Read:WindowSeconds"] = "60",
            ["RateLimiting:Read:QueueLimit"] = "0"
        };
    }

    private static HttpRequestMessage CreateImportRequest(string path, object body, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("X-Api-Key", apiKey);
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

    private static async Task AssertResponseBodyMatchesFixture(HttpResponseMessage response, string fixtureName)
    {
        var expectedFixturePath = Path.Combine(FindFixtureDirectory(), fixtureName);
        var expectedJson = await File.ReadAllTextAsync(expectedFixturePath);
        var actualJson = await response.Content.ReadAsStringAsync();

        Assert.Equal(NormalizeJson(expectedJson), NormalizeJson(actualJson));
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    private static async Task<string> NormalizeJsonAsync(HttpResponseMessage response)
    {
        return NormalizeJson(await response.Content.ReadAsStringAsync());
    }

    private static string NormalizeJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        var builder = new StringBuilder();
        WriteCanonicalJson(document.RootElement, builder);
        return builder.ToString();
    }

    private static void WriteCanonicalJson(JsonElement element, StringBuilder builder)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                builder.Append('{');
                var firstProperty = true;
                foreach (var property in element.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    if (!firstProperty)
                    {
                        builder.Append(',');
                    }

                    firstProperty = false;
                    builder.Append(JsonSerializer.Serialize(property.Name));
                    builder.Append(':');
                    WriteCanonicalJson(property.Value, builder);
                }

                builder.Append('}');
                break;

            case JsonValueKind.Array:
                builder.Append('[');
                var firstItem = true;
                foreach (var item in element.EnumerateArray())
                {
                    if (!firstItem)
                    {
                        builder.Append(',');
                    }

                    firstItem = false;
                    WriteCanonicalJson(item, builder);
                }

                builder.Append(']');
                break;

            case JsonValueKind.String:
                builder.Append(JsonSerializer.Serialize(element.GetString()));
                break;

            case JsonValueKind.Number:
                builder.Append(element.GetRawText());
                break;

            case JsonValueKind.True:
                builder.Append("true");
                break;

            case JsonValueKind.False:
                builder.Append("false");
                break;

            case JsonValueKind.Null:
                builder.Append("null");
                break;

            default:
                throw new InvalidOperationException($"Unsupported JSON value kind: {element.ValueKind}");
        }
    }

    private static string FindFixtureDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidatePath = Path.Combine(
                directory.FullName,
                "tests",
                "contract-fixtures",
                "http");

            if (Directory.Exists(candidatePath))
            {
                return candidatePath;
            }
        }

        throw new DirectoryNotFoundException("Could not find tests/contract-fixtures/http.");
    }
}
