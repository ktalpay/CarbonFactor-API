"""Deterministic error helpers for contract behavior."""

from carbonfactor_api.contracts import ApiError


def not_found_error(entity: str, identifier: str) -> ApiError:
    return ApiError(
        code="not_found",
        message=f"{entity} not found",
        details={"id": identifier},
    )


def invalid_query_error(reason: str) -> ApiError:
    return ApiError(code="invalid_query", message="Invalid query", details={"reason": reason})


def unsupported_filter_error(filter_name: str) -> ApiError:
    return ApiError(
        code="unsupported_filter",
        message="Unsupported filter",
        details={"filter": filter_name},
    )
