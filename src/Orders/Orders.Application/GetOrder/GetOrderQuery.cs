using BuildingBlocks.Application;

namespace Orders.Application.GetOrder;

public sealed record GetOrderQuery(Guid OrderId) : IQuery<OrderResponse>;

/// <summary>What the API sends back for one order. A DTO: plain data, no behavior.</summary>
public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string Status,
    decimal Total,
    string Currency,
    DateTimeOffset CreatedAt,
    string? RejectionReason,
    IReadOnlyList<OrderLineResponse> Lines);

public sealed record OrderLineResponse(string Sku, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal);
