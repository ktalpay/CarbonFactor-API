from carbonfactor_api.transport.status import HTTP_BAD_REQUEST, HTTP_NOT_FOUND, error_status_for_code


def test_status_mapping_for_not_found() -> None:
    assert error_status_for_code("not_found") == HTTP_NOT_FOUND


def test_status_mapping_for_invalid_query() -> None:
    assert error_status_for_code("invalid_query") == HTTP_BAD_REQUEST
