"""HTTP-style status mapping constants and helpers."""

HTTP_OK = 200
HTTP_BAD_REQUEST = 400
HTTP_NOT_FOUND = 404
HTTP_UNPROCESSABLE_ENTITY = 422

_ERROR_STATUS_BY_CODE = {
    "invalid_query": HTTP_BAD_REQUEST,
    "unsupported_filter": HTTP_BAD_REQUEST,
    "not_found": HTTP_NOT_FOUND,
    "validation_error": HTTP_UNPROCESSABLE_ENTITY,
}


def error_status_for_code(code: str) -> int:
    """Map deterministic error codes to HTTP-style status values."""
    return _ERROR_STATUS_BY_CODE.get(code, HTTP_BAD_REQUEST)
