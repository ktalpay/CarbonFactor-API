"""Local transport handlers that emulate route behavior without a server."""

from carbonops_api.application import get_factor_by_id, search_factors
from carbonops_api.contracts import ApiError, FactorDetailResponse, FactorListResponse, FactorQuery
from carbonops_api.transport.envelope import ErrorEnvelope, ResponseEnvelope
from carbonops_api.transport.serialization import (
    serialize_detail_response,
    serialize_list_response,
)
from carbonops_api.transport.status import HTTP_OK, error_status_for_code


_ALLOWED_FILTERS = {"category", "activity", "region", "year"}


def handle_list_factors(query: dict) -> ResponseEnvelope:
    extra_filters = {k: v for k, v in query.items() if k not in _ALLOWED_FILTERS}

    year_value = query.get("year")
    if year_value is not None and not isinstance(year_value, int):
        return _error_response(ApiError(code="invalid_query", message="Invalid query", details={"reason": "year must be an integer"}))

    factor_query = FactorQuery(
        category=query.get("category"),
        activity=query.get("activity"),
        region=query.get("region"),
        year=year_value,
    )
    result = search_factors(query=factor_query, extra_filters=extra_filters)
    if isinstance(result, ApiError):
        return _error_response(result)

    response = FactorListResponse(factors=result, total=len(result))
    return ResponseEnvelope(status=HTTP_OK, data=serialize_list_response(response))


def handle_get_factor(factor_id: str) -> ResponseEnvelope:
    result = get_factor_by_id(factor_id)
    if isinstance(result, ApiError):
        return _error_response(result)

    response = FactorDetailResponse(factor=result)
    return ResponseEnvelope(status=HTTP_OK, data=serialize_detail_response(response))


def _error_response(error: ApiError) -> ResponseEnvelope:
    return ResponseEnvelope(status=error_status_for_code(error.code), error=ErrorEnvelope.from_api_error(error))
