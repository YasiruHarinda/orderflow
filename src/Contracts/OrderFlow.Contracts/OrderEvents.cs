namespace OrderFlow.Contracts;

// Domain events (Part 2) are INTERNAL to one service. Integration events are the
// PUBLIC version that other services may depend on. Keeping them separate means a
// service can refactor its domain without breaking anyone else.

/// <summary>Published by Orders when a customer places an order. Inventory reserves stock.</summary>
public sealed record OrderSubmitted(Guid OrderId, Guid CustomerId, IReadOnlyList<OrderSubmittedLine> Lines, DateTimeOffset OccurredOn);

public sealed record OrderSubmittedLine(string Sku, int Quantity);

/// <summary>Published by Orders when an order is final and successful. Inventory ships the stock; Notifications tells the customer.</summary>
public sealed record OrderConfirmed(Guid OrderId, Guid CustomerId, decimal Total, string Currency, DateTimeOffset OccurredOn);

/// <summary>Published by Orders when an order fails or times out. Inventory releases stock; Notifications tells the customer.</summary>
public sealed record OrderRejected(Guid OrderId, Guid CustomerId, string Reason, DateTimeOffset OccurredOn);
