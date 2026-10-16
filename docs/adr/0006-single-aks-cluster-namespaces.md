# 0006. One AKS cluster, a namespace per environment, managed PostgreSQL for prod

- **Status:** Accepted
- **Date:** 2026-10-16

## Context
The project runs on personal Azure credit, so cost matters. Real companies separate environments into different clusters (or subscriptions) for isolation.

## Decision
- **One AKS cluster** with `orderflow-dev` and `orderflow-prod` **namespaces**, isolated by NetworkPolicies, Pod Security "restricted", separate secrets and separate data stores.
- Burstable nodes (`Standard_B2s_v2`), autoscaling 2–4, Free tier control plane.
- **Prod uses Azure Database for PostgreSQL** (backups, point-in-time restore, patching). **Dev uses in-cluster PostgreSQL** (cheap, disposable). RabbitMQ and Redis run in-cluster for both.
- A **budget alert** and a one-click **destroy** workflow. The cluster is destroyed every evening; everything can be recreated from code.

## Consequences
- ✅ About the cost of a few coffees per month when destroyed nightly.
- ❌ Dev and prod share nodes and the Kubernetes control plane: a noisy dev workload or a cluster upgrade affects prod. Real production would use **separate clusters** (the Terraform modules already support a second instance).
- ❌ In-cluster RabbitMQ is a single node. Production would use the RabbitMQ Cluster Operator (3 nodes, quorum queues) or Azure Service Bus.
