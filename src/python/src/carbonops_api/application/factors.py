"""Application services for factor lookup behavior."""

from carbonops_api.application.ports import FactorRepository
from carbonops_api.contracts import ApiError, FactorQuery, invalid_query_error, not_found_error
from carbonops_api.domain import FactorDto

_ALLOWED_FILTERS = {"category", "activity", "region", "year"}


def list_factors(repository: FactorRepository) -> list[FactorDto]:
    return sorted(repository.list_factors(), key=lambda item: item.id)


def get_factor_by_id(repository: FactorRepository, factor_id: str) -> FactorDto | ApiError:
    factor = repository.get_factor_by_id(factor_id)
    if factor is not None:
        return factor
    return not_found_error("factor", factor_id)


def search_factors(
    repository: FactorRepository,
    query: FactorQuery,
    extra_filters: dict[str, str] | None = None,
) -> list[FactorDto] | ApiError:
    if query.year is not None and query.year <= 0:
        return invalid_query_error("year must be positive")

    if extra_filters:
        unsupported = sorted(set(extra_filters.keys()) - _ALLOWED_FILTERS)
        if unsupported:
            return invalid_query_error(f"unsupported filters: {', '.join(unsupported)}")

    results = list_factors(repository)
    if query.category:
        results = [item for item in results if item.category == query.category]
    if query.activity:
        results = [item for item in results if item.activity == query.activity]
    if query.region:
        results = [item for item in results if item.region == query.region]
    if query.year:
        results = [item for item in results if item.year == query.year]
    return results
