from pathlib import Path


def test_public_endpoints_doc_exists_and_lists_current_routes() -> None:
    doc = _public_endpoints_text()

    expected_routes = [
        "GET /v1/carbon-factors",
        "GET /v1/carbon-factors/search",
        "GET /v1/carbon-factors/{factorId}",
        "POST /v1/carbon-factors/import",
        "GET /health",
        "GET /health/live",
        "GET /health/ready",
        "GET /version",
    ]

    for route in expected_routes:
        assert route in doc


def test_public_endpoints_doc_documents_security_headers_and_non_goals() -> None:
    doc = _public_endpoints_text()

    assert "X-Correlation-Id" in doc
    assert "X-Api-Key: <api_key>" in doc
    assert "Retry-After" in doc
    assert "not a full generated OpenAPI document" in doc
    assert "generated SDK or client documentation" in doc


def _public_endpoints_text() -> str:
    for parent in Path(__file__).resolve().parents:
        candidate = parent / "docs" / "public-endpoints.md"
        if candidate.exists():
            return candidate.read_text()

    raise AssertionError("Could not find docs/public-endpoints.md")
