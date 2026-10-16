using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using Inventory.Application.Abstractions;
using Inventory.Domain;

namespace Inventory.Application.Reservations;

/// <summary>The order was rejected or cancelled: give its reserved stock back.</summary>
public sealed record ReleaseStockCommand(Guid OrderId) : ICommand<Unit>;

/// <summary>The order was confirmed: the reserved stock leaves the warehouse.</summary>
public sealed record CommitStockCommand(Guid OrderId) : ICommand<Unit>;

internal sealed class ReleaseStockCommandHandler(
    IProductRepository products,
    IStockReservationRepository reservations,
    IUnitOfWork unitOfWork,
    IProductCache cache,
    TimeProvider timeProvider)
    : ICommandHandler<ReleaseStockCommand, Unit>
{
    public async Task<Result<Unit>> Handle(ReleaseStockCommand command, CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByOrderIdAsync(command.OrderId, cancellationToken);

        if (reservation is null)
        {
            // Cancelled before we reserved anything (messages arrived out of order).
            // Leave a tombstone so a late "reserve" for this order is refused.
            reservations.Add(StockReservation.CancelledBeforeReserved(command.OrderId, timeProvider.GetUtcNow()));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }

        if (reservation.Status != ReservationStatus.Reserved)
        {
            return Unit.Value; // Already released or committed: nothing to do (idempotent).
        }

        await ApplyToProductsAsync(products, reservation, (product, quantity) => product.Release(quantity), cancellationToken);
        reservation.Release();

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(cancellationToken);
        return Unit.Value;
    }

    internal static async Task ApplyToProductsAsync(
        IProductRepository products,
        StockReservation reservation,
        Func<Product, int, Result> change,
        CancellationToken cancellationToken)
    {
        var found = await products.GetBySkusAsync([.. reservation.Items.Select(i => i.Sku)], cancellationToken);

        foreach (var item in reservation.Items)
        {
            var result = change(found[item.Sku], item.Quantity);
            if (result.IsFailure)
            {
                // The reservation guarantees these numbers; if they don't match, data is corrupt.
                throw new InvalidOperationException($"Reservation for order {reservation.OrderId} is inconsistent: {result.Error.Description}");
            }
        }
    }
}

internal sealed class CommitStockCommandHandler(
    IProductRepository products,
    IStockReservationRepository reservations,
    IUnitOfWork unitOfWork,
    IProductCache cache)
    : ICommandHandler<CommitStockCommand, Unit>
{
    public async Task<Result<Unit>> Handle(CommitStockCommand command, CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByOrderIdAsync(command.OrderId, cancellationToken);
        if (reservation is null || reservation.Status != ReservationStatus.Reserved)
        {
            return Unit.Value; // Nothing reserved, or already done (idempotent).
        }

        await ReleaseStockCommandHandler.ApplyToProductsAsync(
            products, reservation, (product, quantity) => product.Commit(quantity), cancellationToken);
        reservation.Commit();

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(cancellationToken);
        return Unit.Value;
    }
}
