using CarbonOps.Application.Factors;
using CarbonOps.Contracts;

namespace CarbonOps.Application.Tests;

public sealed class CarbonFactorImportBoundaryServiceTests
{
    private readonly CarbonFactorImportBoundaryService service = new();

    [Fact]
    public void ValidateAndAcceptReturnsAcceptedBoundaryResponseForValidRequest()
    {
        var result = service.ValidateAndAccept(CreateRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal("batch-2026-05-21-0001", result.Value!.BatchId);
        Assert.False(result.Value.Persisted);
        Assert.Equal("not_started", result.Value.ImportExecution);
    }

    [Fact]
    public void ValidateAndAcceptReturnsInvalidQueryWhenFactorsIsEmpty()
    {
        var result = service.ValidateAndAccept(CreateRequest() with { Factors = [] });

        Assert.False(result.IsSuccess);
        Assert.Equal("factors must contain at least one item", result.Error!.Details["reason"]);
    }

    [Fact]
    public void ValidateAndAcceptReturnsInvalidQueryWhenRequiredFieldIsMissing()
    {
        var invalid = CreateFactor("id-1") with { ExternalFactorId = " " };
        var result = service.ValidateAndAccept(CreateRequest() with { Factors = [invalid] });

        Assert.False(result.IsSuccess);
        Assert.Equal("factors[0].external_factor_id is required", result.Error!.Details["reason"]);
    }

    [Fact]
    public void ValidateAndAcceptAddsWarningForSourceMismatch()
    {
        var mismatch = CreateFactor("id-1") with { SourceFamily = "transport" };
        var result = service.ValidateAndAccept(CreateRequest() with { Factors = [mismatch] });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Warnings);
    }

    private static ParserCarbonFactorBatchImportRequest CreateRequest()
    {
        return new ParserCarbonFactorBatchImportRequest(
            "1.0",
            "batch-2026-05-21-0001",
            new ParserSourceMetadataDto("parser", "electricity", "synthetic", "pub", "v1"),
            [CreateFactor("id-1")]);
    }

    private static ParserCarbonFactorImportItem CreateFactor(string id)
    {
        return new ParserCarbonFactorImportItem(id, "electricity", "synthetic", "electricity", "grid", 1.2m, "kg");
    }
}
