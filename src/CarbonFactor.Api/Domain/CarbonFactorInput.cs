namespace CarbonFactor.Api.Domain;

public sealed record CarbonFactorInput(
    string? Name,
    string? Category,
    string? Unit,
    decimal? EmissionValue,
    string? Source = null,
    string? Region = null,
    int? EffectiveYear = null);

