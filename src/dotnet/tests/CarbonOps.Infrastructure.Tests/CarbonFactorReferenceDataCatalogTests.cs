using CarbonOps.Domain;

namespace CarbonOps.Infrastructure.Tests;

public sealed class CarbonFactorReferenceDataCatalogTests
{
    [Fact]
    public void GetSampleCarbonFactorsReturnsDeterministicStableRecords()
    {
        var first = CarbonFactorReferenceDataCatalog.GetSampleCarbonFactors();
        var second = CarbonFactorReferenceDataCatalog.GetSampleCarbonFactors();

        Assert.Same(first, second);
        Assert.Equal(["f-001", "f-002", "f-003"], first.Select(factor => factor.Id));
    }

    [Fact]
    public void GetSampleCarbonFactorsContainsUniqueIdsAndRequiredValues()
    {
        var factors = CarbonFactorReferenceDataCatalog.GetSampleCarbonFactors();

        Assert.Equal(factors.Count, factors.Select(factor => factor.Id).Distinct(StringComparer.Ordinal).Count());

        foreach (CarbonFactor factor in factors)
        {
            Assert.False(string.IsNullOrWhiteSpace(factor.Id));
            Assert.False(string.IsNullOrWhiteSpace(factor.Source));
            Assert.False(string.IsNullOrWhiteSpace(factor.Category));
            Assert.False(string.IsNullOrWhiteSpace(factor.Activity));
            Assert.False(string.IsNullOrWhiteSpace(factor.FactorUnit));
        }
    }
}
