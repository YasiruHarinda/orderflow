# OrderFlow — 7-Day Implementation Plan

See [DESIGN.md](DESIGN.md) for the architecture. Each day ends with a **working, committed, pushed** state. Items marked ⭐ are stretch goals; skip them if the day runs long.

## Status (2026-10-16)

The code was built in 17 parts, one commit each (`git log --oneline`):

| Part | Content | Status |
|---|---|---|
| 1–3 | Solution, Order domain, CQRS application layer | ✅ |
| 4–5 | EF Core + API, Aspire + ServiceDefaults | ✅ |
| 6–9 | Inventory, messaging + outbox, saga + notifications, YARP gateway | ✅ |
| 10 | Architecture + Testcontainers integration tests | ✅ |
| 11–12 | Dockerfiles + compose, GitHub Actions CI/CD | ✅ |
| 13 | Helm chart, k3d script, kind e2e pipeline | ✅ |
| 14 | Terraform (bootstrap + main) and pipeline | ✅ validated, **not yet applied** |
| 15–16 | GitOps repo (ArgoCD, platform), observability | ✅ rendered and validated, **not yet deployed** |
| 17 | k6, chaos test, ADRs, runbooks, README | ✅ |

**Still to do (needs your accounts and machine):**
- [ ] Push both repos; check the CI, Kubernetes and CodeQL workflows are green
- [ ] Install Docker Desktop; run `dotnet run --project src/AppHost` and `./scripts/k3d-up.ps1` locally
- [ ] Azure: run bootstrap, set the GitHub variables and the `GITOPS_TOKEN` secret, apply ([infra/README.md](../infra/README.md))
- [ ] Run the chaos test in dev and write the first postmortem with real numbers ([chaos-testing.md](chaos-testing.md))
- [ ] Grafana screenshots + a 3-minute demo video for the README
- [ ] Stretch: authentication (ADR-0012), Kyverno signature enforcement (ADR-0011), KEDA, preview environments

## Day 0 — Setup (≈1–2 hours, before Day 1)

Install (all via `winget`):

| Tool | Command |
|---|---|
| .NET 10 SDK | `winget install Microsoft.DotNet.SDK.10` |
| Docker Desktop | `winget install Docker.DockerDesktop` (enable WSL 2) |
| Azure CLI | `winget install Microsoft.AzureCLI` |
| Terraform | `winget install Hashicorp.Terraform` |
| kubectl | `winget install Kubernetes.kubectl` |
| Helm | `winget install Helm.Helm` |
| k3d | `winget install k3d` |
| GitHub CLI | `winget install GitHub.cli` |
| k6 | `winget install k6` |

Accounts:
- [ ] GitHub repos: `orderflow` (public) and `orderflow-gitops` (public)
- [ ] Azure subscription (free trial or Azure for Students credit) + budget alert at $20
- [ ] SonarCloud account linked to GitHub

**Done when:** `dotnet --version`, `docker run hello-world`, `az login`, `terraform -v` all work.

---

## Day 1 — Solution foundation + Orders service

- [ ] Solution skeleton, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `global.json`
- [ ] `ServiceDefaults` (OpenTelemetry, health checks, resilience defaults)
- [ ] `AppHost` (Aspire) with Postgres, RabbitMQ, Redis resources
- [ ] `BuildingBlocks`: `Result<T>`, `Error`, `Entity`, `AggregateRoot`, domain events, message contracts
- [ ] `Orders.Domain`: `Order` aggregate, `OrderLine`, `Money`, status rules
- [ ] `Orders.Application`: `PlaceOrder`, `GetOrder`, `ListOrders` (CQRS handlers) + FluentValidation decorator
- [ ] `Orders.Infrastructure`: EF Core DbContext, configurations, first migration
- [ ] `Orders.API`: Minimal API endpoints, versioning, ProblemDetails, OpenAPI + Scalar
- [ ] Unit tests for domain and handlers; architecture tests

**Done when:** `dotnet run --project src/AppHost` shows Orders.API healthy in the Aspire dashboard and you can create/read an order via Scalar.

## Day 2 — Inventory, messaging, saga, gateway

- [ ] `Inventory` service (same layering): products, stock, reservation with optimistic concurrency
- [ ] HybridCache + Redis for product reads
- [ ] MassTransit + RabbitMQ in both services; EF Core outbox/inbox
- [ ] Order saga state machine (Submitted → AwaitingStock → Confirmed/Rejected + timeout)
- [ ] `Notifications.Worker` consuming `OrderConfirmed` / `OrderRejected`
- [ ] `Gateway` with YARP routes, JWT auth, rate limiting
- [ ] Integration tests with Testcontainers (outbox delivers, saga completes, out-of-stock path)
- [ ] ⭐ BenchmarkDotNet project for a hot path

**Done when:** placing an order through the gateway ends in `Confirmed` (or `Rejected` when stock is short), and one distributed trace in Aspire spans all four services.

## Day 3 — Containers + CI

- [ ] Multi-stage Dockerfiles (chiseled base, non-root) for all 4 services
- [ ] `deploy/docker/docker-compose.yml` for running without Aspire
- [ ] `.github/workflows/ci.yml`: build, test, coverage gate, SonarCloud, hadolint
- [ ] `.github/workflows/release.yml`: build images, Trivy, Syft SBOM, push to GHCR (ACR comes on Day 5)
- [ ] Dependabot config, gitleaks, branch protection on `main`
- [ ] README badges (build, coverage, quality gate)

**Done when:** a PR shows green checks and a merge to `main` publishes scanned images.

## Day 4 — Kubernetes locally (k3d + Helm)

- [ ] Umbrella Helm chart `deploy/helm/orderflow` with sub-charts per service
- [ ] Probes, resources, security context, HPA, PDB, NetworkPolicies
- [ ] EF Core migration bundle as a Helm pre-upgrade hook Job
- [ ] Dependencies via Helm: PostgreSQL, RabbitMQ, Redis, ingress-nginx
- [ ] k3d cluster script `scripts/k3d-up.ps1`
- [ ] `helm lint` + `helm template` + kubeconform added to CI

**Done when:** `helm install` on k3d gives a working system reachable through ingress.

## Day 5 — Azure infrastructure + GitOps

- [ ] Terraform bootstrap (remote state storage)
- [ ] Modules: network, AKS (workload identity + OIDC), ACR, Key Vault
- [ ] GitHub → Azure OIDC federation; `infra.yml` workflow (plan on PR, apply on merge with approval)
- [ ] ArgoCD install, app-of-apps in `orderflow-gitops` with `dev` and `prod` overlays
- [ ] External Secrets Operator + Key Vault
- [ ] `release.yml` pushes to ACR and commits the new image tag to the gitops repo
- [ ] Argo Rollouts canary for Orders.API
- [ ] ⭐ cert-manager + TLS, cosign signing

**Done when:** merging to `main` deploys to `dev` on AKS automatically, and merging the promotion PR deploys to `prod` with a canary.

> 💰 Run `terraform destroy` at the end of the day.

## Day 6 — Observability + load testing

- [ ] kube-prometheus-stack, Loki, Tempo, OTel Collector (Helm, via ArgoCD)
- [ ] Grafana dashboards as code: golden signals, business metrics, RabbitMQ
- [ ] Custom metrics: `orderflow_orders_placed_total`, saga duration histogram
- [ ] SLO + burn-rate alert rules
- [ ] Canary `AnalysisTemplate` using Prometheus (error rate, p95)
- [ ] k6 scenario; run it and capture HPA scaling + dashboards
- [ ] ⭐ KEDA scaling Notifications.Worker on queue depth
- [ ] ⭐ Deliberately ship a bad version and screenshot the automatic rollback

**Done when:** a load test shows autoscaling on dashboards, and a trace can be followed from gateway to notification in Grafana.

## Day 7 — Reliability, docs, and showcase

- [ ] Chaos run: kill pods/broker during load, verify all orders reach a final state
- [ ] Write `docs/postmortems/0001-*.md`
- [ ] ADRs 0001–0008
- [ ] Runbooks: stuck saga, high error rate, failed rollout, secret rotation
- [ ] DR drill: destroy + rebuild, record the time
- [ ] Final README: architecture diagram, screenshots, "how to run in 5 minutes", tech list
- [ ] 3-minute demo video (push code → canary → dashboards → rollback)
- [ ] Update CV and LinkedIn; pin the repo on GitHub

**Done when:** a stranger can read the README, understand the system in 2 minutes, and run it locally in 5.

---

## Progress log

| Day | Date | Done | Notes / blockers |
|---|---|---|---|
| 0 | | | |
| 1 | | | |
| 2 | | | |
| 3 | | | |
| 4 | | | |
| 5 | | | |
| 6 | | | |
| 7 | | | |
