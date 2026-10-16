using BuildingBlocks.Messaging;
using Inventory.Application.Abstractions;
using Inventory.Infrastructure.Caching;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Inventory.Infrastructure;

public static class DependencyInjection
{
    public const string DatabaseName = "inventorydb";
    public const string RedisName = "redis";

    public static IHostApplicationBuilder AddInventoryInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDbContext<InventoryDbContext>(options => options
            .UseNpgsql(
                builder.Configuration.GetConnectionString(DatabaseName),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", InventoryDbContext.Schema))
            .UseSeeding((context, _) => InventorySeed.Seed(context))
            .UseAsyncSeeding((context, _, ct) => InventorySeed.SeedAsync(context, ct)));
        builder.EnrichNpgsqlDbContext<InventoryDbContext>(settings => settings.DisableRetry = true);

        builder.Services.AddScoped<IProductRepository, ProductRepository>();
        builder.Services.AddScoped<IStockReservationRepository, StockReservationRepository>();
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Caching: Redis is the shared L2 cache when configured. Without it (e.g. in
        // some tests) HybridCache still works with its in-memory L1 cache only.
        if (!string.IsNullOrEmpty(builder.Configuration.GetConnectionString(RedisName)))
        {
            builder.AddRedisDistributedCache(RedisName);
        }

        builder.Services.AddHybridCache(options => options.DefaultEntryOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(1),
            LocalCacheExpiration = TimeSpan.FromSeconds(30),
        });

        builder.Services.AddScoped<ProductQueries>();
        builder.Services.AddScoped<IProductQueries, CachedProductQueries>();
        builder.Services.AddScoped<IProductCache, ProductCache>();

        builder.AddMessaging<InventoryDbContext>("inventory", typeof(DependencyInjection).Assembly);

        return builder;
    }

    public static async Task MigrateInventoryDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await db.Database.MigrateAsync(cancellationToken); // also runs the seeding above
    }
}
