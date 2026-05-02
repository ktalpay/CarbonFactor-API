# Carbon Factor Ingestion

The API supports single-record and batch carbon factor ingestion. Each accepted record is normalized before storage.

## Single Record

`POST /api/carbon-factors`

Request:

```json
{
  "name": "Grid electricity",
  "category": "Electricity",
  "unit": "kg CO2e / kWh",
  "emissionValue": 0.42,
  "source": "Example source",
  "region": "GB",
  "effectiveYear": 2025
}
```

Successful response: `201 Created`

```json
{
  "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "name": "Grid electricity",
  "category": "energy",
  "unit": "kg_co2e_per_kwh",
  "emissionValue": 0.42,
  "source": "Example source",
  "region": "GB",
  "effectiveYear": 2025
}
```

## Batch Record Ingestion

`POST /api/carbon-factors/batch`

Request:

```json
{
  "items": [
    {
      "name": "Grid electricity",
      "category": "energy",
      "unit": "kg_co2e_per_kwh",
      "emissionValue": 0.42
    },
    {
      "name": "Invalid unit",
      "category": "energy",
      "unit": "unknown",
      "emissionValue": 0.42
    }
  ]
}
```

Successful batch response: `200 OK`

```json
{
  "total": 2,
  "accepted": 1,
  "rejected": 1,
  "warningCount": 0,
  "items": [
    {
      "index": 0,
      "status": "accepted",
      "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      "errors": []
    },
    {
      "index": 1,
      "status": "rejected",
      "id": null,
      "errors": [
        {
          "field": "unit",
          "code": "unsupported_unit",
          "message": "Unsupported emission factor unit."
        }
      ]
    }
  ]
}
```

## Validation Behavior

Batch responses preserve the input order by `index`. Valid records are accepted, invalid records are rejected, and the response summarizes all outcomes. This makes partial acceptance visible to callers and avoids hidden failures.

Empty batches return `400 Bad Request` with an `empty_batch` validation error. Malformed JSON returns the standard structured `invalid_request` error response.

## Limitations

- Ingestion currently uses the in-memory store configured for this API foundation.
- The batch endpoint does not run background jobs or asynchronous imports.
- Warnings are represented in the response shape, but the current validation rules only produce accepted or rejected records.

