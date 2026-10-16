using BuildingBlocks.Application;

namespace Orders.Application.PlaceOrder;

/// <summary>"Customer X wants to buy these products." Returns the new order's Id.</summary>
public sealed record PlaceOrderCommand(Guid CustomerId, string Currency, IReadOnlyList<PlaceOrderLine> Lines)
    : ICommand<Guid>;

// Simplification: the client sends the price. A real shop would look prices up on the server.
public sealed record PlaceOrderLine(string Sku, string ProductName, decimal UnitPrice, int Quantity);
