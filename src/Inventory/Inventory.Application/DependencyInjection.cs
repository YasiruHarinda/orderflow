using BuildingBlocks.Application;
using FluentValidation;
using Inventory.Application.Products;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Inventory.Application;

public static class DependencyInjection
{
    /// <summary>Same pattern as Orders: auto-register handlers and validators, then add validation decorators.</summary>
    public static IServiceCollection AddInventoryApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.Scan(scan => scan
            .FromAssemblyOf<GetProductQuery>()
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

        services.AddValidatorsFromAssemblyContaining<GetProductQuery>(includeInternalTypes: true);

        services.Decorate(typeof(ICommandHandler<,>), typeof(ValidationCommandDecorator<,>));
        services.Decorate(typeof(IQueryHandler<,>), typeof(ValidationQueryDecorator<,>));

        return services;
    }
}
