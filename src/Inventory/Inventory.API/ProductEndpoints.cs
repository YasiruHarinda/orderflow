using BuildingBlocks.Application;
using BuildingBlocks.Web;
using Inventory.Application.Products;

namespace Inventory.API;

internal static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/products").WithTags("Products");

        group.MapGet("/", ListProducts)
            .WithName("ListProducts")
            .WithSummary("List products with their available stock.")
            .Produces<PagedResult<ProductResponse>>();

        group.MapGet("/{sku}", GetProduct)
            .WithName("GetProduct")
            .Produces<ProductResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{sku}/stock", UpdateStock)
            .WithName("UpdateStock")
            .WithSummary("Set the quantity physically in the warehouse.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        return app;
    }

    private static async Task<IResult> ListProducts(
        int? page,
        int? pageSize,
        IQueryHandler<ListProductsQuery, PagedResult<ProductResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ListProductsQuery(page ?? 1, pageSize ?? 20), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> GetProduct(
        string sku,
        IQueryHandler<GetProductQuery, ProductResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetProductQuery(sku), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> UpdateStock(
        string sku,
        UpdateStockRequest request,
        ICommandHandler<UpdateStockCommand, Unit> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new UpdateStockCommand(sku, request.QuantityOnHand), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }
}

internal sealed record UpdateStockRequest(int QuantityOnHand);
