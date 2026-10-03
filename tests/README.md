# Tests

Planned automated tests:
- Same idempotency key and same request returns original payment
- Same key and different request returns conflict
- Invalid amounts and unsupported currencies are rejected
- Ledger debit and credit totals balance
- Duplicate event delivery does not duplicate ledger postings
- Outbox retries when broker is unavailable
