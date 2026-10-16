# 0002. MassTransit v8 + RabbitMQ with a transactional outbox

- **Status:** Accepted
- **Date:** 2026-10-16

## Context
When an order is saved, Inventory must be told. Saving (PostgreSQL) and sending (RabbitMQ) are two different systems, so a crash between them either loses the message or announces an order that was never saved. RabbitMQ can also deliver the same message twice.

MassTransit is the most widely used .NET messaging library. **From v9 it requires a commercial licence**; v8 stays Apache 2.0.

## Decision
- **RabbitMQ** as the broker; **MassTransit 8.5.x**. Dependabot is told to ignore MassTransit major updates.
- **Transactional outbox** (MassTransit EF Core outbox): `Publish` writes an `OutboxMessage` row in the *same database transaction* as the business change; a background process delivers it.
- **Inbox**: each received message id is stored, so duplicates are skipped.
- On top of that, handlers are **idempotent by design** (e.g. one `StockReservation` per order, enforced by a unique index).
- **Retries** with back-off (100 ms → 10 s); after the last retry a message goes to an `_error` queue, which has an alert.

## Consequences
- ✅ "Save and publish" is atomic: no lost or phantom messages.
- ✅ Duplicate deliveries are harmless.
- ❌ Messages are delivered slightly later (polling the outbox), and the database stores message rows.
- ❌ Staying on v8 means planning a migration (v9 licence, or another library such as Wolverine or Rebus) before v8 support ends.
