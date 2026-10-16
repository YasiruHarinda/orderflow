# 0010. Gateway API with Envoy Gateway instead of ingress-nginx

- **Status:** Accepted
- **Date:** 2026-10-16

## Context
The first design used ingress-nginx. The community ingress-nginx project was **retired in 2026**, and Kubernetes' recommended successor to the Ingress API is the **Gateway API** (`Gateway`, `HTTPRoute`).

## Decision
- **Envoy Gateway** implements the Gateway API in every cluster (k3d locally, AKS).
- One shared `Gateway` (one public IP) with one listener per environment (prod :80, dev :8080). The Helm chart only creates an `HTTPRoute` to the OrderFlow gateway service.
- **cert-manager** issues Let's Encrypt certificates for HTTPS listeners when a domain is configured.
- The OrderFlow **YARP gateway** stays: it is the application-level gateway (routing to services, rate limiting, health-checked destinations). Envoy is the cluster edge.

## Consequences
- ✅ Standard, portable API, with role separation (platform owns the Gateway, apps own their routes).
- ❌ Two gateway layers. Acceptable here; a larger setup could move rate limiting into Envoy policies.
