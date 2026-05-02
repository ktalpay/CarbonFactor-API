# Configuration

CarbonFactor API is configured through standard ASP.NET Core configuration providers. Local development can use `appsettings.Development.json`, environment variables, or command-line overrides.

No secrets are required for the current in-memory API foundation.

## Required Configuration

There are no required custom configuration values at this stage. The API starts with safe defaults when the `CarbonFactor` section is omitted.

## Optional Configuration

| Key | Default | Description |
| --- | --- | --- |
| `CarbonFactor:DefaultPageSize` | `25` | Default page size for `GET /api/carbon-factors`. Must be greater than zero and less than or equal to `MaxPageSize`. |
| `CarbonFactor:MaxPageSize` | `100` | Maximum allowed page size. Must be between `1` and `500`. |
| `CarbonFactor:EnableSwaggerInProduction` | `false` | Allows Swagger middleware outside development when explicitly enabled. |

## Local Development Example

Use `src/CarbonFactor.Api/appsettings.example.json` as a public-safe reference:

```json
{
  "CarbonFactor": {
    "DefaultPageSize": 25,
    "MaxPageSize": 100,
    "EnableSwaggerInProduction": false
  }
}
```

Environment variable equivalent:

```bash
CarbonFactor__DefaultPageSize=25
CarbonFactor__MaxPageSize=100
CarbonFactor__EnableSwaggerInProduction=false
```

## Validation

Configuration is validated during application startup. Invalid pagination limits prevent the API from starting so bad runtime behavior is caught early.

