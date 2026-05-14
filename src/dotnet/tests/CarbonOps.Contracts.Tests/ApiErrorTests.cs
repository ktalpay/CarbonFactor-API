using System.Text.Json;
using CarbonOps.Contracts;

namespace CarbonOps.Contracts.Tests;

public sealed class ApiErrorTests
{
    [Fact]
    public void NotFoundFactoryMatchesDeterministicContractShape()
    {
        var error = ApiError.NotFound("factor", "f-999");

        Assert.Equal("not_found", error.Code);
        Assert.Equal("factor not found", error.Message);
        Assert.Equal("f-999", error.Details["id"]);
    }

    [Fact]
    public void InvalidQueryFactoryMatchesDeterministicContractShape()
    {
        var error = ApiError.InvalidQuery("year must be positive");

        Assert.Equal("invalid_query", error.Code);
        Assert.Equal("Invalid query", error.Message);
        Assert.Equal("year must be positive", error.Details["reason"]);
    }

    [Fact]
    public void ApiErrorSerializesWithExpectedEnvelopeFields()
    {
        var error = ApiError.InvalidQuery("unsupported query keys");

        var payload = JsonSerializer.SerializeToElement(error);

        AssertObjectPropertyNames(payload, "code", "details", "message");
        AssertObjectPropertyNames(payload.GetProperty("details"), "reason");
        Assert.Equal("invalid_query", payload.GetProperty("code").GetString());
        Assert.Equal("Invalid query", payload.GetProperty("message").GetString());
        Assert.Equal("unsupported query keys", payload.GetProperty("details").GetProperty("reason").GetString());
    }

    [Fact]
    public void NotFoundErrorSerializesWithIdentifierDetailsField()
    {
        var error = ApiError.NotFound("factor", "f-999");

        var payload = JsonSerializer.SerializeToElement(error);

        AssertObjectPropertyNames(payload, "code", "details", "message");
        AssertObjectPropertyNames(payload.GetProperty("details"), "id");
        Assert.Equal("f-999", payload.GetProperty("details").GetProperty("id").GetString());
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
}
