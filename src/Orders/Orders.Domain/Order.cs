using BuildingBlocks.Domain;

namespace Orders.Domain;

/// <summary>
/// The Order aggregate root. All changes to an order and its lines go through
/// this class, so its rules ("invariants") can never be skipped.
/// </summary>
public sealed class Order : AggregateRoot<Guid>
{
    public const int MaxLines = 50;
    public const int MaxQuantityPerLine = 100;

    private readonly List<OrderLine> _lines = [];

    private Order(Guid id, Guid customerId, string currency, DateTimeOffset createdAt)
        : base(id)
    {
        CustomerId = customerId;
        Currency = currency;
        CreatedAt = createdAt;
        Status = OrderStatus.Submitted;
    }

    public Guid CustomerId { get; private set; }

    public string Currency { get; private set; }

    public OrderStatus Status { get; private set; }

    public string? RejectionReason { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    // Read-only view: callers can look at the lines but cannot add or remove them.
    public IReadOnlyList<OrderLine> Lines => _lines;

    public Money Total => _lines.Aggregate(Money.Zero(Currency), (sum, line) => sum.Add(line.LineTotal));

    /// <summary>
    /// The only way to create an order. It returns a failed Result instead of
    /// an invalid Order, so an invalid Order can never exist.
    /// </summary>
    public static Result<Order> Create(
        Guid customerId,
        string currency,
        IReadOnlyCollection<NewOrderLine> lines,
        DateTimeOffset createdAt)
    {
        if (customerId == Guid.Empty)
        {
            return OrderErrors.CustomerRequired;
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return OrderErrors.CurrencyRequired;
        }

        var lineError = ValidateLines(lines);
        if (lineError is not null)
        {
            return lineError;
        }

        var order = new Order(Guid.CreateVersion7(createdAt), customerId, currency, createdAt);
        foreach (var line in lines)
        {
            order._lines.Add(OrderLine.Create(line.Sku, line.ProductName, new Money(line.UnitPrice, currency), line.Quantity));
        }

        order.Raise(new OrderSubmittedDomainEvent(order.Id, createdAt));
        return order;
    }

    public Result MarkAwaitingStock()
    {
        if (Status != OrderStatus.Submitted)
        {
            return OrderErrors.InvalidStatusChange(Status, OrderStatus.AwaitingStock);
        }

        Status = OrderStatus.AwaitingStock;
        return Result.Success();
    }

    public Result Confirm(DateTimeOffset now)
    {
        if (Status != OrderStatus.AwaitingStock)
        {
            return OrderErrors.InvalidStatusChange(Status, OrderStatus.Confirmed);
        }

        Status = OrderStatus.Confirmed;
        Raise(new OrderConfirmedDomainEvent(Id, now));
        return Result.Success();
    }

    public Result Reject(string reason, DateTimeOffset now)
    {
        // A finished order (Confirmed or Rejected) cannot change any more.
        if (Status is OrderStatus.Confirmed or OrderStatus.Rejected)
        {
            return OrderErrors.InvalidStatusChange(Status, OrderStatus.Rejected);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return OrderErrors.RejectionReasonRequired;
        }

        Status = OrderStatus.Rejected;
        RejectionReason = reason;
        Raise(new OrderRejectedDomainEvent(Id, reason, now));
        return Result.Success();
    }

    private static Error? ValidateLines(IReadOnlyCollection<NewOrderLine> lines)
    {
        if (lines.Count == 0)
        {
            return OrderErrors.NoLines;
        }

        if (lines.Count > MaxLines)
        {
            return OrderErrors.TooManyLines;
        }

        if (lines.DistinctBy(l => l.Sku, StringComparer.OrdinalIgnoreCase).Count() != lines.Count)
        {
            return OrderErrors.DuplicateSku;
        }

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line.Sku))
            {
                return OrderErrors.SkuRequired;
            }

            if (line.Quantity is < 1 or > MaxQuantityPerLine)
            {
                return OrderErrors.InvalidQuantity;
            }

            if (line.UnitPrice <= 0)
            {
                return OrderErrors.InvalidPrice;
            }
        }

        return null;
    }
}
