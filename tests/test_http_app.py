from fastapi.testclient import TestClient

from carbonops_api.http.app import create_app


def test_create_app_returns_fastapi_instance() -> None:
    app = create_app()
    assert app.title == "CarbonFactor API"


def test_health_route_status_code() -> None:
    client = TestClient(create_app())
    response = client.get("/health")
    assert response.status_code == 200


def test_health_route_payload_is_deterministic() -> None:
    client = TestClient(create_app())
    response = client.get("/health")
    assert response.json() == {"status": "ok", "adapter": "fastapi"}
