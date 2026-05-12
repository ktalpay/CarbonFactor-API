# Public Safety

CarbonOps-API should be described with careful, bounded language. The project is
the backend API reference project for the CarbonOps platform, currently at a
documentation-baseline and pre-alpha contract stage.

## Wording To Avoid

Avoid language that implies:

- production readiness
- enterprise assurance
- regulatory reporting readiness
- certification or legal assurance
- guaranteed security
- verified carbon inventory calculation
- official emissions report generation
- official source-owner endorsement
- completed .NET and Python implementation paths

## Preferred Wording

Use wording such as:

- CarbonOps platform API reference project
- backend API foundation
- .NET-first API direction
- future independent Python implementation path
- pre-alpha local contract foundation
- synthetic-data-only behavior
- non-production reference implementation
- CarbonFactor domain model

## Positioning Notes

CarbonOps-API is related to, but runtime-independent from, CarbonOps-Parser and
CarbonOps-Web in this phase.

CarbonOps-Parser owns ingestion and parsing. CarbonOps-Web owns frontend user
experience. CarbonOps-API owns backend API contracts and API-facing domain
behavior.
