# 0004. CQRS with in-house handler interfaces instead of MediatR

- **Status:** Accepted
- **Date:** 2026-10-16

## Context
Commands (change data, go through the domain) and queries (read DTOs fast) are separated (CQRS). Cross-cutting behaviour such as validation should not be repeated in every handler. MediatR is the usual library for this, but **since v13 it requires a commercial licence** for many companies.

## Decision
- Two small interfaces in `BuildingBlocks.Application`: `ICommandHandler<TCommand, TResponse>` and `IQueryHandler<TQuery, TResponse>`.
- **Scrutor** registers every handler automatically and wraps it in a **validation decorator** (FluentValidation).
- Handlers are `internal sealed`, so nothing can call one directly and skip validation. An architecture test enforces this.
- Expected failures use a `Result` type, mapped to RFC 9457 problem details at the API edge.

## Consequences
- ✅ No licence risk, about 60 lines of code, and nothing hidden: every step is readable.
- ✅ New use cases need no registration code.
- ❌ No in-process publish/notification pipeline. Not needed: domain events are published through the outbox (0002).
