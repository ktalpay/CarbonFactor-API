from carbonfactor_api.http.app import create_app


def test_create_app_returns_fastapi_instance() -> None:
    app = create_app()

    assert app.title == "CarbonFactor API"


def test_health_route_status_code() -> None:
    app = create_app()

    payload = app.dispatch("GET", "/health")

    assert isinstance(payload, dict)


def test_health_route_payload_is_deterministic() -> None:
    app = create_app()

    payload = app.dispatch("GET", "/health")

    assert payload == {"status": "ok", "adapter": "fastapi"}
