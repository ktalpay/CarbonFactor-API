# CarbonOps-API Python

This directory contains the current Python implementation of CarbonOps-API.

## Layout

- `pyproject.toml`: Python package and test configuration
- `src/carbonops_api`: package source
- `tests`: Python test suite

## Current Scope

The Python implementation preserves the current local API contract foundation:

- `GET /health`
- `GET /factors`
- `GET /factors/{factor_id}`

It includes:

- contract DTOs and error models
- synthetic in-memory sample data
- catalog lookup behavior
- transport envelopes and status mapping
- a thin FastAPI adapter for local testing

## Local Validation

From this directory:

```bash
python --version
python -m pip install -e ".[test]"
python -m pytest -q
```

This path is the active implementation root for the current repository phase.
