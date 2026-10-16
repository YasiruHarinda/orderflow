using System.Reflection;
using BuildingBlocks.Application;
using NetArchTest.Rules;

namespace ArchitectureTests;

/// <summary>
/// Clean Architecture rules, checked on every build. Project references already stop
/// most mistakes; these tests also catch NuGet packages leaking into the wrong layer
/// (for example someone adding EF Core attributes to a domain class).
/// </summary>
public class LayerDependencyTests
{
    private static readonly Assembly OrdersDomain = typeof(Orders.Domain.Order).Assembly;
    private static readonly Assembly OrdersApplication = typeof(Orders.Application.DependencyInjection).Assembly;
    private static readonly Assembly InventoryDomain = typeof(Inventory.Domain.Product).Assembly;
    private static readonly Assembly InventoryApplication = typeof(Inventory.Application.DependencyInjection).Assembly;

    private static readonly string[] Frameworks =
    [
        "Microsoft.EntityFrameworkCore",
        "MassTransit",
        "Microsoft.AspNetCore",
        "Npgsql",
        "StackExchange.Redis",
    ];

    public static TheoryData<string, Assembly> DomainAssemblies => new()
    {
        { "Orders.Domain", OrdersDomain },
        { "Inventory.Domain", InventoryDomain },
    };

    public static TheoryData<string, Assembly> ApplicationAssemblies => new()
    {
        { "Orders.Application", OrdersApplication },
        { "Inventory.Application", InventoryApplication },
    };

    [Theory]
    [MemberData(nameof(DomainAssemblies))]
    public void Domain_depends_on_no_other_layer_or_framework(string name, Assembly domain)
    {
        var service = name.Split('.')[0];
        string[] forbidden = [$"{service}.Application", $"{service}.Infrastructure", $"{service}.API", "FluentValidation", .. Frameworks];

        var result = Types.InAssembly(domain).ShouldNot().HaveDependencyOnAny(forbidden).GetResult();

        result.FailingTypeNames.ShouldBeNull($"{name} must stay pure C#");
    }

    [Theory]
    [MemberData(nameof(ApplicationAssemblies))]
    public void Application_does_not_depend_on_infrastructure_or_frameworks(string name, Assembly application)
    {
        var service = name.Split('.')[0];
        string[] forbidden = [$"{service}.Infrastructure", $"{service}.API", .. Frameworks];

        var result = Types.InAssembly(application).ShouldNot().HaveDependencyOnAny(forbidden).GetResult();

        result.FailingTypeNames.ShouldBeNull($"{name} must only use abstractions");
    }

    [Fact]
    public void Orders_and_Inventory_never_reference_each_other()
    {
        // Services talk ONLY through messages (OrderFlow.Contracts), never through code.
        Types.InAssembly(OrdersApplication).ShouldNot().HaveDependencyOn("Inventory").GetResult().IsSuccessful.ShouldBeTrue();
        Types.InAssembly(InventoryApplication).ShouldNot().HaveDependencyOn("Orders").GetResult().IsSuccessful.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(ApplicationAssemblies))]
    public void Handlers_are_internal_and_sealed(string name, Assembly application)
    {
        // Internal: nobody can call a handler directly and skip the validation decorator.
        var handlerInterfaces = new[] { typeof(ICommandHandler<,>), typeof(IQueryHandler<,>) };
        var handlers = application.GetTypes()
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && handlerInterfaces.Contains(i.GetGenericTypeDefinition())))
            .ToList();

        handlers.ShouldNotBeEmpty();
        handlers.Where(t => t.IsPublic || !t.IsSealed).Select(t => t.FullName).ShouldBeEmpty($"handlers in {name}");
    }
}
