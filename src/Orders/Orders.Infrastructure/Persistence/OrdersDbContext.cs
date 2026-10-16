using BuildingBlocks.Messaging;
using Microsoft.EntityFrameworkCore;
using Orders.Domain;

namespace Orders.Infrastructure.Persistence;

/// <summary>
/// The EF Core "session" with the orders database. Each service owns its own database;
/// no other service may read these tables.
/// </summary>
public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public const string Schema = "orders";

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        // Picks up every IEntityTypeConfiguration class in this project.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);

        // Outbox + inbox tables live in the same database, so they share transactions with orders.
        modelBuilder.AddMessagingTables();
    }
}
