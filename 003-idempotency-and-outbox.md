# ADR 003 — Idempotency and Transactional Outbox

## Status

Accepted

## Context

Payment APIs can receive duplicate requests because clients retry after timeouts.

Distributed systems can also experience:

```text
Database commit succeeds
Message publication fails
```

## Decision

Use two patterns.

### Idempotency

Every payment creation request requires an idempotency key.

The payment service stores the key and original request/result association.

### Transactional Outbox

The payment and its outgoing event are persisted in the same database transaction.

A background publisher sends outbox records to RabbitMQ.

## Consequences

### Benefits

- Duplicate payment protection
- Recovery after broker outages
- Reliable event intent
- Better handling of retries

### Costs

- Additional tables
- Background publisher
- Duplicate message delivery remains possible
- Consumers must be idempotent
