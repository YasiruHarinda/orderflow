using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using Orders.Application.Abstractions;

namespace Orders.Application.ListOrders;

internal sealed class ListOrdersQueryHandler(IOrderQueries queries)
    : IQueryHandler<ListOrdersQuery, PagedResult<OrderSummaryResponse>>
{
    public async Task<Result<PagedResult<OrderSummaryResponse>>> Handle(
        ListOrdersQuery query,
        CancellationToken cancellationToken) =>
        await queries.ListByCustomerAsync(query.CustomerId, query.Page, query.PageSize, cancellationToken);
}
