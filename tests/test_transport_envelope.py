from carbonfactor_api.contracts import ApiError
from carbonfactor_api.transport.envelope import ErrorEnvelope, ResponseEnvelope


def test_success_envelope_serialization() -> None:
    envelope = ResponseEnvelope(status=200, data={"total": 1}, error=None)

    assert envelope.to_dict() == {
        "status": 200,
        "data": {"total": 1},
        "error": None,
    }


def test_error_envelope_serialization() -> None:
    api_error = ApiError(code="invalid_query", message="Invalid query", details={"b": "2", "a": "1"})
    envelope = ResponseEnvelope(status=400, error=ErrorEnvelope.from_api_error(api_error))

    assert envelope.to_dict() == {
        "status": 400,
        "data": None,
        "error": {
            "code": "invalid_query",
            "message": "Invalid query",
            "details": {"a": "1", "b": "2"},
        },
    }


def test_deterministic_dictionary_structure() -> None:
    envelope = ResponseEnvelope(status=200, data={"factors": []})

    assert list(envelope.to_dict().keys()) == ["status", "data", "error"]
