using System.Diagnostics.Metrics;
using MassTransit;
using OrderFlow.Contracts;

namespace Notifications.Worker;

/// <summary>Sends a message to a customer. Swap the implementation for SendGrid, SMS, etc.</summary>
internal interface INotificationSender
{
    Task SendAsync(Guid customerId, string subject, string body, CancellationToken cancellationToken);
}

/// <summary>Simulated sender: writes the "email" to the log, which you can see in Grafana/Loki.</summary>
internal sealed partial class LoggingNotificationSender(ILogger<LoggingNotificationSender> logger) : INotificationSender
{
    private static readonly Meter Meter = new("OrderFlow.Notifications");
    private static readonly Counter<long> Sent = Meter.CreateCounter<long>(
        "orderflow.notifications.sent", unit: "{notification}", description: "Notifications sent to customers.");

    public Task SendAsync(Guid customerId, string subject, string body, CancellationToken cancellationToken)
    {
        LogSent(logger, customerId, subject, body);
        Sent.Add(1);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Notification to customer {CustomerId}: {Subject} - {Body}")]
    private static partial void LogSent(ILogger logger, Guid customerId, string subject, string body);
}

internal sealed class OrderConfirmedConsumer(INotificationSender sender) : IConsumer<OrderConfirmed>
{
    public Task Consume(ConsumeContext<OrderConfirmed> context)
    {
        var m = context.Message;
        return sender.SendAsync(
            m.CustomerId,
            "Your order is confirmed",
            $"Order {m.OrderId} for {m.Total:0.00} {m.Currency} is confirmed and will ship soon.",
            context.CancellationToken);
    }
}

internal sealed class OrderRejectedConsumer(INotificationSender sender) : IConsumer<OrderRejected>
{
    public Task Consume(ConsumeContext<OrderRejected> context)
    {
        var m = context.Message;
        return sender.SendAsync(
            m.CustomerId,
            "We could not complete your order",
            $"Order {m.OrderId} was not completed: {m.Reason}",
            context.CancellationToken);
    }
}
