// k6 load test: customers browse products and place orders, while we check the SLOs.
//
//   k6 run deploy/k6/place-orders.js                          # against http://localhost:8080
//   k6 run -e BASE_URL=http://<gateway-ip>:8080 deploy/k6/place-orders.js   # AKS dev
//
// The gateway rate-limits each client IP (100 requests / 10 s by default). A load test
// sends everything from ONE IP, so run it against an environment with a higher limit
// (dev sets RateLimiting__PermitLimit in the gitops repo), or you will measure 429s.
//
// Thresholds = our SLOs. If any is broken, k6 exits with an error, so this can gate a
// pipeline: "the new version must survive this load".
import http from "k6/http";
import { check, sleep } from "k6";
import { Trend, Counter } from "k6/metrics";
import { uuidv4 } from "https://jslib.k6.io/k6-utils/1.4.0/index.js";

const BASE_URL = __ENV.BASE_URL || "http://localhost:8080";
const SKUS = ["KB-001", "MS-001", "MN-001", "HS-001", "CB-001"];
const JSON_HEADERS = { headers: { "Content-Type": "application/json" } };

// End-to-end time of the saga: POST /orders until the order is Confirmed or Rejected.
const orderCompletion = new Trend("order_completion_seconds", false);
const ordersConfirmed = new Counter("orders_confirmed");
const ordersRejected = new Counter("orders_rejected");

export const options = {
  scenarios: {
    browse: {
      executor: "constant-arrival-rate",
      exec: "browse",
      rate: 20,
      timeUnit: "1s",
      duration: "5m",
      preAllocatedVUs: 20,
    },
    order: {
      executor: "ramping-arrival-rate",
      exec: "placeOrder",
      startRate: 1,
      timeUnit: "1s",
      preAllocatedVUs: 50,
      maxVUs: 200,
      stages: [
        { target: 10, duration: "1m" }, // warm up
        { target: 30, duration: "3m" }, // peak: watch the HPA add pods in Grafana
        { target: 0, duration: "1m" }, // cool down
      ],
    },
  },
  thresholds: {
    http_req_failed: ["rate<0.005"], // SLO: 99.5% of requests succeed
    "http_req_duration{name:place-order}": ["p(95)<300"], // SLO: p95 < 300 ms
    "http_req_duration{name:list-products}": ["p(95)<200"],
    order_completion_seconds: ["p(95)<10"], // orders reach a final status quickly
  },
};

// Runs once before the test: plenty of stock, so we test throughput, not "out of stock".
export function setup() {
  for (const sku of SKUS) {
    const res = http.put(`${BASE_URL}/api/v1/products/${sku}/stock`, JSON.stringify({ quantityOnHand: 1000000 }), JSON_HEADERS);
    check(res, { "restocked": (r) => r.status === 204 });
  }
}

export function browse() {
  const res = http.get(`${BASE_URL}/api/v1/products?pageSize=20`, { tags: { name: "list-products" } });
  check(res, { "products listed": (r) => r.status === 200 });
}

export function placeOrder() {
  const sku = SKUS[Math.floor(Math.random() * SKUS.length)];
  const body = JSON.stringify({
    customerId: uuidv4(),
    currency: "USD",
    lines: [{ sku, productName: "Load test", unitPrice: 10.0, quantity: 1 + Math.floor(Math.random() * 3) }],
  });

  const started = Date.now();
  const res = http.post(`${BASE_URL}/api/v1/orders`, body, Object.assign({ tags: { name: "place-order" } }, JSON_HEADERS));
  if (!check(res, { "order accepted (202)": (r) => r.status === 202 })) {
    return;
  }

  // Follow 1 in 10 orders to its final status, to measure the whole saga.
  if (Math.random() < 0.1) {
    const orderId = res.json("orderId");
    for (let i = 0; i < 40; i++) {
      sleep(0.25);
      const status = http.get(`${BASE_URL}/api/v1/orders/${orderId}`, { tags: { name: "get-order" } }).json("status");
      if (status === "Confirmed" || status === "Rejected") {
        orderCompletion.add((Date.now() - started) / 1000);
        (status === "Confirmed" ? ordersConfirmed : ordersRejected).add(1);
        return;
      }
    }
  }
}
