using CarbonOps.Domain;

namespace CarbonOps.Infrastructure;

public static class CarbonFactorReferenceDataCatalog
{
    private static readonly IReadOnlyList<CarbonFactor> SampleCarbonFactors = CreateSampleCarbonFactors();

    public static IReadOnlyList<CarbonFactor> GetSampleCarbonFactors()
    {
        return SampleCarbonFactors;
    }

    private static IReadOnlyList<CarbonFactor> CreateSampleCarbonFactors()
    {
        return new List<CarbonFactor>
        {
            new(
                "f-001",
                "synthetic",
                "electricity",
                "grid electricity",
                0.42m,
                "kgCO2e/kWh",
                "US-WEST",
                2024,
                "grid sample"),
            new(
                "f-002",
                "synthetic",
                "transport",
                "passenger vehicle",
                0.19m,
                "kgCO2e/km",
                "US",
                2024,
                "vehicle sample"),
            new(
                "f-003",
                "synthetic",
                "electricity",
                "onsite solar",
                0.05m,
                "kgCO2e/kWh",
                "TR",
                2023,
                "low-carbon sample")
        }.AsReadOnly();
    }
}
