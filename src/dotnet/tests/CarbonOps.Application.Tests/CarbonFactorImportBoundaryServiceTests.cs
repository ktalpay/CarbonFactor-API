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
        Assert.Equal(1, result.Value!.AcceptedRecords);
        Assert.Equal(0, result.Value.RejectedRecords);
        Assert.False(result.Value.Persisted);
        Assert.Equal("not_started", result.Value.ImportExecution);
    }

    [Fact]
    public void ValidateAndAcceptReturnsInvalidQueryWhenMissingRequiredFactorField()
    {
        var invalid = CreateFactor("id-1") with { ExternalFactorId = " " };
        var result = service.ValidateAndAccept(CreateRequest() with { Factors = [invalid] });

        Assert.False(result.IsSuccess);
        Assert.Equal("no valid factor rows remain after validation", result.Error!.Details["reason"]);
    }

    [Fact]
    public void ValidateAndAcceptRejectsNegativeFactorValue()
    {
        var invalid = CreateFactor("id-1") with { FactorValue = -1m };
        var result = service.ValidateAndAccept(CreateRequest() with { Factors = [invalid] });

        Assert.False(result.IsSuccess);
        Assert.Equal("no valid factor rows remain after validation", result.Error!.Details["reason"]);
    }

    [Fact]
    public void ValidateAndAcceptRejectsOutOfRangeYear()
    {
        var invalid = CreateFactor("id-1") with { Year = 1800 };
        var result = service.ValidateAndAccept(CreateRequest() with { Factors = [invalid] });

        Assert.False(result.IsSuccess);
        Assert.Equal("no valid factor rows remain after validation", result.Error!.Details["reason"]);
    }

    [Fact]
    public void ValidateAndAcceptRejectsDuplicateExternalFactorId()
    {
        var result = service.ValidateAndAccept(CreateRequest() with { Factors = [CreateFactor("dup"), CreateFactor("dup")] });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.AcceptedRecords);
        Assert.Equal(1, result.Value.RejectedRecords);
        Assert.Contains(result.Value.Errors, e => e.Code == "duplicate");
    }

    [Fact]
    public void ValidateAndAcceptAddsWarningForSourceMismatch()
    {
        var mismatch = CreateFactor("id-1") with { SourceFamily = "transport" };
        var result = service.ValidateAndAccept(CreateRequest() with { Factors = [mismatch, CreateFactor("id-2")] });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Warnings);
        Assert.Equal("source_mismatch", result.Value.Warnings[0].Code);
    }

    [Fact]
    public void ValidateAndAcceptReturnsAcceptedForMixedValidAndInvalidRows()
    {
        var result = service.ValidateAndAccept(CreateRequest() with
        {
            Factors = [CreateFactor("id-1"), CreateFactor("id-2") with { FactorUnit = " " }]
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.AcceptedRecords);
        Assert.Equal(1, result.Value.RejectedRecords);
        Assert.Equal("accepted_with_validation_errors", result.Value.Status);
    }

    [Fact]
    public void ValidateAndAcceptReturnsInvalidQueryWhenZeroValidRowsRemain()
    {
        var result = service.ValidateAndAccept(CreateRequest() with
        {
            Factors = [CreateFactor("id-1") with { FactorUnit = " " }]
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("no valid factor rows remain after validation", result.Error!.Details["reason"]);
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
        return new ParserCarbonFactorImportItem(id, "electricity", "synthetic", "electricity", "grid", 1.2m, "kg", Year: 2024);
    }
}
