from fastapi.testclient import TestClient

from carbonfactor_api.http.app import create_app
from carbonfactor_api.transport.handlers import handle_get_factor, handle_list_factors


def test_get_factors_success() -> None:
    client = TestClient(create_app())
    response = client.get("/factors")
    payload = response.json()
    assert response.status_code == 200
    assert payload["status"] == 200
    assert payload["data"]["total"] >= 1


def test_get_factors_with_category_filter_success() -> None:
    client = TestClient(create_app())
    response = client.get("/factors", params={"category": "electricity"})
    payload = response.json()
    assert response.status_code == 200
    assert payload["status"] == 200
    assert all(item["category"] == "electricity" for item in payload["data"]["factors"])


def test_get_factor_by_id_success() -> None:
    client = TestClient(create_app())
    response = client.get("/factors/f-002")
    payload = response.json()
    assert response.status_code == 200
    assert payload["status"] == 200
    assert payload["data"]["factor"]["id"] == "f-002"


def test_get_factor_missing_returns_not_found() -> None:
    client = TestClient(create_app())
    response = client.get("/factors/missing-factor")
    payload = response.json()
    assert response.status_code == 404
    assert payload["status"] == 404
    assert payload["error"]["code"] == "not_found"


def test_routes_match_transport_handlers() -> None:
    client = TestClient(create_app())
    list_response = client.get("/factors", params={"region": "US"})
    detail_response = client.get("/factors/f-002")
    assert list_response.json() == handle_list_factors({"region": "US"}).to_dict()
    assert detail_response.json() == handle_get_factor("f-002").to_dict()
