using BuildingBlocks.Application;
using Orders.Application.Abstractions;
using Orders.Application.GetOrder;
using Orders.Application.ListOrders;
using Orders.Domain;

namespace Orders.UnitTests.Application;

// Fakes: tiny in-memory versions of the interfaces the Application layer needs.
// They let us test handlers without a real database.

internal sealed class FakeOrderRepository : IOrderRepository
{
    public List<Order> Orders { get; } = [];

    public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        Task.FromResult(Orders.SingleOrDefault(o => o.Id == orderId));

    public void Add(Order order) => Orders.Add(order);
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeOrderQueries : IOrderQueries
{
    public List<OrderResponse> Orders { get; } = [];

    public Task<OrderResponse?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        Task.FromResult(Orders.SingleOrDefault(o => o.Id == orderId));

    public Task<PagedResult<OrderSummaryResponse>> ListByCustomerAsync(
        Guid customerId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var matching = Orders.Where(o => o.CustomerId == customerId).ToList();
        var items = matching
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderSummaryResponse(o.Id, o.Status, o.Total, o.Currency, o.CreatedAt))
            .ToList();
        return Task.FromResult(new PagedResult<OrderSummaryResponse>(items, page, pageSize, matching.Count));
    }
}

/// <summary>A clock that always returns the same time, so tests give the same result every run.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
