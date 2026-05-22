from pathlib import Path


def test_self_hosted_production_readiness_doc_exists_and_defines_target() -> None:
    doc = _self_hosted_readiness_text()

    assert "self-hosted distribution readiness" in doc
    assert ".NET" in doc
    assert "Python" in doc
    assert "database connection string" in doc
    assert "startup database bootstrap" in doc


def test_self_hosted_production_readiness_doc_states_not_ready_yet() -> None:
    doc = _self_hosted_readiness_text()

    assert "not self-hosted production-ready yet" in doc
    assert "REL-002 remains valid as a historical controlled/internal pilot checkpoint" in doc


def _self_hosted_readiness_text() -> str:
    for parent in Path(__file__).resolve().parents:
        candidate = parent / "docs" / "self-hosted-production-readiness.md"
        if candidate.exists():
            return candidate.read_text()

    raise AssertionError("Could not find docs/self-hosted-production-readiness.md")
