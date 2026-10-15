using Orders.Domain;

namespace Orders.UnitTests.Domain;

public class OrderTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    private static NewOrderLine Line(string sku = "SKU-1", decimal price = 10m, int quantity = 1) =>
        new(sku, "Test product", price, quantity);

    private static Order CreateValidOrder() =>
        Order.Create(CustomerId, "USD", [Line("SKU-1", 10m, 2), Line("SKU-2", 5m, 1)], Now).Value;

    // ---------- Creating an order ----------

    [Fact]
    public void Create_with_valid_data_returns_submitted_order()
    {
        var result = Order.Create(CustomerId, "USD", [Line("SKU-1", 10m, 2), Line("SKU-2", 5m, 1)], Now);

        result.IsSuccess.ShouldBeTrue();
        var order = result.Value;
        order.Status.ShouldBe(OrderStatus.Submitted);
        order.Lines.Count.ShouldBe(2);
        order.Total.ShouldBe(new Money(25m, "USD")); // 10*2 + 5*1
    }

    [Fact]
    public void Create_raises_OrderSubmitted_event()
    {
        var order = CreateValidOrder();

        var domainEvent = order.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<OrderSubmittedDomainEvent>();
        domainEvent.OrderId.ShouldBe(order.Id);
    }

    [Fact]
    public void Create_without_customer_fails()
    {
        var result = Order.Create(Guid.Empty, "USD", [Line()], Now);

        result.Error.ShouldBe(OrderErrors.CustomerRequired);
    }

    [Fact]
    public void Create_without_lines_fails()
    {
        var result = Order.Create(CustomerId, "USD", [], Now);

        result.Error.ShouldBe(OrderErrors.NoLines);
    }

    [Fact]
    public void Create_with_same_sku_twice_fails()
    {
        var result = Order.Create(CustomerId, "USD", [Line("SKU-1"), Line("sku-1")], Now);

        result.Error.ShouldBe(OrderErrors.DuplicateSku);
    }

    // [Theory] runs the same test once for every [InlineData] row.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(Order.MaxQuantityPerLine + 1)]
    public void Create_with_invalid_quantity_fails(int quantity)
    {
        var result = Order.Create(CustomerId, "USD", [Line(quantity: quantity)], Now);

        result.Error.ShouldBe(OrderErrors.InvalidQuantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_with_invalid_price_fails(decimal price)
    {
        var result = Order.Create(CustomerId, "USD", [Line(price: price)], Now);

        result.Error.ShouldBe(OrderErrors.InvalidPrice);
    }

    // ---------- Changing status ----------

    [Fact]
    public void Order_can_go_from_submitted_to_confirmed()
    {
        var order = CreateValidOrder();

        order.MarkAwaitingStock().IsSuccess.ShouldBeTrue();
        order.Confirm(Now).IsSuccess.ShouldBeTrue();

        order.Status.ShouldBe(OrderStatus.Confirmed);
        order.DomainEvents.OfType<OrderConfirmedDomainEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Confirm_before_awaiting_stock_fails()
    {
        var order = CreateValidOrder();

        var result = order.Confirm(Now);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Order.InvalidStatusChange");
        order.Status.ShouldBe(OrderStatus.Submitted);
    }

    [Fact]
    public void Reject_stores_reason_and_raises_event()
    {
        var order = CreateValidOrder();

        order.Reject("Out of stock", Now).IsSuccess.ShouldBeTrue();

        order.Status.ShouldBe(OrderStatus.Rejected);
        order.RejectionReason.ShouldBe("Out of stock");
        order.DomainEvents.OfType<OrderRejectedDomainEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Confirmed_order_cannot_be_rejected()
    {
        var order = CreateValidOrder();
        order.MarkAwaitingStock();
        order.Confirm(Now);

        var result = order.Reject("Changed my mind", Now);

        result.IsFailure.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Confirmed);
    }

    [Fact]
    public void Reject_without_reason_fails()
    {
        var order = CreateValidOrder();

        order.Reject(" ", Now).Error.ShouldBe(OrderErrors.RejectionReasonRequired);
    }
}
