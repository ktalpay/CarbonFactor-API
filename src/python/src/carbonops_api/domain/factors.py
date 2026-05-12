"""Domain entities for carbon factor behavior."""

from dataclasses import dataclass


@dataclass(frozen=True)
class FactorDto:
    id: str
    source: str
    category: str
    activity: str
    factor_value: float
    factor_unit: str
    region: str | None = None
    year: int | None = None
    notes: str | None = None
