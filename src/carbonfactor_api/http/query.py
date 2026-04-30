"""HTTP query parsing helpers for factor routes."""

from fastapi import Request

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
