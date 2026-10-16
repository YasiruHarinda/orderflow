using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using Orders.Application.Abstractions;
using Orders.Domain;

namespace Orders.Application.PlaceOrder;

/// <summary>
/// The use case "place an order". It only coordinates:
/// 1. ask the domain to create the Order (the domain checks the rules)
/// 2. add it to the repository
/// 3. save everything in one transaction
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

        orders.Add(result.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return result.Value.Id;
    }
}
