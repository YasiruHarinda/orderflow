using BuildingBlocks.Application;
using Inventory.Application.Reservations;
using MassTransit;
using Microsoft.Extensions.Logging;
using OrderFlow.Contracts;

namespace Inventory.Infrastructure.Messaging;

// Consumers are the "API" for messages: like an HTTP endpoint, they only translate
// the message into a command, call the handler, and send a reply. No business logic.
//
// Each consumer runs inside MassTransit's EF outbox: anything published here is saved
// in the same transaction as the stock changes, and duplicate deliveries are skipped.

/// <summary>An order was placed: try to reserve its stock and tell Orders the result.</summary>
internal sealed partial class OrderSubmittedConsumer(
    ICommandHandler<ReserveStockCommand, Unit> handler,
    TimeProvider timeProvider,
    ILogger<OrderSubmittedConsumer> logger)
    : IConsumer<OrderSubmitted>
{
    public async Task Consume(ConsumeContext<OrderSubmitted> context)
    {
        var message = context.Message;
        var command = new ReserveStockCommand(
            message.OrderId,
            [.. message.Lines.Select(l => new ReserveStockLine(l.Sku, l.Quantity))]);

        var result = await handler.Handle(command, context.CancellationToken);

        if (result.IsSuccess)
        {
            LogReserved(logger, message.OrderId);
            await context.Publish(new StockReserved(message.OrderId, timeProvider.GetUtcNow()));
        }
        else
        {
            // Not enough stock is a normal business outcome, not an error: reply, don't throw.
            LogReservationFailed(logger, message.OrderId, result.Error.Description);
            await context.Publish(new StockReservationFailed(message.OrderId, result.Error.Description, timeProvider.GetUtcNow()));
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Stock reserved for order {OrderId}")]
    private static partial void LogReserved(ILogger logger, Guid orderId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stock reservation failed for order {OrderId}: {Reason}")]
    private static partial void LogReservationFailed(ILogger logger, Guid orderId, string reason);
}

/// <summary>An order was confirmed: the reserved stock leaves the warehouse.</summary>
internal sealed class OrderConfirmedConsumer(ICommandHandler<CommitStockCommand, Unit> handler) : IConsumer<OrderConfirmed>
{
    public Task Consume(ConsumeContext<OrderConfirmed> context) =>
        handler.Handle(new CommitStockCommand(context.Message.OrderId), context.CancellationToken);
}

/// <summary>An order was rejected (or timed out): give its reserved stock back. This is the COMPENSATION step.</summary>
internal sealed class OrderRejectedConsumer(ICommandHandler<ReleaseStockCommand, Unit> handler) : IConsumer<OrderRejected>
{
    public Task Consume(ConsumeContext<OrderRejected> context) =>
        handler.Handle(new ReleaseStockCommand(context.Message.OrderId), context.CancellationToken);
}
