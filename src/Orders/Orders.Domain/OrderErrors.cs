using BuildingBlocks.Domain;

namespace Orders.Domain;

/// <summary>Every way an order can be invalid, in one list.</summary>
public static class OrderErrors
{
    public static readonly Error CustomerRequired =
        new("Order.CustomerRequired", "An order must belong to a customer.");

    public static readonly Error CurrencyRequired =
        new("Order.CurrencyRequired", "An order must have a currency.");

    public static readonly Error NoLines =
        new("Order.NoLines", "An order must contain at least one line.");

    public static readonly Error TooManyLines =
        new("Order.TooManyLines", $"An order can contain at most {Order.MaxLines} lines.");

    public static readonly Error DuplicateSku =
        new("Order.DuplicateSku", "Each product can appear only once in an order.");

    public static readonly Error SkuRequired =
        new("Order.SkuRequired", "Every line must have a SKU.");

    public static readonly Error InvalidQuantity =
        new("Order.InvalidQuantity", $"Quantity must be between 1 and {Order.MaxQuantityPerLine}.");

    public static readonly Error InvalidPrice =
        new("Order.InvalidPrice", "Unit price must be greater than zero.");

    public static readonly Error RejectionReasonRequired =
        new("Order.RejectionReasonRequired", "A rejected order must have a reason.");

    public static Error NotFound(Guid orderId) =>
        new("Order.NotFound", $"Order '{orderId}' was not found.", ErrorType.NotFound);

    public static Error InvalidStatusChange(OrderStatus from, OrderStatus to) =>
        new("Order.InvalidStatusChange", $"Cannot change an order from {from} to {to}.", ErrorType.Conflict);
}
