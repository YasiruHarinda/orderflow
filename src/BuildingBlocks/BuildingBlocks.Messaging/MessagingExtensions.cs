using System.Reflection;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.Messaging;

public static class MessagingExtensions
{
    public const string ConnectionName = "rabbitmq";

    /// <summary>
    /// MassTransit over RabbitMQ with the transactional OUTBOX and INBOX, stored in the service's own database.
    ///
    /// The problem it solves: "save the order" and "send the message" are two different systems
    /// (PostgreSQL and RabbitMQ). If the app crashes between them, you either lose the message
    /// or send a message about an order that was never saved.
    ///
    /// Outbox: published messages are written to an OutboxMessage table in the SAME database
    ///         transaction as the business data. A background process then delivers them.
    ///         Both happen, or neither happens.
    /// Inbox:  every received message id is remembered, so a message delivered twice is
    ///         processed only once.
    /// </summary>
    public static IHostApplicationBuilder AddMessaging<TDbContext>(
        this IHostApplicationBuilder builder,
        string serviceName,
        Assembly consumersAssembly)
        where TDbContext : DbContext
    {
        builder.Services.AddMassTransit(bus =>
        {
            // Queue names get a service prefix, e.g. "inventory-order-confirmed" and
            // "notifications-order-confirmed": each service gets its own copy of every event.
            bus.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(serviceName, false));
            AddConsumersIncludingInternal(bus, consumersAssembly);

            bus.AddEntityFrameworkOutbox<TDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox(); // messages published outside a consumer (e.g. from an API call) go through the outbox too
            });

            bus.AddConfigureEndpointsCallback((context, _, endpoint) =>
            {
                // Retry transient failures (e.g. a concurrency conflict) with growing delays.
                // After the last retry the message moves to an "_error" queue for a human to inspect.
                endpoint.UseMessageRetry(retry => retry.Intervals(100, 500, 1_000, 5_000, 10_000));
                endpoint.UseEntityFrameworkOutbox<TDbContext>(context);
            });

            bus.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.Host(GetRabbitMqUri(builder.Configuration));
                rabbit.ConfigureEndpoints(context);
            });
        });

        return builder;
    }

    /// <summary>For a service with no database of its own (Notifications): no outbox, just consumers.</summary>
    public static IHostApplicationBuilder AddMessaging(
        this IHostApplicationBuilder builder,
        string serviceName,
        Assembly consumersAssembly)
    {
        builder.Services.AddMassTransit(bus =>
        {
            bus.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(serviceName, false));
            AddConsumersIncludingInternal(bus, consumersAssembly);

            bus.AddConfigureEndpointsCallback((_, _, endpoint) =>
                endpoint.UseMessageRetry(retry => retry.Intervals(100, 500, 1_000, 5_000, 10_000)));

            bus.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.Host(GetRabbitMqUri(builder.Configuration));
                rabbit.ConfigureEndpoints(context);
            });
        });

        return builder;
    }

    /// <summary>Adds MassTransit's outbox and inbox tables to a service's EF Core model.</summary>
    public static ModelBuilder AddMessagingTables(this ModelBuilder modelBuilder)
    {
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        return modelBuilder;
    }

    /// <summary>
    /// Registers every consumer in the assembly, public or not. MassTransit's own
    /// AddConsumers(assembly) only scans PUBLIC types, and our consumers are internal (like
    /// the handlers, they are not part of any public API). Without this, no consumer is
    /// registered, no queue is bound, and RabbitMQ silently drops every message.
    /// </summary>
    private static void AddConsumersIncludingInternal(IBusRegistrationConfigurator bus, Assembly assembly)
    {
        var consumerTypes = assembly.GetTypes().Where(type =>
            type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false } &&
            type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>)));

        foreach (var consumerType in consumerTypes)
        {
            bus.AddConsumer(consumerType);
        }
    }

    // e.g. amqp://user:password@rabbitmq:5672 (Aspire and Kubernetes both provide this format).
    private static Uri GetRabbitMqUri(IConfiguration configuration) =>
        new(configuration.GetConnectionString(ConnectionName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionName}' is missing."));
}
