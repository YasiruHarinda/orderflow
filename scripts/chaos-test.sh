#!/usr/bin/env bash
# Chaos test: break things on purpose while customers are placing orders, then prove the
# system still keeps its promises. See docs/chaos-testing.md for how to run it and what
# to write down afterwards.
#
# While the k6 load test runs, this script:
#   - deletes an orders-api pod     (an API instance crashes mid-request)
#   - deletes an inventory-api pod  (a consumer crashes mid-message)
#   - deletes the rabbitmq pod      (the message broker goes away for ~30 seconds)
# Then it waits for everything to settle and checks the invariants:
#   1. no order is stuck in AwaitingStock      (every saga finished)
#   2. no stock is still reserved              (every reservation was committed or released)
#
# Usage: scripts/chaos-test.sh <namespace> <gateway-base-url>
#   scripts/chaos-test.sh orderflow-dev http://<gateway-ip>:8080
# Requires: kubectl (pointing at the cluster), k6, and the in-cluster PostgreSQL (dev).
set -euo pipefail

NS="${1:?namespace, e.g. orderflow-dev}"
BASE_URL="${2:?gateway URL, e.g. http://1.2.3.4:8080}"
SETTLE_SECONDS="${SETTLE_SECONDS:-180}" # > the stuck-order timeout (2 min), so timeouts can finish

log() { echo "[$(date +%H:%M:%S)] $*"; }
kill_one() { # $1 = label value of app.kubernetes.io/name
  local pod
  pod="$(kubectl -n "$NS" get pods -l "app.kubernetes.io/name=$1" -o name | shuf -n 1)"
  log "CHAOS: deleting $pod"
  kubectl -n "$NS" delete "$pod" --wait=false
}
sql() { # $1 = database, $2 = query
  kubectl -n "$NS" exec postgres-0 -- psql -U orderflow -d "$1" -tAc "$2"
}

log "Starting load test (5 minutes) against $BASE_URL"
k6 run -e BASE_URL="$BASE_URL" --quiet deploy/k6/place-orders.js > k6-chaos.log 2>&1 &
K6_PID=$!

sleep 60;  kill_one orders-api
sleep 45;  kill_one inventory-api
sleep 45;  kill_one rabbitmq
sleep 60;  kill_one inventory-api

log "Waiting for the load test to finish"
wait "$K6_PID" && K6_RESULT=passed || K6_RESULT="FAILED (see k6-chaos.log)"

log "Letting retries, outbox delivery and the timeout monitor finish (${SETTLE_SECONDS}s)"
sleep "$SETTLE_SECONDS"

log "Results"
echo "  k6 thresholds (SLOs): $K6_RESULT"
echo "  Orders by status:"
sql ordersdb 'SELECT "Status", COUNT(*) FROM orders.orders GROUP BY "Status" ORDER BY 1' | sed 's/^/    /'

stuck="$(sql ordersdb "SELECT COUNT(*) FROM orders.orders WHERE \"Status\" IN ('Submitted','AwaitingStock')")"
reserved="$(sql inventorydb 'SELECT COALESCE(SUM("QuantityReserved"),0) FROM inventory.products')"
echo "  Invariant 1 - orders not finished:      $stuck (expected 0)"
echo "  Invariant 2 - units still reserved:     $reserved (expected 0)"

if [[ "$stuck" == "0" && "$reserved" == "0" ]]; then
  log "PASSED: no order was lost or stuck, and no stock leaked, despite the failures."
else
  log "FAILED: write a postmortem (docs/postmortems/TEMPLATE.md)."
  exit 1
fi
