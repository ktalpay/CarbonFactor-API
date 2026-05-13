using System.Text.Json;
using CarbonOps.Contracts;

namespace CarbonOps.Contracts.Tests;

public sealed class FactorContractTests
{
    [Fact]
    public void FactorDtoSerializesWithPythonParityFieldNames()
    {
        var factor = CreateFactor();

        var payload = JsonSerializer.SerializeToElement(factor);

        Assert.Equal("elec-tr-2024", payload.GetProperty("id").GetString());
        Assert.Equal("synthetic", payload.GetProperty("source").GetString());
        Assert.Equal("electricity", payload.GetProperty("category").GetString());
        Assert.Equal("grid electricity", payload.GetProperty("activity").GetString());
        Assert.Equal(0.42m, payload.GetProperty("factor_value").GetDecimal());
        Assert.Equal("kgCO2e/kWh", payload.GetProperty("factor_unit").GetString());
        Assert.Equal("TR", payload.GetProperty("region").GetString());
        Assert.Equal(2024, payload.GetProperty("year").GetInt32());
        Assert.Equal("baseline test factor", payload.GetProperty("notes").GetString());
    }

    [Fact]
    public void FactorQueryUsesSupportedFilterFields()
    {
        var query = new FactorQuery(
            Category: "electricity",
            Activity: "grid electricity",
            Region: "TR",
            Year: 2024);

        var payload = JsonSerializer.SerializeToElement(query);

        Assert.Equal("electricity", payload.GetProperty("category").GetString());
        Assert.Equal("grid electricity", payload.GetProperty("activity").GetString());
        Assert.Equal("TR", payload.GetProperty("region").GetString());
        Assert.Equal(2024, payload.GetProperty("year").GetInt32());
    }

    [Fact]
    public void FactorListResponseCarriesFactorsAndTotal()
    {
        var factor = CreateFactor();
        var response = new FactorListResponse([factor], 1);

        var payload = JsonSerializer.SerializeToElement(response);

        Assert.Equal(1, payload.GetProperty("total").GetInt32());
        Assert.Equal("elec-tr-2024", payload.GetProperty("factors")[0].GetProperty("id").GetString());
    }

    [Fact]
    public void FactorDetailResponseCarriesFactor()
    {
        var factor = CreateFactor();
        var response = new FactorDetailResponse(factor);

        var payload = JsonSerializer.SerializeToElement(response);

        Assert.Equal("elec-tr-2024", payload.GetProperty("factor").GetProperty("id").GetString());
    }

    private static FactorDto CreateFactor()
    {
        return new FactorDto(
            "elec-tr-2024",
            "synthetic",
            "electricity",
            "grid electricity",
            0.42m,
            "kgCO2e/kWh",
            "TR",
            2024,
            "baseline test factor");
    }
}
