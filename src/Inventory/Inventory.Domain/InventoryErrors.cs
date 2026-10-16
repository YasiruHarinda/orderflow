using BuildingBlocks.Domain;

namespace Inventory.Domain;

public static class InventoryErrors
{
    public static readonly Error ProductDetailsRequired =
        new("Inventory.ProductDetailsRequired", "SKU, name and currency are required.");

    public static readonly Error InvalidPrice =
        new("Inventory.InvalidPrice", "Price must be greater than zero.");

    public static readonly Error InvalidQuantity =
        new("Inventory.InvalidQuantity", "Quantity is not valid for this operation.");

    public static Error ProductNotFound(string sku) =>
        new("Inventory.ProductNotFound", $"Product '{sku}' does not exist.", ErrorType.NotFound);

    public static Error InsufficientStock(string sku, int requested, int available) =>
        new("Inventory.InsufficientStock", $"Not enough stock for '{sku}': requested {requested}, available {available}.", ErrorType.Conflict);

    public static Error StockBelowReserved(string sku, int requested, int reserved) =>
        new("Inventory.StockBelowReserved", $"Cannot set stock of '{sku}' to {requested}: {reserved} are already reserved.", ErrorType.Conflict);

    public static Error OrderCancelled(Guid orderId) =>
        new("Inventory.OrderCancelled", $"Order '{orderId}' was cancelled; stock will not be reserved.", ErrorType.Conflict);

    public static Error ReservationNotActive(Guid orderId, ReservationStatus status) =>
        new("Inventory.ReservationNotActive", $"The reservation for order '{orderId}' is already {status}.", ErrorType.Conflict);
}
