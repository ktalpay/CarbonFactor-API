using CarbonFactor.Api.Contracts;

namespace CarbonFactor.Api.Tests.Support;

public static class CarbonFactorTestRequests
{
    public static CarbonFactorCreateRequest Valid(
        string name = "Grid electricity",
        string category = "energy",
        string unit = "kg_co2e",
        decimal emissionValue = 0.42m,
        string source = "test-source",
        string region = "GB",
        int effectiveYear = 2025) =>
        new(
            Name: name,
            Category: category,
            Unit: unit,
            EmissionValue: emissionValue,
            Source: source,
            Region: region,
            EffectiveYear: effectiveYear);
}

