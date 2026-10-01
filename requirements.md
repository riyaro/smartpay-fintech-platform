# SmartPay Requirements

## 1. Actors

### Customer
Can register, complete simulated KYC, create a wallet, add simulated funds, and make merchant payments.

### Merchant
Can register and receive payments.

### Platform
Runs identity, wallet, payment, risk, ledger, and notification workflows.

## 2. Identity requirements

- Register customer
- Register merchant
- Authenticate users
- Maintain account status
- Maintain simulated KYC status
- Never connect to a real KYC provider

Possible account states:

```text
PendingKyc -> Active
PendingKyc -> Rejected
Active -> Suspended
Suspended -> Active
```

## 3. Wallet requirements

A wallet contains:

- Wallet ID
- Owner ID
- Currency
- Available balance
- Status
- Created timestamp

Initial supported currency: **SAR**.

The wallet service owns wallet state.

## 4. Payment requirements

A payment request contains:

- Idempotency key
- Customer wallet
- Merchant
- Amount
- Currency
- Reference

Rules:

- Amount must be greater than zero
- Currency must match supported wallet currency
- Account must be active
- Payment must pass risk checks
- Duplicate idempotency keys must not create duplicate payments

### Payment state machine

```text
Created -> Pending -> Authorized -> Captured -> Completed
                    |              |
                    v              v
                  Failed        Cancelled

Completed -> Refunded
```

Invalid state transitions must be rejected.

## 5. Idempotency requirements

For:

```http
POST /payments
Idempotency-Key: abc-123
```

The same key and equivalent request must return the original payment result.

A key cannot silently be reused for a different request payload.

The idempotency record must survive application restarts.

## 6. Ledger requirements

The ledger records completed money movement using double-entry accounting.

For a SAR 100 payment:

```text
Debit  Customer Wallet     100
Credit Merchant Wallet     100
```

The sum of debits must equal the sum of credits.

Ledger records should be append-oriented. Existing financial entries should not be mutated to "fix" history.

## 7. Risk requirements

Initial rule examples:

- Payment amount exceeds configured maximum
- Too many payment attempts in a short period
- Account is blocked
- Currency mismatch
- Repeated suspicious payment pattern

Risk decisions:

```text
Approved
Review
Rejected
```

This is a simulated rule engine, not a real fraud-detection product.

## 8. Notification requirements

Notifications are asynchronous.

Examples:

- Payment completed
- Payment failed
- Payment refunded

Notification failure must not roll back a successfully completed payment.

## 9. Reliability requirements

- Idempotent payment creation
- Idempotent event consumers
- Transactional outbox
- Retry handling
- Dead-letter strategy
- Structured logging
- Correlation IDs

## 10. Consistency requirements

Strong consistency is preferred inside a service transaction.

Cross-service workflows use eventual consistency.

The system must not attempt a distributed database transaction across all services.

## 11. Security requirements

- Authentication and authorization
- Secrets must not be committed
- Sensitive data should not be logged
- Input validation
- API rate limiting where appropriate
- Least-privilege service access

## 12. Observability requirements

Each request should carry a correlation ID.

Important metrics:

- Payment success/failure count
- Payment processing duration
- Outbox backlog
- Consumer failures
- Risk rejection count
- Notification failures
- Ledger posting failures

## 13. Explicitly excluded

This project does not implement:

- Real banking integrations
- Real card processing
- Real KYC verification
- Real money movement
- Government identity verification
- Production financial compliance
