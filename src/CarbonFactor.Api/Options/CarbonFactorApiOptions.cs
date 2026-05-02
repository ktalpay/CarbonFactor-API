namespace CarbonFactor.Api.Options;

public sealed class CarbonFactorApiOptions
{
    public const string SectionName = "CarbonFactor";

    public int DefaultPageSize { get; init; } = 25;

    public int MaxPageSize { get; init; } = 100;

    public bool EnableSwaggerInProduction { get; init; }
}

