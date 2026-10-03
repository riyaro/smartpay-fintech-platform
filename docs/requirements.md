# SmartPay Requirements

## Scope
Simulated digital payments in SAR. No real money movement or external banking/KYC integration.

## Actors
- Customer: registers, completes simulated KYC, owns a wallet, makes payments.
- Merchant: receives simulated payments.
- Platform: identity, wallet, payment, risk, ledger, notification.

## Payment rules
- Amount must be greater than zero.
- Initial currency is SAR.
- Payment creation requires an `Idempotency-Key`.
- Same key + same request returns the original payment in the demo.
- Same key + different request returns conflict.
- Invalid payment state transitions must be rejected.

## Payment lifecycle
`Created -> Pending -> Authorized -> Captured -> Completed`; alternative paths include `Failed`, `Cancelled`, and `Refunded`.

## Ledger and reliability
Completed payments should eventually create balanced debit/credit entries. Planned reliability patterns: persistent idempotency, transactional outbox, idempotent consumers, retries, dead-letter handling, correlation IDs, structured logging.
