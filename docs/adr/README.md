# Architecture Decision Records

An ADR records one important decision: the situation, what we chose, what we gave up. Decisions are never edited later; a new ADR *supersedes* an old one. This keeps the reasoning visible for the next person (or the next interview).

| # | Decision | Status |
|---|---|---|
| [0001](0001-microservices-database-per-service.md) | Microservices with a database per service | Accepted |
| [0002](0002-masstransit-v8-transactional-outbox.md) | MassTransit v8 + RabbitMQ with a transactional outbox | Accepted |
| [0003](0003-choreographed-saga-with-timeout.md) | Choreographed saga with a timeout monitor | Accepted |
| [0004](0004-cqrs-without-mediatr.md) | CQRS with in-house handler interfaces instead of MediatR | Accepted |
| [0005](0005-aspire-local-helm-kubernetes.md) | .NET Aspire for local development, Helm for Kubernetes | Accepted |
| [0006](0006-single-aks-cluster-namespaces.md) | One AKS cluster, namespace per environment, managed PostgreSQL for prod | Accepted |
| [0007](0007-gitops-argocd-separate-repo.md) | GitOps with ArgoCD and a separate gitops repository | Accepted |
| [0008](0008-canary-argo-rollouts.md) | Canary releases with Argo Rollouts and Prometheus analysis | Accepted |
| [0009](0009-opentelemetry-grafana-stack.md) | OpenTelemetry through a collector, Grafana LGTM stack | Accepted |
| [0010](0010-gateway-api-envoy.md) | Gateway API with Envoy Gateway instead of ingress-nginx | Accepted |
| [0011](0011-ghcr-cosign-supply-chain.md) | GHCR, Trivy, SBOM and cosign for the supply chain | Accepted |
| [0012](0012-authentication-deferred.md) | End-user authentication deferred | Accepted (temporary) |

Format: *Context → Decision → Consequences* (Michael Nygard).
