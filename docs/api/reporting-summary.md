# Carbon Factor Reporting-Support Summary

The summary endpoint exposes technical dataset counts that can support API consumers building dashboards, data-quality checks, or operational reporting views.

This endpoint is not a regulatory compliance report and does not certify carbon accounting outputs.

## Endpoint

`GET /api/carbon-factors/summary`

## Response

```json
{
  "totalCount": 3,
  "countByCategory": [
    {
      "key": "energy",
      "count": 2
    },
    {
      "key": "transport",
      "count": 1
    }
  ],
  "countByUnit": [
    {
      "key": "kg_co2e",
      "count": 1
    },
    {
      "key": "kg_co2e_per_kwh",
      "count": 2
    }
  ],
  "countBySource": [
    {
      "key": "Example source",
      "count": 3
    }
  ],
  "countByRegion": [
    {
      "key": "GB",
      "count": 3
    }
  ],
  "minEffectiveYear": 2020,
  "maxEffectiveYear": 2025
}
```

## Behavior

- Empty datasets return `200 OK` with `totalCount` set to `0`, empty count arrays, and null effective-year bounds.
- Count buckets are sorted by key for deterministic responses.
- Source and region counts omit missing values.

## Limitations

- The endpoint summarizes the active in-memory dataset only.
- It does not apply calculations, accounting methods, or external assurance.
- It does not produce certified regulatory reports.

