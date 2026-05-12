# Implementation Options

CarbonOps-API is documented as a .NET-first API foundation with a future
independent Python implementation option.

## .NET

.NET is the intended primary platform implementation path. The repository now
contains an initial .NET 8 Clean Architecture solution skeleton at `src/dotnet`.

Current .NET boundaries are:

- `CarbonOps.Domain`
- `CarbonOps.Application`
- `CarbonOps.Contracts`
- `CarbonOps.Infrastructure`
- `CarbonOps.Api`
- matching smoke-test projects under `src/dotnet/tests`

Future .NET work should:

- preserve the documented carbon factor lookup contract
- support testable use cases outside the web host
- add persistence only when explicitly scoped
- keep production hardening work separate from baseline documentation tasks

The skeleton includes only minimal API host startup needed to build and test.
It does not add carbon factor routes, persistence, authentication,
authorization, parser execution, or production runtime behavior.

## Python

Python is planned as an independent implementation option, similar to the
CarbonOps-Parser repository model.

The current Python implementation root now exists at `src/python`.

Future Python work should:

- implement the same conceptual API and domain behavior independently
- avoid depending on .NET code
- keep framework adapters thin
- avoid parser execution unless explicitly scoped
- provide parity evidence against the documented contract

This phase reorganizes the existing Python implementation into `src/python`
without changing runtime behavior, and introduces a first clean-architecture
package slice under `src/python/src/carbonops_api`.

That slice now includes an application repository port plus an in-memory
infrastructure adapter so lookup use cases no longer depend directly on
synthetic data modules.

## Shared Documentation Contract

Both implementation paths should align to:

- [API Boundaries](api-boundaries.md)
- [Domain Model](domain-model.md)
- [API Contract](api-contract.md)
- [Parity Model](parity-model.md)
