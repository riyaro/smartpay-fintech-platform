# ADR 003 — Idempotency and Transactional Outbox

**Status:** Accepted

Payment creation uses idempotency keys. Payment state and outgoing event intent will eventually be saved in one database transaction through a transactional outbox. The current scaffold demonstrates idempotency in memory only; persistence and event publishing are follow-up work.
