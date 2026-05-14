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

        AssertObjectPropertyNames(
            payload,
            "activity",
            "category",
            "factor_unit",
            "factor_value",
            "id",
            "notes",
            "region",
            "source",
            "year");
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

        AssertObjectPropertyNames(payload, "activity", "category", "region", "year");
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

        AssertObjectPropertyNames(payload, "factors", "total");
        Assert.Equal(1, payload.GetProperty("total").GetInt32());
        Assert.Equal("elec-tr-2024", payload.GetProperty("factors")[0].GetProperty("id").GetString());
    }

    [Fact]
    public void FactorDetailResponseCarriesFactor()
    {
        var factor = CreateFactor();
        var response = new FactorDetailResponse(factor);

        var payload = JsonSerializer.SerializeToElement(response);

        AssertObjectPropertyNames(payload, "factor");
        Assert.Equal("elec-tr-2024", payload.GetProperty("factor").GetProperty("id").GetString());
    }

    private static void AssertObjectPropertyNames(JsonElement payload, params string[] expectedPropertyNames)
    {
        var actualPropertyNames = payload
            .EnumerateObject()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            expectedPropertyNames.OrderBy(name => name, StringComparer.Ordinal).ToArray(),
            actualPropertyNames);
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
