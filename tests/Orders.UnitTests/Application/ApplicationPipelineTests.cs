using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using Microsoft.Extensions.DependencyInjection;
using Orders.Application;
using Orders.Application.Abstractions;
using Orders.Application.GetOrder;
using Orders.Application.ListOrders;
using Orders.Application.PlaceOrder;
using Orders.Domain;

namespace Orders.UnitTests.Application;

/// <summary>
/// Tests the real DI wiring from AddOrdersApplication(): handlers are found automatically
/// and every request goes through the validation decorator first.
/// </summary>
public sealed class ApplicationPipelineTests : IDisposable
{
    private readonly FakeOrderRepository _orders = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeOrderQueries _queries = new();
    private readonly ServiceProvider _provider;

    public ApplicationPipelineTests()
    {
        var services = new ServiceCollection();
        services.AddOrdersApplication();
        services.AddSingleton<IOrderRepository>(_orders);
        services.AddSingleton<IUnitOfWork>(_unitOfWork);
        services.AddSingleton<IOrderQueries>(_queries);
        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
    }

    public void Dispose() => _provider.Dispose();

    [Fact]
    public void Command_handler_is_wrapped_in_validation_decorator()
    {
        var handler = _provider.GetRequiredService<ICommandHandler<PlaceOrderCommand, Guid>>();

        handler.ShouldBeOfType<ValidationCommandDecorator<PlaceOrderCommand, Guid>>();
    }

    [Fact]
    public async Task Invalid_command_is_stopped_before_the_handler_runs()
    {
        var handler = _provider.GetRequiredService<ICommandHandler<PlaceOrderCommand, Guid>>();
        var command = new PlaceOrderCommand(Guid.Empty, "usd", []);

        var result = await handler.Handle(command, CancellationToken.None);

        var error = result.Error.ShouldBeOfType<ValidationError>();
        error.Errors.Count.ShouldBe(3); // CustomerId, Currency, Lines
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Valid_command_reaches_the_handler()
    {
        var handler = _provider.GetRequiredService<ICommandHandler<PlaceOrderCommand, Guid>>();
        var command = new PlaceOrderCommand(Guid.NewGuid(), "USD", [new PlaceOrderLine("SKU-1", "Mouse", 20m, 1)]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Unknown_order_returns_not_found()
    {
        var handler = _provider.GetRequiredService<IQueryHandler<GetOrderQuery, OrderResponse>>();
        var id = Guid.NewGuid();

        var result = await handler.Handle(new GetOrderQuery(id), CancellationToken.None);

        result.Error.ShouldBe(OrderErrors.NotFound(id));
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Page_size_over_the_limit_is_rejected()
    {
        var handler = _provider.GetRequiredService<IQueryHandler<ListOrdersQuery, PagedResult<OrderSummaryResponse>>>();

        var result = await handler.Handle(new ListOrdersQuery(Guid.NewGuid(), PageSize: 1000), CancellationToken.None);

        result.Error.ShouldBeOfType<ValidationError>();
    }

    [Fact]
    public async Task List_returns_only_that_customers_orders_with_paging()
    {
        var customerId = Guid.NewGuid();
        for (var i = 0; i < 3; i++)
        {
            _queries.Orders.Add(new OrderResponse(Guid.NewGuid(), customerId, "Submitted", 10m, "USD", DateTimeOffset.UtcNow, null, []));
        }

        _queries.Orders.Add(new OrderResponse(Guid.NewGuid(), Guid.NewGuid(), "Submitted", 10m, "USD", DateTimeOffset.UtcNow, null, []));
        var handler = _provider.GetRequiredService<IQueryHandler<ListOrdersQuery, PagedResult<OrderSummaryResponse>>>();

        var result = await handler.Handle(new ListOrdersQuery(customerId, Page: 1, PageSize: 2), CancellationToken.None);

        var page = result.Value;
        page.Items.Count.ShouldBe(2);
        page.TotalCount.ShouldBe(3);
        page.HasNextPage.ShouldBeTrue();
    }
}
