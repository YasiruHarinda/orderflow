# 0001. Microservices with a database per service

- **Status:** Accepted
- **Date:** 2026-10-14

## Context
OrderFlow has three areas that change for different reasons: taking orders, managing stock, and telling customers what happened. The project's goal is also to practise the operational side of distributed systems (deployments, messaging, observability), which a single process would not exercise.

## Decision
- Three services, **Orders**, **Inventory** and **Notifications**, plus a **Gateway**.
- Each service **owns its data**: Orders and Inventory each have their own PostgreSQL database. No service reads another service's tables.
- Services talk **only through messages** (`OrderFlow.Contracts`); an architecture test fails the build if Orders and Inventory reference each other's code.
- Inside each service: **Clean Architecture** (Domain → Application → Infrastructure → API).

## Consequences
- ✅ Services deploy, scale and fail independently. Inventory being down does not stop customers placing orders.
- ✅ A schema change in one database cannot break another service.
- ❌ No cross-service transactions or joins. Consistency is **eventual** and needs patterns (outbox, saga, idempotency). See 0002 and 0003.
- ❌ More moving parts to run and observe. That cost is accepted because practising this is the point of the project. For a small real product, a *modular monolith* would usually be the better first step.
