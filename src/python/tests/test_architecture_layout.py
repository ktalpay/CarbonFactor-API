import ast
from importlib import import_module
from pathlib import Path


def _module_root() -> Path:
    return Path(__file__).resolve().parents[1] / "src" / "carbonops_api"


def _imported_modules(path: Path) -> set[str]:
    tree = ast.parse(path.read_text())
    modules: set[str] = set()
    for node in ast.walk(tree):
        if isinstance(node, ast.Import):
            modules.update(alias.name for alias in node.names)
        elif isinstance(node, ast.ImportFrom) and node.module is not None:
            modules.add(node.module)
    return modules


def test_architecture_packages_are_importable() -> None:
    package_names = [
        "carbonops_api.api",
        "carbonops_api.application",
        "carbonops_api.contracts",
        "carbonops_api.domain",
        "carbonops_api.infrastructure",
    ]
    for package_name in package_names:
        assert import_module(package_name) is not None


def test_domain_modules_do_not_import_forbidden_layers() -> None:
    domain_root = _module_root() / "domain"
    forbidden_prefixes = (
        "fastapi",
        "starlette",
        "carbonops_api.application",
        "carbonops_api.api",
        "carbonops_api.http",
        "carbonops_api.infrastructure",
        "carbonops_api.transport",
    )
    for path in domain_root.glob("*.py"):
        imported = _imported_modules(path)
        assert all(not module.startswith(forbidden_prefixes) for module in imported)


def test_application_modules_do_not_import_http_frameworks() -> None:
    application_root = _module_root() / "application"
    forbidden_prefixes = (
        "fastapi",
        "starlette",
        "carbonops_api.api",
        "carbonops_api.http",
        "carbonops_api.infrastructure",
    )
    for path in application_root.glob("*.py"):
        imported = _imported_modules(path)
        assert all(not module.startswith(forbidden_prefixes) for module in imported)


def test_compatibility_imports_still_resolve() -> None:
    contracts_module = import_module("carbonops_api.contracts")
    catalog_module = import_module("carbonops_api.catalog")
    errors_module = import_module("carbonops_api.errors")
    sample_data_module = import_module("carbonops_api.sample_data")

    assert hasattr(contracts_module, "FactorDto")
    assert hasattr(catalog_module, "list_factors")
    assert hasattr(errors_module, "invalid_query_error")
    assert hasattr(sample_data_module, "SAMPLE_FACTORS")
