using BuildingBlocks.Application;
using BuildingBlocks.Web;
using Orders.Application.GetOrder;
using Orders.Application.ListOrders;
using Orders.Application.PlaceOrder;

namespace Orders.API;

/// <summary>
/// The HTTP layer is thin: turn the request into a command/query, call the handler,
/// turn the Result into an HTTP response. No business logic here.
/// </summary>
internal static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/orders").WithTags("Orders");

        group.MapPost("/", PlaceOrder)
            .WithName("PlaceOrder")
            .WithSummary("Place a new order. Stock is reserved asynchronously.")
            .Produces<PlaceOrderResponse>(StatusCodes.Status202Accepted)
            .ProducesValidationProblem();

        group.MapGet("/{orderId:guid}", GetOrder)
            .WithName("GetOrder")
            .WithSummary("Get one order with its lines and current status.")
            .Produces<OrderResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", ListOrders)
            .WithName("ListOrders")
            .WithSummary("List a customer's orders, newest first.")
            .Produces<PagedResult<OrderSummaryResponse>>()
            .ProducesValidationProblem();

        return app;
    }

    private static async Task<IResult> PlaceOrder(
        PlaceOrderCommand command,
        ICommandHandler<PlaceOrderCommand, Guid> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(command, cancellationToken);

        // 202 Accepted (not 201 Created): the order exists, but it is not finished yet;
        // stock reservation happens in the background. The Location header says where
        // to check its status.
        return result.IsSuccess
            ? TypedResults.AcceptedAtRoute(new PlaceOrderResponse(result.Value), "GetOrder", new { orderId = result.Value })
            : result.Error.ToProblem();
    }

    private static async Task<IResult> GetOrder(
        Guid orderId,
        IQueryHandler<GetOrderQuery, OrderResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetOrderQuery(orderId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> ListOrders(
        Guid customerId,
        int? page,
        int? pageSize,
        IQueryHandler<ListOrdersQuery, PagedResult<OrderSummaryResponse>> handler,
        CancellationToken cancellationToken)
    {
        var query = new ListOrdersQuery(customerId, page ?? 1, pageSize ?? 20);
        var result = await handler.Handle(query, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}

internal sealed record PlaceOrderResponse(Guid OrderId);
