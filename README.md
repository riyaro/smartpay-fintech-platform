# SmartPay — Event-Driven Digital Payment Platform

Portfolio/learning scaffold for a simulated SAR digital payments platform using .NET 8, C#, PostgreSQL, RabbitMQ, Docker, and an Angular starter.

**Safety scope:** no real banking, card, payment processor, or government KYC integrations. All funds and payments are simulated.

## Current status
- Six ASP.NET Core minimal API services
- Shared building-blocks library
- In-memory demo endpoints, including Payment Service idempotency behavior
- Docker Compose with separate PostgreSQL containers and RabbitMQ
- Architecture, requirements, ADRs, and frontend starter

**Important limitation:** the initial APIs store data in memory. Data resets on service restart. PostgreSQL and RabbitMQ are included for the next implementation phase but are not yet connected to the demo APIs. Persistent idempotency, transactional outbox, real event consumers, persistent ledger, authentication, and automated tests remain follow-up work.

## Build
Requires .NET 8 SDK:
```bash
dotnet restore SmartPay.sln
dotnet build SmartPay.sln
```

## Start infrastructure
```bash
docker compose -f deploy/docker-compose.yml up -d
```

## Run each API in its own terminal
```bash
dotnet run --project src/Services/Identity/SmartPay.IdentityService
dotnet run --project src/Services/Wallet/SmartPay.WalletService
dotnet run --project src/Services/Payment/SmartPay.PaymentService
dotnet run --project src/Services/Risk/SmartPay.RiskService
dotnet run --project src/Services/Ledger/SmartPay.LedgerService
dotnet run --project src/Services/Notification/SmartPay.NotificationService
```

## Payment demo
Payment API default URL: `http://localhost:5103`
```bash
curl -X POST http://localhost:5103/payments \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: demo-payment-001" \
  -d '{"customerWalletId":"wallet-customer-001","merchantId":"merchant-demo-001","amount":100,"currency":"SAR","reference":"Order-1001"}'
```
Health check: `GET /health`. Repeat the same request with the same key to see demo idempotency. Reusing a key with a different payload returns conflict. This is in-memory only, not production-grade.

## Structure
- `src/BuildingBlocks` — shared technical primitives
- `src/Services` — Identity, Wallet, Payment, Risk, Ledger, Notification
- `frontend/smartpay-web` — Angular starter files
- `deploy` — local PostgreSQL and RabbitMQ
- `docs` — requirements, architecture, and ADRs

## Roadmap
1. PostgreSQL + EF Core persistence
2. Authentication and simulated KYC
3. Wallet funding and reservation
4. Persistent idempotency and request fingerprints
5. Transactional outbox
6. RabbitMQ publishers/consumers, retries and dead-letter handling
7. Double-entry ledger persistence and invariants
8. Risk rules, notification consumers, audit events
9. Angular API integration
10. Tests, observability, and deployment hardening
