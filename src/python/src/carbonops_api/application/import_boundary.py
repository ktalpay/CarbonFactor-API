"""Application boundary behavior for parser-output carbon factor imports."""

from carbonops_api.contracts import (
    ApiError,
    CarbonFactorImportBoundaryResponse,
    ParserCarbonFactorBatchImportRequest,
    invalid_query_error,
)

_SUPPORTED_CONTRACT_VERSION = "1.0"


def validate_import_boundary(
    request: ParserCarbonFactorBatchImportRequest,
) -> CarbonFactorImportBoundaryResponse | ApiError:
    if not request.contract_version.strip():
        return invalid_query_error("contract_version is required")
    if request.contract_version != _SUPPORTED_CONTRACT_VERSION:
        return invalid_query_error(f"unsupported contract_version: {request.contract_version}")
    if not request.batch_id.strip():
        return invalid_query_error("batch_id is required")
    if not request.source.source_family.strip() or not request.source.source_provider.strip():
        return invalid_query_error("source metadata is required")
    if not request.factors:
        return invalid_query_error("factors must be non-empty")

    warnings: list[str] = []
    required_missing = 0
    accepted = 0
    for idx, factor in enumerate(request.factors):
        missing = _missing_factor_fields(factor)
        if missing:
            required_missing += 1
            continue
        if (
            factor.source_family != request.source.source_family
            or factor.source_provider != request.source.source_provider
        ):
            warnings.append(
                f"factors[{idx}] source metadata does not match batch source"
            )
        accepted += 1

    if accepted == 0:
        return invalid_query_error("no valid factors found in batch")

    return CarbonFactorImportBoundaryResponse(
        batch_id=request.batch_id,
        accepted_records=accepted,
        rejected_records=required_missing,
        status="accepted_for_validation_boundary",
        persisted=False,
        import_execution="not_started",
        warnings=warnings,
        errors=[],
    )


def _missing_factor_fields(factor) -> list[str]:
    missing: list[str] = []
    if not factor.external_factor_id.strip():
        missing.append("external_factor_id")
    if not factor.source_family.strip():
        missing.append("source_family")
    if not factor.source_provider.strip():
        missing.append("source_provider")
    if not factor.category.strip():
        missing.append("category")
    if not factor.activity.strip():
        missing.append("activity")
    if factor.factor_value is None:
        missing.append("factor_value")
    if not factor.factor_unit.strip():
        missing.append("factor_unit")
    return missing
