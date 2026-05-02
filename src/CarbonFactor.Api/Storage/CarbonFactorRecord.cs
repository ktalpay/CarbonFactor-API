using DomainCarbonFactor = CarbonFactor.Api.Domain.CarbonFactor;

namespace CarbonFactor.Api.Storage;

public sealed record CarbonFactorRecord(
    Guid Id,
    string Name,
    string Category,
    string Unit,
    decimal EmissionValue,
    string? Source,
    string? Region,
    int? EffectiveYear)
{
    public static CarbonFactorRecord FromDomain(DomainCarbonFactor factor) =>
        new(
            factor.Id,
            factor.Name,
            factor.Category,
            factor.Unit,
            factor.EmissionValue,
            factor.Source,
            factor.Region,
            factor.EffectiveYear);
}
