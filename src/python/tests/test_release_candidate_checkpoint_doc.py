from pathlib import Path


def test_release_candidate_checkpoint_doc_exists_and_states_verdicts() -> None:
    doc = _release_candidate_checkpoint_text()

    assert "Controlled/internal pilot checkpoint: GO with constraints." in doc
    assert "Broad production release: NO-GO." in doc
    assert "Production deployment performed | NO." in doc
    assert "GitHub release" in doc
    assert "version tag" in doc


def test_release_candidate_checkpoint_doc_mentions_required_blockers() -> None:
    doc = _release_candidate_checkpoint_text()

    expected_blockers = [
        "durable audit persistence",
        "distributed or edge-aware rate limiting",
        "DB-backed token lifecycle",
        "secret manager integration",
    ]

    for blocker in expected_blockers:
        assert blocker in doc


def _release_candidate_checkpoint_text() -> str:
    for parent in Path(__file__).resolve().parents:
        candidate = parent / "docs" / "release-candidate-checkpoint.md"
        if candidate.exists():
            return candidate.read_text()

    raise AssertionError("Could not find docs/release-candidate-checkpoint.md")
