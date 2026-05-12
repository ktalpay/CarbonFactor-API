# Clean Code Guidelines

CarbonOps-API should favor small, explicit, testable code paths as it evolves
from documentation baseline to implementation work.

## General Guidelines

- Keep API adapters thin.
- Keep domain concepts independent from framework details.
- Prefer explicit DTOs and response models over loose dictionaries at public
  boundaries.
- Keep error codes deterministic and documented.
- Preserve endpoint behavior unless a task explicitly changes the contract.
- Avoid cross-repository imports from CarbonOps-Parser or CarbonOps-Web unless a
  later task defines an integration boundary.

## .NET Guidelines

Future .NET work should:

- separate API, application, domain, and infrastructure projects when scoped
- keep controllers or minimal API handlers thin
- put use-case orchestration outside transport handlers
- keep persistence behind interfaces
- keep domain models free from ASP.NET and database attributes unless explicitly
  justified

## Python Guidelines

Future Python work should:

- remain independent from the .NET implementation
- keep FastAPI or other framework code at the adapter boundary
- keep domain and application code importable without starting a server
- avoid adding parser execution into the API process unless explicitly scoped

## Documentation Guidelines

- Use CarbonOps-API for the repository and product name.
- Use CarbonFactor only for the domain entity or concept.
- State local-only, pre-alpha, or non-production limits clearly.
- Do not imply certification, regulatory readiness, or official source-owner
  endorsement.
