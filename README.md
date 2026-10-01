# SmartPay — Event-Driven Digital Payment Platform

SmartPay is a portfolio-grade digital payments platform designed to demonstrate production-style backend engineering with **C#, ASP.NET Core, Angular, PostgreSQL, Redis, RabbitMQ, Docker, and AWS-oriented architecture**.

> This project is for learning and portfolio purposes. It does **not** connect to real banks, cards, payment processors, or government KYC systems.

## What this project demonstrates

- Microservice architecture and service boundaries
- Clean Architecture principles
- Idempotent payment APIs
- Transactional Outbox Pattern
- Event-driven communication
- Double-entry accounting ledger
- Payment state machine
- Rule-based fraud/risk checks
- Async notifications
- PostgreSQL and Redis
- Unit and integration testing
- Docker-based local development
- AWS-oriented deployment design

## Business scenario

A customer can:

1. Register an account
2. Complete simulated KYC
3. Create a SAR wallet
4. Add simulated funds
5. Pay a merchant
6. Have the payment evaluated by risk rules
7. Move through a controlled payment lifecycle
8. Record completed money movement in an immutable-style double-entry ledger
9. Receive an asynchronous notification

## Architecture

```text
                         +-------------------+
                         |   Angular Web App |
                         +---------+---------+
                                   |
                                   v
                         +-------------------+
                         |    API Gateway    |
                         +---------+---------+
                                   |
              +--------------------+--------------------+
              |                    |                    |
              v                    v                    v
       +-------------+      +-------------+      +-------------+
       |  Identity   |      |   Wallet    |      |   Payment   |
       |  Service    |      |   Service   |      |   Service   |
       +-------------+      +-------------+      +------+------+
                                                        |
                                                        v
                                                 +-------------+
                                                 | Risk/Fraud  |
                                                 |   Service   |
                                                 +------+------+
                                                        |
                                                        v
                                               +----------------+
                                               | RabbitMQ/Event |
                                               |    Broker      |
                                               +---+---------+--+
                                                   |         |
                                                   v         v
                                            +----------+ +------------+
                                            |  Ledger  | |Notification|
                                            | Service  | |  Service   |
                                            +----------+ +------------+
```

## Core design decisions

### 1. Microservices

The project uses business boundaries instead of splitting code into arbitrary technical layers.

### 2. Database per service

Each service owns its data. A service does not directly read or write another service's database.

### 3. Idempotency

Payment creation accepts an `Idempotency-Key`. Repeating the same request with the same key must not create another payment.

### 4. Transactional Outbox

When Payment Service changes payment state, it writes the business change and an outbox message in the same database transaction. A background publisher later sends the event.

This protects against:

```text
Database commit succeeds
        +
Message publish fails
```

### 5. Double-entry ledger

A completed SAR 100 payment is represented as:

```text
Customer Wallet      Debit   100 SAR
Merchant Wallet      Credit 100 SAR
```

Every transaction must balance:

```text
Total Debits == Total Credits
```

The ledger is treated as the accounting record for completed money movement.

## Payment lifecycle

```text
Created
   |
   v
Pending
   |
   +------> Failed
   |
   v
Authorized
   |
   +------> Cancelled
   |
   v
Captured
   |
   v
Completed
   |
   v
Refunded
```

## Technology

- .NET 8
- C#
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- Redis
- RabbitMQ
- Angular
- Docker
- xUnit
- Moq
- Testcontainers
- OpenAPI / Swagger
- AWS-oriented deployment

## Repository structure

```text
smartpay-fintech-platform/
├── README.md
├── docs/
│   ├── requirements.md
│   ├── architecture.md
│   └── adr/
│       ├── 001-microservices.md
│       ├── 002-database-per-service.md
│       └── 003-idempotency-and-outbox.md
├── src/
│   ├── BuildingBlocks/
│   ├── Services/
│   │   ├── Identity/
│   │   ├── Wallet/
│   │   ├── Payment/
│   │   ├── Risk/
│   │   ├── Ledger/
│   │   └── Notification/
│   └── Gateway/
├── tests/
├── frontend/
└── deploy/
```

## Learning path

Build the project in this order:

1. Architecture and domain boundaries
2. .NET solution and shared building blocks
3. Identity Service
4. Wallet Service
5. Payment Service
6. Idempotency
7. Transactional Outbox
8. RabbitMQ events
9. Ledger Service
10. Risk Service
11. Notification Service
12. Angular frontend
13. Tests
14. Docker
15. AWS deployment

## Interview questions this project should prepare you for

- Why microservices instead of a modular monolith?
- Why database-per-service?
- How do you prevent duplicate payments?
- What happens if the database commit succeeds but RabbitMQ is unavailable?
- What happens when a consumer receives the same event twice?
- Why use a ledger instead of simply updating a balance?
- Where should a distributed transaction stop?
- How do you make payment processing idempotent?
- How do retries affect financial operations?
- How would you scale Payment Service?
- How would you monitor a payment across multiple services?

## Status

Phase 1 — architecture and requirements documentation.
