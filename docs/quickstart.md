# Quickstart (Pre-Alpha / Safe Local Inspection)

This quickstart is intentionally conservative: CarbonOps-API is pre-alpha and currently oriented toward inspectable architecture and deterministic local behavior for carbon accounting and emission factors API workflows.

## 1) Inspect first

Review these files before running anything:

- `README.md`
- `docs/architecture.md`
- `docs/limitations.md`
- `src/dotnet/README.md`
- `src/python/README.md`

## 2) Inspect implemented ASP.NET Core endpoint wiring

- `src/dotnet/src/CarbonOps.Api/Program.cs`
- `src/dotnet/src/CarbonOps.Api/CarbonFactorEndpoints.cs`
- `src/dotnet/src/CarbonOps.Api/OperationalEndpoints.cs`

Implemented endpoints in this repository:

- `GET /carbon-factors/`
- `GET /carbon-factors/search`
- `GET /carbon-factors/{factorId}`
- `GET /health`
- `GET /health/live`
- `GET /health/ready`
- `GET /version`

## 3) Validate .NET solution locally

From `src/dotnet`:

```bash
dotnet restore CarbonOps.Api.sln
dotnet build CarbonOps.Api.sln
dotnet test CarbonOps.Api.sln
```

## 4) Optional: validate Python implementation path

From `src/python`:

```bash
python --version
python -m pip install -e ".[test]"
python -m pytest -q
```

## Notes on scope and safety

- Current data is synthetic and deterministic.
- This API is a climate-tech data platform baseline, not a production-ready carbon emissions authority.
- CarbonOps-Parser remains a separate ingestion/normalization project; CarbonOps-API is the API surface layer over curated platform data.
