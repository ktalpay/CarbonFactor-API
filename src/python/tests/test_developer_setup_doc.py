from pathlib import Path


def test_developer_setup_doc_exists_and_mentions_validation_paths() -> None:
    doc = _developer_setup_text()

    expected_terms = [
        "dotnet test src/dotnet/tests/CarbonOps.Api.Tests/CarbonOps.Api.Tests.csproj",
        "dotnet test src/dotnet/tests/CarbonOps.Contracts.Tests/CarbonOps.Contracts.Tests.csproj",
        "python -m pytest -q",
        "python -m json.tool tests/contract-parity/contract-parity-baseline.json",
        "tests/contract-fixtures/http/*.json",
        "scripts/ops/validate-dotnet-package.sh --check-only",
    ]

    for term in expected_terms:
        assert term in doc


def test_developer_setup_doc_mentions_hygiene_boundaries() -> None:
    doc = _developer_setup_text()

    assert "Do not commit generated" in doc
    assert "Do not commit or paste" in doc
    assert "real API keys" in doc
    assert ".python-version" in doc


def _developer_setup_text() -> str:
    for parent in Path(__file__).resolve().parents:
        candidate = parent / "docs" / "developer-setup.md"
        if candidate.exists():
            return candidate.read_text()

    raise AssertionError("Could not find docs/developer-setup.md")
