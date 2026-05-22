import json
from pathlib import Path


REQUIRED_HTTP_FIXTURES = {
    "carbon-factors-list.v1.json",
    "carbon-factors-search-electricity.v1.json",
    "carbon-factor-f001.v1.json",
    "import-accepted.v1.json",
    "import-unauthorized-missing-api-key.v1.json",
    "search-invalid-year.v1.json",
    "factor-not-found.v1.json",
    "rate-limited.v1.json",
    "health.json",
    "health-live.json",
    "health-ready.json",
    "version.json",
}


def test_http_response_fixtures_are_available_and_valid_json() -> None:
    fixture_dir = _fixture_dir()
    actual_fixture_names = {path.name for path in fixture_dir.glob("*.json")}

    assert REQUIRED_HTTP_FIXTURES.issubset(actual_fixture_names)

    for fixture_name in REQUIRED_HTTP_FIXTURES:
        json.loads((fixture_dir / fixture_name).read_text())


def test_modern_dotnet_fixture_gap_is_explicit_for_python_runtime() -> None:
    manifest = json.loads(_manifest_path().read_text())

    assert "no response fixture byte-for-byte comparison in PT-001" in manifest["non_goals"]
    assert "PT-002 response fixture comparison" in manifest["follow_ups"]
    assert "does not yet implement the modern /carbon-factors import" in manifest["python_side"]["status"]


def _fixture_dir() -> Path:
    for parent in Path(__file__).resolve().parents:
        candidate = parent / "tests" / "contract-fixtures" / "http"
        if candidate.exists():
            return candidate

    raise AssertionError("Could not find tests/contract-fixtures/http")


def _manifest_path() -> Path:
    for parent in Path(__file__).resolve().parents:
        candidate = parent / "tests" / "contract-parity" / "contract-parity-baseline.json"
        if candidate.exists():
            return candidate

    raise AssertionError("Could not find tests/contract-parity/contract-parity-baseline.json")
