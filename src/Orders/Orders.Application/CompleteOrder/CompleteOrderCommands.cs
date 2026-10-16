using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using Orders.Application.Abstractions;
using Orders.Application.Metrics;
using Orders.Domain;

namespace Orders.Application.CompleteOrder;

// These commands finish an order. They are sent by message consumers (Inventory's
// replies) and by the timeout monitor, not by HTTP clients.

/// <summary>Inventory reserved everything: the order is confirmed.</summary>
public sealed record ConfirmOrderCommand(Guid OrderId) : ICommand<Unit>;

/// <summary>Stock was not available, or Inventory did not answer in time: the order is rejected.</summary>
public sealed record RejectOrderCommand(Guid OrderId, string Reason, RejectionKind Kind) : ICommand<Unit>;

internal sealed class ConfirmOrderCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<ConfirmOrderCommand, Unit>
{
    public async Task<Result<Unit>> Handle(ConfirmOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return OrderErrors.NotFound(command.OrderId);
        }

        var now = timeProvider.GetUtcNow();
        var result = order.Confirm(now);
        if (result.IsFailure)
        {
            return result.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken); // also publishes OrderConfirmed via the outbox
        OrderMetrics.OrderConfirmed(now - order.CreatedAt);
        return Unit.Value;
    }
}

internal sealed class RejectOrderCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<RejectOrderCommand, Unit>
{
    public async Task<Result<Unit>> Handle(RejectOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return OrderErrors.NotFound(command.OrderId);
        }

        var now = timeProvider.GetUtcNow();
        var result = order.Reject(command.Reason, now);
        if (result.IsFailure)
        {
            return result.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken); // also publishes OrderRejected via the outbox
        OrderMetrics.OrderRejected(command.Kind, now - order.CreatedAt);
        return Unit.Value;
    }
}
