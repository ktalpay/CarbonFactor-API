using CarbonFactor.Api.Domain;

namespace CarbonFactor.Api.Tests;

public sealed class CarbonFactorDomainValidationTests
{
    private static readonly Guid FactorId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private readonly CarbonFactorValidator validator = new(new CarbonFactorNormalizer());

    [Fact]
    public void ValidFactorCreation_NormalizesInput()
    {
        var result = validator.Create(
            FactorId,
            new CarbonFactorInput(
                Name: "  Grid   electricity  ",
                Category: "Electricity",
                Unit: "kg CO2e / kWh",
                EmissionValue: 0.42m,
                Source: "  Example source  ",
                Region: "  GB  ",
                EffectiveYear: 2025));

        Assert.True(result.IsValid);
        Assert.NotNull(result.Factor);
        Assert.Equal("Grid electricity", result.Factor.Name);
        Assert.Equal("energy", result.Factor.Category);
        Assert.Equal("kg_co2e_per_kwh", result.Factor.Unit);
        Assert.Equal("Example source", result.Factor.Source);
        Assert.Equal("GB", result.Factor.Region);
    }

    [Fact]
    public void MissingName_ReturnsRequiredError()
    {
        var result = validator.Create(
            FactorId,
            ValidInput() with { Name = " " });

        var error = Assert.Single(result.Errors);
        Assert.Equal("name", error.Field);
        Assert.Equal("required", error.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("1000000000.01")]
    public void InvalidEmissionValue_ReturnsRangeOrRequiredError(string? value)
    {
        decimal? emissionValue = value is null ? null : decimal.Parse(value);

        var result = validator.Create(
            FactorId,
            ValidInput() with { EmissionValue = emissionValue });

        Assert.Contains(result.Errors, error => error.Field == "emissionValue");
    }

    [Fact]
    public void UnsupportedUnit_ReturnsUnsupportedUnitError()
    {
        var result = validator.Create(
            FactorId,
            ValidInput() with { Unit = "bananas" });

        var error = Assert.Single(result.Errors);
        Assert.Equal("unit", error.Field);
        Assert.Equal("unsupported_unit", error.Code);
    }

    [Fact]
    public void UnsupportedCategory_ReturnsUnsupportedCategoryError()
    {
        var result = validator.Create(
            FactorId,
            ValidInput() with { Category = "unknown-category" });

        var error = Assert.Single(result.Errors);
        Assert.Equal("category", error.Field);
        Assert.Equal("unsupported_category", error.Code);
    }

    [Theory]
    [InlineData("Transportation", "kg co2e per km", "transport", "kg_co2e_per_km")]
    [InlineData("Material", "tonnes CO2e", "materials", "t_co2e")]
    [InlineData("Agri", "gCO2e", "agriculture", "g_co2e")]
    public void CommonTextualVariations_NormalizeDeterministically(
        string category,
        string unit,
        string expectedCategory,
        string expectedUnit)
    {
        var result = validator.Create(
            FactorId,
            ValidInput() with { Category = category, Unit = unit });

        Assert.True(result.IsValid);
        Assert.Equal(expectedCategory, result.Factor!.Category);
        Assert.Equal(expectedUnit, result.Factor.Unit);
    }

    [Fact]
    public void ValidationErrors_AreReturnedInDeterministicOrder()
    {
        var result = validator.Create(
            Guid.Empty,
            new CarbonFactorInput(
                Name: "",
                Category: "unknown-category",
                Unit: "unknown-unit",
                EmissionValue: -1,
                EffectiveYear: 1800));

        Assert.Equal(
            ["id", "name", "category", "unit", "emissionValue", "effectiveYear"],
            result.Errors.Select(error => error.Field));
    }

    private static CarbonFactorInput ValidInput() =>
        new(
            Name: "Grid electricity",
            Category: "energy",
            Unit: "kg_co2e",
            EmissionValue: 0.42m,
            Source: "Example source",
            Region: "GB",
            EffectiveYear: 2025);
}

