"""CLI helpers for CarbonOps API."""


def status_text() -> str:
    """Return deterministic project status text."""
    return (
        "Project: carbonops-api\n"
        "Status: pre-alpha\n"
        "Scope: in-memory contract foundation for carbon factor lookup"
    )


def main() -> None:
    """Print status summary."""
    print(status_text())
