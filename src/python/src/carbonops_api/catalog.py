"""Compatibility facade for factor application services."""

from carbonops_api.application.factors import get_factor_by_id as _get_factor_by_id
from carbonops_api.application.factors import list_factors as _list_factors
from carbonops_api.application.factors import search_factors as _search_factors
from carbonops_api.contracts import ApiError, FactorQuery
from carbonops_api.domain import FactorDto
from carbonops_api.infrastructure import InMemoryFactorRepository


def list_factors() -> list[FactorDto]:
    return _list_factors(InMemoryFactorRepository())


def get_factor_by_id(factor_id: str) -> FactorDto | ApiError:
    return _get_factor_by_id(InMemoryFactorRepository(), factor_id)


def search_factors(query: FactorQuery, extra_filters: dict[str, str] | None = None) -> list[FactorDto] | ApiError:
    return _search_factors(InMemoryFactorRepository(), query, extra_filters)


__all__ = ["get_factor_by_id", "list_factors", "search_factors"]
