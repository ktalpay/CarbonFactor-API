from carbonops_api.infrastructure import InMemoryFactorRepository


def test_in_memory_repository_lists_deterministic_sample_data() -> None:
    repository = InMemoryFactorRepository()

    factors = repository.list_factors()

    assert len(factors) == 3
    assert [factor.id for factor in factors] == ["f-001", "f-002", "f-003"]


def test_in_memory_repository_get_by_id_returns_expected_factor() -> None:
    repository = InMemoryFactorRepository()

    factor = repository.get_factor_by_id("f-002")

    assert factor is not None
    assert factor.id == "f-002"
    assert factor.category == "transport"


def test_in_memory_repository_get_by_id_returns_none_for_missing_factor() -> None:
    repository = InMemoryFactorRepository()

    factor = repository.get_factor_by_id("missing-factor")

    assert factor is None
