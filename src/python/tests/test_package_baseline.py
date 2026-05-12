from carbonops_api import __version__
from carbonops_api.cli import status_text


def test_package_import() -> None:
    assert __version__ == "0.1.0"


def test_status_output_is_conservative() -> None:
    output = status_text().lower()
    assert "carbonops-api" in output
    assert "pre-alpha" in output
    assert "contract foundation" in output
