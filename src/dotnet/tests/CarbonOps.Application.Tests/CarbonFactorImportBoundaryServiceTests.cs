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
        Assert.Equal("accepted", result.Value.Status);
        Assert.Equal("accepted", result.Value.ValidationStatus);
        Assert.Equal(1, result.Value.TotalRecords);
        Assert.Equal(0, result.Value.WarningCount);
        Assert.Equal(0, result.Value.ErrorCount);
        Assert.False(result.Value.HasWarnings);
        Assert.False(result.Value.HasErrors);
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
    public void ValidateAndAcceptRequiresSourcePublication()
    {
        var request = CreateRequest() with { Source = CreateRequest().Source with { Publication = " " } };
        var result = service.ValidateAndAccept(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("source.publication is required", result.Error!.Details["reason"]);
    }

    [Fact]
    public void ValidateAndAcceptRequiresSourcePublicationVersion()
    {
        var request = CreateRequest() with { Source = CreateRequest().Source with { PublicationVersion = " " } };
        var result = service.ValidateAndAccept(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("source.publication_version is required", result.Error!.Details["reason"]);
    }

    [Fact]
    public void ValidateAndAcceptValidatesParserMetadataWhenPresent()
    {
        var request = CreateRequest() with
        {
            ParserMetadata = new ParserProvenanceMetadataDto(" ", "1.0")
        };
        var result = service.ValidateAndAccept(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("parser_metadata.parser_name is required when parser_metadata is provided", result.Error!.Details["reason"]);
    }

    [Fact]
    public void ValidateAndAcceptRejectsDuplicateFactorIdentityForLaterRow()
    {
        var result = service.ValidateAndAccept(CreateRequest() with { Factors = [CreateFactor("id-1"), CreateFactor("id-2")] });

        Assert.True(result.IsSuccess);
        var duplicate = Assert.Single(result.Value!.Errors.Where(e => e.Code == "duplicate_factor_identity"));
        Assert.Equal(1, duplicate.RowIndex);
    }

    [Fact]
    public void ValidateAndAcceptAllowsDifferentFactorVersionInIdentity()
    {
        var first = CreateFactor("id-1") with { FactorVersion = "1.0" };
        var second = CreateFactor("id-2") with { FactorVersion = "2.0" };
        var result = service.ValidateAndAccept(CreateRequest() with { Factors = [first, second] });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.AcceptedRecords);
        Assert.Equal(0, result.Value.RejectedRecords);
    }

    [Fact]
    public void ValidateAndAcceptNormalizesBlankNullRegionYearAndFactorVersionInIdentity()
    {
        var first = CreateFactor("id-1") with { Region = null, Year = null, FactorVersion = null };
        var second = CreateFactor("id-2") with { Region = " ", Year = null, FactorVersion = " " };
        var result = service.ValidateAndAccept(CreateRequest() with { Factors = [first, second] });

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value!.Errors, e => e.Code == "duplicate_factor_identity" && e.RowIndex == 1);
    }

    [Fact]
    public void ValidateAndAcceptAddsWarningForSourceMismatch()
    {
        var mismatch = CreateFactor("id-1") with { SourceFamily = "transport" };
        var result = service.ValidateAndAccept(CreateRequest() with { Factors = [mismatch, CreateFactor("id-2")] });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Warnings);
        Assert.Equal("accepted_with_warnings", result.Value.Status);
        Assert.Equal(result.Value.Status, result.Value.ValidationStatus);
        Assert.Equal(2, result.Value.AcceptedRecords);
        Assert.Equal(0, result.Value.RejectedRecords);
        Assert.Equal(1, result.Value.WarningCount);
        Assert.Equal(0, result.Value.ErrorCount);
        Assert.True(result.Value.HasWarnings);
        Assert.False(result.Value.HasErrors);
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
        Assert.Equal(result.Value.Status, result.Value.ValidationStatus);
        Assert.Equal(2, result.Value.TotalRecords);
        Assert.Equal(0, result.Value.WarningCount);
        Assert.Equal(1, result.Value.ErrorCount);
        Assert.False(result.Value.HasWarnings);
        Assert.True(result.Value.HasErrors);
    }


    [Fact]
    public void ValidateAndAcceptReturnsDeterministicMessageOrdering()
    {
        var invalid0 = CreateFactor("dup") with { FactorUnit = " ", ExternalFactorId = " " };
        var valid = CreateFactor("ok-1") with { SourceFamily = "transport" };
        var invalid2 = CreateFactor("dup") with { FactorUnit = " " };

        var result = service.ValidateAndAccept(CreateRequest() with { Factors = [invalid0, valid, invalid2] });

        Assert.True(result.IsSuccess);
        Assert.Equal([0, 2, 2, 2], result.Value!.Errors.Select(e => e.RowIndex));
        Assert.Equal(["external_factor_id", "external_factor_id", "factor_identity", "factor_unit"], result.Value.Errors.Select(e => e.Field));
        Assert.Equal([1], result.Value.Warnings.Select(w => w.RowIndex));
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
