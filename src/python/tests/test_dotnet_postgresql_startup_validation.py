from pathlib import Path


def test_dotnet_postgresql_startup_script_exists_and_has_safe_contract() -> None:
    script = _repo_root() / "scripts" / "ops" / "validate-dotnet-postgresql-startup.sh"
    script_text = script.read_text()

    assert script.exists()
    assert "CARBONOPS_POSTGRESQL_TEST_DSN" in script_text
    assert "--check-only" in script_text
    assert "Persistence__PostgreSql__BootstrapOnStartup=true" in script_text
    assert "Persistence__PostgreSql__BootstrapMode=CreateMissing" in script_text
    assert "/health/ready" in script_text
    assert "/v1/carbon-factors" in script_text
    assert "Password=" not in script_text
    assert "Host=localhost" not in script_text


def test_dotnet_postgresql_startup_docs_reference_script_and_boundaries() -> None:
    developer_setup = (_repo_root() / "docs" / "developer-setup.md").read_text()
    self_hosted_readiness = (_repo_root() / "docs" / "self-hosted-production-readiness.md").read_text()

    combined_docs = developer_setup + "\n" + self_hosted_readiness

    assert "scripts/ops/validate-dotnet-postgresql-startup.sh" in combined_docs
    assert "CARBONOPS_POSTGRESQL_TEST_DSN" in combined_docs
    assert "startup DB bootstrap" in combined_docs
    assert "import still returns `persisted=false` and `import_execution=\"not_started\"`" in combined_docs
    assert "Python self-hosted DB support" in combined_docs


def _repo_root() -> Path:
    for parent in Path(__file__).resolve().parents:
        if (parent / "docs").exists() and (parent / "scripts").exists():
            return parent

    raise AssertionError("Could not find repository root")
