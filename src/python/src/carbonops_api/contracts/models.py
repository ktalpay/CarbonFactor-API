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


@dataclass(frozen=True)
class ParserSourceMetadataDto:
    source_family: str
    source_provider: str


@dataclass(frozen=True)
class ParserCarbonFactorImportItem:
    external_factor_id: str
    source_family: str
    source_provider: str
    category: str
    activity: str
    factor_value: float
    factor_unit: str


@dataclass(frozen=True)
class ParserCarbonFactorBatchImportRequest:
    contract_version: str
    batch_id: str
    source: ParserSourceMetadataDto
    factors: list[ParserCarbonFactorImportItem]


@dataclass(frozen=True)
class CarbonFactorImportBoundaryResponse:
    batch_id: str
    accepted_records: int
    rejected_records: int
    status: str
    persisted: bool
    import_execution: str
    warnings: list[str] = field(default_factory=list)
    errors: list[str] = field(default_factory=list)

    def to_dict(self) -> dict:
        return asdict(self)
