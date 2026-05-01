using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CarbonFactor.Api.Tests;

public sealed class OpenApiDocumentationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public OpenApiDocumentationTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task SwaggerJson_InDevelopment_DescribesCarbonFactorEndpoints()
    {
        using var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/carbon-factors", out _));
        Assert.True(paths.TryGetProperty("/api/carbon-factors/batch", out _));
        Assert.True(paths.TryGetProperty("/api/carbon-factors/summary", out _));
        Assert.True(paths.TryGetProperty("/api/carbon-factors/{id}", out _));
    }
}

