using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CarbonOps.Api.Tests;

public sealed class CarbonFactorImportSecurityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string CurrentApiKey = "test-import-api-key";
    private const string CurrentApiKeyHash = "f9ffdcc248b716dfc2dbee5492ecbabab672b7d19522859ad3b0a0fd49a86fd0";
    private const string PreviousApiKey = "previous-import-api-key";
    private const string PreviousApiKeyHash = "dcf2d8f1700d7f49fae4215241cbf24c0fb633cc3783e49e24a754891552e91d";
    private const string WrongApiKey = "wrong-import-api-key";
    private const string WrongApiKeyHash = "ff51c126165fcd11e442ff49e3a50c16b1adcdb8753bed9acd77bc7b125de163";
    private const string PlaintextDevKey = "dev-import-key-not-for-production";
    private const string ConfiguredTenantId = "tenant-dev-001";
    private const string AttackerTenantId = "tenant-attacker-999";
    private const string ImportScope = "carbon_factors:import";
    private const string WrongScope = "carbon_factors:read";
    private readonly WebApplicationFactory<Program> sourceFactory;

    public CarbonFactorImportSecurityTests(WebApplicationFactory<Program> factory)
    {
        sourceFactory = factory;
    }

    public static TheoryData<string[]?, string> RejectedApiKeyHeaderCases => new()
    {
        { null, "missing API key" },
        { [string.Empty], "missing API key" },
        { ["   "], "missing API key" },
        { [CurrentApiKey, WrongApiKey], "invalid API key" }
    };

    public static TheoryData<string?, string> InvalidCurrentHashCases => new()
    {
        { null, "import endpoint API key hash is not configured" },
        { string.Empty, "import endpoint API key hash is not configured" },
        { CurrentApiKeyHash[..63], "import endpoint API key hash is invalid" },
        { CurrentApiKeyHash.ToUpperInvariant(), "import endpoint API key hash is invalid" },
        { "zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz", "import endpoint API key hash is invalid" }
    };

    public static TheoryData<string?, string, string> InvalidPreviousHashCases => new()
    {
        { "not-a-sha256-hex", "import endpoint previous API key hash is invalid", CurrentApiKey },
        { string.Empty, "import endpoint previous API key hash is invalid", CurrentApiKey },
        { "   ", "import endpoint previous API key hash is invalid", CurrentApiKey }
    };

    public static TheoryData<string?, string, string> InvalidRevokedHashCases => new()
    {
        { "not-a-sha256-hex", "revoked API key hash is invalid", CurrentApiKey },
        { string.Empty, "revoked API key hash is invalid", CurrentApiKey },
        { "   ", "revoked API key hash is invalid", CurrentApiKey }
    };

    [Theory]
    [MemberData(nameof(RejectedApiKeyHeaderCases))]
    public async Task ImportRejectsMissingBlankWhitespaceAndDuplicateApiKeyHeaders(string[]? apiKeyHeaderValues, string expectedReason)
    {
        using var factory = CreateFactory(
            CreateApiKeyConfiguration(
                previousKeyHashes: [PreviousApiKeyHash],
                plaintextKey: PlaintextDevKey));
        using var client = factory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), apiKeyHeaderValues);

        var response = await client.SendAsync(request);

        var responseBody = await AssertUnauthorizedEnvelope(response, expectedReason);
        AssertNoSensitiveValues(responseBody, apiKeyHeaderValues ?? []);
    }

    [Theory]
    [MemberData(nameof(InvalidCurrentHashCases))]
    public async Task ImportFailsClosedForMissingBlankOrInvalidCurrentHashConfig(string? configuredHash, string expectedReason)
    {
        using var factory = CreateFactory(
            CreateApiKeyConfiguration(
                currentKeyHash: configuredHash,
                previousKeyHashes: [PreviousApiKeyHash],
                plaintextKey: PlaintextDevKey));
        using var client = factory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), [CurrentApiKey]);

        var response = await client.SendAsync(request);

        var responseBody = await AssertUnauthorizedEnvelope(response, expectedReason);
        AssertNoSensitiveValues(responseBody, configuredHash);
    }

    [Fact]
    public async Task ImportRejectsWrongApiKeyWithoutLeakingProvidedKeyOrConfiguredMaterial()
    {
        using var factory = CreateFactory(
            CreateApiKeyConfiguration(
                previousKeyHashes: [PreviousApiKeyHash],
                plaintextKey: PlaintextDevKey));
        using var client = factory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), [WrongApiKey]);

        var response = await client.SendAsync(request);

        var responseBody = await AssertUnauthorizedEnvelope(response, "invalid API key");
        AssertNoSensitiveValues(responseBody, WrongApiKey, WrongApiKeyHash);
    }

    [Fact]
    public async Task ImportAcceptsCurrentApiKeyAndPreservesResponseInvariants()
    {
        using var factory = CreateFactory(CreateApiKeyConfiguration(previousKeyHashes: [PreviousApiKeyHash]));
        using var client = factory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), [CurrentApiKey]);

        var response = await client.SendAsync(request);

        var responseBody = await AssertAcceptedImportResponseInvariants(response);
        AssertNoSensitiveValues(responseBody);
    }

    [Fact]
    public async Task ImportAcceptsPreviousApiKeyWithoutExposingRotationMetadata()
    {
        using var factory = CreateFactory(CreateApiKeyConfiguration(previousKeyHashes: [PreviousApiKeyHash]));
        using var client = factory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), [PreviousApiKey]);

        var response = await client.SendAsync(request);

        var responseBody = await AssertAcceptedImportResponseInvariants(response);
        AssertNoSensitiveValues(responseBody);
        Assert.DoesNotContain("previous", responseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rotation", responseBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ImportRejectsRevokedCurrentApiKeyBeforeCurrentAcceptance()
    {
        using var factory = CreateFactory(
            CreateApiKeyConfiguration(
                previousKeyHashes: [PreviousApiKeyHash],
                revokedKeyHashes: [CurrentApiKeyHash]));
        using var client = factory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), [CurrentApiKey]);

        var response = await client.SendAsync(request);

        var responseBody = await AssertUnauthorizedEnvelope(response, "API key is revoked");
        AssertNoSensitiveValues(responseBody);
    }

    [Fact]
    public async Task ImportRejectsRevokedPreviousApiKeyBeforePreviousAcceptance()
    {
        using var factory = CreateFactory(
            CreateApiKeyConfiguration(
                previousKeyHashes: [PreviousApiKeyHash],
                revokedKeyHashes: [PreviousApiKeyHash]));
        using var client = factory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), [PreviousApiKey]);

        var response = await client.SendAsync(request);

        var responseBody = await AssertUnauthorizedEnvelope(response, "API key is revoked");
        AssertNoSensitiveValues(responseBody);
    }

    [Theory]
    [MemberData(nameof(InvalidPreviousHashCases))]
    public async Task ImportFailsClosedForInvalidOrBlankPreviousHashConfig(
        string? configuredHash,
        string expectedReason,
        string apiKey)
    {
        using var factory = CreateFactory(
            CreateApiKeyConfiguration(
                previousKeyHashes: [configuredHash],
                plaintextKey: PlaintextDevKey));
        using var client = factory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), [apiKey]);

        var response = await client.SendAsync(request);

        var responseBody = await AssertUnauthorizedEnvelope(response, expectedReason);
        AssertNoSensitiveValues(responseBody, configuredHash);
    }

    [Theory]
    [MemberData(nameof(InvalidRevokedHashCases))]
    public async Task ImportFailsClosedForInvalidOrBlankRevokedHashConfig(
        string? configuredHash,
        string expectedReason,
        string apiKey)
    {
        using var factory = CreateFactory(
            CreateApiKeyConfiguration(
                revokedKeyHashes: [configuredHash],
                plaintextKey: PlaintextDevKey));
        using var client = factory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), [apiKey]);

        var response = await client.SendAsync(request);

        var responseBody = await AssertUnauthorizedEnvelope(response, expectedReason);
        AssertNoSensitiveValues(responseBody, configuredHash);
    }

    [Fact]
    public async Task ImportFailsClosedWhenTenantConfigIsMissing()
    {
        using var factory = CreateFactory(CreateApiKeyConfiguration(tenantId: null));
        using var client = factory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), [CurrentApiKey]);

        var response = await client.SendAsync(request);

        var responseBody = await AssertUnauthorizedEnvelope(response, "import tenant is not configured");
        AssertNoSensitiveValues(responseBody);
    }

    [Fact]
    public async Task ImportFailsClosedWhenTenantConfigIsBlank()
    {
        using var factory = CreateFactory(CreateApiKeyConfiguration(tenantId: "   "));
        using var client = factory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), [CurrentApiKey]);

        var response = await client.SendAsync(request);

        var responseBody = await AssertUnauthorizedEnvelope(response, "import tenant is not configured");
        AssertNoSensitiveValues(responseBody);
    }

    [Fact]
    public async Task ImportTenantIdComesFromConfigAndIsNotRequestControllable()
    {
        using var factory = CreateFactory(CreateApiKeyConfiguration());
        using var client = factory.CreateClient();
        var importRequest = CreateValidImportRequest();
        importRequest["tenant_id"] = AttackerTenantId;
        ((Dictionary<string, object>)importRequest["source"])["tenant_id"] = AttackerTenantId;
        using var request = CreateImportRequestMessage(
            importRequest,
            [CurrentApiKey],
            $"/carbon-factors/import?tenant_id={AttackerTenantId}");
        request.Headers.TryAddWithoutValidation("X-Tenant-Id", AttackerTenantId);

        var response = await client.SendAsync(request);

        var responseBody = await AssertAcceptedImportResponseInvariants(response);
        var payload = JsonDocument.Parse(responseBody).RootElement;
        Assert.Equal(ConfiguredTenantId, payload.GetProperty("audit").GetProperty("tenant_id").GetString());
        Assert.DoesNotContain(AttackerTenantId, responseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportFailsClosedWhenScopesAreMissing()
    {
        using var factory = CreateFactory(CreateApiKeyConfiguration(includeScopes: false));
        using var client = factory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), [CurrentApiKey]);

        var response = await client.SendAsync(request);

        var responseBody = await AssertUnauthorizedEnvelope(response, "import endpoint scope is not configured");
        AssertNoSensitiveValues(responseBody);
    }

    [Fact]
    public async Task ImportFailsClosedWhenAnyConfiguredScopeItemIsBlank()
    {
        using var factory = CreateFactory(CreateApiKeyConfiguration(scopes: [ImportScope, "   "]));
        using var client = factory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), [CurrentApiKey]);

        var response = await client.SendAsync(request);

        var responseBody = await AssertUnauthorizedEnvelope(response, "import endpoint scope is not configured");
        AssertNoSensitiveValues(responseBody, ImportScope);
    }

    [Fact]
    public async Task ImportFailsClosedWhenConfiguredScopeIsWrong()
    {
        using var factory = CreateFactory(CreateApiKeyConfiguration(scopes: [WrongScope]));
        using var client = factory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), [CurrentApiKey]);

        var response = await client.SendAsync(request);

        var responseBody = await AssertUnauthorizedEnvelope(response, "API key is not permitted to import carbon factors");
        AssertNoSensitiveValues(responseBody, WrongScope, ImportScope);
    }

    [Fact]
    public async Task ImportScopeComparisonIsCaseSensitive()
    {
        const string WrongCaseScope = "Carbon_Factors:Import";

        using var factory = CreateFactory(CreateApiKeyConfiguration(scopes: [WrongCaseScope]));
        using var client = factory.CreateClient();
        using var request = CreateImportRequestMessage(CreateValidImportRequest(), [CurrentApiKey]);

        var response = await client.SendAsync(request);

        var responseBody = await AssertUnauthorizedEnvelope(response, "API key is not permitted to import carbon factors");
        AssertNoSensitiveValues(responseBody, WrongCaseScope, ImportScope);
    }

    [Theory]
    [InlineData("/carbon-factors")]
    [InlineData("/carbon-factors/search?category=electricity")]
    [InlineData("/carbon-factors/f-001")]
    public async Task ReadEndpointsRemainPublicWithoutApiKey(string path)
    {
        using var factory = CreateFactory(CreateApiKeyConfiguration());
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private WebApplicationFactory<Program> CreateFactory(Dictionary<string, string?> configurationValues)
    {
        return sourceFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(configurationValues);
            });
        });
    }

    private static Dictionary<string, string?> CreateApiKeyConfiguration(
        string? currentKeyHash = CurrentApiKeyHash,
        string? tenantId = ConfiguredTenantId,
        IReadOnlyList<string?>? scopes = null,
        bool includeScopes = true,
        string? plaintextKey = null,
        IReadOnlyList<string?>? previousKeyHashes = null,
        IReadOnlyList<string?>? revokedKeyHashes = null)
    {
        var configuration = new Dictionary<string, string?>();

        if (currentKeyHash is not null)
        {
            configuration["Security:ApiKey:ImportEndpointKeyHash"] = currentKeyHash;
        }

        if (tenantId is not null)
        {
            configuration["Security:ApiKey:ImportTenantId"] = tenantId;
        }

        if (includeScopes)
        {
            AddConfigurationArray(configuration, "Security:ApiKey:ImportEndpointScopes", scopes ?? [ImportScope]);
        }

        if (plaintextKey is not null)
        {
            configuration["Security:ApiKey:ImportEndpointKey"] = plaintextKey;
        }

        AddConfigurationArray(configuration, "Security:ApiKey:ImportEndpointPreviousKeyHashes", previousKeyHashes);
        AddConfigurationArray(configuration, "Security:ApiKey:RevokedKeyHashes", revokedKeyHashes);

        return configuration;
    }

    private static void AddConfigurationArray(
        Dictionary<string, string?> configuration,
        string keyPrefix,
        IReadOnlyList<string?>? values)
    {
        if (values is null)
        {
            return;
        }

        for (var index = 0; index < values.Count; index++)
        {
            configuration[$"{keyPrefix}:{index}"] = values[index];
        }
    }

    private static HttpRequestMessage CreateImportRequestMessage(
        object request,
        IEnumerable<string?>? apiKeyHeaderValues,
        string path = "/carbon-factors/import")
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(request)
        };

        if (apiKeyHeaderValues is not null)
        {
            foreach (var apiKeyHeaderValue in apiKeyHeaderValues)
            {
                requestMessage.Headers.TryAddWithoutValidation("X-Api-Key", apiKeyHeaderValue);
            }
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

    private static async Task<string> AssertUnauthorizedEnvelope(HttpResponseMessage response, string expectedReason)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());

        var responseBody = await response.Content.ReadAsStringAsync();
        var payload = JsonDocument.Parse(responseBody).RootElement;

        AssertObjectPropertyNames(payload, "code", "details", "message");
        Assert.Equal("unauthorized", payload.GetProperty("code").GetString());
        Assert.Equal("Unauthorized", payload.GetProperty("message").GetString());
        Assert.Equal(expectedReason, payload.GetProperty("details").GetProperty("reason").GetString());

        AssertNoSensitiveValues(responseBody);
        return responseBody;
    }

    private static async Task<string> AssertAcceptedImportResponseInvariants(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var payload = JsonDocument.Parse(responseBody).RootElement;

        AssertObjectPropertyNames(
            payload,
            "accepted_records",
            "audit",
            "batch_id",
            "error_count",
            "errors",
            "has_errors",
            "has_warnings",
            "import_execution",
            "persisted",
            "rejected_records",
            "status",
            "total_records",
            "validation_status",
            "warning_count",
            "warnings");
        AssertObjectPropertyNames(
            payload.GetProperty("audit"),
            "audit_id",
            "authentication_scheme",
            "batch_id",
            "contract_version",
            "evaluated_at_utc",
            "generated_at_utc",
            "parser_name",
            "parser_run_id",
            "parser_version",
            "publication",
            "publication_version",
            "source_family",
            "source_provider",
            "source_system",
            "tenant_id");
        Assert.Equal(1, payload.GetProperty("total_records").GetInt32());
        Assert.Equal(1, payload.GetProperty("accepted_records").GetInt32());
        Assert.Equal(0, payload.GetProperty("rejected_records").GetInt32());
        Assert.Equal(0, payload.GetProperty("warning_count").GetInt32());
        Assert.Equal(0, payload.GetProperty("error_count").GetInt32());
        Assert.Equal("accepted", payload.GetProperty("status").GetString());
        Assert.Equal("accepted", payload.GetProperty("validation_status").GetString());
        Assert.Equal(ConfiguredTenantId, payload.GetProperty("audit").GetProperty("tenant_id").GetString());
        Assert.Equal("api_key", payload.GetProperty("audit").GetProperty("authentication_scheme").GetString());
        Assert.False(payload.GetProperty("has_warnings").GetBoolean());
        Assert.False(payload.GetProperty("has_errors").GetBoolean());
        Assert.False(payload.GetProperty("persisted").GetBoolean());
        Assert.Equal("not_started", payload.GetProperty("import_execution").GetString());

        return responseBody;
    }

    private static void AssertNoSensitiveValues(string responseBody, params string?[] additionalSensitiveValues)
    {
        var sensitiveValues = new List<string?>
        {
            CurrentApiKey,
            CurrentApiKeyHash,
            PreviousApiKey,
            PreviousApiKeyHash,
            WrongApiKey,
            WrongApiKeyHash,
            PlaintextDevKey
        };
        sensitiveValues.AddRange(additionalSensitiveValues);

        foreach (var sensitiveValue in sensitiveValues.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            Assert.DoesNotContain(sensitiveValue!, responseBody, StringComparison.Ordinal);
        }
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
