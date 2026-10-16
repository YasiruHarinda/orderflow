using Orders.Application.PlaceOrder;
using Orders.Domain;

namespace Orders.UnitTests.Application;

public class PlaceOrderCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);

    private readonly FakeOrderRepository _orders = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private PlaceOrderCommandHandler CreateHandler() => new(_orders, _unitOfWork, new FixedTimeProvider(Now));

    [Fact]
    public async Task Valid_command_saves_order_and_returns_its_id()
    {
        var command = new PlaceOrderCommand(
            Guid.NewGuid(),
            "USD",
            [new PlaceOrderLine("SKU-1", "Keyboard", 49.99m, 2)]);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var saved = _orders.Orders.ShouldHaveSingleItem();
        saved.Id.ShouldBe(result.Value);
        saved.CreatedAt.ShouldBe(Now);
        saved.Status.ShouldBe(OrderStatus.AwaitingStock);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Domain_rule_failure_is_returned_and_nothing_is_saved()
    {
        // Same SKU twice passes input validation, but the domain rejects it.
        var command = new PlaceOrderCommand(
            Guid.NewGuid(),
            "USD",
            [new PlaceOrderLine("SKU-1", "Keyboard", 10m, 1), new PlaceOrderLine("SKU-1", "Keyboard", 10m, 1)]);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Error.ShouldBe(OrderErrors.DuplicateSku);
        _orders.Orders.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(0);
    }
}
