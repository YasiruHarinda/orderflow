using BuildingBlocks.Domain;

namespace Inventory.Domain;

/// <summary>
/// A product and its stock. Stock has two numbers:
///   OnHand   = physically in the warehouse
///   Reserved = promised to orders that are not finished yet
///   Available = OnHand - Reserved  (what new orders can still get)
/// </summary>
public sealed class Product : AggregateRoot<Guid>
{
    private Product(Guid id, string sku, string name, decimal price, string currency, int quantityOnHand)
        : base(id)
    {
        Sku = sku;
        Name = name;
        Price = price;
        Currency = currency;
        QuantityOnHand = quantityOnHand;
    }

    public string Sku { get; private set; }

    public string Name { get; private set; }

    public decimal Price { get; private set; }

    public string Currency { get; private set; }

    public int QuantityOnHand { get; private set; }

    public int QuantityReserved { get; private set; }

    public int QuantityAvailable => QuantityOnHand - QuantityReserved;

    public static Result<Product> Create(string sku, string name, decimal price, string currency, int quantityOnHand)
    {
        if (string.IsNullOrWhiteSpace(sku) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(currency))
        {
            return InventoryErrors.ProductDetailsRequired;
        }

        if (price <= 0)
        {
            return InventoryErrors.InvalidPrice;
        }

        if (quantityOnHand < 0)
        {
            return InventoryErrors.InvalidQuantity;
        }

        return new Product(Guid.CreateVersion7(), sku, name, price, currency, quantityOnHand);
    }

    public bool CanReserve(int quantity) => quantity > 0 && quantity <= QuantityAvailable;

    /// <summary>Promise stock to an order. The stock is still in the warehouse.</summary>
    public Result Reserve(int quantity)
    {
        if (quantity <= 0)
        {
            return InventoryErrors.InvalidQuantity;
        }

        if (quantity > QuantityAvailable)
        {
            return InventoryErrors.InsufficientStock(Sku, quantity, QuantityAvailable);
        }

        QuantityReserved += quantity;
        return Result.Success();
    }

    /// <summary>The order was cancelled: the promised stock becomes available again.</summary>
    public Result Release(int quantity)
    {
        if (quantity <= 0 || quantity > QuantityReserved)
        {
            return InventoryErrors.InvalidQuantity;
        }

        QuantityReserved -= quantity;
        return Result.Success();
    }

    /// <summary>The order was confirmed: the stock leaves the warehouse.</summary>
    public Result Commit(int quantity)
    {
        if (quantity <= 0 || quantity > QuantityReserved)
        {
            return InventoryErrors.InvalidQuantity;
        }

        QuantityReserved -= quantity;
        QuantityOnHand -= quantity;
        return Result.Success();
    }

    /// <summary>A stock count or a delivery: set how many are physically in the warehouse.</summary>
    public Result SetStockOnHand(int quantity)
    {
        if (quantity < 0)
        {
            return InventoryErrors.InvalidQuantity;
        }

        if (quantity < QuantityReserved)
        {
            return InventoryErrors.StockBelowReserved(Sku, quantity, QuantityReserved);
        }

        QuantityOnHand = quantity;
        return Result.Success();
    }
}
