using Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

/// <summary>
/// Demo products, inserted the first time the database is migrated.
/// LT-001 has zero stock on purpose, so you can see an order get rejected.
/// </summary>
internal static class InventorySeed
{
    private static readonly (string Sku, string Name, decimal Price, int Stock)[] Products =
    [
        ("KB-001", "Mechanical Keyboard", 89.99m, 50),
        ("MS-001", "Wireless Mouse", 29.99m, 100),
        ("MN-001", "27\" 4K Monitor", 349.00m, 20),
        ("HS-001", "Noise-cancelling Headset", 129.50m, 30),
        ("CB-001", "USB-C Cable", 9.99m, 500),
        ("LT-001", "Laptop Stand", 39.00m, 0),
    ];

    public static async Task SeedAsync(DbContext context, CancellationToken cancellationToken)
    {
        var products = context.Set<Product>();
        if (await products.AnyAsync(cancellationToken))
        {
            return;
        }

        foreach (var (sku, name, price, stock) in Products)
        {
            products.Add(Product.Create(sku, name, price, "USD", stock).Value);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public static void Seed(DbContext context) => SeedAsync(context, CancellationToken.None).GetAwaiter().GetResult();
}
