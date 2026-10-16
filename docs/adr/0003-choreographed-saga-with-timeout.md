# 0003. Choreographed saga with a timeout monitor

- **Status:** Accepted
- **Date:** 2026-10-16

## Context
Placing an order spans two services: Orders (the order) and Inventory (the stock). There is no distributed transaction, so the process must be a **saga**: a sequence of local transactions, with **compensation** if a later step fails.

Two styles exist:
- **Orchestration**: a central state machine sends commands (MassTransit sagas). Timeouts need a message scheduler (a RabbitMQ plugin or Quartz).
- **Choreography**: each service reacts to events; no central coordinator.

## Decision
**Choreography**, with the **Order aggregate as the state machine** (`Submitted → AwaitingStock → Confirmed | Rejected`):

1. Orders publishes `OrderSubmitted` (outbox).
2. Inventory reserves **all or nothing** and replies `StockReserved` or `StockReservationFailed`.
3. Orders confirms or rejects and publishes `OrderConfirmed` / `OrderRejected`.
4. Inventory **commits** stock on `OrderConfirmed` and **releases** it on `OrderRejected` (compensation).
5. A **`StuckOrderMonitor`** rejects orders that waited longer than 2 minutes. Its `OrderRejected` triggers the same compensation.

Out-of-order messages are handled: a release that arrives *before* the reservation leaves a **tombstone**, so the late reservation is refused.

## Consequences
- ✅ No extra infrastructure (no scheduler plugin); the rules stay in the domain model and are unit-tested.
- ✅ Several monitor instances can run at once: optimistic concurrency (`xmin`) lets only one change an order.
- ❌ The flow is spread over several consumers; you need traces (Tempo) to see it end to end.
- ❌ With many more steps, an orchestrated state machine would be easier to follow. Revisit if the flow grows (e.g. payments).
