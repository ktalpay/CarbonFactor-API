# CarbonOps-API Python

This directory contains the current Python implementation of CarbonOps-API.

## Layout

- `pyproject.toml`: Python package and test configuration
- `src/carbonops_api`: package source
- `tests`: Python test suite

## Transitional Package Layout

The package now includes a first clean-architecture-oriented slice:

- `carbonops_api/domain`: domain-facing factor entity concepts
- `carbonops_api/application`: application use cases for factor lookup
- `carbonops_api/contracts`: public DTOs, query models, response models, and
  error helpers
- `carbonops_api/infrastructure`: synthetic in-memory sample data
- `carbonops_api/composition.py`: default application wiring for the current
  in-memory repository implementation
- `carbonops_api/api`: API boundary wrapper for the current HTTP adapter
- `carbonops_api/http`: current FastAPI adapter kept for compatibility
- `carbonops_api/transport`: framework-neutral transport behavior

Compatibility facades remain at the top level for:

- `carbonops_api.catalog`
- `carbonops_api.errors`
- `carbonops_api.sample_data`

## Current Scope

The Python implementation preserves the current local API contract foundation:

- `GET /health`
- `GET /factors`
- `GET /factors/{factor_id}`

It includes:

- contract DTOs and error models
- synthetic in-memory sample data
- an application repository port for factor lookup
- an in-memory repository adapter for current synthetic data
- centralized composition wiring for the default repository adapter
- catalog lookup behavior wired through composition
- transport envelopes and status mapping
- a thin FastAPI adapter for local testing
- transitional clean architecture packages with compatibility imports

## Local Validation

From this directory:

```bash
python --version
python -m pip install -e ".[test]"
python -m pytest -q
```

This path is the active implementation root for the current repository phase.
