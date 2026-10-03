# SmartPay Architecture

## Services
- Identity: account and simulated KYC status
- Wallet: wallet ownership and balance projection
- Payment: payment lifecycle, idempotency, orchestration, outbox
- Risk: simulated risk decisions
- Ledger: double-entry journal
- Notification: asynchronous delivery attempts
- BuildingBlocks: shared technical primitives only

## Target payment flow
1. Client submits payment with an idempotency key.
2. Payment Service validates request and checks the key.
3. Risk Service evaluates payment.
4. Payment Service writes payment state and outbox message atomically.
5. Publisher sends an event to RabbitMQ.
6. Ledger and Notification consumers process events idempotently.

## Demo limitation
The initial scaffold uses in-memory state. Docker Compose includes PostgreSQL and RabbitMQ for later phases, but demo APIs are not yet connected to them.
