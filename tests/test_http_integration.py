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


def test_invalid_query_status_matches_envelope_status() -> None:
    client = TestClient(create_app())
    response = client.get("/factors", params={"foo": "x"})
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


def test_unknown_route_is_framework_404_not_envelope() -> None:
    client = TestClient(create_app())
    response = client.get("/unknown")
    payload = response.json()
    assert response.status_code == 404
    assert payload == {"detail": "Not Found"}


def test_success_payload_has_deterministic_top_level_keys() -> None:
    client = TestClient(create_app())
    payload = client.get("/factors").json()
    assert list(payload.keys()) == ["status", "data", "error"]


def test_error_payload_has_deterministic_top_level_keys() -> None:
    client = TestClient(create_app())
    payload = client.get("/factors/missing").json()
    assert list(payload.keys()) == ["status", "data", "error"]
