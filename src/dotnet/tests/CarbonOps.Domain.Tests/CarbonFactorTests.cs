using CarbonOps.Domain;

namespace CarbonOps.Domain.Tests;

public sealed class CarbonFactorTests
{
    [Fact]
    public void CreatesCarbonFactorWithRequiredAndOptionalFields()
    {
        var factor = new CarbonFactor(
            "elec-tr-2024",
            "synthetic",
            "electricity",
            "grid electricity",
            0.42m,
            "kgCO2e/kWh",
            "TR",
            2024,
            "baseline test factor");

        Assert.Equal("elec-tr-2024", factor.Id);
        Assert.Equal("synthetic", factor.Source);
        Assert.Equal("electricity", factor.Category);
        Assert.Equal("grid electricity", factor.Activity);
        Assert.Equal(0.42m, factor.FactorValue);
        Assert.Equal("kgCO2e/kWh", factor.FactorUnit);
        Assert.Equal("TR", factor.Region);
        Assert.Equal(2024, factor.Year);
        Assert.Equal("baseline test factor", factor.Notes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsBlankRequiredTextFields(string invalidText)
    {
        var exception = Assert.Throws<ArgumentException>(() => new CarbonFactor(
            invalidText,
            "synthetic",
            "electricity",
            "grid electricity",
            0.42m,
            "kgCO2e/kWh"));

        Assert.Equal("id", exception.ParamName);
    }
}
