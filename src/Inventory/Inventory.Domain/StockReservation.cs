using BuildingBlocks.Domain;

namespace Inventory.Domain;

/// <summary>
/// Remembers which stock was reserved for which order. One per order (OrderId is unique),
/// which is what makes reserving, releasing and committing idempotent: doing it twice
/// has the same effect as doing it once.
/// </summary>
public sealed class StockReservation : AggregateRoot<Guid>
{
    private readonly List<ReservedItem> _items = [];

    private StockReservation(Guid id, Guid orderId, ReservationStatus status, DateTimeOffset createdAt)
        : base(id)
    {
        OrderId = orderId;
        Status = status;
        CreatedAt = createdAt;
    }

    public Guid OrderId { get; private set; }

    public ReservationStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<ReservedItem> Items => _items;

    public static StockReservation Reserve(Guid orderId, IEnumerable<ReservedItem> items, DateTimeOffset now)
    {
        var reservation = new StockReservation(Guid.CreateVersion7(now), orderId, ReservationStatus.Reserved, now);
        reservation._items.AddRange(items);
        return reservation;
    }

    /// <summary>
    /// A "tombstone": the order was cancelled before its stock was ever reserved
    /// (messages can arrive out of order). It stops a late reservation from happening.
    /// </summary>
    public static StockReservation CancelledBeforeReserved(Guid orderId, DateTimeOffset now) =>
        new(Guid.CreateVersion7(now), orderId, ReservationStatus.Released, now);

    public Result Release()
    {
        if (Status != ReservationStatus.Reserved)
        {
            return InventoryErrors.ReservationNotActive(OrderId, Status);
        }

        Status = ReservationStatus.Released;
        return Result.Success();
    }

    public Result Commit()
    {
        if (Status != ReservationStatus.Reserved)
        {
            return InventoryErrors.ReservationNotActive(OrderId, Status);
        }

        Status = ReservationStatus.Committed;
        return Result.Success();
    }
}

public enum ReservationStatus
{
    Reserved,
    Released,
    Committed,
}

public sealed class ReservedItem(string sku, int quantity)
{
    public string Sku { get; private set; } = sku;

    public int Quantity { get; private set; } = quantity;
}
