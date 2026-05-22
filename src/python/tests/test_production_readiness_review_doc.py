from pathlib import Path


def test_production_readiness_review_doc_exists_and_states_verdicts() -> None:
    doc = _production_readiness_review_text()

    assert "Controlled/internal pilot | GO with constraints" in doc
    assert "Broad production exposure | NO-GO" in doc
    assert "REL-002" in doc


def test_production_readiness_review_doc_mentions_required_blockers() -> None:
    doc = _production_readiness_review_text()

    expected_blockers = [
        "durable audit persistence",
        "distributed or edge-aware rate limiting",
        "DB-backed token registry and lifecycle",
        "secret manager integration",
    ]

    for blocker in expected_blockers:
        assert blocker in doc


def _production_readiness_review_text() -> str:
    for parent in Path(__file__).resolve().parents:
        candidate = parent / "docs" / "production-readiness-review.md"
        if candidate.exists():
            return candidate.read_text()

    raise AssertionError("Could not find docs/production-readiness-review.md")
