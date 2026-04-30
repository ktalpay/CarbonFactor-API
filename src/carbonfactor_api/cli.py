"""CLI helpers for CarbonFactor API."""


def status_text() -> str:
    """Return deterministic project status text."""
    return (
        "Project: carbonfactor-api\n"
        "Status: pre-alpha\n"
        "Scope: in-memory contract foundation for carbon factor lookup"
    )


def main() -> None:
    """Print status summary."""
    print(status_text())
