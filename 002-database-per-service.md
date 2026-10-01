# ADR 002 — Database per Service

## Status

Accepted

## Context

Direct shared-database access creates tight coupling between services.

## Decision

Each service owns its persistence.

A service must not directly query or modify another service's database.

## Consequences

Cross-service information must use:

- APIs
- Events
- Read models
- Duplicated reference data where justified

This increases some data duplication but preserves service ownership.
