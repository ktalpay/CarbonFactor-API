from fastapi.testclient import TestClient

from carbonfactor_api.http.app import create_app


def test_route_status_matches_envelope_status() -> None:
    client = TestClient(create_app())
    response = client.get("/factors")
    payload = response.json()
    assert response.status_code == payload["status"]


def test_response_json_has_deterministic_keys() -> None:
    client = TestClient(create_app())
    response = client.get("/factors")
    payload = response.json()
    assert list(payload.keys()) == ["status", "data", "error"]


def test_unsupported_filter_matches_transport_behavior() -> None:
    client = TestClient(create_app())
    response = client.get("/factors", params={"unsupported": "x"})
    payload = response.json()
    assert response.status_code == 400
    assert payload["status"] == 400
    assert payload["error"]["code"] == "invalid_query"


def test_not_found_behavior_is_consistent() -> None:
    client = TestClient(create_app())
    response = client.get("/factors/does-not-exist")
    payload = response.json()
    assert response.status_code == 404
    assert payload["status"] == 404
    assert payload["error"] == {
        "code": "not_found",
        "message": "factor not found",
        "details": {"id": "does-not-exist"},
    }
