using Orders.Domain;

namespace Orders.Application.Abstractions;

/// <summary>
/// WRITE side: loads and adds whole Order aggregates.
/// Defined here, implemented in Orders.Infrastructure with EF Core (Part 4).
/// </summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken);

    void Add(Order order);
}
