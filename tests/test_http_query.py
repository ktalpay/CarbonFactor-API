from fastapi import Request

from carbonfactor_api.http.query import build_factor_query, unsupported_query_keys


def _request_from_query_string(query_string: str) -> Request:
    return Request({"type": "http", "query_string": query_string.encode(), "headers": []})


def test_build_factor_query_includes_supported_params_only() -> None:
    query = build_factor_query(category="electricity", activity="grid", region="US", year=2024)
    assert query == {"category": "electricity", "activity": "grid", "region": "US", "year": 2024}


def test_build_factor_query_omits_none_values() -> None:
    query = build_factor_query(category=None, activity="grid", region=None, year=None)
    assert query == {"activity": "grid"}


def test_unsupported_query_keys_detects_only_unknown_keys() -> None:
    request = _request_from_query_string("category=electricity&foo=1&year=2024&bar=2")
    assert unsupported_query_keys(request) == ["bar", "foo"]
