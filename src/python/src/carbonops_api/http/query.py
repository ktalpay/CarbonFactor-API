"""HTTP query parsing helpers for factor routes."""

from dataclasses import replace

from fastapi import Request

from carbonops_api.errors import invalid_query_error
from carbonops_api.transport.envelope import ErrorEnvelope, ResponseEnvelope
from carbonops_api.transport.status import error_status_for_code

_SUPPORTED_QUERY_KEYS = {"category", "activity", "region", "year"}


def build_factor_query(
    *,
    category: str | None,
    activity: str | None,
    region: str | None,
    year: int | None,
) -> dict[str, str | int]:
    return {
        key: value
        for key, value in {
            "category": category,
            "activity": activity,
            "region": region,
            "year": year,
        }.items()
        if value is not None
    }


def unsupported_query_keys(request: Request) -> list[str]:
    return sorted({key for key in request.query_params.keys() if key not in _SUPPORTED_QUERY_KEYS})


def unsupported_query_envelope(unsupported_keys: list[str]) -> ResponseEnvelope:
    error = invalid_query_error("unsupported query keys")
    error = replace(error, details={"unsupported_query_keys": sorted(unsupported_keys)})
    return ResponseEnvelope(status=error_status_for_code(error.code), error=ErrorEnvelope.from_api_error(error))
