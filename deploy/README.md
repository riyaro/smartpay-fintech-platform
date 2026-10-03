# Local Infrastructure

Start databases and RabbitMQ:
`docker compose -f deploy/docker-compose.yml up -d`

These are local-only sample credentials. Current APIs are in-memory and are not connected to these containers yet.
RabbitMQ management UI: http://localhost:15672 (smartpay / local_dev_only).
