using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CarbonFactor.Api.Contracts;
using CarbonFactor.Api.Errors;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CarbonFactor.Api.Tests;

public sealed class ApiErrorResponseTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient client;

    public ApiErrorResponseTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task InvalidPayload_ReturnsStructuredError()
    {
        using var content = new StringContent("{ invalid json", Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/carbon-factors", content);

        var error = await ReadError(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_request", error.Code);
        Assert.Equal("body", Assert.Single(error.Errors).Field);
    }

    [Fact]
    public async Task MissingRequiredField_ReturnsValidationErrors()
    {
        var request = new CarbonFactorCreateRequest(
            Name: null,
            Category: "energy",
            Unit: "kg_co2e",
            EmissionValue: 0.42m);

        using var response = await client.PostAsJsonAsync("/api/carbon-factors", request);

        var error = await ReadError(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("validation_failed", error.Code);
        Assert.Contains(error.Errors, item => item.Field == "name" && item.Code == "required");
    }

    [Fact]
    public async Task UnsupportedUnit_ReturnsValidationError()
    {
        var request = new CarbonFactorCreateRequest(
            Name: "Grid electricity",
            Category: "energy",
            Unit: "unsupported-unit",
            EmissionValue: 0.42m);

        using var response = await client.PostAsJsonAsync("/api/carbon-factors", request);

        var error = await ReadError(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(error.Errors, item => item.Field == "unit" && item.Code == "unsupported_unit");
    }

    [Fact]
    public async Task NotFound_ReturnsStructuredError()
    {
        using var response = await client.GetAsync($"/api/carbon-factors/{Guid.NewGuid():D}");

        var error = await ReadError(response);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("carbon_factor_not_found", error.Code);
        Assert.Empty(error.Errors);
    }

    [Fact]
    public async Task ErrorResponseShape_IsPredictable()
    {
        using var response = await client.GetAsync("/api/carbon-factors/not-a-guid");

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(root.TryGetProperty("status", out _));
        Assert.True(root.TryGetProperty("code", out _));
        Assert.True(root.TryGetProperty("title", out _));
        Assert.True(root.TryGetProperty("detail", out _));
        Assert.True(root.TryGetProperty("traceId", out _));
        Assert.True(root.TryGetProperty("errors", out _));
    }

    private static async Task<ApiErrorResponse> ReadError(HttpResponseMessage response)
    {
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        Assert.NotNull(error);
        return error;
    }
}

