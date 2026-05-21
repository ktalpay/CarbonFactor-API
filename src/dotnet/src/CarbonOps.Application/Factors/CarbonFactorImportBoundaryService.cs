using System.Security.Cryptography;
using System.Text;
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
        if (string.IsNullOrWhiteSpace(request.Source.Publication)) return Invalid("source.publication is required");
        if (string.IsNullOrWhiteSpace(request.Source.PublicationVersion)) return Invalid("source.publication_version is required");
        if (request.ParserMetadata is not null)
        {
            if (string.IsNullOrWhiteSpace(request.ParserMetadata.ParserName)) return Invalid("parser_metadata.parser_name is required when parser_metadata is provided");
            if (string.IsNullOrWhiteSpace(request.ParserMetadata.ParserVersion)) return Invalid("parser_metadata.parser_version is required when parser_metadata is provided");
        }

        if (request.Factors is null || request.Factors.Count == 0) return Invalid("factors must contain at least one item");

        var errors = new List<CarbonFactorImportValidationMessage>();
        var warnings = new List<CarbonFactorImportValidationMessage>();
        var seenExternalIds = new HashSet<string>(StringComparer.Ordinal);
        var seenFactorIdentities = new HashSet<string>(StringComparer.Ordinal);

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

            var factorIdentity = BuildFactorIdentity(factor);
            if (!seenFactorIdentities.Add(factorIdentity))
            {
                rowErrors.Add(new CarbonFactorImportValidationMessage(index, externalId, "factor_identity", "duplicate_factor_identity", "factor identity must be unique within batch"));
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

        var orderedWarnings = warnings
            .OrderBy(w => w.RowIndex)
            .ThenBy(w => w.Field, StringComparer.Ordinal)
            .ThenBy(w => w.Code, StringComparer.Ordinal)
            .ToArray();
        var orderedErrors = errors
            .OrderBy(e => e.RowIndex)
            .ThenBy(e => e.Field, StringComparer.Ordinal)
            .ThenBy(e => e.Code, StringComparer.Ordinal)
            .ToArray();

        var rejectedRows = orderedErrors.Select(e => e.RowIndex).Distinct().Count();
        var acceptedRows = request.Factors.Count - rejectedRows;

        if (acceptedRows == 0)
        {
            return Invalid("no valid factor rows remain after validation");
        }

        var validationStatus = rejectedRows > 0
            ? "accepted_with_validation_errors"
            : orderedWarnings.Length > 0
                ? "accepted_with_warnings"
                : "accepted";

        return ApplicationResult<CarbonFactorImportBoundaryResponse>.Success(
            new CarbonFactorImportBoundaryResponse(
                request.BatchId,
                BuildAuditMetadata(request),
                acceptedRows,
                rejectedRows,
                validationStatus,
                validationStatus,
                request.Factors.Count,
                orderedWarnings.Length,
                orderedErrors.Length,
                orderedWarnings.Length > 0,
                orderedErrors.Length > 0,
                false,
                "not_started",
                orderedWarnings,
                orderedErrors));
    }

    private static CarbonFactorImportBoundaryAuditMetadata BuildAuditMetadata(ParserCarbonFactorBatchImportRequest request)
    {
        var source = request.Source;
        var parser = request.ParserMetadata;
        var auditId = BuildAuditId(request, parser?.ParserRunId);

        return new CarbonFactorImportBoundaryAuditMetadata(
            auditId,
            request.BatchId,
            request.ContractVersion,
            source.SourceSystem,
            source.SourceFamily,
            source.SourceProvider,
            source.Publication,
            source.PublicationVersion,
            parser?.ParserName,
            parser?.ParserVersion,
            parser?.ParserRunId,
            parser?.GeneratedAtUtc,
            parser?.GeneratedAtUtc,
            "unscoped",
            "none");
    }

    private static string BuildAuditId(ParserCarbonFactorBatchImportRequest request, string? parserRunId)
    {
        var canonical = string.Join("|",
            NormalizeRequiredIdentityField(request.ContractVersion),
            NormalizeRequiredIdentityField(request.BatchId),
            NormalizeRequiredIdentityField(request.Source.SourceProvider),
            NormalizeRequiredIdentityField(request.Source.SourceFamily),
            NormalizeRequiredIdentityField(request.Source.Publication),
            NormalizeRequiredIdentityField(request.Source.PublicationVersion),
            NormalizeOptionalIdentityField(parserRunId));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }


    private static string BuildFactorIdentity(ParserCarbonFactorImportItem factor)
    {
        return string.Join(
            "|",
            NormalizeRequiredIdentityField(factor.SourceProvider),
            NormalizeRequiredIdentityField(factor.SourceFamily),
            NormalizeRequiredIdentityField(factor.Category),
            NormalizeRequiredIdentityField(factor.Activity),
            NormalizeOptionalIdentityField(factor.Region),
            NormalizeOptionalIdentityField(factor.Year?.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            NormalizeOptionalIdentityField(factor.FactorVersion),
            NormalizeRequiredIdentityField(factor.FactorUnit));
    }

    private static string NormalizeRequiredIdentityField(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string NormalizeOptionalIdentityField(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
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
