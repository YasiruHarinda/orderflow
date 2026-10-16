using System.Diagnostics.Metrics;

namespace Orders.Application.Metrics;

/// <summary>
/// Business metrics, exported through OpenTelemetry to Prometheus (Part 16) as e.g.
/// orderflow_orders_placed_total. Technical metrics (HTTP latency, CPU) come for free;
/// these answer business questions: "how many orders are failing, and why?"
/// </summary>
public static class OrderMetrics
{
    public const string MeterName = "OrderFlow.Orders";

    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> Placed = Meter.CreateCounter<long>(
        "orderflow.orders.placed", unit: "{order}", description: "Orders accepted by the API.");

    private static readonly Counter<long> Confirmed = Meter.CreateCounter<long>(
        "orderflow.orders.confirmed", unit: "{order}", description: "Orders that reached Confirmed.");

    private static readonly Counter<long> Rejected = Meter.CreateCounter<long>(
        "orderflow.orders.rejected", unit: "{order}", description: "Orders that reached Rejected, by reason.");

    private static readonly Histogram<double> ProcessingDuration = Meter.CreateHistogram<double>(
        "orderflow.orders.processing.duration", unit: "s", description: "Time from placing an order to its final status.");

    public static void OrderPlaced() => Placed.Add(1);

    public static void OrderConfirmed(TimeSpan processingTime)
    {
        Confirmed.Add(1);
        ProcessingDuration.Record(processingTime.TotalSeconds, new KeyValuePair<string, object?>("status", "confirmed"));
    }

    public static void OrderRejected(RejectionKind kind, TimeSpan processingTime)
    {
        // Tag with a small, fixed set of values. Never put free text (like the reason
        // message) in a metric tag: every distinct value creates a new time series.
        var reason = new KeyValuePair<string, object?>("reason", kind == RejectionKind.Timeout ? "timeout" : "stock_unavailable");
        Rejected.Add(1, reason);
        ProcessingDuration.Record(processingTime.TotalSeconds, new KeyValuePair<string, object?>("status", "rejected"));
    }
}

public enum RejectionKind
{
    StockUnavailable,
    Timeout,
}
