from carbonops_api.errors import invalid_query_error, not_found_error, unsupported_filter_error


def test_not_found_error() -> None:
    error = not_found_error("factor", "f-999")
    assert error.code == "not_found"
    assert error.details["id"] == "f-999"


def test_invalid_query_error() -> None:
    error = invalid_query_error("year must be positive")
    assert error.code == "invalid_query"
    assert "year" in error.details["reason"]


def test_unsupported_filter_error() -> None:
    error = unsupported_filter_error("source")
    assert error.code == "unsupported_filter"
    assert error.details["filter"] == "source"
