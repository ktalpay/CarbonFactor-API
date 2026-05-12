"""In-memory repository adapters for factor lookup behavior."""

from carbonops_api.application import FactorRepository
from carbonops_api.domain import FactorDto
from carbonops_api.infrastructure.sample_data import SAMPLE_FACTORS


class InMemoryFactorRepository(FactorRepository):
    """Repository adapter backed by synthetic in-memory factor data."""

    def __init__(self, factors: tuple[FactorDto, ...] | list[FactorDto] | None = None) -> None:
        self._factors = tuple(SAMPLE_FACTORS if factors is None else factors)

    def list_factors(self) -> list[FactorDto]:
        return list(self._factors)

    def get_factor_by_id(self, factor_id: str) -> FactorDto | None:
        for factor in self._factors:
            if factor.id == factor_id:
                return factor
        return None
