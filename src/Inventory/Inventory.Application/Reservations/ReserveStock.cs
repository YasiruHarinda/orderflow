using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using FluentValidation;
using Inventory.Application.Abstractions;
using Inventory.Domain;

namespace Inventory.Application.Reservations;

/// <summary>Reserve stock for every line of an order. Sent when an order is submitted.</summary>
public sealed record ReserveStockCommand(Guid OrderId, IReadOnlyList<ReserveStockLine> Lines) : ICommand<Unit>;

public sealed record ReserveStockLine(string Sku, int Quantity);

internal sealed class ReserveStockCommandValidator : AbstractValidator<ReserveStockCommand>
{
    public ReserveStockCommandValidator()
    {
        RuleFor(c => c.OrderId).NotEmpty();
        RuleFor(c => c.Lines)
            .NotEmpty()
            .Must(lines => lines is null || lines.Select(l => l.Sku).Distinct().Count() == lines.Count)
            .WithMessage("Each SKU may appear only once.");
        RuleForEach(c => c.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Sku).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        });
    }
}

/// <summary>
/// ALL-OR-NOTHING: either every line is reserved, or none is.
/// Step 1 only checks. Step 2 changes data, and only runs if every check passed,
/// so a failure can never leave half an order reserved.
/// </summary>
internal sealed class ReserveStockCommandHandler(
    IProductRepository products,
    IStockReservationRepository reservations,
    IUnitOfWork unitOfWork,
    IProductCache cache,
    TimeProvider timeProvider)
    : ICommandHandler<ReserveStockCommand, Unit>
{
    public async Task<Result<Unit>> Handle(ReserveStockCommand command, CancellationToken cancellationToken)
    {
        // Idempotency: the same message may be delivered twice. If we already handled
        // this order, return the same answer again without reserving twice.
        var existing = await reservations.GetByOrderIdAsync(command.OrderId, cancellationToken);
        if (existing is not null)
        {
            return existing.Status == ReservationStatus.Released
                ? InventoryErrors.OrderCancelled(command.OrderId)
                : Unit.Value;
        }

        var skus = command.Lines.Select(l => l.Sku).ToList();
        var found = await products.GetBySkusAsync(skus, cancellationToken);

        // Step 1: check every line.
        foreach (var line in command.Lines)
        {
            if (!found.TryGetValue(line.Sku, out var product))
            {
                return InventoryErrors.ProductNotFound(line.Sku);
            }

            if (!product.CanReserve(line.Quantity))
            {
                return InventoryErrors.InsufficientStock(line.Sku, line.Quantity, product.QuantityAvailable);
            }
        }

        // Step 2: everything is available, so reserve it all.
        foreach (var line in command.Lines)
        {
            found[line.Sku].Reserve(line.Quantity);
        }

        reservations.Add(StockReservation.Reserve(
            command.OrderId,
            command.Lines.Select(l => new ReservedItem(l.Sku, l.Quantity)),
            timeProvider.GetUtcNow()));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(cancellationToken);
        return Unit.Value;
    }
}
