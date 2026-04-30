from carbonfactor_api.contracts import FactorDetailResponse, FactorDto, FactorListResponse, FactorQuery


def test_dto_creation() -> None:
    dto = FactorDto(
        id="f-1",
        source="synthetic",
        category="electricity",
        activity="grid",
        factor_value=0.45,
        factor_unit="kgCO2e/kWh",
    )
    assert dto.id == "f-1"


def test_query_defaults() -> None:
    query = FactorQuery()
    assert query.category is None
    assert query.year is None


def test_response_serialization() -> None:
    dto = FactorDto(
        id="f-1",
        source="synthetic",
        category="electricity",
        activity="grid",
        factor_value=0.45,
        factor_unit="kgCO2e/kWh",
    )
    list_response = FactorListResponse(factors=[dto], total=1).to_dict()
    detail_response = FactorDetailResponse(factor=dto).to_dict()
    assert list_response["total"] == 1
    assert detail_response["factor"]["id"] == "f-1"
