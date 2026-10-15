using BuildingBlocks.Domain;

namespace Orders.Domain;

/// <summary>
/// One product in an order. It is an entity (it has an Id) but it belongs to
/// <see cref="Order"/>: the constructor is private and the factory is internal,
/// so only Order can create lines. That keeps all order rules in one place.
/// </summary>
public sealed class OrderLine : Entity<Guid>
{
    private OrderLine(Guid id, string sku, string productName, Money unitPrice, int quantity)
        : base(id)
    {
        Sku = sku;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public string Sku { get; private set; }

    public string ProductName { get; private set; }

    public Money UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    public Money LineTotal => UnitPrice.Multiply(Quantity);

    internal static OrderLine Create(string sku, string productName, Money unitPrice, int quantity) =>
        new(Guid.CreateVersion7(), sku, productName, unitPrice, quantity);
}
