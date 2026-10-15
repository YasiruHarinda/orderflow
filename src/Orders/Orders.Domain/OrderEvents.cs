using BuildingBlocks.Domain;

namespace Orders.Domain;

public sealed record OrderSubmittedDomainEvent(Guid OrderId, DateTimeOffset OccurredOn) : IDomainEvent;

public sealed record OrderConfirmedDomainEvent(Guid OrderId, DateTimeOffset OccurredOn) : IDomainEvent;

public sealed record OrderRejectedDomainEvent(Guid OrderId, string Reason, DateTimeOffset OccurredOn) : IDomainEvent;
