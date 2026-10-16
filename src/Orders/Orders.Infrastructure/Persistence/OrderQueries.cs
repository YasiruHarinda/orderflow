using BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore;
using Orders.Application.Abstractions;
using Orders.Application.GetOrder;
using Orders.Application.ListOrders;

namespace Orders.Infrastructure.Persistence;

/// <summary>
/// The read side. AsNoTracking() tells EF we will not change these objects,
/// which skips change tracking and makes reads faster.
/// </summary>
internal sealed class OrderQueries(OrdersDbContext db) : IOrderQueries
{
    public async Task<OrderResponse?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
        {
            return null;
        }

        return new OrderResponse(
            order.Id,
            order.CustomerId,
            order.Status.ToString(),
            order.Total.Amount,
            order.Currency,
            order.CreatedAt,
            order.RejectionReason,
            [.. order.Lines.Select(l => new OrderLineResponse(l.Sku, l.ProductName, l.UnitPrice.Amount, l.Quantity, l.LineTotal.Amount))]);
    }

    public async Task<PagedResult<OrderSummaryResponse>> ListByCustomerAsync(
        Guid customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = db.Orders.AsNoTracking().Where(o => o.CustomerId == customerId);

        var totalCount = await query.CountAsync(cancellationToken);

        // Only the columns the list needs are selected; the total is calculated in SQL.
        var rows = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                o.Id,
                o.Status,
                o.Currency,
                o.CreatedAt,
                Total = o.Lines.Sum(l => l.UnitPrice.Amount * l.Quantity),
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(r => new OrderSummaryResponse(r.Id, r.Status.ToString(), r.Total, r.Currency, r.CreatedAt))
            .ToList();

        return new PagedResult<OrderSummaryResponse>(items, page, pageSize, totalCount);
    }
}
