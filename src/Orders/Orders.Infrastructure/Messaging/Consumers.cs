using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using MassTransit;
using Microsoft.Extensions.Logging;
using OrderFlow.Contracts;
using Orders.Application.CompleteOrder;
using Orders.Application.Metrics;

namespace Orders.Infrastructure.Messaging;

// The SAGA, choreography style: there is no central coordinator. Each service reacts to
// events, and the Order aggregate's state machine decides what is allowed:
//
//   Orders  --OrderSubmitted-->  Inventory
//   Orders  <--StockReserved---  Inventory   => Confirm  --OrderConfirmed--> Inventory (commit), Notifications
//   Orders  <--ReservationFailed Inventory   => Reject   --OrderRejected-->  Inventory (release), Notifications
//   timeout (StuckOrderMonitor)               => Reject   --OrderRejected-->  ... (compensation)

internal sealed partial class StockReservedConsumer(
    ICommandHandler<ConfirmOrderCommand, Unit> handler,
    ILogger<StockReservedConsumer> logger)
    : IConsumer<StockReserved>
{
    public async Task Consume(ConsumeContext<StockReserved> context)
    {
        var result = await handler.Handle(new ConfirmOrderCommand(context.Message.OrderId), context.CancellationToken);

        // A conflict means the order already reached a final status (a duplicate message,
        // or the timeout won the race). That's expected: log it and acknowledge the message.
        // If the order was rejected, Inventory releases the stock when it sees OrderRejected.
        if (result.IsFailure)
        {
            LogIgnored(logger, context.Message.OrderId, result.Error.Description);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "StockReserved for order {OrderId} ignored: {Reason}")]
    private static partial void LogIgnored(ILogger logger, Guid orderId, string reason);
}

internal sealed partial class StockReservationFailedConsumer(
    ICommandHandler<RejectOrderCommand, Unit> handler,
    ILogger<StockReservationFailedConsumer> logger)
    : IConsumer<StockReservationFailed>
{
    public async Task Consume(ConsumeContext<StockReservationFailed> context)
    {
        var message = context.Message;
        var result = await handler.Handle(
            new RejectOrderCommand(message.OrderId, message.Reason, RejectionKind.StockUnavailable),
            context.CancellationToken);

        if (result.IsFailure && result.Error.Type != ErrorType.Conflict)
        {
            LogFailed(logger, message.OrderId, result.Error.Description);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not reject order {OrderId}: {Reason}")]
    private static partial void LogFailed(ILogger logger, Guid orderId, string reason);
}
