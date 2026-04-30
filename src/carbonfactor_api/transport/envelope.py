"""Deterministic transport response envelopes."""

from dataclasses import dataclass

from carbonfactor_api.contracts import ApiError


@dataclass(frozen=True)
class ErrorEnvelope:
    code: str
    message: str
    details: dict[str, str]

    @classmethod
    def from_api_error(cls, error: ApiError) -> "ErrorEnvelope":
        return cls(code=error.code, message=error.message, details=dict(error.details))

    def to_dict(self) -> dict:
        return {
            "code": self.code,
            "message": self.message,
            "details": dict(sorted(self.details.items())),
        }


@dataclass(frozen=True)
class ResponseEnvelope:
    status: int
    data: dict | None = None
    error: ErrorEnvelope | None = None

    def to_dict(self) -> dict:
        payload = {"status": self.status, "data": self.data}
        if self.error is None:
            payload["error"] = None
        else:
            payload["error"] = self.error.to_dict()
        return payload
