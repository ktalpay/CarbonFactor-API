# Transport Boundary (Pre-Alpha)

## Purpose

The transport boundary provides deterministic request/response behavior without introducing a real web framework.

## Envelope Structure

All handler-like functions return a `ResponseEnvelope`:

- `status`: HTTP-style numeric status
- `data`: serialized payload for successful outcomes
- `error`: serialized error payload for failures

Error payload shape (`ErrorEnvelope`):

- `code`
- `message`
- `details`

## Status Mapping

Current mappings:

- `200` for success
- `400` for invalid query and unsupported filters
- `404` for not-found
- `422` reserved for validation-style errors where applicable

## Serialization

Transport serialization helpers convert contract objects into transport-safe dictionaries:

- factor DTO serialization
- list response serialization
- detail response serialization
- deterministic error serialization

Dictionary keys are deterministic and test-covered.

## Local Handler Functions

- `handle_list_factors(query: dict)`
- `handle_get_factor(factor_id: str)`

These functions call the existing in-memory catalog service and map outcomes into deterministic envelopes.

## Current Limitations

- No HTTP server implementation yet
- No framework route decorators
- No persistence or external provider integration
- Not production-ready

## HTTP parity

The local FastAPI routes delegate to transport handlers and return the same deterministic envelope shape and status mapping.
