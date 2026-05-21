using CarbonOps.Contracts;

namespace CarbonOps.Application.Factors;

public sealed class CarbonFactorImportBoundaryService
{
    private const int MinSupportedYear = 1900;
    private const int MaxSupportedYear = 2100;

    private static readonly HashSet<string> SupportedContractVersions = new(StringComparer.Ordinal)
    {
        "1.0"
    };

    public ApplicationResult<CarbonFactorImportBoundaryResponse> ValidateAndAccept(
        ParserCarbonFactorBatchImportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ContractVersion)) return Invalid("contract_version is required");
        if (!SupportedContractVersions.Contains(request.ContractVersion)) return Invalid($"unsupported contract_version: {request.ContractVersion}");
        if (string.IsNullOrWhiteSpace(request.BatchId)) return Invalid("batch_id is required");
        if (request.Source is null) return Invalid("source is required");
        if (string.IsNullOrWhiteSpace(request.Source.SourceFamily)) return Invalid("source.source_family is required");
        if (string.IsNullOrWhiteSpace(request.Source.SourceProvider)) return Invalid("source.source_provider is required");
        if (request.Factors is null || request.Factors.Count == 0) return Invalid("factors must contain at least one item");

        var errors = new List<CarbonFactorImportValidationMessage>();
        var warnings = new List<CarbonFactorImportValidationMessage>();
        var seenExternalIds = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < request.Factors.Count; index++)
        {
            var factor = request.Factors[index];
            var rowErrors = new List<CarbonFactorImportValidationMessage>();
            var externalId = string.IsNullOrWhiteSpace(factor.ExternalFactorId) ? null : factor.ExternalFactorId;

            AddRequiredFieldError(rowErrors, index, externalId, "external_factor_id", factor.ExternalFactorId);
            AddRequiredFieldError(rowErrors, index, externalId, "source_family", factor.SourceFamily);
            AddRequiredFieldError(rowErrors, index, externalId, "source_provider", factor.SourceProvider);
            AddRequiredFieldError(rowErrors, index, externalId, "category", factor.Category);
            AddRequiredFieldError(rowErrors, index, externalId, "activity", factor.Activity);
            AddRequiredFieldError(rowErrors, index, externalId, "factor_unit", factor.FactorUnit);

            if (factor.FactorValue < 0)
            {
                rowErrors.Add(new CarbonFactorImportValidationMessage(index, externalId, "factor_value", "out_of_range", "factor_value must be greater than or equal to 0"));
            }

            if (factor.Year is < MinSupportedYear or > MaxSupportedYear)
            {
                rowErrors.Add(new CarbonFactorImportValidationMessage(index, externalId, "year", "out_of_range", $"year must be between {MinSupportedYear} and {MaxSupportedYear}"));
            }

            if (!string.IsNullOrWhiteSpace(externalId) && !seenExternalIds.Add(externalId))
            {
                rowErrors.Add(new CarbonFactorImportValidationMessage(index, externalId, "external_factor_id", "duplicate", "external_factor_id must be unique within batch"));
            }

            if (!string.IsNullOrWhiteSpace(factor.SourceFamily)
                && !string.IsNullOrWhiteSpace(factor.SourceProvider)
                && (!string.Equals(request.Source.SourceFamily, factor.SourceFamily, StringComparison.Ordinal)
                || !string.Equals(request.Source.SourceProvider, factor.SourceProvider, StringComparison.Ordinal)))
            {
                warnings.Add(new CarbonFactorImportValidationMessage(index, externalId, "source", "source_mismatch", "factor source_family/source_provider differs from batch source metadata"));
            }

            errors.AddRange(rowErrors);
        }

        var rejectedRows = errors.Select(e => e.RowIndex).Distinct().Count();
        var acceptedRows = request.Factors.Count - rejectedRows;

        if (acceptedRows == 0)
        {
            return Invalid("no valid factor rows remain after validation");
        }

        return ApplicationResult<CarbonFactorImportBoundaryResponse>.Success(
            new CarbonFactorImportBoundaryResponse(
                request.BatchId,
                acceptedRows,
                rejectedRows,
                rejectedRows == 0 ? "accepted_boundary_only" : "accepted_with_validation_errors",
                false,
                "not_started",
                warnings,
                errors));
    }

    private static void AddRequiredFieldError(ICollection<CarbonFactorImportValidationMessage> errors, int rowIndex, string? externalId, string field, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) return;

        errors.Add(new CarbonFactorImportValidationMessage(rowIndex, externalId, field, "required", $"{field} is required"));
    }

    private static ApplicationResult<CarbonFactorImportBoundaryResponse> Invalid(string reason)
    {
        return ApplicationResult<CarbonFactorImportBoundaryResponse>.Failure(ApiError.InvalidQuery(reason));
    }
}
