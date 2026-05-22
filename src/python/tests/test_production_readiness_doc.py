from pathlib import Path


def test_production_readiness_doc_exists_and_states_verdicts() -> None:
    doc = _production_readiness_text()

    assert "Controlled/internal pilot readiness" in doc
    assert "Broad production readiness" in doc
    assert "Broad production is a no-go" in doc
    assert "REL-001" in doc


def test_production_readiness_doc_mentions_required_gaps() -> None:
    doc = _production_readiness_text()

    expected_gaps = [
        "durable audit persistence",
        "Distributed or edge rate limiting",
        "DB-backed token lifecycle",
        "Secret manager integration",
    ]

    for gap in expected_gaps:
        assert gap in doc


def _production_readiness_text() -> str:
    for parent in Path(__file__).resolve().parents:
        candidate = parent / "docs" / "production-readiness.md"
        if candidate.exists():
            return candidate.read_text()

    raise AssertionError("Could not find docs/production-readiness.md")
