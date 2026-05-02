using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CarbonFactor.Api.Contracts;
using CarbonFactor.Api.Errors;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CarbonFactor.Api.Tests;

public sealed class CarbonFactorIngestionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient client;

    public CarbonFactorIngestionTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task SingleIngestion_WithValidRecord_ReturnsCreatedRecord()
    {
        using var response = await client.PostAsJsonAsync("/api/carbon-factors", ValidRequest());

        var created = await response.Content.ReadFromJsonAsync<CarbonFactorResponse>(JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, Guid.Parse(created.Id));
        Assert.Equal("Grid electricity", created.Name);
        Assert.Equal("energy", created.Category);
        Assert.Equal("kg_co2e_per_kwh", created.Unit);
    }

    [Fact]
    public async Task BatchIngestion_WithValidRecords_ReturnsAcceptedSummary()
    {
        var request = new CarbonFactorBatchIngestRequest([
            ValidRequest(),
            ValidRequest() with { Name = "Diesel fuel", Category = "energy", Unit = "kg CO2e / liter" }
        ]);

        using var response = await client.PostAsJsonAsync("/api/carbon-factors/batch", request);

        var result = await response.Content.ReadFromJsonAsync<CarbonFactorIngestionResponse>(JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(2, result.Total);
        Assert.Equal(2, result.Accepted);
        Assert.Equal(0, result.Rejected);
        Assert.All(result.Items, item => Assert.Equal("accepted", item.Status));
    }

    [Fact]
    public async Task BatchIngestion_WithMixedValidity_ReturnsPerRecordResults()
    {
        var request = new CarbonFactorBatchIngestRequest([
            ValidRequest() with { Name = "First" },
            ValidRequest() with { Unit = "bad-unit" },
            ValidRequest() with { Name = "Third" }
        ]);

        using var response = await client.PostAsJsonAsync("/api/carbon-factors/batch", request);

        var result = await response.Content.ReadFromJsonAsync<CarbonFactorIngestionResponse>(JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(3, result.Total);
        Assert.Equal(2, result.Accepted);
        Assert.Equal(1, result.Rejected);
        Assert.Equal([0, 1, 2], result.Items.Select(item => item.Index));
        Assert.Equal("accepted", result.Items[0].Status);
        Assert.Equal("rejected", result.Items[1].Status);
        Assert.Equal("accepted", result.Items[2].Status);
        Assert.Contains(result.Items[1].Errors, error => error.Field == "unit" && error.Code == "unsupported_unit");
    }

    [Fact]
    public async Task BatchIngestion_WithEmptyBatch_ReturnsValidationError()
    {
        using var response = await client.PostAsJsonAsync("/api/carbon-factors/batch", new CarbonFactorBatchIngestRequest([]));

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(error);
        Assert.Contains(error.Errors, item => item.Field == "items" && item.Code == "empty_batch");
    }

    [Fact]
    public async Task BatchIngestion_WithMalformedRequest_ReturnsStructuredError()
    {
        using var content = new StringContent("{ nope", Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/carbon-factors/batch", content);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("invalid_request", error.Code);
    }

    [Fact]
    public async Task BatchIngestion_ResponseOrderIsDeterministic()
    {
        var request = new CarbonFactorBatchIngestRequest([
            ValidRequest() with { Unit = "bad-unit" },
            ValidRequest() with { Name = "Accepted" },
            ValidRequest() with { Category = "bad-category" }
        ]);

        using var response = await client.PostAsJsonAsync("/api/carbon-factors/batch", request);

        var result = await response.Content.ReadFromJsonAsync<CarbonFactorIngestionResponse>(JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal([0, 1, 2], result.Items.Select(item => item.Index));
        Assert.Equal(["rejected", "accepted", "rejected"], result.Items.Select(item => item.Status));
    }

    private static CarbonFactorCreateRequest ValidRequest() =>
        new(
            Name: "Grid electricity",
            Category: "Electricity",
            Unit: "kg CO2e / kWh",
            EmissionValue: 0.42m,
            Source: "Example source",
            Region: "GB",
            EffectiveYear: 2025);
}

