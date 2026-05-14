using System.Net;
using System.Net.Http.Json;
using CarbonOps.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CarbonOps.Api.Tests;

public sealed class CarbonFactorEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public CarbonFactorEndpointsTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task ListCarbonFactorsReturnsDeterministicResponse()
    {
        var response = await client.GetAsync("/carbon-factors");

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<FactorListResponse>();

        Assert.NotNull(payload);
        Assert.Equal(3, payload.Total);
        Assert.Equal(["f-001", "f-002", "f-003"], payload.Factors.Select(factor => factor.Id));
    }

    [Fact]
    public async Task GetCarbonFactorByIdReturnsMatchingFactor()
    {
        var response = await client.GetAsync("/carbon-factors/f-002");

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<FactorDetailResponse>();

        Assert.NotNull(payload);
        Assert.Equal("f-002", payload.Factor.Id);
        Assert.Equal("transport", payload.Factor.Category);
        Assert.Equal("US", payload.Factor.Region);
    }

    [Fact]
    public async Task GetCarbonFactorByIdReturnsNotFoundForMissingFactor()
    {
        var response = await client.GetAsync("/carbon-factors/missing-factor");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiError>();

        Assert.NotNull(payload);
        Assert.Equal("not_found", payload.Code);
        Assert.Equal("factor not found", payload.Message);
        Assert.Equal("missing-factor", payload.Details["id"].ToString());
    }

    [Fact]
    public async Task SearchCarbonFactorsReturnsMatchingFactorsForSupportedFilters()
    {
        var response = await client.GetAsync(
            "/carbon-factors/search?category=electricity&activity=grid%20electricity&region=US-WEST&year=2024");

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<FactorListResponse>();

        Assert.NotNull(payload);
        Assert.Equal(1, payload.Total);
        Assert.Equal("f-001", payload.Factors.Single().Id);
    }

    [Fact]
    public async Task SearchCarbonFactorsReturnsInvalidQueryForNonPositiveYear()
    {
        var response = await client.GetAsync("/carbon-factors/search?year=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiError>();

        Assert.NotNull(payload);
        Assert.Equal("invalid_query", payload.Code);
        Assert.Equal("Invalid query", payload.Message);
        Assert.Equal("year must be positive", payload.Details["reason"].ToString());
    }

    [Fact]
    public async Task SearchCarbonFactorsReturnsInvalidQueryForUnsupportedFilters()
    {
        var response = await client.GetAsync("/carbon-factors/search?category=electricity&zeta=1&alpha=2");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiError>();

        Assert.NotNull(payload);
        Assert.Equal("invalid_query", payload.Code);
        Assert.Equal("unsupported filters: alpha, zeta", payload.Details["reason"].ToString());
    }
}
