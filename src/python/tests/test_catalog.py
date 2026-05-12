from carbonops_api.catalog import get_factor_by_id, list_factors, search_factors
from carbonops_api.contracts import ApiError, FactorQuery


def test_list_all_factors() -> None:
    factors = list_factors()
    assert len(factors) == 3


def test_get_existing_factor() -> None:
    factor = get_factor_by_id("f-001")
    assert not isinstance(factor, ApiError)
    assert factor.id == "f-001"


def test_get_missing_factor_returns_error() -> None:
    result = get_factor_by_id("f-999")
    assert isinstance(result, ApiError)
    assert result.code == "not_found"


def test_filter_by_category() -> None:
    result = search_factors(FactorQuery(category="electricity"))
    assert not isinstance(result, ApiError)
    assert len(result) == 2


def test_filter_by_region_and_year() -> None:
    result = search_factors(FactorQuery(region="US-WEST", year=2024))
    assert not isinstance(result, ApiError)
    assert len(result) == 1
    assert result[0].id == "f-003"


def test_invalid_filter_behavior() -> None:
    result = search_factors(FactorQuery(), extra_filters={"source": "synthetic"})
    assert isinstance(result, ApiError)
    assert result.code == "invalid_query"


def test_deterministic_ordering() -> None:
    ids = [item.id for item in list_factors()]
    assert ids == sorted(ids)
