from carbonops_api.composition import default_factor_repository, get_factor_by_id, list_factors, search_factors
from carbonops_api.contracts import ApiError, FactorQuery
from carbonops_api.infrastructure import InMemoryFactorRepository


def test_default_factor_repository_uses_in_memory_adapter() -> None:
    repository = default_factor_repository()

    assert isinstance(repository, InMemoryFactorRepository)


def test_composed_factor_lookup_matches_current_behavior() -> None:
    factors = list_factors()
    result = get_factor_by_id("f-002")
    filtered = search_factors(FactorQuery(category="electricity"))

    assert [factor.id for factor in factors] == ["f-001", "f-002", "f-003"]
    assert not isinstance(result, ApiError)
    assert result.id == "f-002"
    assert not isinstance(filtered, ApiError)
    assert [factor.id for factor in filtered] == ["f-001", "f-003"]


def test_composed_missing_factor_returns_existing_error_contract() -> None:
    result = get_factor_by_id("missing-factor")

    assert isinstance(result, ApiError)
    assert result.code == "not_found"
