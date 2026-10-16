using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using Orders.Application.Abstractions;
using Orders.Domain;

namespace Orders.Application.GetOrder;

internal sealed class GetOrderQueryHandler(IOrderQueries queries) : IQueryHandler<GetOrderQuery, OrderResponse>
{
    public async Task<Result<OrderResponse>> Handle(GetOrderQuery query, CancellationToken cancellationToken)
    {
        var order = await queries.GetByIdAsync(query.OrderId, cancellationToken);
        if (order is null)
        {
            return OrderErrors.NotFound(query.OrderId);
        }

        return order;
    }
}
