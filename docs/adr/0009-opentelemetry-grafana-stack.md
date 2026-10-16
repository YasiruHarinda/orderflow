# 0009. OpenTelemetry through a collector, Grafana LGTM stack

- **Status:** Accepted
- **Date:** 2026-10-16
- Changes the first draft, which had each service expose a Prometheus `/metrics` endpoint.

## Context
Services must produce logs, metrics and traces that can be correlated: from an alert, to the slow trace, to the log lines of that request.

## Decision
- Services use only the **OpenTelemetry** SDK (ServiceDefaults) and send **OTLP** to one address: the **OpenTelemetry Collector**.
- The collector routes **metrics → Prometheus** (remote write), **traces → Tempo**, **logs → Loki**. Grafana shows all three with links between them (exemplars, trace ↔ logs).
- Resource attributes (`service.name`, `service.version`, `deployment.environment.name`) become labels, used by dashboards, alerts and canary analysis.
- Business metrics (`orderflow_orders_*`) next to technical ones; low-cardinality tags only.
- The Prometheus `/metrics` exporter for .NET was still in beta, another reason to push through the collector.

## Consequences
- ✅ Changing a backend (e.g. to Azure Monitor) is a collector change, not an application change.
- ✅ Locally, the same telemetry goes to the Aspire dashboard or the `otel-lgtm` container.
- ❌ The collector is in the critical path for telemetry; it has a memory limiter, and the apps keep working if it is down (telemetry is dropped, not requests).
