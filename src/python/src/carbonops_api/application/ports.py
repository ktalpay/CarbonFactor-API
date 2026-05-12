"""Application repository ports for factor lookup behavior."""

from typing import Protocol

from carbonops_api.domain import FactorDto


class FactorRepository(Protocol):
    """Repository port for factor lookup use cases."""

    def list_factors(self) -> list[FactorDto]:
        """Return all available factors."""

    def get_factor_by_id(self, factor_id: str) -> FactorDto | None:
        """Return a factor by id when available."""
