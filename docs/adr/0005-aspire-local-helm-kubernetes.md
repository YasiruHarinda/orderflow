# 0005. .NET Aspire for local development, Helm for Kubernetes

- **Status:** Accepted
- **Date:** 2026-10-16

## Context
Developers need the whole system (4 services, PostgreSQL, RabbitMQ, Redis) running in seconds. Kubernetes needs production-grade manifests. Aspire can also generate deployment artefacts, but generated manifests hide the details this project wants to show and control.

## Decision
- **Local inner loop: .NET Aspire** (`dotnet run --project src/AppHost`): containers, connection strings, and a dashboard with logs, traces and metrics.
- **Containers end to end: `compose.yaml`** with the real Dockerfiles and a Grafana LGTM container.
- **Kubernetes: a hand-written Helm chart**, one generic chart where each service is a values entry. The same chart is used locally (k3d), in CI (kind) and in AKS.
- **ServiceDefaults** gives every service the same OpenTelemetry, health checks (`/health/live`, `/health/ready`) and HTTP resilience.

## Consequences
- ✅ Fast local feedback, and identical deployment logic in every cluster.
- ✅ Every production setting (probes, security context, PDBs, NetworkPolicies) is explicit and reviewable.
- ❌ Two descriptions of the system (AppHost and Helm) must be kept in step. The kind end-to-end test in CI catches drift.
