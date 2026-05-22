import json
from pathlib import Path


def test_contract_parity_baseline_records_python_status() -> None:
    manifest = _load_manifest()

    assert manifest["task"] == "PT-001"
    assert manifest["python_side"]["package_root"] == "src/python"
    assert "GET /factors" in manifest["python_side"]["current_routes"]
    assert "GET /factors/{factor_id}" in manifest["python_side"]["current_routes"]
    assert "legacy read-only contract foundation" in manifest["python_side"]["status"]


def test_contract_parity_baseline_documents_pt001_non_goals() -> None:
    manifest = _load_manifest()

    assert "no generated client in PT-001" in manifest["non_goals"]
    assert "no OpenAPI contract drift check in PT-001" in manifest["non_goals"]
    assert "no response fixture byte-for-byte comparison in PT-001" in manifest["non_goals"]


def _load_manifest() -> dict:
    for parent in Path(__file__).resolve().parents:
        candidate = parent / "tests" / "contract-parity" / "contract-parity-baseline.json"
        if candidate.exists():
            return json.loads(candidate.read_text())

    raise AssertionError("Could not find tests/contract-parity/contract-parity-baseline.json")
