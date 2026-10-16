using MassTransit;
using Orders.Application.Abstractions;
using Orders.Domain;
using Orders.Infrastructure.Messaging;

namespace Orders.Infrastructure.Persistence;

/// <summary>
/// Saves changes AND publishes the matching integration events, atomically.
///
/// IPublishEndpoint here is MassTransit's outbox: Publish() does not talk to RabbitMQ,
/// it adds an OutboxMessage row to this same DbContext. The single SaveChangesAsync()
/// below then commits the order and its messages in one transaction.
/// </summary>
internal sealed class UnitOfWork(OrdersDbContext db, IPublishEndpoint publishEndpoint) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        var ordersWithEvents = db.ChangeTracker.Entries<Order>()
            .Select(entry => entry.Entity)
            .Where(order => order.DomainEvents.Count > 0)
            .ToList();

        foreach (var order in ordersWithEvents)
        {
            foreach (var domainEvent in order.DomainEvents)
            {
                var message = IntegrationEventMapper.ToIntegrationEvent(order, domainEvent);
                if (message is not null)
                {
                    await publishEndpoint.Publish(message, message.GetType(), cancellationToken);
                }
            }

            order.ClearDomainEvents();
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
