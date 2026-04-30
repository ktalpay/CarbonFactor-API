from carbonfactor_api.transport.handlers import handle_get_factor, handle_list_factors


def test_list_handler_success() -> None:
    envelope = handle_list_factors({})
    payload = envelope.to_dict()

    assert payload["status"] == 200
    assert payload["data"]["total"] >= 1
    assert payload["error"] is None


def test_filter_handler_success() -> None:
    envelope = handle_list_factors({"category": "transport"})
    payload = envelope.to_dict()

    assert payload["status"] == 200
    assert payload["data"]["total"] >= 1


def test_get_handler_success() -> None:
    list_payload = handle_list_factors({}).to_dict()
    factor_id = list_payload["data"]["factors"][0]["id"]

    envelope = handle_get_factor(factor_id)
    payload = envelope.to_dict()

    assert payload["status"] == 200
    assert payload["data"]["factor"]["id"] == factor_id


def test_get_missing_factor_maps_to_not_found_status() -> None:
    payload = handle_get_factor("missing-factor").to_dict()

    assert payload["status"] == 404
    assert payload["error"]["code"] == "not_found"


def test_invalid_query_maps_to_error_status() -> None:
    payload = handle_list_factors({"year": "not-an-int"}).to_dict()

    assert payload["status"] == 400
    assert payload["error"]["code"] == "invalid_query"


def test_output_envelope_is_deterministic() -> None:
    payload = handle_list_factors({}).to_dict()
    assert list(payload.keys()) == ["status", "data", "error"]
