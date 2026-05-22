using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CarbonOps.Api.Tests;

public sealed class EnvironmentConfigHardeningTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ValidProductionKeyHash = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string ValidPreviousKeyHash = "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";
    private const string ValidRevokedKeyHash = "dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd";
    private const string DevelopmentKeyHash = "30a4c897d0e50f229980eade456279df21f20e222337457f88f5d530a3e82be4";
    private const string PlaintextApiKey = "plaintext-production-key-should-not-leak";
    private const string ValidTenantId = "tenant-prod-001";
    private const string DevelopmentTenantId = "tenant-dev-001";
    private const string ImportScope = "carbon_factors:import";
    private const string WrongScope = "carbon_factors:read";
    private const string ConnectionString = "Host=db.example.internal;Username=carbonops;Password=should-not-leak";
    private readonly WebApplicationFactory<Program> sourceFactory;

    public EnvironmentConfigHardeningTests(WebApplicationFactory<Program> factory)
    {
        sourceFactory = factory;
    }

    [Fact]
    public void ProductionFailsStartupWhenImportEndpointKeyHashIsMissing()
    {
        var configuration = CreateValidProductionConfiguration(currentKeyHash: null);

        var exception = AssertProductionStartupFailure(configuration);

        Assert.Contains("Security:ApiKey:ImportEndpointKeyHash is required", FlattenExceptionMessages(exception), StringComparison.Ordinal);
        AssertFailureDoesNotLeakSecrets(exception);
    }

    [Fact]
    public void ProductionFailsStartupWhenImportEndpointKeyHashIsInvalid()
    {
        var configuration = CreateValidProductionConfiguration(currentKeyHash: "invalid-hash");

        var exception = AssertProductionStartupFailure(configuration);

        Assert.Contains("Security:ApiKey:ImportEndpointKeyHash must be lowercase SHA-256 hex", FlattenExceptionMessages(exception), StringComparison.Ordinal);
        AssertFailureDoesNotLeakSecrets(exception);
    }

    [Fact]
    public void ProductionFailsStartupWhenImportEndpointKeyHashUsesDevelopmentPlaceholder()
    {
        var configuration = CreateValidProductionConfiguration(currentKeyHash: DevelopmentKeyHash);

        var exception = AssertProductionStartupFailure(configuration);

        Assert.Contains("Security:ApiKey:ImportEndpointKeyHash must not use the development placeholder hash", FlattenExceptionMessages(exception), StringComparison.Ordinal);
        AssertFailureDoesNotLeakSecrets(exception);
    }

    [Fact]
    public void ProductionFailsStartupWhenPlaintextImportEndpointKeyIsConfigured()
    {
        var configuration = CreateValidProductionConfiguration(plaintextKey: PlaintextApiKey);

        var exception = AssertProductionStartupFailure(configuration);

        Assert.Contains("Security:ApiKey:ImportEndpointKey must not be configured in Production", FlattenExceptionMessages(exception), StringComparison.Ordinal);
        AssertFailureDoesNotLeakSecrets(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(DevelopmentTenantId)]
    public void ProductionFailsStartupWhenImportTenantIdIsMissingBlankOrDevelopmentPlaceholder(string? tenantId)
    {
        var configuration = CreateValidProductionConfiguration(tenantId: tenantId);

        var exception = AssertProductionStartupFailure(configuration);
        var message = FlattenExceptionMessages(exception);

        Assert.Contains("Security:ApiKey:ImportTenantId", message, StringComparison.Ordinal);
        AssertFailureDoesNotLeakSecrets(exception);
    }

    [Fact]
    public void ProductionFailsStartupWhenImportEndpointScopesAreMissing()
    {
        var configuration = CreateValidProductionConfiguration(includeScopes: false);

        var exception = AssertProductionStartupFailure(configuration);

        Assert.Contains("Security:ApiKey:ImportEndpointScopes is required", FlattenExceptionMessages(exception), StringComparison.Ordinal);
        AssertFailureDoesNotLeakSecrets(exception);
    }

    [Fact]
    public void ProductionFailsStartupWhenImportEndpointScopesDoNotIncludeRequiredScope()
    {
        var configuration = CreateValidProductionConfiguration(scopes: [WrongScope]);

        var exception = AssertProductionStartupFailure(configuration);

        Assert.Contains("Security:ApiKey:ImportEndpointScopes must include the required import scope", FlattenExceptionMessages(exception), StringComparison.Ordinal);
        AssertFailureDoesNotLeakSecrets(exception);
    }

    [Fact]
    public void ProductionFailsStartupWhenImportEndpointScopesContainBlankValue()
    {
        var configuration = CreateValidProductionConfiguration(scopes: [ImportScope, "   "]);

        var exception = AssertProductionStartupFailure(configuration);

        Assert.Contains("Security:ApiKey:ImportEndpointScopes must not contain blank values", FlattenExceptionMessages(exception), StringComparison.Ordinal);
        AssertFailureDoesNotLeakSecrets(exception);
    }

    [Theory]
    [InlineData("Security:ApiKey:ImportEndpointPreviousKeyHashes:0", "Security:ApiKey:ImportEndpointPreviousKeyHashes entries must be lowercase SHA-256 hex")]
    [InlineData("Security:ApiKey:RevokedKeyHashes:0", "Security:ApiKey:RevokedKeyHashes entries must be lowercase SHA-256 hex")]
    public void ProductionFailsStartupWhenPreviousOrRevokedHashContainsInvalidEntry(
        string configurationKey,
        string expectedMessage)
    {
        var configuration = CreateValidProductionConfiguration();
        configuration[configurationKey] = "not-a-sha256-hash";

        var exception = AssertProductionStartupFailure(configuration);

        Assert.Contains(expectedMessage, FlattenExceptionMessages(exception), StringComparison.Ordinal);
        AssertFailureDoesNotLeakSecrets(exception);
    }

    [Theory]
    [InlineData("RateLimiting:Import:PermitLimit", "0", "RateLimiting:Import:PermitLimit must be greater than zero")]
    [InlineData("RateLimiting:Import:WindowSeconds", "0", "RateLimiting:Import:WindowSeconds must be greater than zero")]
    [InlineData("RateLimiting:Import:QueueLimit", "-1", "RateLimiting:Import:QueueLimit must be zero or greater")]
    [InlineData("RateLimiting:Read:PermitLimit", "0", "RateLimiting:Read:PermitLimit must be greater than zero")]
    [InlineData("RateLimiting:Read:WindowSeconds", "0", "RateLimiting:Read:WindowSeconds must be greater than zero")]
    [InlineData("RateLimiting:Read:QueueLimit", "-1", "RateLimiting:Read:QueueLimit must be zero or greater")]
    public void ProductionFailsStartupWhenRateLimitingConfigurationIsInvalid(
        string configurationKey,
        string configuredValue,
        string expectedMessage)
    {
        var configuration = CreateValidProductionConfiguration();
        configuration[configurationKey] = configuredValue;

        var exception = AssertProductionStartupFailure(configuration);

        Assert.Contains(expectedMessage, FlattenExceptionMessages(exception), StringComparison.Ordinal);
        AssertFailureDoesNotLeakSecrets(exception);
    }

    [Fact]
    public async Task ProductionStartsSuccessfullyWithValidSafeConfiguration()
    {
        using var factory = CreateFactory("Production", CreateValidProductionConfiguration());
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TestingCanStartWithDevelopmentPlaceholders()
    {
        using var factory = CreateFactory(
            "Testing",
            CreateValidProductionConfiguration(
                currentKeyHash: DevelopmentKeyHash,
                tenantId: DevelopmentTenantId,
                plaintextKey: PlaintextApiKey));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private Exception AssertProductionStartupFailure(Dictionary<string, string?> configuration)
    {
        using var factory = CreateFactory("Production", configuration);
        return Assert.ThrowsAny<Exception>(() => factory.CreateClient());
    }

    private WebApplicationFactory<Program> CreateFactory(
        string environmentName,
        Dictionary<string, string?> configurationValues)
    {
        return sourceFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environmentName);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(configurationValues);
            });
        });
    }

    private static Dictionary<string, string?> CreateValidProductionConfiguration(
        string? currentKeyHash = ValidProductionKeyHash,
        string? tenantId = ValidTenantId,
        IReadOnlyList<string?>? scopes = null,
        bool includeScopes = true,
        string? plaintextKey = null)
    {
        var configuration = new Dictionary<string, string?>
        {
            ["Security:ApiKey:ImportEndpointPreviousKeyHashes:0"] = ValidPreviousKeyHash,
            ["Security:ApiKey:RevokedKeyHashes:0"] = ValidRevokedKeyHash,
            ["Persistence:PostgreSql:ConnectionString"] = ConnectionString,
            ["RateLimiting:Import:PermitLimit"] = "10",
            ["RateLimiting:Import:WindowSeconds"] = "60",
            ["RateLimiting:Import:QueueLimit"] = "0",
            ["RateLimiting:Read:PermitLimit"] = "60",
            ["RateLimiting:Read:WindowSeconds"] = "60",
            ["RateLimiting:Read:QueueLimit"] = "0"
        };

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

        return configuration;
    }

    private static void AddConfigurationArray(
        Dictionary<string, string?> configuration,
        string keyPrefix,
        IReadOnlyList<string?> values)
    {
        for (var index = 0; index < values.Count; index++)
        {
            configuration[$"{keyPrefix}:{index}"] = values[index];
        }
    }

    private static string FlattenExceptionMessages(Exception exception)
    {
        var messages = new List<string>();
        for (var currentException = exception; currentException is not null; currentException = currentException.InnerException)
        {
            messages.Add(currentException.Message);
        }

        return string.Join(Environment.NewLine, messages);
    }

    private static void AssertFailureDoesNotLeakSecrets(Exception exception)
    {
        var message = FlattenExceptionMessages(exception);

        Assert.DoesNotContain(ValidProductionKeyHash, message, StringComparison.Ordinal);
        Assert.DoesNotContain(ValidPreviousKeyHash, message, StringComparison.Ordinal);
        Assert.DoesNotContain(ValidRevokedKeyHash, message, StringComparison.Ordinal);
        Assert.DoesNotContain(DevelopmentKeyHash, message, StringComparison.Ordinal);
        Assert.DoesNotContain(PlaintextApiKey, message, StringComparison.Ordinal);
        Assert.DoesNotContain(ValidTenantId, message, StringComparison.Ordinal);
        Assert.DoesNotContain(DevelopmentTenantId, message, StringComparison.Ordinal);
        Assert.DoesNotContain(ImportScope, message, StringComparison.Ordinal);
        Assert.DoesNotContain(WrongScope, message, StringComparison.Ordinal);
        Assert.DoesNotContain(ConnectionString, message, StringComparison.Ordinal);
    }
}
