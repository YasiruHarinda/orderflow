# Chaos testing

**Question:** if parts of the system crash while customers are ordering, do we lose orders or stock?
**Claim (from the design):** no. The outbox, inbox, retries, idempotent handlers and the saga timeout should make every order finish (Confirmed or Rejected), with every reserved unit either committed or released.

`scripts/chaos-test.sh` tests that claim. It runs the k6 load test, kills pods during it, waits, then checks two invariants directly in the databases.

## Run it (against dev)
```
az aks get-credentials --resource-group rg-orderflow --name aks-orderflow
GATEWAY_IP=$(kubectl -n envoy-gateway-system get svc -l gateway.envoyproxy.io/owning-gateway-name=orderflow -o jsonpath='{.items[0].status.loadBalancer.ingress[0].ip}')
scripts/chaos-test.sh orderflow-dev "http://$GATEWAY_IP:8080"
```
While it runs, watch Grafana (**OrderFlow – Overview**): error rate, queue depth, pod restarts.

## What happens, step by step
| Time | Fault | What should save us |
|---|---|---|
| +60 s | an `orders-api` pod is deleted | The other replica and the Service keep serving; `preStop` drain; requests in flight may fail and the client retries |
| +105 s | an `inventory-api` pod is deleted mid-message | The unacknowledged message is redelivered; the inbox and the unique reservation per order prevent double-reserving |
| +150 s | `rabbitmq-0` is deleted | Publishers keep writing to the outbox table; consumers reconnect; the outbox delivers the backlog when the broker is back |
| +210 s | `inventory-api` again | Same as above |
| end + 3 min | — | The timeout monitor rejects anything still waiting; `OrderRejected` releases its stock |

## Record the result
Fill in a postmortem from `docs/postmortems/TEMPLATE.md` **even if it passed**: write down what you saw (error-rate spike height, recovery time, queue peak, k6 summary). If an invariant failed, that's a real bug found, which is the most valuable outcome. Keep the k6 summary (`k6-chaos.log`) and Grafana screenshots with it.
