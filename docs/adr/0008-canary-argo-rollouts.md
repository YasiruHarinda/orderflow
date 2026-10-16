# 0008. Canary releases with Argo Rollouts and Prometheus analysis

- **Status:** Accepted
- **Date:** 2026-10-16

## Context
A rolling update replaces every pod with the new version. If the new version is broken in a way tests did not catch, every user is affected until someone notices and rolls back.

## Decision
- In **prod**, `orders-api` is an Argo **Rollout** instead of a Deployment. Steps: 20% for 3 minutes → 50% for 3 minutes → 100%.
- An **AnalysisTemplate** queries Prometheus every minute **for the new version only** (`service_version` label): success rate ≥ 99% and p95 latency ≤ 500 ms. Two failed checks → **automatic rollback**.
- Traffic is split by replica count (no service mesh needed).
- Dev uses plain rolling updates (fast feedback).

## Consequences
- ✅ A bad release reaches a fraction of users for minutes, and is rolled back without a human.
- ✅ The decision is based on the same SLO metrics as the alerts.
- ❌ Replica-based splitting is coarse (with 2 replicas, 20% really means 1 pod). Gateway API traffic routing would give exact percentages.
- ❌ Releases take about 6 minutes longer.
