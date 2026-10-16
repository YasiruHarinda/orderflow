using BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orders.Application.Abstractions;
using Orders.Application.CompleteOrder;
using Orders.Application.Metrics;

namespace Orders.Infrastructure.Background;

public sealed class StuckOrderOptions
{
    public const string Section = "Orders:StuckOrders";

    /// <summary>How long an order may wait for Inventory before it is rejected.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(2);

    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// The saga's safety net. If Inventory is down, or a message is lost in an error queue,
/// an order would wait forever. Every 30 seconds this rejects orders that waited too long;
/// the resulting OrderRejected event makes Inventory release any stock (compensation).
///
/// Several pods may run this at once. That is safe: optimistic concurrency (xmin) lets only
/// one of them change a given order; the others get a concurrency error and skip it.
/// </summary>
internal sealed partial class StuckOrderMonitor(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<StuckOrderOptions> options,
    ILogger<StuckOrderMonitor> logger)
    : BackgroundService
{
    private const int BatchSize = 100;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.CheckInterval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RejectStuckOrdersAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Never let one failed run (e.g. database briefly down) stop the monitor.
                LogRunFailed(logger, ex);
            }
        }
    }

    private async Task RejectStuckOrdersAsync(CancellationToken cancellationToken)
    {
        var createdBefore = timeProvider.GetUtcNow() - options.Value.Timeout;

        IReadOnlyList<Guid> stuckOrderIds;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var orders = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            stuckOrderIds = await orders.GetIdsAwaitingStockAsync(createdBefore, BatchSize, cancellationToken);
        }

        foreach (var orderId in stuckOrderIds)
        {
            // A fresh scope (and DbContext) per order, so one failure doesn't affect the rest.
            await using var scope = scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<RejectOrderCommand, Unit>>();

            try
            {
                await handler.Handle(
                    new RejectOrderCommand(orderId, "Timed out waiting for stock reservation.", RejectionKind.Timeout),
                    cancellationToken);
                LogTimedOut(logger, orderId);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another pod, or a late StockReserved message, changed this order first. Fine.
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Order {OrderId} timed out waiting for stock and was rejected")]
    private static partial void LogTimedOut(ILogger logger, Guid orderId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Stuck order check failed")]
    private static partial void LogRunFailed(ILogger logger, Exception exception);
}
