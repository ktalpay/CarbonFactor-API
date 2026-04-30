"""Data contracts for CarbonFactor API foundation."""

from dataclasses import asdict, dataclass, field


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


@dataclass(frozen=True)
class FactorQuery:
    category: str | None = None
    activity: str | None = None
    region: str | None = None
    year: int | None = None


@dataclass(frozen=True)
class ApiError:
    code: str
    message: str
    details: dict[str, str] = field(default_factory=dict)


@dataclass(frozen=True)
class FactorListResponse:
    factors: list[FactorDto]
    total: int

    def to_dict(self) -> dict:
        return {"factors": [asdict(f) for f in self.factors], "total": self.total}


@dataclass(frozen=True)
class FactorDetailResponse:
    factor: FactorDto

    def to_dict(self) -> dict:
        return {"factor": asdict(self.factor)}
