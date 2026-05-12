"""Application composition for the current in-memory CarbonOps API."""

from carbonops_api.application import get_factor_by_id as _get_factor_by_id
from carbonops_api.application import list_factors as _list_factors
from carbonops_api.application import search_factors as _search_factors
from carbonops_api.application import FactorRepository
from carbonops_api.contracts import ApiError, FactorQuery
from carbonops_api.domain import FactorDto
from carbonops_api.infrastructure import InMemoryFactorRepository


def default_factor_repository() -> FactorRepository:
    return InMemoryFactorRepository()


def list_factors(repository: FactorRepository | None = None) -> list[FactorDto]:
    return _list_factors(repository or default_factor_repository())


def get_factor_by_id(factor_id: str, repository: FactorRepository | None = None) -> FactorDto | ApiError:
    return _get_factor_by_id(repository or default_factor_repository(), factor_id)


def search_factors(
    query: FactorQuery,
    extra_filters: dict[str, str] | None = None,
    repository: FactorRepository | None = None,
) -> list[FactorDto] | ApiError:
    return _search_factors(repository or default_factor_repository(), query, extra_filters)
