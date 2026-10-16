using BuildingBlocks.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orders.Application.Abstractions;
using Orders.Infrastructure.Background;
using Orders.Infrastructure.Persistence;

namespace Orders.Infrastructure;

public static class DependencyInjection
{
    public const string DatabaseName = "ordersdb";

    public static IHostApplicationBuilder AddOrdersInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDbContext<OrdersDbContext>(options =>
            options.UseNpgsql(
                builder.Configuration.GetConnectionString(DatabaseName),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", OrdersDbContext.Schema)));

        // Adds the database health check, tracing and metrics from Aspire.
        // EF's own retries are off: they conflict with the explicit transactions the
        // messaging outbox uses (Part 7), and message retries cover transient errors.
        builder.EnrichNpgsqlDbContext<OrdersDbContext>(settings => settings.DisableRetry = true);

        builder.Services.AddScoped<IOrderRepository, OrderRepository>();
        builder.Services.AddScoped<IOrderQueries, OrderQueries>();
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

        builder.AddMessaging<OrdersDbContext>("orders", typeof(DependencyInjection).Assembly);

        builder.Services.AddOptions<StuckOrderOptions>().BindConfiguration(StuckOrderOptions.Section);
        builder.Services.AddHostedService<StuckOrderMonitor>();

        return builder;
    }

    /// <summary>Brings the database schema up to date by applying any pending migrations.</summary>
    public static async Task MigrateOrdersDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}
