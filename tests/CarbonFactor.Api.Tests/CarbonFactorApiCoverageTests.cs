using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarbonFactor.Api.Contracts;
using CarbonFactor.Api.Domain;
using CarbonFactor.Api.Errors;
using CarbonFactor.Api.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CarbonFactor.Api.Tests;

public sealed class CarbonFactorApiCoverageTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task CreatedRecord_CanBeRetrievedById()
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        using var createResponse = await client.PostAsJsonAsync(
            "/api/carbon-factors",
            CarbonFactorTestRequests.Valid(name: "Lookup factor", unit: "kg CO2e / kWh"));
        var created = await createResponse.Content.ReadFromJsonAsync<CarbonFactorResponse>(JsonOptions);

        using var getResponse = await client.GetAsync($"/api/carbon-factors/{created!.Id}");

        var retrieved = await getResponse.Content.ReadFromJsonAsync<CarbonFactorResponse>(JsonOptions);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal("Lookup factor", retrieved.Name);
        Assert.Equal("kg_co2e_per_kwh", retrieved.Unit);
    }

    [Fact]
    public async Task Query_FilterByRegionEffectiveYearAndSearch_ReturnsExpectedRecord()
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        await Seed(client, [
            CarbonFactorTestRequests.Valid(name: "Grid electricity 2025", region: "GB", effectiveYear: 2025),
            CarbonFactorTestRequests.Valid(name: "Grid electricity 2024", region: "GB", effectiveYear: 2024),
            CarbonFactorTestRequests.Valid(name: "Regional diesel", unit: "kg_co2e_per_liter", region: "TR", effectiveYear: 2025)
        ]);

        using var response = await client.GetAsync("/api/carbon-factors?region=gb&effectiveYear=2025&search=electricity");

        var result = await response.Content.ReadFromJsonAsync<CarbonFactorQueryResponse>(JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        var item = Assert.Single(result.Items);
        Assert.Equal("Grid electricity 2025", item.Name);
    }

    [Theory]
    [InlineData("/api/carbon-factors?category=unsupported")]
    [InlineData("/api/carbon-factors?unit=unsupported")]
    public async Task Query_UnsupportedNormalizedFilters_ReturnValidationErrors(string url)
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        using var response = await client.GetAsync(url);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("validation_failed", error.Code);
        Assert.NotEmpty(error.Errors);
    }

    [Fact]
    public async Task BatchIngestion_WithMissingItems_ReturnsRequiredItemsError()
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/carbon-factors/batch", new { });

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(error);
        Assert.Contains(error.Errors, item => item.Field == "items" && item.Code == "required");
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(2101)]
    public void DomainValidation_EffectiveYearOutsideSupportedRange_ReturnsError(int effectiveYear)
    {
        var validator = new CarbonFactorValidator(new CarbonFactorNormalizer());

        var result = validator.Create(
            Guid.NewGuid(),
            new CarbonFactorInput(
                Name: "Year edge",
                Category: "energy",
                Unit: "kg_co2e",
                EmissionValue: 0.42m,
                EffectiveYear: effectiveYear));

        Assert.Contains(result.Errors, error => error.Field == "effectiveYear" && error.Code == "invalid_range");
    }

    private static async Task Seed(HttpClient client, IReadOnlyList<CarbonFactorCreateRequest> requests)
    {
        foreach (var request in requests)
        {
            using var response = await client.PostAsJsonAsync("/api/carbon-factors", request);
            response.EnsureSuccessStatusCode();
        }
    }
}

