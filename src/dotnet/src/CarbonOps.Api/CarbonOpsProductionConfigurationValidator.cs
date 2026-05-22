using Microsoft.Extensions.Options;

namespace CarbonOps.Api;

internal sealed class CarbonOpsProductionConfigurationValidator
{
    internal const string DevelopmentImportEndpointKeyHash =
        "30a4c897d0e50f229980eade456279df21f20e222337457f88f5d530a3e82be4";

    private const string RequiredImportScope = "carbon_factors:import";
    private const string DevelopmentTenantId = "tenant-dev-001";
    private const string TestTenantId = "tenant-test-001";
    private const string PlaintextImportEndpointKey =
        $"{ApiKeyAuthenticationOptions.SectionName}:ImportEndpointKey";

    private readonly IConfiguration configuration;
    private readonly ApiKeyAuthenticationOptions apiKeyOptions;
    private readonly CarbonOpsRateLimitingOptions rateLimitingOptions;

    public CarbonOpsProductionConfigurationValidator(
        IConfiguration configuration,
        IOptions<ApiKeyAuthenticationOptions> apiKeyOptions,
        IOptions<CarbonOpsRateLimitingOptions> rateLimitingOptions)
    {
        this.configuration = configuration;
        this.apiKeyOptions = apiKeyOptions.Value;
        this.rateLimitingOptions = rateLimitingOptions.Value;
    }

    public void Validate()
    {
        var failures = new List<string>();

        ValidateApiKeyConfiguration(failures);
        ValidateRateLimitingConfiguration(failures);

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "Production configuration validation failed: "
                + string.Join("; ", failures.OrderBy(failure => failure, StringComparer.Ordinal)));
        }
    }

    private void ValidateApiKeyConfiguration(ICollection<string> failures)
    {
        var configuredHash = apiKeyOptions.ImportEndpointKeyHash?.Trim();
        if (string.IsNullOrWhiteSpace(configuredHash))
        {
            failures.Add($"{ApiKeyAuthenticationOptions.SectionName}:ImportEndpointKeyHash is required");
        }
        else if (!ApiKeyHashVerifier.IsValidSha256HexHash(configuredHash))
        {
            failures.Add($"{ApiKeyAuthenticationOptions.SectionName}:ImportEndpointKeyHash must be lowercase SHA-256 hex");
        }
        else if (string.Equals(configuredHash, DevelopmentImportEndpointKeyHash, StringComparison.Ordinal))
        {
            failures.Add($"{ApiKeyAuthenticationOptions.SectionName}:ImportEndpointKeyHash must not use the development placeholder hash");
        }

        if (configuration[PlaintextImportEndpointKey] is not null)
        {
            failures.Add($"{PlaintextImportEndpointKey} must not be configured in Production");
        }

        var tenantId = apiKeyOptions.ImportTenantId?.Trim();
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            failures.Add($"{ApiKeyAuthenticationOptions.SectionName}:ImportTenantId is required");
        }
        else if (IsDevelopmentTenantPlaceholder(tenantId))
        {
            failures.Add($"{ApiKeyAuthenticationOptions.SectionName}:ImportTenantId must not use a development or test placeholder");
        }

        var configuredScopes = apiKeyOptions.ImportEndpointScopes ?? [];
        if (configuredScopes.Length == 0)
        {
            failures.Add($"{ApiKeyAuthenticationOptions.SectionName}:ImportEndpointScopes is required");
        }
        else
        {
            if (configuredScopes.Any(scope => string.IsNullOrWhiteSpace(scope)))
            {
                failures.Add($"{ApiKeyAuthenticationOptions.SectionName}:ImportEndpointScopes must not contain blank values");
            }

            var normalizedScopes = configuredScopes.Select(scope => scope?.Trim()).ToArray();
            if (!normalizedScopes.Contains(RequiredImportScope, StringComparer.Ordinal))
            {
                failures.Add($"{ApiKeyAuthenticationOptions.SectionName}:ImportEndpointScopes must include the required import scope");
            }
        }

        ValidateHashArray(
            apiKeyOptions.ImportEndpointPreviousKeyHashes,
            $"{ApiKeyAuthenticationOptions.SectionName}:ImportEndpointPreviousKeyHashes",
            failures);
        ValidateHashArray(
            apiKeyOptions.RevokedKeyHashes,
            $"{ApiKeyAuthenticationOptions.SectionName}:RevokedKeyHashes",
            failures);
    }

    private static bool IsDevelopmentTenantPlaceholder(string tenantId)
    {
        return string.Equals(tenantId, DevelopmentTenantId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(tenantId, TestTenantId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(tenantId, "dev", StringComparison.OrdinalIgnoreCase)
            || string.Equals(tenantId, "test", StringComparison.OrdinalIgnoreCase)
            || string.Equals(tenantId, "development", StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateHashArray(
        IEnumerable<string?>? configuredHashes,
        string configurationKey,
        ICollection<string> failures)
    {
        foreach (var configuredHash in configuredHashes ?? [])
        {
            if (!ApiKeyHashVerifier.IsValidSha256HexHash(configuredHash?.Trim()))
            {
                failures.Add($"{configurationKey} entries must be lowercase SHA-256 hex");
                return;
            }
        }
    }

    private void ValidateRateLimitingConfiguration(ICollection<string> failures)
    {
        ValidateRateLimitingPolicy(
            rateLimitingOptions.Import,
            $"{CarbonOpsRateLimitingOptions.SectionName}:Import",
            failures);
        ValidateRateLimitingPolicy(
            rateLimitingOptions.Read,
            $"{CarbonOpsRateLimitingOptions.SectionName}:Read",
            failures);
    }

    private static void ValidateRateLimitingPolicy(
        CarbonOpsRateLimitingPolicyOptions policyOptions,
        string configurationKey,
        ICollection<string> failures)
    {
        if (policyOptions.PermitLimit <= 0)
        {
            failures.Add($"{configurationKey}:PermitLimit must be greater than zero");
        }

        if (policyOptions.WindowSeconds <= 0)
        {
            failures.Add($"{configurationKey}:WindowSeconds must be greater than zero");
        }

        if (policyOptions.QueueLimit < 0)
        {
            failures.Add($"{configurationKey}:QueueLimit must be zero or greater");
        }
    }
}
