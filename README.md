# CarbonFactor API

CarbonFactor API is a .NET API foundation for ingesting, validating, normalizing, querying, and summarizing carbon factor records.

## API Documentation

Run locally:

```bash
dotnet run --project src/CarbonFactor.Api
```

Swagger/OpenAPI is available in local development at:

```text
http://localhost:5000/swagger
```

Endpoint groups:

- Health: `GET /health`
- Carbon Factors: ingestion, lookup, query, batch ingestion, and technical summary endpoints under `/api/carbon-factors`

Run tests:

```bash
dotnet test
```
