# 0012. End-user authentication deferred

- **Status:** Accepted (temporary)
- **Date:** 2026-10-16

## Context
The first design had JWT validation at the gateway (Keycloak or Entra ID). Doing it properly needs an identity provider, token flows for the load tests, and role checks for the stock endpoint. The project's focus is the platform.

## Decision
- The public API is **not authenticated** in this version. Mitigations: rate limiting per client IP, validation of every input, NetworkPolicies so only the gateway reaches the services, and no personal data stored (customers are random GUIDs).
- `PUT /products/{sku}/stock` is an admin operation and is **the first thing to protect**.

## Consequences
- ❌ Anyone who can reach the gateway can place orders or change stock. **This must not go to real production.**
- Plan: Entra ID (or Keycloak via Aspire locally) → JWT bearer validation in the YARP gateway with an `admin` policy on the stock route → forward the user id to the services.
