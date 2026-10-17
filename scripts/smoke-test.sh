#!/usr/bin/env bash
# End-to-end smoke test against a running OrderFlow (local k3d, kind in CI, or AKS):
#   1. the product catalogue is served
#   2. an order for an item in stock is CONFIRMED (Orders -> RabbitMQ -> Inventory -> Orders)
#   3. an order for an out-of-stock item is REJECTED
#
# Usage: scripts/smoke-test.sh [base-url]      (default http://localhost:8080)
set -euo pipefail

BASE_URL="${1:-http://localhost:8080}"
CUSTOMER_ID="$(cat /proc/sys/kernel/random/uuid 2>/dev/null || uuidgen)"

fail() { echo "FAIL: $*" >&2; exit 1; }

place_order() { # $1 = sku, prints the new order id
  curl -fsS -X POST "$BASE_URL/api/v1/orders" \
    -H "Content-Type: application/json" \
    -d "{\"customerId\":\"$CUSTOMER_ID\",\"currency\":\"USD\",\"lines\":[{\"sku\":\"$1\",\"productName\":\"Smoke test\",\"unitPrice\":10.0,\"quantity\":1}]}" \
    | jq -r '.orderId'
}

wait_for_status() { # $1 = order id, $2 = expected status
  for _ in $(seq 1 60); do
    status="$(curl -fsS "$BASE_URL/api/v1/orders/$1" | jq -r '.status')"
    [[ "$status" == "$2" ]] && { echo "  order $1 is $status"; return 0; }
    sleep 2
  done
  fail "order $1 is '$status', expected '$2'"
}

echo "0. Web store"
home="$(curl -fsS "$BASE_URL/")"
[[ "$home" == *'<div id="root">'* ]] || fail "the gateway does not serve the web store at /"
deep_link="$(curl -fsS "$BASE_URL/orders/smoke-test")"
[[ "$deep_link" == *'<div id="root">'* ]] || fail "deep links do not fall back to the web store"
echo "  store and deep links are served"

echo "1. Product catalogue"
count="$(curl -fsS "$BASE_URL/api/v1/products?pageSize=50" | jq '.totalCount')"
[[ "$count" -ge 1 ]] || fail "expected products, got $count"
echo "  $count products"

echo "2. Order an item in stock (KB-001)"
wait_for_status "$(place_order KB-001)" Confirmed

echo "3. Order an item with no stock (LT-001)"
wait_for_status "$(place_order LT-001)" Rejected

echo "Smoke test passed."
