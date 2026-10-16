using BuildingBlocks.Application;
using Orders.Application.GetOrder;
using Orders.Application.ListOrders;

namespace Orders.Application.Abstractions;

/// <summary>
/// READ side: returns ready-to-send DTOs straight from the database,
/// without loading the domain model. Fast, and it can never change data.
/// </summary>
public interface IOrderQueries
{
    Task<OrderResponse?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task<PagedResult<OrderSummaryResponse>> ListByCustomerAsync(
        Guid customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
