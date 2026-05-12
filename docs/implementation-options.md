# Implementation Options

CarbonOps-API is documented as a .NET-first API foundation with a future
independent Python implementation option.

## .NET

.NET is the intended primary platform implementation path.

Future .NET work should:

- introduce API, application, domain, and infrastructure boundaries
- preserve the documented carbon factor lookup contract
- support testable use cases outside the web host
- add persistence only when explicitly scoped
- keep production hardening work separate from baseline documentation tasks

API-001 does not split the .NET project, create `src/dotnet`, or change runtime
behavior.

## Python

Python is planned as an independent implementation option, similar to the
CarbonOps-Parser repository model.

Future Python work should:

- implement the same conceptual API and domain behavior independently
- avoid depending on .NET code
- keep framework adapters thin
- avoid parser execution unless explicitly scoped
- provide parity evidence against the documented contract

API-001 does not add `src/python` or new Python implementation code.

## Shared Documentation Contract

Both implementation paths should align to:

- [API Boundaries](api-boundaries.md)
- [Domain Model](domain-model.md)
- [API Contract](api-contract.md)
- [Parity Model](parity-model.md)
