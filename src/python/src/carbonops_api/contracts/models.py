"""Contract DTOs and response models."""

from dataclasses import asdict, dataclass, field

from carbonops_api.domain import FactorDto


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
    details: dict[str, object] = field(default_factory=dict)


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
