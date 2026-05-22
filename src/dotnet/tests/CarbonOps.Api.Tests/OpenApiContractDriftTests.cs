using System.Text.Json.Nodes;
using CarbonOps.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CarbonOps.Api.Tests;

public sealed class OpenApiContractDriftTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string BaselineRelativePath = "tests/contract-fixtures/openapi/openapi-public-metadata-baseline.json";

    private readonly WebApplicationFactory<Program> sourceFactory;

    public OpenApiContractDriftTests(WebApplicationFactory<Program> factory)
    {
        sourceFactory = factory;
    }

    [Fact]
    public void BaselineFileIsPresentAndValidJson()
    {
        var baseline = LoadOpenApiMetadataBaseline();

        Assert.Equal(1, baseline["schema_version"]!.GetValue<int>());
        Assert.Equal("PT-003", baseline["task"]!.GetValue<string>());
        Assert.Equal("baseline", baseline["status"]!.GetValue<string>());
        Assert.False(baseline["openapi_support"]!["generation_available"]!.GetValue<bool>());
    }

    [Fact]
    public void LiveEndpointMetadataMatchesBaselineRouteMethodSet()
    {
        var expected = ReadBaselineEndpoints()
            .Select(endpoint => endpoint.RouteId)
            .ToArray();

        var actual = ReadLivePublicEndpoints()
            .Select(endpoint => endpoint.RouteId)
            .ToArray();

        AssertEndpointSetsEqual(expected, actual);
    }

    [Fact]
    public void LiveEndpointMetadataHasExpectedAuthRateLimitAndVersionCategories()
    {
        var expected = ReadBaselineEndpoints()
            .ToDictionary(endpoint => endpoint.RouteId, StringComparer.Ordinal);
        var actual = ReadLivePublicEndpoints()
            .ToDictionary(endpoint => endpoint.RouteId, StringComparer.Ordinal);

        foreach (var (routeId, expectedEndpoint) in expected)
        {
            Assert.True(actual.TryGetValue(routeId, out var actualEndpoint), $"Missing live endpoint metadata for {routeId}.");
            Assert.Equal(expectedEndpoint.Authentication, actualEndpoint.Authentication);
            Assert.Equal(expectedEndpoint.RateLimitPolicy, actualEndpoint.RateLimitPolicy);
            Assert.Equal(expectedEndpoint.RouteVersionFamily, actualEndpoint.RouteVersionFamily);
            Assert.Equal(expectedEndpoint.ContractFamily, actualEndpoint.ContractFamily);
        }
    }

    [Fact]
    public void LiveEndpointExamplesMatchBaselineResponseStatusMetadata()
    {
        var expected = ReadBaselineEndpoints()
            .ToDictionary(endpoint => endpoint.RouteId, StringComparer.Ordinal);
        var actual = ReadLivePublicEndpoints()
            .ToDictionary(endpoint => endpoint.RouteId, StringComparer.Ordinal);

        foreach (var (routeId, expectedEndpoint) in expected)
        {
            Assert.True(actual.TryGetValue(routeId, out var actualEndpoint), $"Missing live endpoint metadata for {routeId}.");
            Assert.Equal(expectedEndpoint.MetadataResponseStatusCodes, actualEndpoint.MetadataResponseStatusCodes);
        }
    }

    [Fact]
    public void BaselineRouteSetMatchesContractParityDotNetRoutes()
    {
        var openApiBaselineRoutes = ReadBaselineEndpoints()
            .Select(endpoint => endpoint.RouteId)
            .ToArray();
        var parityBaselineRoutes = ReadDotNetRoutesFromContractParityBaseline();

        AssertEndpointSetsEqual(parityBaselineRoutes, openApiBaselineRoutes);
    }

    [Fact]
    public void LiveRouteTableDoesNotExposeUndocumentedPublicRoutes()
    {
        var documentedRoutes = ReadBaselineEndpoints()
            .Select(endpoint => endpoint.RouteId)
            .ToHashSet(StringComparer.Ordinal);
        var liveRoutes = ReadLivePublicEndpoints()
            .Select(endpoint => endpoint.RouteId)
            .ToArray();

        var undocumentedRoutes = liveRoutes
            .Where(route => !documentedRoutes.Contains(route))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(undocumentedRoutes);
    }

    [Fact]
    public void VersionedHealthRouteIsNotPartOfPublicMetadataSet()
    {
        var baseline = LoadOpenApiMetadataBaseline();
        var excludedRoutes = baseline["excluded_public_routes"]!
            .AsArray()
            .Select(route => route!.GetValue<string>())
            .ToArray();
        var liveRoutes = ReadLivePublicEndpoints()
            .Select(endpoint => endpoint.RouteId)
            .ToArray();

        Assert.Contains("GET /v1/health", excludedRoutes);
        Assert.DoesNotContain("GET /v1/health", liveRoutes);
    }

    private PublicEndpointMetadata[] ReadLivePublicEndpoints()
    {
        using var factory = sourceFactory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
        using var _ = factory.CreateClient();

        return factory.Services
            .GetRequiredService<IEnumerable<EndpointDataSource>>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .SelectMany(endpoint => ReadHttpMethods(endpoint)
                .Select(method => CreateLiveMetadata(method, endpoint)))
            .OrderBy(endpoint => endpoint.RouteId, StringComparer.Ordinal)
            .ToArray();
    }

    private static PublicEndpointMetadata CreateLiveMetadata(string method, RouteEndpoint endpoint)
    {
        var routePath = endpoint.RoutePattern.RawText ?? endpoint.RoutePattern.ToString() ?? string.Empty;
        var path = NormalizeRoutePath(routePath);

        return new PublicEndpointMetadata(
            Method: method,
            Path: path,
            RouteVersionFamily: ResolveRouteVersionFamily(path),
            ContractFamily: ResolveContractFamily(method, path),
            Authentication: ResolveAuthentication(method, path),
            RateLimitPolicy: ResolveRateLimitPolicy(endpoint),
            MetadataResponseStatusCodes: ReadEndpointExampleStatusCodes(endpoint));
    }

    private static string[] ReadHttpMethods(RouteEndpoint endpoint)
    {
        return endpoint.Metadata
            .GetMetadata<HttpMethodMetadata>()?
            .HttpMethods
            .Order(StringComparer.Ordinal)
            .ToArray() ?? [];
    }

    private static string NormalizeRoutePath(string routePath)
    {
        var normalizedPath = routePath.StartsWith("/", StringComparison.Ordinal)
            ? routePath
            : $"/{routePath}";

        while (normalizedPath.Length > 1 && normalizedPath.EndsWith("/", StringComparison.Ordinal))
        {
            normalizedPath = normalizedPath[..^1];
        }

        return normalizedPath;
    }

    private static string ResolveRouteVersionFamily(string path)
    {
        if (path.StartsWith("/v1/carbon-factors", StringComparison.Ordinal))
        {
            return "v1";
        }

        if (path.StartsWith("/carbon-factors", StringComparison.Ordinal))
        {
            return "legacy";
        }

        return "operational";
    }

    private static string ResolveContractFamily(string method, string path)
    {
        if (string.Equals(method, "POST", StringComparison.Ordinal)
            && path.EndsWith("/carbon-factors/import", StringComparison.Ordinal))
        {
            return "carbon_factor_import";
        }

        if (path.StartsWith("/carbon-factors", StringComparison.Ordinal)
            || path.StartsWith("/v1/carbon-factors", StringComparison.Ordinal))
        {
            return "carbon_factor_read";
        }

        return "operational";
    }

    private static string ResolveAuthentication(string method, string path)
    {
        return string.Equals(method, "POST", StringComparison.Ordinal)
            && path.EndsWith("/carbon-factors/import", StringComparison.Ordinal)
            ? "api_key"
            : "public";
    }

    private static string ResolveRateLimitPolicy(RouteEndpoint endpoint)
    {
        var policyName = endpoint.Metadata
            .Select(TryReadRateLimitPolicyName)
            .FirstOrDefault(policy => !string.IsNullOrWhiteSpace(policy));

        return policyName switch
        {
            "carbonops-import" => "import",
            "carbonops-read" => "read",
            _ => "none"
        };
    }

    private static string? TryReadRateLimitPolicyName(object metadata)
    {
        var metadataType = metadata.GetType();
        if (metadataType.FullName?.Contains("RateLimiting", StringComparison.Ordinal) != true)
        {
            return null;
        }

        return metadataType.GetProperty("PolicyName")?.GetValue(metadata) as string;
    }

    private static int[] ReadEndpointExampleStatusCodes(RouteEndpoint endpoint)
    {
        return endpoint.Metadata
            .OfType<EndpointExample>()
            .Select(example => example.StatusCode)
            .Distinct()
            .Order()
            .ToArray();
    }

    private static PublicEndpointMetadata[] ReadBaselineEndpoints()
    {
        return LoadOpenApiMetadataBaseline()["public_endpoint_metadata"]!
            .AsArray()
            .Select(endpoint => endpoint!.AsObject())
            .Select(endpoint => new PublicEndpointMetadata(
                Method: endpoint["method"]!.GetValue<string>(),
                Path: endpoint["path"]!.GetValue<string>(),
                RouteVersionFamily: endpoint["route_version_family"]!.GetValue<string>(),
                ContractFamily: endpoint["contract_family"]!.GetValue<string>(),
                Authentication: endpoint["authentication"]!.GetValue<string>(),
                RateLimitPolicy: endpoint["rate_limit_policy"]!.GetValue<string>(),
                MetadataResponseStatusCodes: endpoint["metadata_response_status_codes"]!
                    .AsArray()
                    .Select(statusCode => statusCode!.GetValue<int>())
                    .Order()
                    .ToArray()))
            .OrderBy(endpoint => endpoint.RouteId, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] ReadDotNetRoutesFromContractParityBaseline()
    {
        return LoadJsonFile("tests/contract-parity/contract-parity-baseline.json")["route_families"]!
            .AsArray()
            .Where(family => string.Equals(family!["runtime"]!.GetValue<string>(), ".NET", StringComparison.Ordinal))
            .SelectMany(family => family!["routes"]!.AsArray())
            .Select(route => route!.GetValue<string>())
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static JsonObject LoadOpenApiMetadataBaseline()
    {
        return LoadJsonFile(BaselineRelativePath);
    }

    private static JsonObject LoadJsonFile(string relativePath)
    {
        var jsonPath = FindRepositoryFile(relativePath);
        var json = JsonNode.Parse(File.ReadAllText(jsonPath))?.AsObject();

        Assert.NotNull(json);
        return json!;
    }

    private static string FindRepositoryFile(string relativePath)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidatePath = Path.Combine(
                [directory.FullName, ..relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries)]);

            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }
        }

        throw new FileNotFoundException($"Could not find {relativePath}.");
    }

    private static void AssertEndpointSetsEqual(string[] expected, string[] actual)
    {
        var missingRoutes = expected
            .Except(actual, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var extraRoutes = actual
            .Except(expected, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            missingRoutes.Length == 0 && extraRoutes.Length == 0,
            $"Endpoint metadata drift detected.{Environment.NewLine}"
            + $"Missing: {string.Join(", ", missingRoutes)}{Environment.NewLine}"
            + $"Extra: {string.Join(", ", extraRoutes)}");
    }

    private sealed record PublicEndpointMetadata(
        string Method,
        string Path,
        string RouteVersionFamily,
        string ContractFamily,
        string Authentication,
        string RateLimitPolicy,
        int[] MetadataResponseStatusCodes)
    {
        public string RouteId => $"{Method} {Path}";
    }
}
