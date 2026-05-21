using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace CarbonOps.Api.Tests;

public sealed class CarbonFactorEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;
    private readonly WebApplicationFactory<Program> factory;

    public CarbonFactorEndpointsTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
    }

    [Fact]
    public async Task ListCarbonFactorsReturnsDeterministicResponse()
    {
        var response = await client.GetAsync("/carbon-factors");

        response.EnsureSuccessStatusCode();

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        AssertObjectPropertyNames(payload, "factors", "total");
        Assert.Equal(3, payload.GetProperty("total").GetInt32());

        var factors = payload.GetProperty("factors");

        Assert.Equal(3, factors.GetArrayLength());
        Assert.Equal(["f-001", "f-002", "f-003"], factors.EnumerateArray().Select(factor => factor.GetProperty("id").GetString()));
        AssertFactorShape(factors[0]);
    }

    [Fact]
    public async Task ListCarbonFactorsAppliesOffsetAndLimit()
    {
        var response = await client.GetAsync("/carbon-factors?offset=1&limit=1");

        response.EnsureSuccessStatusCode();

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(3, payload.GetProperty("total").GetInt32());
        Assert.Equal(["f-002"], payload.GetProperty("factors").EnumerateArray().Select(factor => factor.GetProperty("id").GetString()));
    }

    [Fact]
    public async Task ListCarbonFactorsReturnsInvalidQueryForNegativeOffset()
    {
        var response = await client.GetAsync("/carbon-factors?offset=-1");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        AssertErrorShape(payload, "invalid_query", "Invalid query");
        Assert.Equal("offset must be zero or positive", payload.GetProperty("details").GetProperty("reason").GetString());
    }

    [Fact]
    public async Task ListCarbonFactorsReturnsInvalidQueryForUnsupportedFilters()
    {
        var response = await client.GetAsync("/carbon-factors?category=electricity");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        AssertErrorShape(payload, "invalid_query", "Invalid query");
        Assert.Equal("unsupported filters: category", payload.GetProperty("details").GetProperty("reason").GetString());
    }

    [Fact]
    public async Task GetCarbonFactorByIdReturnsMatchingFactor()
    {
        var response = await client.GetAsync("/carbon-factors/f-002");

        response.EnsureSuccessStatusCode();

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        AssertObjectPropertyNames(payload, "factor");

        var factor = payload.GetProperty("factor");

        AssertFactorShape(factor);
        Assert.Equal("f-002", factor.GetProperty("id").GetString());
        Assert.Equal("transport", factor.GetProperty("category").GetString());
        Assert.Equal("US", factor.GetProperty("region").GetString());
    }

    [Fact]
    public async Task GetCarbonFactorByIdReturnsNotFoundForMissingFactor()
    {
        var response = await client.GetAsync("/carbon-factors/missing-factor");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        AssertErrorShape(payload, "not_found", "factor not found");
        Assert.Equal("missing-factor", payload.GetProperty("details").GetProperty("id").GetString());
    }

    [Fact]
    public async Task GetCarbonFactorByIdReturnsInvalidQueryForWhitespaceFactorId()
    {
        var response = await client.GetAsync("/carbon-factors/%20%20");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        AssertErrorShape(payload, "invalid_query", "Invalid query");
        Assert.Equal("factorId is required", payload.GetProperty("details").GetProperty("reason").GetString());
    }

    [Fact]
    public async Task GetCarbonFactorByIdReturnsInvalidQueryForEmbeddedWhitespace()
    {
        var response = await client.GetAsync("/carbon-factors/f-0%2001");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        AssertErrorShape(payload, "invalid_query", "Invalid query");
        Assert.Equal("factorId must not contain whitespace", payload.GetProperty("details").GetProperty("reason").GetString());
    }

    [Fact]
    public async Task SearchCarbonFactorsReturnsMatchingFactorsForSupportedFilters()
    {
        var response = await client.GetAsync(
            "/carbon-factors/search?category=electricity&activity=grid%20electricity&region=US-WEST&year=2024");

        response.EnsureSuccessStatusCode();

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        AssertObjectPropertyNames(payload, "factors", "total");
        Assert.Equal(1, payload.GetProperty("total").GetInt32());
        Assert.Equal("f-001", payload.GetProperty("factors")[0].GetProperty("id").GetString());
    }

    [Fact]
    public async Task SearchCarbonFactorsAppliesOffsetAndLimit()
    {
        var response = await client.GetAsync("/carbon-factors/search?year=2024&offset=1&limit=1");

        response.EnsureSuccessStatusCode();

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        AssertObjectPropertyNames(payload, "factors", "total");
        Assert.Equal(2, payload.GetProperty("total").GetInt32());
        Assert.Equal(["f-002"], payload.GetProperty("factors").EnumerateArray().Select(factor => factor.GetProperty("id").GetString()));
    }

    [Fact]
    public async Task SearchCarbonFactorsReturnsEmptyCollectionForSupportedFilterWithNoMatches()
    {
        var response = await client.GetAsync("/carbon-factors/search?category=electricity&region=EU");

        response.EnsureSuccessStatusCode();

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        AssertObjectPropertyNames(payload, "factors", "total");
        Assert.Equal(0, payload.GetProperty("total").GetInt32());
        Assert.Empty(payload.GetProperty("factors").EnumerateArray());
    }

    [Fact]
    public async Task SearchCarbonFactorsReturnsInvalidQueryForNonPositiveYear()
    {
        var response = await client.GetAsync("/carbon-factors/search?year=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        AssertErrorShape(payload, "invalid_query", "Invalid query");
        Assert.Equal("year must be positive", payload.GetProperty("details").GetProperty("reason").GetString());
    }

    [Fact]
    public async Task SearchCarbonFactorsReturnsInvalidQueryForNonIntegerYear()
    {
        var response = await client.GetAsync("/carbon-factors/search?year=two-thousand-twenty-four");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        AssertErrorShape(payload, "invalid_query", "Invalid query");
        Assert.Equal("year must be an integer", payload.GetProperty("details").GetProperty("reason").GetString());
    }

    [Fact]
    public async Task SearchCarbonFactorsReturnsInvalidQueryForNonPositiveLimit()
    {
        var response = await client.GetAsync("/carbon-factors/search?limit=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        AssertErrorShape(payload, "invalid_query", "Invalid query");
        Assert.Equal("limit must be positive", payload.GetProperty("details").GetProperty("reason").GetString());
    }

    [Fact]
    public async Task SearchCarbonFactorsReturnsInvalidQueryForEmptyStringFilters()
    {
        var response = await client.GetAsync("/carbon-factors/search?category=");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        AssertErrorShape(payload, "invalid_query", "Invalid query");
        Assert.Equal("category must not be empty", payload.GetProperty("details").GetProperty("reason").GetString());
    }

    [Fact]
    public async Task SearchCarbonFactorsReturnsInvalidQueryForDuplicateFilterValues()
    {
        var response = await client.GetAsync("/carbon-factors/search?region=TR&region=US");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        AssertErrorShape(payload, "invalid_query", "Invalid query");
        Assert.Equal("region must be provided once", payload.GetProperty("details").GetProperty("reason").GetString());
    }

    [Fact]
    public async Task SearchCarbonFactorsReturnsInvalidQueryForUnsupportedFilters()
    {
        var response = await client.GetAsync("/carbon-factors/search?category=electricity&zeta=1&alpha=2");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        AssertErrorShape(payload, "invalid_query", "Invalid query");
        Assert.Equal(
            "unsupported filters: alpha, zeta",
            payload.GetProperty("details").GetProperty("reason").GetString());
    }

    [Fact]
    public async Task ErrorResponsesRemainJsonWhenMappedByMiddleware()
    {
        var response = await client.GetAsync("/carbon-factors/missing-factor");

        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task ImportCarbonFactorsReturnsAcceptedBoundaryResponseForValidRequest()
    {
        var response = await client.PostAsJsonAsync("/carbon-factors/import", CreateValidImportRequest());

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.False(payload.GetProperty("persisted").GetBoolean());
        Assert.Equal("not_started", payload.GetProperty("import_execution").GetString());
    }

    [Fact]
    public async Task ImportCarbonFactorsReturnsInvalidQueryForEmptyFactors()
    {
        var request = CreateValidImportRequest();
        request["factors"] = Array.Empty<object>();

        var response = await client.PostAsJsonAsync("/carbon-factors/import", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("factors must contain at least one item", payload.GetProperty("details").GetProperty("reason").GetString());
    }

    [Fact]
    public async Task ImportCarbonFactorsReturnsInvalidQueryForMissingRequiredField()
    {
        var request = CreateValidImportRequest();
        request["batch_id"] = " ";

        var response = await client.PostAsJsonAsync("/carbon-factors/import", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("batch_id is required", payload.GetProperty("details").GetProperty("reason").GetString());
    }

    public async Task HealthEndpointReturnsDeterministicPayload()
    {
        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        AssertObjectPropertyNames(payload, "status");
        Assert.Equal("ok", payload.GetProperty("status").GetString());
    }

    [Fact]
    public async Task LivenessEndpointReturnsDeterministicPayload()
    {
        var response = await client.GetAsync("/health/live");

        response.EnsureSuccessStatusCode();

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        AssertObjectPropertyNames(payload, "check", "status");
        Assert.Equal("liveness", payload.GetProperty("check").GetString());
        Assert.Equal("ok", payload.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ReadinessEndpointReturnsDeterministicPayload()
    {
        var response = await client.GetAsync("/health/ready");

        response.EnsureSuccessStatusCode();

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        AssertObjectPropertyNames(payload, "check", "status");
        Assert.Equal("readiness", payload.GetProperty("check").GetString());
        Assert.Equal("ok", payload.GetProperty("status").GetString());
    }

    [Fact]
    public async Task VersionEndpointReturnsDeterministicPayload()
    {
        var response = await client.GetAsync("/version");

        response.EnsureSuccessStatusCode();

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        AssertObjectPropertyNames(payload, "name", "version");
        Assert.Equal("CarbonOps API", payload.GetProperty("name").GetString());
        Assert.Equal("0.1.0", payload.GetProperty("version").GetString());
    }

    [Fact]
    public void CurrentCarbonFactorRoutesExposeEndpointExamples()
    {
        var endpoints = factory.Services
            .GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText is not null)
            .ToDictionary(endpoint => endpoint.RoutePattern.RawText!, StringComparer.Ordinal);

        AssertRouteExamples(
            endpoints,
            "/health",
            OperationalEndpointExamples.HealthSuccess);
        AssertRouteExamples(
            endpoints,
            "/health/live",
            OperationalEndpointExamples.LivenessSuccess);
        AssertRouteExamples(
            endpoints,
            "/health/ready",
            OperationalEndpointExamples.ReadinessSuccess);
        AssertRouteExamples(
            endpoints,
            "/version",
            OperationalEndpointExamples.VersionSuccess);
        AssertRouteExamples(
            endpoints,
            "/carbon-factors/",
            CarbonFactorEndpointExamples.ListFactorsSuccess);
        AssertRouteExamples(
            endpoints,
            "/carbon-factors/search",
            CarbonFactorEndpointExamples.SearchFactorsSuccess,
            CarbonFactorEndpointExamples.SearchFactorsInvalidQuery);
        AssertRouteExamples(
            endpoints,
            "/carbon-factors/{factorId}",
            CarbonFactorEndpointExamples.GetFactorByIdSuccess,
            CarbonFactorEndpointExamples.GetFactorByIdNotFound);
    }

    private static void AssertFactorShape(JsonElement factor)
    {
        AssertObjectPropertyNames(
            factor,
            "activity",
            "category",
            "factor_unit",
            "factor_value",
            "id",
            "notes",
            "region",
            "source",
            "year");
    }

    private static void AssertErrorShape(JsonElement payload, string code, string message)
    {
        AssertObjectPropertyNames(payload, "code", "details", "message");
        Assert.Equal(code, payload.GetProperty("code").GetString());
        Assert.Equal(message, payload.GetProperty("message").GetString());
    }

    private static void AssertRouteExamples(
        IReadOnlyDictionary<string, RouteEndpoint> endpoints,
        string routePattern,
        params EndpointExample[] expectedExamples)
    {
        var endpoint = Assert.Contains(routePattern, endpoints);
        var actualExamples = endpoint.Metadata.OfType<EndpointExample>().ToArray();

        Assert.Equal(expectedExamples.Length, actualExamples.Length);

        for (var index = 0; index < expectedExamples.Length; index++)
        {
            Assert.Equal(expectedExamples[index], actualExamples[index]);
        }
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
    private static void AssertObjectPropertyNames(JsonElement payload, params string[] expectedPropertyNames)
    {
        var actualPropertyNames = payload
            .EnumerateObject()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            expectedPropertyNames.OrderBy(name => name, StringComparer.Ordinal).ToArray(),
            actualPropertyNames);
    }
}
