using Microsoft.EntityFrameworkCore;
using Orders.Application.Abstractions;
using Orders.Domain;

namespace Orders.Infrastructure.Persistence;

internal sealed class OrderRepository(OrdersDbContext db) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        db.Orders
            .Include(o => o.Lines)
            .SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken);

    public void Add(Order order) => db.Orders.Add(order);

    // Uses the (Status, CreatedAt) index from OrderConfiguration.
    public async Task<IReadOnlyList<Guid>> GetIdsAwaitingStockAsync(
        DateTimeOffset createdBefore,
        int maxCount,
        CancellationToken cancellationToken) =>
        await db.Orders
            .Where(o => o.Status == OrderStatus.AwaitingStock && o.CreatedAt < createdBefore)
            .OrderBy(o => o.CreatedAt)
            .Select(o => o.Id)
            .Take(maxCount)
            .ToListAsync(cancellationToken);
}
