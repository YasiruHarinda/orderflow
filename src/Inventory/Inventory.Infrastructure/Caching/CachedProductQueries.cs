using BuildingBlocks.Application;
using Inventory.Application.Abstractions;
using Inventory.Application.Products;
using Inventory.Infrastructure.Persistence;
using Microsoft.Extensions.Caching.Hybrid;

namespace Inventory.Infrastructure.Caching;

/// <summary>
/// Decorator that adds caching to product reads.
/// HybridCache has two levels: L1 = memory inside this pod (fastest), L2 = Redis
/// (shared by all pods). It also prevents a "stampede": if 100 requests miss the
/// cache at once, only ONE goes to the database and the other 99 wait for it.
/// </summary>
internal sealed class CachedProductQueries(ProductQueries inner, HybridCache cache) : IProductQueries
{
    public const string Tag = "products";
    private static readonly string[] Tags = [Tag];

    public async Task<ProductResponse?> GetBySkuAsync(string sku, CancellationToken cancellationToken) =>
        await cache.GetOrCreateAsync(
            $"products:sku:{sku}",
            async ct => await inner.GetBySkuAsync(sku, ct),
            tags: Tags,
            cancellationToken: cancellationToken);

    public async Task<PagedResult<ProductResponse>> ListAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        await cache.GetOrCreateAsync(
            $"products:list:{page}:{pageSize}",
            async ct => await inner.ListAsync(page, pageSize, ct),
            tags: Tags,
            cancellationToken: cancellationToken);
}

/// <summary>Stock changed: drop every cached product entry, in memory and in Redis.</summary>
internal sealed class ProductCache(HybridCache cache) : IProductCache
{
    public async Task InvalidateAsync(CancellationToken cancellationToken) =>
        await cache.RemoveByTagAsync(CachedProductQueries.Tag, cancellationToken);
}
