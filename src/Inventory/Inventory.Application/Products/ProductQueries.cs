using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using FluentValidation;
using Inventory.Application.Abstractions;
using Inventory.Domain;

namespace Inventory.Application.Products;

public sealed record ProductResponse(string Sku, string Name, decimal Price, string Currency, int QuantityAvailable);

// ---------- Get one product ----------

public sealed record GetProductQuery(string Sku) : IQuery<ProductResponse>;

internal sealed class GetProductQueryHandler(IProductQueries queries) : IQueryHandler<GetProductQuery, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(GetProductQuery query, CancellationToken cancellationToken)
    {
        var product = await queries.GetBySkuAsync(query.Sku, cancellationToken);
        if (product is null)
        {
            return InventoryErrors.ProductNotFound(query.Sku);
        }

        return product;
    }
}

// ---------- List products ----------

public sealed record ListProductsQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<ProductResponse>>;

internal sealed class ListProductsQueryValidator : AbstractValidator<ListProductsQuery>
{
    public ListProductsQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
    }
}

internal sealed class ListProductsQueryHandler(IProductQueries queries)
    : IQueryHandler<ListProductsQuery, PagedResult<ProductResponse>>
{
    public async Task<Result<PagedResult<ProductResponse>>> Handle(ListProductsQuery query, CancellationToken cancellationToken) =>
        await queries.ListAsync(query.Page, query.PageSize, cancellationToken);
}
