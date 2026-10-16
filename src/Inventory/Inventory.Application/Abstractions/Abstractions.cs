using BuildingBlocks.Application;
using Inventory.Application.Products;
using Inventory.Domain;

namespace Inventory.Application.Abstractions;

// Contracts the Application layer needs. Inventory.Infrastructure implements them.

public interface IProductRepository
{
    Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, Product>> GetBySkusAsync(IReadOnlyCollection<string> skus, CancellationToken cancellationToken);
}

public interface IStockReservationRepository
{
    Task<StockReservation?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    void Add(StockReservation reservation);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Read side. The implementation is cached, so results can be a few seconds old.</summary>
public interface IProductQueries
{
    Task<ProductResponse?> GetBySkuAsync(string sku, CancellationToken cancellationToken);

    Task<PagedResult<ProductResponse>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
}

/// <summary>Called after stock changes, so readers do not see old numbers for long.</summary>
public interface IProductCache
{
    Task InvalidateAsync(CancellationToken cancellationToken);
}
