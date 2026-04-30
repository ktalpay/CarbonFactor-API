"""In-memory factor catalog service."""

from carbonfactor_api.contracts import ApiError, FactorDto, FactorQuery
from carbonfactor_api.errors import invalid_query_error, not_found_error
from carbonfactor_api.sample_data import SAMPLE_FACTORS

_ALLOWED_FILTERS = {"category", "activity", "region", "year"}


def list_factors() -> list[FactorDto]:
    return sorted(SAMPLE_FACTORS, key=lambda item: item.id)


def get_factor_by_id(factor_id: str) -> FactorDto | ApiError:
    for factor in SAMPLE_FACTORS:
        if factor.id == factor_id:
            return factor
    return not_found_error("factor", factor_id)


def search_factors(query: FactorQuery, extra_filters: dict[str, str] | None = None) -> list[FactorDto] | ApiError:
    if query.year is not None and query.year <= 0:
        return invalid_query_error("year must be positive")

    if extra_filters:
        unsupported = sorted(set(extra_filters.keys()) - _ALLOWED_FILTERS)
        if unsupported:
            return invalid_query_error(f"unsupported filters: {', '.join(unsupported)}")

    results = list_factors()
    if query.category:
        results = [item for item in results if item.category == query.category]
    if query.activity:
        results = [item for item in results if item.activity == query.activity]
    if query.region:
        results = [item for item in results if item.region == query.region]
    if query.year:
        results = [item for item in results if item.year == query.year]
    return results
