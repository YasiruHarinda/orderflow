using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using FluentValidation;
using Inventory.Application.Abstractions;
using Inventory.Domain;

namespace Inventory.Application.Products;

/// <summary>Admin action: set how many units are physically in the warehouse.</summary>
public sealed record UpdateStockCommand(string Sku, int QuantityOnHand) : ICommand<Unit>;

internal sealed class UpdateStockCommandValidator : AbstractValidator<UpdateStockCommand>
{
    public UpdateStockCommandValidator()
    {
        RuleFor(c => c.Sku).NotEmpty().MaximumLength(32);
        RuleFor(c => c.QuantityOnHand).InclusiveBetween(0, 1_000_000);
    }
}

internal sealed class UpdateStockCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork,
    IProductCache cache)
    : ICommandHandler<UpdateStockCommand, Unit>
{
    public async Task<Result<Unit>> Handle(UpdateStockCommand command, CancellationToken cancellationToken)
    {
        var product = await products.GetBySkuAsync(command.Sku, cancellationToken);
        if (product is null)
        {
            return InventoryErrors.ProductNotFound(command.Sku);
        }

        var result = product.SetStockOnHand(command.QuantityOnHand);
        if (result.IsFailure)
        {
            return result.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(cancellationToken);
        return Unit.Value;
    }
}
