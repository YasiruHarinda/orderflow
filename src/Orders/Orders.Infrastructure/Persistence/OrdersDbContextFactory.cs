using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Orders.Infrastructure.Persistence;

/// <summary>
/// Used only by the "dotnet ef" tool to create migrations. Creating a migration
/// does not connect to a database, so the connection string is a placeholder.
/// </summary>
public sealed class OrdersDbContextFactory : IDesignTimeDbContextFactory<OrdersDbContext>
{
    public OrdersDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<OrdersDbContext>()
            .UseNpgsql("Host=localhost;Database=ordersdb")
            .Options);
}
