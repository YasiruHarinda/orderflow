using BuildingBlocks.Domain;
using OrderFlow.Contracts;
using Orders.Domain;

namespace Orders.Infrastructure.Messaging;

/// <summary>Translates internal domain events into the public messages other services understand.</summary>
internal static class IntegrationEventMapper
{
    public static object? ToIntegrationEvent(Order order, IDomainEvent domainEvent) => domainEvent switch
    {
        OrderSubmittedDomainEvent e => new OrderSubmitted(
            order.Id,
            order.CustomerId,
            [.. order.Lines.Select(l => new OrderSubmittedLine(l.Sku, l.Quantity))],
            e.OccurredOn),

        OrderConfirmedDomainEvent e => new OrderConfirmed(order.Id, order.CustomerId, order.Total.Amount, order.Currency, e.OccurredOn),

        OrderRejectedDomainEvent e => new OrderRejected(order.Id, order.CustomerId, e.Reason, e.OccurredOn),

        _ => null,
    };
}
