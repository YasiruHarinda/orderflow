using Orders.Application.Abstractions;

namespace Orders.Infrastructure.Persistence;

internal sealed class UnitOfWork(OrdersDbContext db) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
