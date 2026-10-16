using BuildingBlocks.Application;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orders.Application.PlaceOrder;

namespace Orders.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers every handler and validator in this project automatically, then wraps
    /// each handler in a validation decorator. Adding a new use case needs no changes here.
    /// </summary>
    public static IServiceCollection AddOrdersApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        // Scrutor scans this assembly and registers each handler under its interface,
        // e.g. PlaceOrderCommandHandler => ICommandHandler<PlaceOrderCommand, Guid>.
        services.Scan(scan => scan
            .FromAssemblyOf<PlaceOrderCommand>()
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

        services.AddValidatorsFromAssemblyContaining<PlaceOrderCommand>(includeInternalTypes: true);

        // Asking DI for ICommandHandler<X, Y> now returns: ValidationDecorator -> real handler.
        services.Decorate(typeof(ICommandHandler<,>), typeof(ValidationCommandDecorator<,>));
        services.Decorate(typeof(IQueryHandler<,>), typeof(ValidationQueryDecorator<,>));

        return services;
    }
}
