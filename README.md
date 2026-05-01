# CarbonFactor API

CarbonFactor API is a .NET 8 API foundation for ingesting, validating, normalizing, querying, and summarizing carbon factor records.

The project is intentionally small and reviewable. It demonstrates API contract consistency, domain validation boundaries, deterministic normalization, in-memory persistence, query pagination, and technical reporting-support summaries.

## What The API Does

- Accepts single and batch carbon factor records.
- Validates required fields, supported categories, supported units, numeric emission values, and effective-year bounds.
- Normalizes common category and unit variations into stable values.
- Stores accepted records in an in-memory repository.
- Exposes lookup, query, pagination, and summary endpoints.
- Returns predictable structured error responses.

## What The API Does Not Do

- It does not include authentication or authorization yet.
- It does not persist data beyond process lifetime.
- It does not import CSV or spreadsheet files yet.
- It does not perform assurance, certification, or regulatory report generation.
- It does not include private data, customer data, credentials, or production secrets.

## Local Development

Restore and build:

```bash
dotnet restore
dotnet build
```

Run locally:

```bash
dotnet run --project src/CarbonFactor.Api
```

Run tests:

```bash
dotnet test
```

## API Documentation

Swagger/OpenAPI is available in local development:

```text
http://localhost:5000/swagger
```

The generated OpenAPI JSON is available at:

```text
http://localhost:5000/swagger/v1/swagger.json
```

## Endpoint Overview

- Health: `GET /health`
- Query factors: `GET /api/carbon-factors`
- Create one factor: `POST /api/carbon-factors`
- Batch ingest factors: `POST /api/carbon-factors/batch`
- Get a factor by id: `GET /api/carbon-factors/{id}`
- Dataset summary: `GET /api/carbon-factors/summary`

## Documentation

- [Documentation index](docs/index.md)
- [Current architecture state](docs/architecture/current-state.md)
- [Error handling](docs/api/error-handling.md)
- [Ingestion](docs/api/ingestion.md)
- [Querying and pagination](docs/api/querying.md)
- [Reporting-support summary](docs/api/reporting-summary.md)
- [Configuration](docs/configuration.md)
- [Roadmap](docs/roadmap.md)

## Public-Safe Limitations

This repository is a technical API foundation. The sample contracts and documentation are generic, and runtime configuration examples do not contain secrets.
