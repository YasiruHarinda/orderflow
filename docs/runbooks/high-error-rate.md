# Runbook: high error rate (error budget burning)

**Alerts:** `OrderFlowErrorBudgetBurnFast` (critical), `OrderFlowErrorBudgetBurnSlow` (warning)
**Meaning:** a service returns too many 5xx responses for its 99.5% SLO.

## 1. Was something just deployed? (most incidents are)
```
kubectl -n argocd get applications            # recent sync?
kubectl argo rollouts get rollout orders-api -n orderflow-prod   # canary in progress?
```
- Canary in progress: Argo Rollouts aborts it automatically; check it did (`Status: Degraded`, stable pods serving).
- Recent sync of a plain Deployment: **roll back first, investigate second.** In the gitops repo:
  ```
  git revert <promotion commit>    # restores the previous image tag
  git push                         # ArgoCD deploys it within minutes
  ```

## 2. Which service, which endpoint?
Grafana → **OrderFlow – Overview** → *Error rate* panel, then Explore with:
```
sum by (http_route, http_response_status_code) (rate(http_server_request_duration_seconds_count{service_name="orders-api", http_response_status_code=~"5.."}[5m]))
```

## 3. Why?
- Grafana → Explore → **Tempo** → search `status = error` for the service → open a trace to see which call failed (database? RabbitMQ? downstream service?).
- From the trace, *Logs for this span* → Loki shows the exception.
- Readiness: `kubectl -n orderflow-prod get pods` — pods `0/1 READY` mean a dependency check failed (`/health/ready` shows which).

## 4. Common causes
| Symptom in traces/logs | Likely cause | Action |
|---|---|---|
| `Npgsql` timeouts | Database overloaded or unreachable | Azure portal → PostgreSQL metrics; scale SKU |
| `BrokerUnreachableException` | RabbitMQ down | `kubectl -n orderflow-prod get pods -l app.kubernetes.io/name=rabbitmq` |
| `DbUpdateConcurrencyException` bursts | Hot product, many concurrent orders | Usually retried automatically; check `_error` queues |
| `OutOfMemory` / restarts | Memory limit too low | Raise `resources.limits.memory` in gitops values |

## 5. Afterwards
Write a postmortem (`docs/postmortems/TEMPLATE.md`) for every critical alert.
