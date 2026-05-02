using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarbonFactor.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CarbonFactor.Api.Tests;

public sealed class CarbonFactorSummaryTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Summary_WithEmptyDataset_ReturnsZeroSummary()
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/carbon-factors/summary");

        var summary = await ReadSummary(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, summary.TotalCount);
        Assert.Empty(summary.CountByCategory);
        Assert.Empty(summary.CountByUnit);
        Assert.Empty(summary.CountBySource);
        Assert.Empty(summary.CountByRegion);
        Assert.Null(summary.MinEffectiveYear);
        Assert.Null(summary.MaxEffectiveYear);
    }

    [Fact]
    public async Task Summary_WithMultipleCategories_ReturnsCategoryCounts()
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        await Seed(client, [
            Request("A", "energy", "kg_co2e"),
            Request("B", "transport", "kg_co2e_per_km"),
            Request("C", "energy", "kg_co2e_per_kwh")
        ]);

        using var response = await client.GetAsync("/api/carbon-factors/summary");

        var summary = await ReadSummary(response);
        Assert.Equal(3, summary.TotalCount);
        Assert.Equal(
            [new CarbonFactorSummaryCount("energy", 2), new CarbonFactorSummaryCount("transport", 1)],
            summary.CountByCategory);
    }

    [Fact]
    public async Task Summary_WithMultipleUnits_ReturnsUnitCounts()
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        await Seed(client, [
            Request("A", "energy", "kg_co2e"),
            Request("B", "energy", "kg_co2e"),
            Request("C", "energy", "g_co2e")
        ]);

        using var response = await client.GetAsync("/api/carbon-factors/summary");

        var summary = await ReadSummary(response);
        Assert.Equal(
            [new CarbonFactorSummaryCount("g_co2e", 1), new CarbonFactorSummaryCount("kg_co2e", 2)],
            summary.CountByUnit);
    }

    [Fact]
    public async Task Summary_BucketsAreSortedDeterministically()
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        await Seed(client, [
            Request("A", "transport", "kg_co2e_per_km", source: "z-source", region: "TR"),
            Request("B", "energy", "kg_co2e", source: "a-source", region: "GB")
        ]);

        using var response = await client.GetAsync("/api/carbon-factors/summary");

        var summary = await ReadSummary(response);
        Assert.Equal(["energy", "transport"], summary.CountByCategory.Select(item => item.Key));
        Assert.Equal(["a-source", "z-source"], summary.CountBySource.Select(item => item.Key));
        Assert.Equal(["GB", "TR"], summary.CountByRegion.Select(item => item.Key));
    }

    [Fact]
    public async Task Summary_ReturnsCorrectTotalsAndEffectiveYearRange()
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        await Seed(client, [
            Request("A", "energy", "kg_co2e", effectiveYear: 2020),
            Request("B", "transport", "kg_co2e_per_km", effectiveYear: 2025)
        ]);

        using var response = await client.GetAsync("/api/carbon-factors/summary");

        var summary = await ReadSummary(response);
        Assert.Equal(2, summary.TotalCount);
        Assert.Equal(2020, summary.MinEffectiveYear);
        Assert.Equal(2025, summary.MaxEffectiveYear);
    }

    private static async Task Seed(HttpClient client, IReadOnlyList<CarbonFactorCreateRequest> requests)
    {
        foreach (var request in requests)
        {
            using var response = await client.PostAsJsonAsync("/api/carbon-factors", request);
            response.EnsureSuccessStatusCode();
        }
    }

    private static async Task<CarbonFactorSummaryResponse> ReadSummary(HttpResponseMessage response)
    {
        var summary = await response.Content.ReadFromJsonAsync<CarbonFactorSummaryResponse>(JsonOptions);
        Assert.NotNull(summary);
        return summary;
    }

    private static CarbonFactorCreateRequest Request(
        string name,
        string category,
        string unit,
        string source = "summary-test",
        string region = "GB",
        int effectiveYear = 2025) =>
        new(
            Name: name,
            Category: category,
            Unit: unit,
            EmissionValue: 0.42m,
            Source: source,
            Region: region,
            EffectiveYear: effectiveYear);
}

