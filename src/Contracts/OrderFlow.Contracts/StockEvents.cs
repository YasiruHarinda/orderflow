namespace OrderFlow.Contracts;

/// <summary>Published by Inventory: every line of the order is reserved.</summary>
public sealed record StockReserved(Guid OrderId, DateTimeOffset OccurredOn);

/// <summary>Published by Inventory: the order could not be reserved (nothing was reserved).</summary>
public sealed record StockReservationFailed(Guid OrderId, string Reason, DateTimeOffset OccurredOn);
