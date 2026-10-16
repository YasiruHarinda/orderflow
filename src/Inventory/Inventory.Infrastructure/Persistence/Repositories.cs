using BuildingBlocks.Application;
using Inventory.Application.Abstractions;
using Inventory.Application.Products;
using Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

internal sealed class ProductRepository(InventoryDbContext db) : IProductRepository
{
    public Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken) =>
        db.Products.SingleOrDefaultAsync(p => p.Sku == sku, cancellationToken);

    public async Task<IReadOnlyDictionary<string, Product>> GetBySkusAsync(
        IReadOnlyCollection<string> skus,
        CancellationToken cancellationToken) =>
        await db.Products
            .Where(p => skus.Contains(p.Sku))
            .ToDictionaryAsync(p => p.Sku, cancellationToken);
}

internal sealed class StockReservationRepository(InventoryDbContext db) : IStockReservationRepository
{
    public Task<StockReservation?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        db.StockReservations.SingleOrDefaultAsync(r => r.OrderId == orderId, cancellationToken);

    public void Add(StockReservation reservation) => db.StockReservations.Add(reservation);
}

internal sealed class UnitOfWork(InventoryDbContext db) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}

/// <summary>Reads straight from the database. Wrapped by <see cref="CachedProductQueries"/>.</summary>
internal sealed class ProductQueries(InventoryDbContext db) : IProductQueries
{
    public Task<ProductResponse?> GetBySkuAsync(string sku, CancellationToken cancellationToken) =>
        db.Products
            .AsNoTracking()
            .Where(p => p.Sku == sku)
            .Select(p => new ProductResponse(p.Sku, p.Name, p.Price, p.Currency, p.QuantityOnHand - p.QuantityReserved))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<ProductResponse>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var totalCount = await db.Products.CountAsync(cancellationToken);
        var items = await db.Products
            .AsNoTracking()
            .OrderBy(p => p.Sku)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductResponse(p.Sku, p.Name, p.Price, p.Currency, p.QuantityOnHand - p.QuantityReserved))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductResponse>(items, page, pageSize, totalCount);
    }
}
