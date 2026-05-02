# API Error Handling

The API returns structured JSON errors for validation failures, missing resources, malformed requests, and unexpected failures.

## Error Shape

```json
{
  "status": 400,
  "code": "validation_failed",
  "title": "Validation failed.",
  "detail": "One or more request fields failed validation.",
  "traceId": "0HMSAMPLE",
  "errors": [
    {
      "field": "unit",
      "code": "unsupported_unit",
      "message": "Unsupported emission factor unit."
    }
  ]
}
```

`errors` is an ordered array of field-level validation errors. It is empty for errors that do not map to individual input fields.

## Validation Behavior

Requests are validated at the API boundary before records are accepted. Required fields return `required` errors. Unsupported known fields return deterministic error codes such as `unsupported_unit` or `unsupported_category`.

Malformed JSON returns a structured `invalid_request` response instead of exposing framework exception details.

## Status Codes

| Status | Code examples | Meaning |
| --- | --- | --- |
| `400` | `invalid_request`, `validation_failed`, `invalid_identifier` | The request payload, route value, or query input is invalid. |
| `404` | `carbon_factor_not_found` | No carbon factor exists for the requested identifier. |
| `500` | `internal_error` | The API encountered an unexpected error. Internal details are not returned. |

## Examples

Missing required field:

```json
{
  "status": 400,
  "code": "validation_failed",
  "title": "Validation failed.",
  "detail": "One or more request fields failed validation.",
  "traceId": "0HMSAMPLE",
  "errors": [
    {
      "field": "name",
      "code": "required",
      "message": "Name is required."
    }
  ]
}
```

Not found:

```json
{
  "status": 404,
  "code": "carbon_factor_not_found",
  "title": "Carbon factor not found.",
  "detail": "No carbon factor record was found for the supplied identifier.",
  "traceId": "0HMSAMPLE",
  "errors": []
}
```

