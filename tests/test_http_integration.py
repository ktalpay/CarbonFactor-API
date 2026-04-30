from carbonfactor_api.http.app import create_app


def test_route_status_matches_envelope_status() -> None:
    app = create_app()
    payload = app.dispatch("GET", "/factors")
    assert payload["status"] == 200


def test_response_json_has_deterministic_keys() -> None:
    app = create_app()
    payload = app.dispatch("GET", "/factors")
    assert list(payload.keys()) == ["status", "data", "error"]


def test_unsupported_filter_matches_transport_behavior() -> None:
    app = create_app()
    payload = app.dispatch("GET", "/factors", unsupported="x")
    assert payload["status"] == 400
    assert payload["error"]["code"] == "invalid_query"


def test_not_found_behavior_is_consistent() -> None:
    app = create_app()
    payload = app.dispatch("GET", "/factors/does-not-exist")
    assert payload["status"] == 404
    assert payload["error"] == {
        "code": "not_found",
        "message": "factor not found",
        "details": {"id": "does-not-exist"},
    }
