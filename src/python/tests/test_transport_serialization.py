from carbonops_api.contracts import ApiError, FactorDetailResponse, FactorDto, FactorListResponse
from carbonops_api.transport.serialization import (
    serialize_detail_response,
    serialize_error,
    serialize_factor,
    serialize_list_response,
)


def _factor() -> FactorDto:
    return FactorDto(
        id="f-1",
        source="demo",
        category="transport",
        activity="car",
        factor_value=0.2,
        factor_unit="kgCO2e/km",
        region="US",
        year=2025,
        notes="test",
    )


def test_factor_dto_serialization() -> None:
    payload = serialize_factor(_factor())
    assert payload["id"] == "f-1"
    assert payload["factor_value"] == 0.2


def test_list_response_serialization() -> None:
    response = FactorListResponse(factors=[_factor()], total=1)
    assert serialize_list_response(response) == {"factors": [serialize_factor(_factor())], "total": 1}


def test_detail_response_serialization() -> None:
    response = FactorDetailResponse(factor=_factor())
    assert serialize_detail_response(response) == {"factor": serialize_factor(_factor())}


def test_error_serialization() -> None:
    payload = serialize_error(ApiError(code="invalid_query", message="Invalid query", details={"b": "2", "a": "1"}))
    assert payload == {"code": "invalid_query", "message": "Invalid query", "details": {"a": "1", "b": "2"}}


def test_deterministic_output_keys_values() -> None:
    payload = serialize_list_response(FactorListResponse(factors=[_factor()], total=1))
    assert list(payload.keys()) == ["factors", "total"]
    assert list(payload["factors"][0].keys()) == [
        "id",
        "source",
        "category",
        "activity",
        "factor_value",
        "factor_unit",
        "region",
        "year",
        "notes",
    ]
