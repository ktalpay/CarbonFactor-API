using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarbonOps.Api.Tests;

public sealed class CarbonFactorEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestApiKey = "test-import-api-key";
    private const string TestApiKeyHash = "f9ffdcc248b716dfc2dbee5492ecbabab672b7d19522859ad3b0a0fd49a86fd0";
    private const string PreviousApiKey = "previous-import-api-key";
    private const string PreviousApiKeyHash = "dcf2d8f1700d7f49fae4215241cbf24c0fb633cc3783e49e24a754891552e91d";
    private const string TestTenantId = "tenant-dev-001";
    private const string ImportScope = "carbon_factors:import";
    private readonly HttpClient client;
    private readonly WebApplicationFactory<Program> sourceFactory;
    private readonly WebApplicationFactory<Program> factory;

    public CarbonFactorEndpointsTests(WebApplicationFactory<Program> factory)
    {
        sourceFactory = factory;
        this.factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(CreateApiKeyConfiguration());
            });
        });

        client = this.factory.CreateClient();
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
    public async Task ImportCarbonFactorsReturnsUnauthorizedWhenApiKeyHashConfigIsMissing()
    {
        using var hashlessFactory = sourceFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(CreateApiKeyConfiguration(apiKeyHash: null));
            });
        });

        using var hashlessClient = hashlessFactory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), TestApiKey);

        var response = await hashlessClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        var payload = JsonDocument.Parse(responseBody).RootElement;
        AssertErrorShape(payload, "unauthorized", "Unauthorized");
        Assert.Equal("import endpoint API key hash is not configured", payload.GetProperty("details").GetProperty("reason").GetString());
        Assert.DoesNotContain(TestApiKey, responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain(TestApiKeyHash, responseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportCarbonFactorsReturnsUnauthorizedWhenApiKeyHashConfigIsInvalid()
    {
        const string InvalidConfiguredHash = "not-a-sha256-hex";

        using var invalidHashFactory = sourceFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(CreateApiKeyConfiguration(apiKeyHash: InvalidConfiguredHash));
            });
        });

        using var invalidHashClient = invalidHashFactory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), TestApiKey);

        var response = await invalidHashClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        var payload = JsonDocument.Parse(responseBody).RootElement;
        AssertErrorShape(payload, "unauthorized", "Unauthorized");
        Assert.Equal("import endpoint API key hash is invalid", payload.GetProperty("details").GetProperty("reason").GetString());
        Assert.DoesNotContain(TestApiKey, responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain(InvalidConfiguredHash, responseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportCarbonFactorsReturnsUnauthorizedWhenApiKeyIsMissing()
    {
        var response = await client.PostAsJsonAsync("/carbon-factors/import", CreateValidImportRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        var payload = JsonDocument.Parse(responseBody).RootElement;
        AssertErrorShape(payload, "unauthorized", "Unauthorized");
        Assert.Equal("missing API key", payload.GetProperty("details").GetProperty("reason").GetString());
        Assert.DoesNotContain(TestApiKey, responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain(TestApiKeyHash, responseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportCarbonFactorsReturnsUnauthorizedWhenApiKeyIsInvalid()
    {
        var response = await PostImportRequestAsync(CreateValidImportRequest(), "invalid-key");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        var payload = JsonDocument.Parse(responseBody).RootElement;
        AssertErrorShape(payload, "unauthorized", "Unauthorized");
        Assert.Equal("invalid API key", payload.GetProperty("details").GetProperty("reason").GetString());
        Assert.DoesNotContain(TestApiKey, responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain(TestApiKeyHash, responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain("invalid-key", responseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportCarbonFactorsReturnsAcceptedWhenPreviousApiKeyHashMatches()
    {
        using var rotationFactory = sourceFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(CreateApiKeyConfiguration(previousKeyHashes: [PreviousApiKeyHash]));
            });
        });

        using var rotationClient = rotationFactory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), PreviousApiKey);

        var response = await rotationClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        var payload = JsonDocument.Parse(responseBody).RootElement;
        Assert.Equal(TestTenantId, payload.GetProperty("audit").GetProperty("tenant_id").GetString());
        Assert.Equal("api_key", payload.GetProperty("audit").GetProperty("authentication_scheme").GetString());
        Assert.DoesNotContain("previous", responseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rotation", responseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(PreviousApiKey, responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain(PreviousApiKeyHash, responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain(TestApiKeyHash, responseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportCarbonFactorsReturnsUnauthorizedWhenCurrentApiKeyHashIsRevoked()
    {
        using var revokedFactory = sourceFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(CreateApiKeyConfiguration(
                    previousKeyHashes: [PreviousApiKeyHash],
                    revokedKeyHashes: [TestApiKeyHash]));
            });
        });

        using var revokedClient = revokedFactory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), TestApiKey);

        var response = await revokedClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        var payload = JsonDocument.Parse(responseBody).RootElement;
        AssertErrorShape(payload, "unauthorized", "Unauthorized");
        Assert.Equal("API key is revoked", payload.GetProperty("details").GetProperty("reason").GetString());
        Assert.DoesNotContain(TestApiKey, responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain(TestApiKeyHash, responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain(PreviousApiKeyHash, responseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportCarbonFactorsReturnsUnauthorizedWhenPreviousApiKeyHashIsRevoked()
    {
        using var revokedFactory = sourceFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(CreateApiKeyConfiguration(
                    previousKeyHashes: [PreviousApiKeyHash],
                    revokedKeyHashes: [PreviousApiKeyHash]));
            });
        });

        using var revokedClient = revokedFactory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), PreviousApiKey);

        var response = await revokedClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        var payload = JsonDocument.Parse(responseBody).RootElement;
        AssertErrorShape(payload, "unauthorized", "Unauthorized");
        Assert.Equal("API key is revoked", payload.GetProperty("details").GetProperty("reason").GetString());
        Assert.DoesNotContain(PreviousApiKey, responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain(PreviousApiKeyHash, responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain(TestApiKeyHash, responseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportCarbonFactorsReturnsUnauthorizedWhenPreviousApiKeyHashConfigIsInvalid()
    {
        const string InvalidConfiguredHash = "not-a-sha256-hex";

        using var invalidPreviousHashFactory = sourceFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(CreateApiKeyConfiguration(previousKeyHashes: [InvalidConfiguredHash]));
            });
        });

        using var invalidPreviousHashClient = invalidPreviousHashFactory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), TestApiKey);

        var response = await invalidPreviousHashClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        var payload = JsonDocument.Parse(responseBody).RootElement;
        AssertErrorShape(payload, "unauthorized", "Unauthorized");
        Assert.Equal("import endpoint previous API key hash is invalid", payload.GetProperty("details").GetProperty("reason").GetString());
        Assert.DoesNotContain(TestApiKey, responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain(TestApiKeyHash, responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain(InvalidConfiguredHash, responseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportCarbonFactorsReturnsUnauthorizedWhenRevokedApiKeyHashConfigIsInvalid()
    {
        const string InvalidConfiguredHash = "not-a-sha256-hex";

        using var invalidRevokedHashFactory = sourceFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(CreateApiKeyConfiguration(revokedKeyHashes: [InvalidConfiguredHash]));
            });
        });

        using var invalidRevokedHashClient = invalidRevokedHashFactory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), TestApiKey);

        var response = await invalidRevokedHashClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        var payload = JsonDocument.Parse(responseBody).RootElement;
        AssertErrorShape(payload, "unauthorized", "Unauthorized");
        Assert.Equal("revoked API key hash is invalid", payload.GetProperty("details").GetProperty("reason").GetString());
        Assert.DoesNotContain(TestApiKey, responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain(TestApiKeyHash, responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain(InvalidConfiguredHash, responseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportCarbonFactorsDoesNotRequirePlaintextKeyConfigWhenHashMatches()
    {
        using var plaintextFactory = sourceFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(CreateApiKeyConfiguration(plaintextKey: "wrong-plaintext-key"));
            });
        });

        using var plaintextClient = plaintextFactory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), TestApiKey);

        var response = await plaintextClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }


    [Fact]
    public async Task ImportCarbonFactorsReturnsUnauthorizedWhenTenantConfigIsMissing()
    {
        using var tenantlessFactory = sourceFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(CreateApiKeyConfiguration(tenantId: string.Empty));
            });
        });

        using var tenantlessClient = tenantlessFactory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), TestApiKey);

        var response = await tenantlessClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        var payload = JsonDocument.Parse(responseBody).RootElement;
        AssertErrorShape(payload, "unauthorized", "Unauthorized");
        Assert.Equal("import tenant is not configured", payload.GetProperty("details").GetProperty("reason").GetString());
        Assert.DoesNotContain(TestApiKey, responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain(TestApiKeyHash, responseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportCarbonFactorsReturnsUnauthorizedWhenImportScopesAreMissing()
    {
        using var scopelessFactory = sourceFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(CreateApiKeyConfiguration(includeScope: false));
            });
        });

        using var scopelessClient = scopelessFactory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), TestApiKey);

        var response = await scopelessClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        var payload = JsonDocument.Parse(responseBody).RootElement;
        AssertErrorShape(payload, "unauthorized", "Unauthorized");
        Assert.Equal("import endpoint scope is not configured", payload.GetProperty("details").GetProperty("reason").GetString());
        Assert.DoesNotContain(TestApiKey, responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain(TestApiKeyHash, responseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportCarbonFactorsReturnsUnauthorizedWhenImportScopesAreEmpty()
    {
        using var emptyScopeFactory = sourceFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(CreateApiKeyConfiguration(importScope: string.Empty));
            });
        });

        using var emptyScopeClient = emptyScopeFactory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), TestApiKey);

        var response = await emptyScopeClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        var payload = JsonDocument.Parse(responseBody).RootElement;
        AssertErrorShape(payload, "unauthorized", "Unauthorized");
        Assert.Equal("import endpoint scope is not configured", payload.GetProperty("details").GetProperty("reason").GetString());
        Assert.DoesNotContain(TestApiKey, responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain(TestApiKeyHash, responseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportCarbonFactorsReturnsUnauthorizedWhenImportScopeIsInsufficient()
    {
        const string WrongScope = "carbon_factors:read";

        using var wrongScopeFactory = sourceFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(CreateApiKeyConfiguration(importScope: WrongScope));
            });
        });

        using var wrongScopeClient = wrongScopeFactory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), TestApiKey);

        var response = await wrongScopeClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        var payload = JsonDocument.Parse(responseBody).RootElement;
        AssertErrorShape(payload, "unauthorized", "Unauthorized");
        Assert.Equal("API key is not permitted to import carbon factors", payload.GetProperty("details").GetProperty("reason").GetString());
        Assert.DoesNotContain(TestApiKey, responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain(TestApiKeyHash, responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain(WrongScope, responseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportCarbonFactorsReturnsAcceptedBoundaryResponseForValidRequest()
    {
        var response = await PostImportRequestAsync(CreateValidImportRequest());

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(1, payload.GetProperty("total_records").GetInt32());
        Assert.Equal(1, payload.GetProperty("accepted_records").GetInt32());
        Assert.Equal(0, payload.GetProperty("rejected_records").GetInt32());
        Assert.Equal(0, payload.GetProperty("warning_count").GetInt32());
        Assert.Equal(0, payload.GetProperty("error_count").GetInt32());
        Assert.Equal("accepted", payload.GetProperty("validation_status").GetString());
        Assert.Equal("batch-1", payload.GetProperty("audit").GetProperty("batch_id").GetString());
        Assert.Equal(TestTenantId, payload.GetProperty("audit").GetProperty("tenant_id").GetString());
        Assert.Equal("api_key", payload.GetProperty("audit").GetProperty("authentication_scheme").GetString());
        Assert.False(payload.GetProperty("has_warnings").GetBoolean());
        Assert.False(payload.GetProperty("has_errors").GetBoolean());
        Assert.False(payload.GetProperty("persisted").GetBoolean());
        Assert.Equal("not_started", payload.GetProperty("import_execution").GetString());
    }


    [Fact]
    public async Task ImportCarbonFactorsReturnsDeterministicAuditIdForEquivalentRequests()
    {
        var request = CreateValidImportRequest();

        var first = await PostImportRequestAsync(request);
        var second = await PostImportRequestAsync(request);

        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, second.StatusCode);

        var firstPayload = JsonDocument.Parse(await first.Content.ReadAsStringAsync()).RootElement;
        var secondPayload = JsonDocument.Parse(await second.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(
            firstPayload.GetProperty("audit").GetProperty("audit_id").GetString(),
            secondPayload.GetProperty("audit").GetProperty("audit_id").GetString());
    }

    [Fact]
    public async Task ImportCarbonFactorsReturnsAcceptedForMixedBatchWithValidationErrors()
    {
        var request = CreateValidImportRequest();
        request["factors"] = new object[]
        {
            CreateFactor("id-1", "electricity", "synthetic", "electricity", "grid", 1.1m, "kg", 2024),
            CreateFactor("id-2", "electricity", "synthetic", "electricity", "grid", 1.2m, " ", 2024)
        };

        var response = await PostImportRequestAsync(request);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(2, payload.GetProperty("total_records").GetInt32());
        Assert.Equal(1, payload.GetProperty("accepted_records").GetInt32());
        Assert.Equal(1, payload.GetProperty("rejected_records").GetInt32());
        Assert.Equal(0, payload.GetProperty("warning_count").GetInt32());
        Assert.Equal(1, payload.GetProperty("error_count").GetInt32());
        Assert.Equal("accepted_with_validation_errors", payload.GetProperty("status").GetString());
        Assert.Equal("accepted_with_validation_errors", payload.GetProperty("validation_status").GetString());
        Assert.False(payload.GetProperty("has_warnings").GetBoolean());
        Assert.True(payload.GetProperty("has_errors").GetBoolean());
        Assert.True(payload.GetProperty("errors").GetArrayLength() > 0);
        Assert.False(payload.GetProperty("persisted").GetBoolean());
        Assert.Equal("not_started", payload.GetProperty("import_execution").GetString());
    }

    [Fact]
    public async Task ImportCarbonFactorsReturnsInvalidQueryForEmptyFactors()
    {
        var request = CreateValidImportRequest();
        request["factors"] = Array.Empty<object>();

        var response = await PostImportRequestAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("factors must contain at least one item", payload.GetProperty("details").GetProperty("reason").GetString());
    }

    [Fact]
    public async Task ImportCarbonFactorsReturnsInvalidQueryForMissingRequiredField()
    {
        var request = CreateValidImportRequest();
        request["batch_id"] = " ";

        var response = await PostImportRequestAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("batch_id is required", payload.GetProperty("details").GetProperty("reason").GetString());
    }

    [Fact]
    public async Task ImportCarbonFactorsReturnsInvalidQueryWhenZeroValidRowsRemain()
    {
        var request = CreateValidImportRequest();
        request["factors"] = new object[]
        {
            CreateFactor("id-1", "electricity", "synthetic", "electricity", "grid", 1.1m, " ", 2024)
        };

        var response = await PostImportRequestAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("no valid factor rows remain after validation", payload.GetProperty("details").GetProperty("reason").GetString());
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


    private static Dictionary<string, string?> CreateApiKeyConfiguration(
        string? apiKeyHash = TestApiKeyHash,
        string? tenantId = TestTenantId,
        string? importScope = ImportScope,
        bool includeScope = true,
        string? plaintextKey = null,
        IReadOnlyList<string>? previousKeyHashes = null,
        IReadOnlyList<string>? revokedKeyHashes = null)
    {
        var configuration = new Dictionary<string, string?>();

        if (apiKeyHash is not null)
        {
            configuration["Security:ApiKey:ImportEndpointKeyHash"] = apiKeyHash;
        }

        if (previousKeyHashes is not null)
        {
            for (var index = 0; index < previousKeyHashes.Count; index++)
            {
                configuration[$"Security:ApiKey:ImportEndpointPreviousKeyHashes:{index}"] = previousKeyHashes[index];
            }
        }

        if (revokedKeyHashes is not null)
        {
            for (var index = 0; index < revokedKeyHashes.Count; index++)
            {
                configuration[$"Security:ApiKey:RevokedKeyHashes:{index}"] = revokedKeyHashes[index];
            }
        }

        if (plaintextKey is not null)
        {
            configuration["Security:ApiKey:ImportEndpointKey"] = plaintextKey;
        }

        if (tenantId is not null)
        {
            configuration["Security:ApiKey:ImportTenantId"] = tenantId;
        }

        if (includeScope)
        {
            configuration["Security:ApiKey:ImportEndpointScopes:0"] = importScope;
        }

        return configuration;
    }

    private Task<HttpResponseMessage> PostImportRequestAsync(object request, string? apiKey = TestApiKey)
    {
        return client.SendAsync(CreateImportRequestMessage(request, apiKey));
    }

    private static HttpRequestMessage CreateImportRequestMessage(object request, string? apiKey = TestApiKey)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/carbon-factors/import")
        {
            Content = JsonContent.Create(request)
        };

        if (apiKey is not null)
        {
            requestMessage.Headers.Add("X-Api-Key", apiKey);
        }

        return requestMessage;
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

    private static Dictionary<string, object> CreateFactor(
        string externalFactorId,
        string sourceFamily,
        string sourceProvider,
        string category,
        string activity,
        decimal factorValue,
        string factorUnit,
        int? year)
    {
        return new Dictionary<string, object>
        {
            ["external_factor_id"] = externalFactorId,
            ["source_family"] = sourceFamily,
            ["source_provider"] = sourceProvider,
            ["category"] = category,
            ["activity"] = activity,
            ["factor_value"] = factorValue,
            ["factor_unit"] = factorUnit,
            ["year"] = year!
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
