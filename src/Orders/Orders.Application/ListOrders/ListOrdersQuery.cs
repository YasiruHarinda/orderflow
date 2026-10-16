using BuildingBlocks.Application;

namespace Orders.Application.ListOrders;

public sealed record ListOrdersQuery(Guid CustomerId, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<OrderSummaryResponse>>;

/// <summary>A short version of an order, for lists. No lines, so it stays small and fast.</summary>
public sealed record OrderSummaryResponse(Guid Id, string Status, decimal Total, string Currency, DateTimeOffset CreatedAt);
