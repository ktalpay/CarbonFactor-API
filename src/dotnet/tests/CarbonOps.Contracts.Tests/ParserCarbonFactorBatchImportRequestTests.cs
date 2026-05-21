using System.Text.Json;
using CarbonOps.Contracts;

namespace CarbonOps.Contracts.Tests;

public sealed class ParserCarbonFactorBatchImportRequestTests
{
    [Fact]
    public void SerializesWithDeterministicPythonFriendlyFieldNames()
    {
        var request = CreateRequest();

        var payload = JsonSerializer.SerializeToElement(request);

        AssertObjectPropertyNames(payload, "batch_id", "contract_version", "factors", "parser_metadata", "source");
        Assert.Equal("1.0", payload.GetProperty("contract_version").GetString());
        Assert.Equal("batch-2026-05-21-0001", payload.GetProperty("batch_id").GetString());

        var source = payload.GetProperty("source");
        AssertObjectPropertyNames(source, "publication", "publication_version", "published_at_utc", "source_family", "source_provider", "source_system");

        var item = payload.GetProperty("factors")[0];
        AssertObjectPropertyNames(
            item,
            "activity",
            "category",
            "external_factor_id",
            "factor_unit",
            "factor_value",
            "factor_version",
            "notes",
            "provenance",
            "region",
            "source_family",
            "source_provider",
            "year");
    }

    [Fact]
    public void CarriesRequiredBatchAndSourceMetadata()
    {
        var request = CreateRequest();

        Assert.Equal("1.0", request.ContractVersion);
        Assert.Equal("batch-2026-05-21-0001", request.BatchId);
        Assert.Equal("carbonops-parser", request.Source.SourceSystem);
        Assert.Equal("electricity", request.Source.SourceFamily);
        Assert.Equal("synthetic", request.Source.SourceProvider);
        Assert.Equal("v2026.05", request.Source.PublicationVersion);
    }

    [Fact]
    public void SupportsMultipleFactorRecords()
    {
        var request = CreateRequest() with
        {
            Factors =
            [
                CreateFactor("elec-tr-2024"),
                CreateFactor("elec-de-2024") with { Region = "DE", FactorValue = 0.32m }
            ]
        };

        var payload = JsonSerializer.SerializeToElement(request);

        Assert.Equal(2, payload.GetProperty("factors").GetArrayLength());
        Assert.Equal("elec-tr-2024", payload.GetProperty("factors")[0].GetProperty("external_factor_id").GetString());
        Assert.Equal("elec-de-2024", payload.GetProperty("factors")[1].GetProperty("external_factor_id").GetString());
    }

    [Fact]
    public void OptionalFieldsSerializeAsPredictableNulls()
    {
        var request = CreateRequest() with
        {
            ParserMetadata = null,
            Factors = [CreateFactor("elec-tr-2024") with { Region = null, Year = null, FactorVersion = null, Notes = null, Provenance = null }]
        };

        var payload = JsonSerializer.SerializeToElement(request);
        var factor = payload.GetProperty("factors")[0];

        Assert.Equal(JsonValueKind.Null, payload.GetProperty("parser_metadata").ValueKind);
        Assert.Equal(JsonValueKind.Null, factor.GetProperty("region").ValueKind);
        Assert.Equal(JsonValueKind.Null, factor.GetProperty("year").ValueKind);
        Assert.Equal(JsonValueKind.Null, factor.GetProperty("factor_version").ValueKind);
        Assert.Equal(JsonValueKind.Null, factor.GetProperty("notes").ValueKind);
        Assert.Equal(JsonValueKind.Null, factor.GetProperty("provenance").ValueKind);
    }

    private static ParserCarbonFactorBatchImportRequest CreateRequest()
    {
        return new ParserCarbonFactorBatchImportRequest(
            "1.0",
            "batch-2026-05-21-0001",
            new ParserSourceMetadataDto(
                "carbonops-parser",
                "electricity",
                "synthetic",
                "Synthetic Electricity Dataset",
                "v2026.05",
                DateTimeOffset.Parse("2026-05-21T00:00:00Z")),
            [CreateFactor("elec-tr-2024")],
            new ParserProvenanceMetadataDto(
                "CarbonOps.Parser",
                "0.1.0",
                "run-0001",
                DateTimeOffset.Parse("2026-05-21T09:30:00Z")));
    }

    private static ParserCarbonFactorImportItem CreateFactor(string externalFactorId)
    {
        return new ParserCarbonFactorImportItem(
            externalFactorId,
            "electricity",
            "synthetic",
            "electricity",
            "grid electricity",
            0.42m,
            "kgCO2e/kWh",
            "TR",
            2024,
            "1.0",
            "baseline test factor",
            "parser-derived");
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
