"""Serialization helpers for transport-safe payloads."""

from dataclasses import asdict

from carbonfactor_api.contracts import ApiError, FactorDetailResponse, FactorDto, FactorListResponse


def serialize_factor(factor: FactorDto) -> dict:
    return asdict(factor)


def serialize_list_response(response: FactorListResponse) -> dict:
    return {
        "factors": [serialize_factor(factor) for factor in response.factors],
        "total": response.total,
    }


def serialize_detail_response(response: FactorDetailResponse) -> dict:
    return {"factor": serialize_factor(response.factor)}


def serialize_error(error: ApiError) -> dict:
    return {
        "code": error.code,
        "message": error.message,
        "details": dict(sorted(error.details.items())),
    }
