from pathlib import Path


def test_architecture_doc_exists_and_covers_current_boundaries() -> None:
    doc = _architecture_text()

    expected_terms = [
        ".NET API Architecture",
        "Public HTTP Boundary",
        "Ingestion Boundary",
        "Persistence Architecture",
        "Security Boundary",
        "Observability Boundary",
        "Rate Limiting And Operational Boundaries",
        "Deployment And Package Boundary",
        "Cross-Runtime And Parity Boundary",
    ]

    for term in expected_terms:
        assert term in doc


def test_architecture_doc_states_python_modern_parity_is_not_complete() -> None:
    doc = _architecture_text()

    assert "Python modern API parity is not complete." in doc
    assert "Python/.NET full production equivalence is not claimed." in doc


def _architecture_text() -> str:
    for parent in Path(__file__).resolve().parents:
        candidate = parent / "docs" / "architecture.md"
        if candidate.exists():
            return candidate.read_text()

    raise AssertionError("Could not find docs/architecture.md")
