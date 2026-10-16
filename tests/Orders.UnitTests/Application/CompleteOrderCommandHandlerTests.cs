using Orders.Application.CompleteOrder;
using Orders.Application.Metrics;
using Orders.Domain;

namespace Orders.UnitTests.Application;

public class CompleteOrderCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);

    private readonly FakeOrderRepository _orders = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FixedTimeProvider _time = new(Now);

    private Order AddAwaitingOrder()
    {
        var order = Order.Create(Guid.NewGuid(), "USD", [new NewOrderLine("SKU-1", "Keyboard", 10m, 1)], Now.AddSeconds(-5)).Value;
        order.MarkAwaitingStock();
        _orders.Add(order);
        return order;
    }

    [Fact]
    public async Task Confirm_moves_order_to_confirmed()
    {
        var order = AddAwaitingOrder();

        var result = await new ConfirmOrderCommandHandler(_orders, _unitOfWork, _time)
            .Handle(new ConfirmOrderCommand(order.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Confirmed);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Duplicate_confirm_is_a_conflict_and_saves_nothing()
    {
        var order = AddAwaitingOrder();
        var handler = new ConfirmOrderCommandHandler(_orders, _unitOfWork, _time);
        await handler.Handle(new ConfirmOrderCommand(order.Id), CancellationToken.None);

        var second = await handler.Handle(new ConfirmOrderCommand(order.Id), CancellationToken.None);

        second.Error.Code.ShouldBe("Order.InvalidStatusChange");
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Reject_after_timeout_stores_the_reason()
    {
        var order = AddAwaitingOrder();

        await new RejectOrderCommandHandler(_orders, _unitOfWork, _time)
            .Handle(new RejectOrderCommand(order.Id, "Timed out", RejectionKind.Timeout), CancellationToken.None);

        order.Status.ShouldBe(OrderStatus.Rejected);
        order.RejectionReason.ShouldBe("Timed out");
    }

    [Fact]
    public async Task Unknown_order_returns_not_found()
    {
        var result = await new ConfirmOrderCommandHandler(_orders, _unitOfWork, _time)
            .Handle(new ConfirmOrderCommand(Guid.NewGuid()), CancellationToken.None);

        result.Error.Code.ShouldBe("Order.NotFound");
    }
}
