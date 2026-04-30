from carbonfactor_api.http.app import create_app
from carbonfactor_api.transport.handlers import handle_get_factor, handle_list_factors


def test_get_factors_success() -> None:
    app = create_app()
    payload = app.dispatch("GET", "/factors")
    assert payload["status"] == 200
    assert payload["data"]["total"] >= 1


def test_get_factors_with_category_filter_success() -> None:
    app = create_app()
    payload = app.dispatch("GET", "/factors", category="electricity")
    assert payload["status"] == 200
    assert all(item["category"] == "electricity" for item in payload["data"]["factors"])


def test_get_factor_by_id_success() -> None:
    app = create_app()
    payload = app.dispatch("GET", "/factors/f-002")
    assert payload["status"] == 200
    assert payload["data"]["factor"]["id"] == "f-002"


def test_get_factor_missing_returns_not_found() -> None:
    app = create_app()
    payload = app.dispatch("GET", "/factors/missing-factor")
    assert payload["status"] == 404
    assert payload["error"]["code"] == "not_found"


def test_routes_match_transport_handlers() -> None:
    app = create_app()
    list_payload = app.dispatch("GET", "/factors", region="US")
    detail_payload = app.dispatch("GET", "/factors/f-002")
    assert list_payload == handle_list_factors({"region": "US"}).to_dict()
    assert detail_payload == handle_get_factor("f-002").to_dict()
