using System.Net;
using CarbonOps.Application.Factors;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CarbonOps.Api.Tests;

public sealed class ApiCompositionSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;
    private readonly HttpClient client;

    public ApiCompositionSmokeTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
    }

    [Fact]
    public void ApplicationServicesResolveFromApiCompositionRoot()
    {
        using var scope = factory.Services.CreateScope();

        var useCases = scope.ServiceProvider.GetRequiredService<CarbonFactorUseCases>();
        var repository = scope.ServiceProvider.GetRequiredService<ICarbonFactorRepository>();

        Assert.NotNull(useCases);
        Assert.NotNull(repository);
    }

    [Fact]
    public void ApiCompositionRegistersExpectedGetEndpoints()
    {
        using var scope = factory.Services.CreateScope();

        var routeEndpoints = scope.ServiceProvider
            .GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .Select(endpoint => NormalizeRoutePattern(endpoint.RoutePattern.RawText))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Subset(
            new HashSet<string>(StringComparer.Ordinal)
            {
                "/health",
                "/health/live",
                "/health/ready",
                "/version",
                "/carbon-factors",
                "/carbon-factors/import",
                "/carbon-factors/search",
                "/carbon-factors/{factorId}"
            },
            routeEndpoints);
    }

    [Theory]
    [InlineData("/health", HttpStatusCode.OK)]
    [InlineData("/health/live", HttpStatusCode.OK)]
    [InlineData("/health/ready", HttpStatusCode.OK)]
    [InlineData("/version", HttpStatusCode.OK)]
    [InlineData("/carbon-factors", HttpStatusCode.OK)]
    [InlineData("/carbon-factors/search", HttpStatusCode.OK)]
    [InlineData("/carbon-factors/f-001", HttpStatusCode.OK)]
    public async Task ExpectedGetEndpointsAreReachable(string route, HttpStatusCode expectedStatusCode)
    {
        var response = await client.GetAsync(route);

        Assert.Equal(expectedStatusCode, response.StatusCode);
    }

    private static string NormalizeRoutePattern(string? routePattern)
    {
        if (string.IsNullOrWhiteSpace(routePattern))
        {
            return "/";
        }

        var trimmed = routePattern.TrimEnd('/');
        return trimmed.Length == 0 ? "/" : trimmed;
    }
}
