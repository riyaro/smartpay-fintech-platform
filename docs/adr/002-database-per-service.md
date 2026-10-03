# ADR 002 — Database per Service

**Status:** Accepted

Each service owns its persistence. No service directly queries or modifies another service's database. Cross-service information moves through APIs, events, or read models.
