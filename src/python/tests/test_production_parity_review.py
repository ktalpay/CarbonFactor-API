from pathlib import Path


def test_production_parity_review_document_exists_and_references_evidence() -> None:
    review = _review_text()

    assert "PT-001" in review
    assert "PT-002" in review
    assert "PT-003" in review
    assert "OPS-033" in review
    assert "REL-001" in review


def test_production_parity_review_states_python_modern_parity_gap() -> None:
    review = _review_text()

    assert "Python modern API parity is not complete." in review
    assert "Ready to claim Python/.NET production equivalence: no." in review


def _review_text() -> str:
    for parent in Path(__file__).resolve().parents:
        candidate = parent / "docs" / "production-parity-review.md"
        if candidate.exists():
            return candidate.read_text()

    raise AssertionError("Could not find docs/production-parity-review.md")
