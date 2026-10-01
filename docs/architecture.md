# SmartPay Architecture

## Service boundaries

### Identity Service

Owns:

- Users
- Credentials
- Roles
- KYC simulation
- Account status

It does not own wallets or payments.

### Wallet Service

Owns:

- Wallets
- Wallet status
- Balance/read model
- Simulated funding

It does not own payment state.

### Payment Service

Owns:

- Payment aggregate
- Payment state machine
- Idempotency records
- Payment orchestration
- Outbox messages

It does not directly write the Ledger database.

### Risk Service

Owns:

- Risk rules
- Risk decisions
- Risk configuration

It evaluates a payment but does not own the payment aggregate.

### Ledger Service

Owns:

- Ledger accounts
- Journal transactions
- Debit entries
- Credit entries

It is the accounting source of record for completed money movement.

### Notification Service

Owns:

- Notification requests
- Delivery attempts
- Delivery status

It consumes events asynchronously.

## Database ownership

Conceptually:

```text
Identity DB
Wallet DB
Payment DB
Risk DB
Ledger DB
Notification DB
```

No service directly queries another service's database.

Cross-service information is obtained through:

- APIs
- Events
- Read models
- Carefully duplicated reference data

## Payment flow

```text
Client
  |
  | POST /payments
  | Idempotency-Key
  v
Payment Service
  |
  +--> Validate request
  |
  +--> Check idempotency
  |
  +--> Ask Risk Service
  |
  +--> Save Payment
  |
  +--> Save Outbox Message
  |
  +--> Commit DB transaction
  |
  v
Outbox Publisher
  |
  v
RabbitMQ
  |
  +----------+-------------+
  |                        |
  v                        v
Ledger Consumer       Notification Consumer
```

## Why the outbox exists

Without an outbox:

```text
BEGIN
Save payment
COMMIT

Publish event
   |
   +-- broker unavailable
```

The payment exists but downstream services do not know about it.

With an outbox:

```text
BEGIN
Save payment
Save outbox message
COMMIT

Later:
Outbox Publisher -> RabbitMQ
```

The database transaction protects the relationship between the business change and the event intent.

## Duplicate message handling

At-least-once delivery means a consumer may see:

```text
PaymentCompleted
PaymentCompleted
```

Consumers therefore need their own idempotency strategy, such as a processed-event table or unique event identifier.

## Ledger model

A journal transaction contains multiple entries.

Example:

```text
Transaction: PAY-10001

Entry 1
Account: CustomerWallet
Type: Debit
Amount: 100 SAR

Entry 2
Account: MerchantWallet
Type: Credit
Amount: 100 SAR
```

Invariant:

```text
SUM(Debits) = SUM(Credits)
```

## Consistency model

Within Payment Service:

```text
Payment + Idempotency + Outbox
```

should commit atomically.

Across services:

```text
Payment -> Event -> Ledger
Payment -> Event -> Notification
```

is eventually consistent.

## Failure scenarios

### RabbitMQ unavailable

Payment transaction can still commit with an outbox record. The publisher retries later.

### Publisher crashes after publishing

The same outbox message may be published again. Consumers must be idempotent.

### Consumer crashes after processing

A retry may deliver the same event again. The consumer checks its processed-event record.

### Notification provider fails

Retry notification delivery. Do not reverse a completed payment solely because notification delivery failed.

### Risk service unavailable

Payment creation should follow an explicit policy. For this portfolio project, the default policy is fail closed for a payment requiring a risk decision.

## Deployment direction

AWS target:

```text
Angular
  -> S3 + CloudFront

API Gateway
  -> ECS/Fargate services

PostgreSQL
  -> Amazon RDS

Redis
  -> ElastiCache

RabbitMQ
  -> Amazon MQ or equivalent managed broker

Secrets
  -> AWS Secrets Manager

Logs/Metrics
  -> CloudWatch
```

This is the target architecture; local development will use Docker Compose.
