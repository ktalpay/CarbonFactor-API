# Developer Setup (DOC-003)

Date: 2026-05-22

## Purpose And Scope

This guide documents local development and validation workflows for CarbonOps-API.

It covers local setup, test execution, fixture validation, Docker package validation, local API smoke checks, contribution hygiene, generated artifact hygiene, and secrets hygiene.

This is not a production runbook, deployment automation guide, generated OpenAPI guide, or generated client guide. It does not claim production readiness.

## Prerequisites

Recommended local tools:

- Git
- .NET 8 SDK
- Python `3.11` or newer for `src/python`
- `pip`
- Docker, optional for package validation and container smoke checks
- GitHub CLI, optional for issue/PR workflow
- `bash` or `zsh` on macOS/Linux

Windows is not documented in this guide. Commands may differ under PowerShell, `cmd.exe`, or Windows-specific shell environments.

## Repository Layout Quick Reference

Important paths:

- `src/dotnet`: .NET solution, API, application, contracts, domain, infrastructure, and .NET tests.
- `src/python`: Python legacy/local contract foundation and Python tests.
- `docs`: architecture, endpoint, readiness, runbook, packaging, and workflow documentation.
- `tests/contract-parity`: PT-001 parity baseline manifest.
- `tests/contract-fixtures/http`: PT-002 checked-in HTTP response fixtures.
- `tests/contract-fixtures/openapi`: PT-003 OpenAPI-adjacent metadata baseline.
- `scripts/ops`: local operator/developer helper scripts.
- `Dockerfile`: .NET 8 container packaging baseline.
- `.dockerignore`: Docker build-context exclusions.

## Fresh Clone Setup

Clone the repository:

```bash
git clone https://github.com/ktalpay/CarbonOps-API.git
cd CarbonOps-API
```

Inspect local state before making changes:

```bash
git status --short --branch
```

Keep local environment files out of commits. A local `.python-version` may exist on developer machines; if it is untracked, leave it untracked unless the team explicitly decides to add it.

## .NET Setup And Validation

From the repository root:

```bash
dotnet restore src/dotnet/CarbonOps.Api.sln
```

Run targeted .NET tests:

```bash
dotnet test src/dotnet/tests/CarbonOps.Api.Tests/CarbonOps.Api.Tests.csproj
dotnet test src/dotnet/tests/CarbonOps.Contracts.Tests/CarbonOps.Contracts.Tests.csproj
```

Run the full .NET solution test suite:

```bash
dotnet test src/dotnet/CarbonOps.Api.sln
```

Known warning:

- `CarbonFactorEndpointsTests.HealthEndpointReturnsDeterministicPayload` currently triggers xUnit analyzer warning `xUnit1013` because it is a public helper-like method not marked with `[Fact]`. This warning is known and separate from DOC-003.

## Python Setup And Validation

From `src/python`:

```bash
python -m pip install -e ".[test]"
python -m pytest -q
```

Focused docs and fixture tests:

```bash
python -m pytest -q \
  tests/test_contract_parity_baseline.py \
  tests/test_response_fixtures.py \
  tests/test_production_parity_review.py \
  tests/test_public_endpoints_doc.py \
  tests/test_architecture_doc.py
```

The Python side is a legacy/local read-only `/factors` contract foundation. It is not production-equivalent to the modern `.NET` `/carbon-factors`, `/v1`, import, auth, rate limiting, correlation id, or audit behavior.

## JSON Fixture Validation

Validate the PT-001 parity manifest:

```bash
python -m json.tool tests/contract-parity/contract-parity-baseline.json >/dev/null
```

Validate the PT-003 metadata baseline:

```bash
python -m json.tool tests/contract-fixtures/openapi/openapi-public-metadata-baseline.json >/dev/null
```

Validate all PT-002 HTTP response fixtures:

```bash
for file in tests/contract-fixtures/http/*.json; do
  python -m json.tool "$file" >/dev/null
done
```

Fixture updates must be intentional and reviewed. Do not regenerate fixtures during test execution into tracked paths.

## Docker And Package Validation

Docker is optional for local development. If Docker is not installed, package validation commands that need Docker will fail clearly.

Check the packaging script syntax:

```bash
bash -n scripts/ops/validate-dotnet-package.sh
```

Check Docker packaging prerequisites without building an image:

```bash
scripts/ops/validate-dotnet-package.sh --check-only
```

Build the local container image through the helper script:

```bash
IMAGE_TAG=carbonops-api:local scripts/ops/validate-dotnet-package.sh
```

Optional Development-mode container smoke check:

```bash
docker run --rm -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  carbonops-api:local
```

In another terminal:

```bash
curl -i http://localhost:8080/health
```

Do not commit publish output, Docker build output, Docker image layers, or generated artifacts.

## Local API Run

Run the ASP.NET Core API locally:

```bash
dotnet run --project src/dotnet/src/CarbonOps.Api/CarbonOps.Api.csproj
```

The checked-in Development launch profile uses:

```text
http://localhost:5163
```

Smoke checks:

```bash
curl -i http://localhost:5163/health
curl -i http://localhost:5163/v1/carbon-factors
```

Development config uses non-production placeholders. Do not use Development config for Production.

## Working On Tasks

Recommended task workflow:

1. Start from the latest `develop`.
2. Create a focused feature branch.
3. Keep scope narrow and aligned with the issue.
4. Do not mutate unrelated files.
5. Do not commit generated artifacts.
6. Do not commit secrets or local environment files.
7. Run relevant validation before opening a PR.
8. Leave issue closure and merge decisions to the requested workflow.

Useful branch setup:

```bash
git fetch origin
git checkout develop
git pull --ff-only origin develop
git checkout -b feature/<task-name>
```

Before staging, inspect:

```bash
git status --short --branch
git diff --stat
```

Stage only files that belong to the task.

## Validation Checklist Before PR

Run the checks relevant to your change:

```bash
git diff --check
git status --short
```

.NET checks:

```bash
dotnet test src/dotnet/tests/CarbonOps.Api.Tests/CarbonOps.Api.Tests.csproj
dotnet test src/dotnet/tests/CarbonOps.Contracts.Tests/CarbonOps.Contracts.Tests.csproj
dotnet test src/dotnet/CarbonOps.Api.sln
```

Python checks when Python or docs tests are touched:

```bash
cd src/python
python -m pytest -q
```

JSON fixture checks when fixture or parity files are touched:

```bash
python -m json.tool tests/contract-parity/contract-parity-baseline.json >/dev/null
python -m json.tool tests/contract-fixtures/openapi/openapi-public-metadata-baseline.json >/dev/null
for file in tests/contract-fixtures/http/*.json; do
  python -m json.tool "$file" >/dev/null
done
```

Docker checks when packaging files are touched:

```bash
bash -n scripts/ops/validate-dotnet-package.sh
scripts/ops/validate-dotnet-package.sh --check-only
IMAGE_TAG=carbonops-api:local scripts/ops/validate-dotnet-package.sh
```

Before committing, confirm generated artifacts are not staged:

```bash
git diff --cached --name-only
```

## Generated Artifact Hygiene

Do not commit generated or local-only files such as:

- `bin/`
- `obj/`
- `__pycache__/`
- `.pytest_cache/`
- `.DS_Store`
- `publish/`
- `artifacts/`
- Docker image layers or build output
- `.agent-handoff/`
- local environment files
- `.python-version` if it is local-only and untracked

If these appear after running tests or local tools, leave them unstaged unless a task explicitly says otherwise.

## Secrets Hygiene

Do not commit or paste:

- real API keys,
- plaintext import keys,
- production key hashes when avoidable,
- tenant values,
- connection strings,
- secret manager values,
- raw headers or request bodies that may contain secrets.

Do not put secrets in issues, PR descriptions, logs, screenshots, shell transcripts, or docs examples. Use placeholders such as `<api_key>` and `<production_lowercase_sha256_hex>`.

## Common Troubleshooting

### .NET SDK Missing Or Wrong Version

Run:

```bash
dotnet --version
```

Install the .NET 8 SDK if restore/build/test commands fail because the SDK is missing or too old.

### Python Editable Install Failed

From `src/python`, verify the Python version:

```bash
python --version
```

The package requires Python `3.11` or newer. Then reinstall test extras:

```bash
python -m pip install -e ".[test]"
```

### Pytest Cannot Find Fixture Paths

Run fixture-related tests from `src/python` or from a location under the repository. The tests walk parent directories to find repository-level fixture paths.

### Docker Not Installed

Packaging validation that calls Docker will fail with a clear message. Install Docker or skip Docker validation for docs/code changes that do not touch packaging.

### Production Startup Validation Fails During Local Production Test

Production startup validation is strict by design. Use Development for ordinary local smoke checks. If testing Production startup, provide safe non-secret placeholder-like values that pass validation and never paste real secrets into shell history or logs.

### Rate-Limit Tests Are Flaky

Rate-limit tests should override rate-limit config with isolated low values. Avoid relying on shared local API process state or repeated manual calls when validating rate-limit behavior.

### xUnit1013 Warning

The existing `CarbonFactorEndpointsTests.HealthEndpointReturnsDeterministicPayload` warning is known. It should be cleaned up in a dedicated test-maintenance task unless it starts blocking validation.

## Relationship To Other Docs

Related docs:

- `docs/architecture.md`
- `docs/public-endpoints.md`
- `docs/deployment-packaging.md`
- `docs/environment-config-hardening.md`
- `docs/production-runbook.md`
- `docs/response-fixtures.md`
- `docs/openapi-contract-drift.md`
