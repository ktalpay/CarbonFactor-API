namespace CarbonOps.Api;

internal sealed class ApiKeyAuthenticationOptions
{
    public const string SectionName = "Security:ApiKey";
    public const string HeaderName = "X-Api-Key";

    public string? ImportEndpointKeyHash { get; init; }

    public string? ImportTenantId { get; init; }

    public string[] ImportEndpointScopes { get; init; } = [];
}
