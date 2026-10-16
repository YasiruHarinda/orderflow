# Runbook: orders stuck or timing out

**Alerts:** `OrderFlowOrdersTimingOut`, `OrderFlowQueueBacklog`, `OrderFlowDeadLetters`
**Meaning:** the order saga is not completing. Orders are rejected by the timeout monitor, messages are piling up, or messages failed every retry.

## How the saga should flow
```
Orders --OrderSubmitted--> Inventory --StockReserved / StockReservationFailed--> Orders
Orders --OrderConfirmed / OrderRejected--> Inventory (commit / release), Notifications
```
If Inventory does not answer within 2 minutes, `StuckOrderMonitor` rejects the order, and the `OrderRejected` event releases any stock that was reserved.

## 1. Find where messages stop
RabbitMQ UI: `kubectl -n orderflow-prod port-forward svc/rabbitmq 15672` → http://localhost:15672 (user `orderflow`, password in Key Vault: `prod-rabbitmq-password`).

| Queue growing | Who should consume it | Check |
|---|---|---|
| `inventory-order-submitted` | inventory-api | Are inventory pods running and ready? |
| `orders-stock-reserved` / `orders-stock-reservation-failed` | orders-api | Are orders pods running? |
| `notifications-*` | notifications-worker | Worker running? |
| `*_error` | nobody: these failed all retries | Step 3 |

## 2. Is the outbox delivering?
Messages are first written to the `OutboxMessage` table, then delivered. Rows that never leave mean RabbitMQ is unreachable from the service:
```sql
SELECT COUNT(*) FROM orders."OutboxMessage";      -- should stay near 0
```

## 3. Messages in an `_error` queue
1. Open one in the RabbitMQ UI → read the `MT-Fault-Message` and `MT-Fault-StackTrace` headers.
2. Fix the cause (code bug → hotfix through the normal pipeline).
3. Move the messages back to the original queue (RabbitMQ UI → *Move messages*, shovel plugin). Handlers are idempotent, so reprocessing is safe.

## 4. Verify recovery
- Dashboard: *Rejections by reason* → `timeout` drops to 0; queue graph back to ~0.
- No order left unfinished:
  ```sql
  SELECT "Status", COUNT(*) FROM orders.orders GROUP BY 1;
  ```
