# ADR 001 — Use Microservices for Domain Boundaries

## Status

Accepted

## Context

SmartPay is intended as a portfolio project demonstrating distributed-system and fintech engineering concepts.

The business domains have different responsibilities:

- Identity
- Wallet
- Payment
- Risk
- Ledger
- Notification

## Decision

Use independently deployable services with explicit domain ownership.

## Consequences

### Benefits

- Clear ownership
- Independent scaling
- Failure isolation
- Independent persistence
- Strong demonstration of distributed-system patterns

### Costs

- More infrastructure
- Network failures
- Eventual consistency
- More complicated debugging
- Need for distributed tracing and correlation IDs

The project intentionally accepts these costs because they are part of the learning objectives.
