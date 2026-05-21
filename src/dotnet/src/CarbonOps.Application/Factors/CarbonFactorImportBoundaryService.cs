using CarbonOps.Contracts;

namespace CarbonOps.Application.Factors;

public sealed class CarbonFactorImportBoundaryService
{
    private static readonly HashSet<string> SupportedContractVersions = new(StringComparer.Ordinal)
    {
        "1.0"
    };

    public ApplicationResult<CarbonFactorImportBoundaryResponse> ValidateAndAccept(
        ParserCarbonFactorBatchImportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ContractVersion))
        {
            return Invalid("contract_version is required");
        }

        if (!SupportedContractVersions.Contains(request.ContractVersion))
        {
            return Invalid($"unsupported contract_version: {request.ContractVersion}");
        }

        if (string.IsNullOrWhiteSpace(request.BatchId))
        {
            return Invalid("batch_id is required");
        }

        if (request.Source is null)
        {
            return Invalid("source is required");
        }

        if (request.Factors is null || request.Factors.Count == 0)
        {
            return Invalid("factors must contain at least one item");
        }

        var warnings = new List<string>();

        for (var index = 0; index < request.Factors.Count; index++)
        {
            var factor = request.Factors[index];
            var prefix = $"factors[{index}]";

            if (string.IsNullOrWhiteSpace(factor.ExternalFactorId)) return Invalid($"{prefix}.external_factor_id is required");
            if (string.IsNullOrWhiteSpace(factor.SourceFamily)) return Invalid($"{prefix}.source_family is required");
            if (string.IsNullOrWhiteSpace(factor.SourceProvider)) return Invalid($"{prefix}.source_provider is required");
            if (string.IsNullOrWhiteSpace(factor.Category)) return Invalid($"{prefix}.category is required");
            if (string.IsNullOrWhiteSpace(factor.Activity)) return Invalid($"{prefix}.activity is required");
            if (string.IsNullOrWhiteSpace(factor.FactorUnit)) return Invalid($"{prefix}.factor_unit is required");

            if (!string.Equals(request.Source.SourceFamily, factor.SourceFamily, StringComparison.Ordinal)
                || !string.Equals(request.Source.SourceProvider, factor.SourceProvider, StringComparison.Ordinal))
            {
                warnings.Add($"{prefix} source_family/source_provider differs from batch source metadata");
            }
        }

        return ApplicationResult<CarbonFactorImportBoundaryResponse>.Success(
            new CarbonFactorImportBoundaryResponse(
                request.BatchId,
                request.Factors.Count,
                0,
                "accepted_boundary_only",
                false,
                "not_started",
                warnings,
                []));
    }

    private static ApplicationResult<CarbonFactorImportBoundaryResponse> Invalid(string reason)
    {
        return ApplicationResult<CarbonFactorImportBoundaryResponse>.Failure(ApiError.InvalidQuery(reason));
    }
}
