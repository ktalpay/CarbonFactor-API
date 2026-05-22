# Deployment Packaging Baseline (OPS-032)

Date: 2026-05-22

OPS-032 adds a minimal, reproducible packaging baseline for CarbonOps-API. The scope is intentionally limited to container packaging, package validation, and runtime configuration documentation. It does not add infrastructure provisioning, deployment automation, secret manager integration, or a production runbook.

## Packaging Model

The repository root is the Docker build context.

The root `Dockerfile` uses official Microsoft .NET 8 images:

- `mcr.microsoft.com/dotnet/sdk:8.0` for restore, build, and publish stages,
- `mcr.microsoft.com/dotnet/aspnet:8.0` for the runtime stage.

The image is built with:

- Release configuration,
- `dotnet publish`,
- `/p:UseAppHost=false`,
- no repository secrets copied into the image,
- no local environment files copied into the image,
- the built-in non-root `APP_UID` user from the .NET runtime image,
- container port `8080`.

The `.dockerignore` excludes local caches, source control metadata, generated build outputs, publish artifacts, test results, local environment files, and task-run artifacts from the build context.

## Build Command

Run from the repository root:

```bash
docker build -f Dockerfile -t carbonops-api:local .
```

The helper script runs the same package validation without pushing an image:

```bash
scripts/ops/validate-dotnet-package.sh
```

To validate prerequisites without building:

```bash
scripts/ops/validate-dotnet-package.sh --check-only
```

## Local Container Smoke Check

For a local packaging smoke check without production secrets, run the image in Development mode:

```bash
docker run --rm -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  carbonops-api:local
```

Then call:

```bash
curl -i http://localhost:8080/health
```

Development mode uses the existing safe non-production development placeholders. Do not use Development mode for production deployment.

## Production Runtime Configuration Boundary

Production containers must receive configuration from the runtime environment. The image does not bake in active secrets or production tenant values.

Required production environment variables:

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
Security__ApiKey__ImportEndpointKeyHash=<production_lowercase_sha256_hex>
Security__ApiKey__ImportTenantId=<production_tenant_id>
Security__ApiKey__ImportEndpointScopes__0=carbon_factors:import
RateLimiting__Import__PermitLimit=10
RateLimiting__Import__WindowSeconds=60
RateLimiting__Import__QueueLimit=0
RateLimiting__Read__PermitLimit=60
RateLimiting__Read__WindowSeconds=60
RateLimiting__Read__QueueLimit=0
```

Optional production environment variables:

```text
Security__ApiKey__ImportEndpointPreviousKeyHashes__0=<production_lowercase_sha256_hex>
Security__ApiKey__RevokedKeyHashes__0=<production_lowercase_sha256_hex>
Persistence__UsePostgreSql=false
Persistence__PostgreSql__ConnectionString=<external_connection_string_if_enabled>
```

Do not set `Security__ApiKey__ImportEndpointKey` in production. OPS-031 production validation rejects plaintext API key configuration.

## Production Startup Validation

When `ASPNETCORE_ENVIRONMENT=Production`, OPS-031 validation runs during startup before the API serves requests. The container will fail fast when required security or rate limiting configuration is missing or unsafe.

See `docs/environment-config-hardening.md` for the complete validation rules.

## Health Endpoint

Use the existing operational health endpoint for container smoke checks:

```text
GET /health
```

The health, liveness, readiness, and version endpoints remain unversioned:

- `GET /health`
- `GET /health/live`
- `GET /health/ready`
- `GET /version`

## Validation

Packaging validation commands:

```bash
bash -n scripts/ops/validate-dotnet-package.sh
scripts/ops/validate-dotnet-package.sh --check-only
docker build -f Dockerfile -t carbonops-api:ops-032 .
```

Repository validation commands:

```bash
git diff --check
dotnet test src/dotnet/tests/CarbonOps.Api.Tests/CarbonOps.Api.Tests.csproj
dotnet test src/dotnet/CarbonOps.Api.sln
```

## Limitations

OPS-032 does not add:

- Kubernetes manifests,
- Helm charts,
- Terraform,
- cloud-provider deployment manifests,
- API gateway or WAF configuration,
- GitHub environment secrets,
- secret manager integration,
- release automation,
- production runbook content,
- distributed rate limiting,
- durable audit persistence,
- DB-backed token registry,
- generated publish output committed to the repository,
- Docker image layers committed to the repository.

OPS-033 covers the production runbook.
