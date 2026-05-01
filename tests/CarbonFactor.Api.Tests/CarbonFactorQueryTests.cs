using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarbonFactor.Api.Contracts;
using CarbonFactor.Api.Errors;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CarbonFactor.Api.Tests;

public sealed class CarbonFactorQueryTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Query_FilterByCategory_ReturnsMatchingRecords()
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        await Seed(client, [
            Request("Grid electricity", "energy", "kg_co2e_per_kwh"),
            Request("Train travel", "transport", "kg_co2e_per_km")
        ]);

        using var response = await client.GetAsync("/api/carbon-factors?category=Electricity");

        var result = await ReadQuery(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = Assert.Single(result.Items);
        Assert.Equal("Grid electricity", item.Name);
    }

    [Fact]
    public async Task Query_FilterByUnit_ReturnsMatchingRecords()
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        await Seed(client, [
            Request("Grid electricity", "energy", "kg_co2e_per_kwh"),
            Request("Diesel fuel", "energy", "kg_co2e_per_liter")
        ]);

        using var response = await client.GetAsync("/api/carbon-factors?unit=kg%20CO2e%20/%20liter");

        var result = await ReadQuery(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = Assert.Single(result.Items);
        Assert.Equal("Diesel fuel", item.Name);
    }

    [Fact]
    public async Task Query_FilterBySource_ReturnsMatchingRecords()
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        await Seed(client, [
            Request("A", "energy", "kg_co2e", source: "dataset-a"),
            Request("B", "energy", "kg_co2e", source: "dataset-b")
        ]);

        using var response = await client.GetAsync("/api/carbon-factors?source=DATASET-B");

        var result = await ReadQuery(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = Assert.Single(result.Items);
        Assert.Equal("B", item.Name);
    }

    [Fact]
    public async Task Query_PaginatesResults()
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        await Seed(client, [
            Request("Alpha", "energy", "kg_co2e"),
            Request("Bravo", "energy", "kg_co2e"),
            Request("Charlie", "energy", "kg_co2e")
        ]);

        using var response = await client.GetAsync("/api/carbon-factors?page=2&pageSize=2");

        var result = await ReadQuery(response);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        var item = Assert.Single(result.Items);
        Assert.Equal("Charlie", item.Name);
    }

    [Theory]
    [InlineData("/api/carbon-factors?page=0")]
    [InlineData("/api/carbon-factors?pageSize=0")]
    [InlineData("/api/carbon-factors?pageSize=101")]
    public async Task Query_InvalidPaginationParameters_ReturnValidationError(string url)
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        using var response = await client.GetAsync(url);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(error);
        Assert.Contains(error.Errors, item => item.Field is "page" or "pageSize");
    }

    [Fact]
    public async Task Query_EmptyResult_ReturnsValidPage()
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/carbon-factors?search=missing");

        var result = await ReadQuery(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Query_OrderIsDeterministicByNameThenId()
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        await Seed(client, [
            Request("Charlie", "energy", "kg_co2e"),
            Request("Alpha", "energy", "kg_co2e"),
            Request("Bravo", "energy", "kg_co2e")
        ]);

        using var response = await client.GetAsync("/api/carbon-factors?pageSize=10");

        var result = await ReadQuery(response);
        Assert.Equal(["Alpha", "Bravo", "Charlie"], result.Items.Select(item => item.Name));
    }

    private static async Task Seed(HttpClient client, IReadOnlyList<CarbonFactorCreateRequest> requests)
    {
        foreach (var request in requests)
        {
            using var response = await client.PostAsJsonAsync("/api/carbon-factors", request);
            response.EnsureSuccessStatusCode();
        }
    }

    private static async Task<CarbonFactorQueryResponse> ReadQuery(HttpResponseMessage response)
    {
        var result = await response.Content.ReadFromJsonAsync<CarbonFactorQueryResponse>(JsonOptions);
        Assert.NotNull(result);
        return result;
    }

    private static CarbonFactorCreateRequest Request(
        string name,
        string category,
        string unit,
        string source = "query-test") =>
        new(
            Name: name,
            Category: category,
            Unit: unit,
            EmissionValue: 0.42m,
            Source: source,
            Region: "GB",
            EffectiveYear: 2025);
}

