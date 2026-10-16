using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using Orders.Application.Abstractions;
using Orders.Application.Metrics;
using Orders.Domain;

namespace Orders.Application.PlaceOrder;

/// <summary>
/// The use case "place an order". It only coordinates:
/// 1. ask the domain to create the Order (the domain checks the rules)
/// 2. add it to the repository
/// 3. save everything (order + outgoing message) in one transaction
/// </summary>
internal sealed class PlaceOrderCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<PlaceOrderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        var lines = command.Lines
            .Select(l => new NewOrderLine(l.Sku, l.ProductName, l.UnitPrice, l.Quantity))
            .ToList();

        var result = Order.Create(command.CustomerId, command.Currency, lines, timeProvider.GetUtcNow());
        if (result.IsFailure)
        {
            return result.Error;
        }

        var order = result.Value;

        // The "please reserve stock" message (OrderSubmitted) is saved in the same
        // transaction as the order (outbox), so from now on the order is waiting for stock.
        order.MarkAwaitingStock();

        orders.Add(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        OrderMetrics.OrderPlaced();

        return order.Id;
    }
}
