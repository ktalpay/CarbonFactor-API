"""Compatibility facade for factor application services."""

from carbonops_api.composition import get_factor_by_id as _get_factor_by_id
from carbonops_api.composition import list_factors as _list_factors
from carbonops_api.composition import search_factors as _search_factors
from carbonops_api.contracts import ApiError, FactorQuery
from carbonops_api.domain import FactorDto


def list_factors() -> list[FactorDto]:
    return _list_factors()


def get_factor_by_id(factor_id: str) -> FactorDto | ApiError:
    return _get_factor_by_id(factor_id)


def search_factors(query: FactorQuery, extra_filters: dict[str, str] | None = None) -> list[FactorDto] | ApiError:
    return _search_factors(query, extra_filters)


__all__ = ["get_factor_by_id", "list_factors", "search_factors"]
