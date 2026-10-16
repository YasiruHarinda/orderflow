using Inventory.Domain;

namespace Inventory.UnitTests;

public class ProductTests
{
    private static Product NewProduct(int onHand = 10) => Product.Create("KB-001", "Keyboard", 89.99m, "USD", onHand).Value;

    [Fact]
    public void Reserve_reduces_available_but_not_on_hand()
    {
        var product = NewProduct(onHand: 10);

        product.Reserve(3).IsSuccess.ShouldBeTrue();

        product.QuantityOnHand.ShouldBe(10);
        product.QuantityReserved.ShouldBe(3);
        product.QuantityAvailable.ShouldBe(7);
    }

    [Fact]
    public void Cannot_reserve_more_than_available()
    {
        var product = NewProduct(onHand: 2);

        var result = product.Reserve(3);

        result.Error.Code.ShouldBe("Inventory.InsufficientStock");
        product.QuantityReserved.ShouldBe(0);
    }

    [Fact]
    public void Release_makes_stock_available_again()
    {
        var product = NewProduct(onHand: 10);
        product.Reserve(4);

        product.Release(4).IsSuccess.ShouldBeTrue();

        product.QuantityAvailable.ShouldBe(10);
    }

    [Fact]
    public void Commit_removes_stock_from_the_warehouse()
    {
        var product = NewProduct(onHand: 10);
        product.Reserve(4);

        product.Commit(4).IsSuccess.ShouldBeTrue();

        product.QuantityOnHand.ShouldBe(6);
        product.QuantityReserved.ShouldBe(0);
    }

    [Fact]
    public void Stock_cannot_be_set_below_what_is_reserved()
    {
        var product = NewProduct(onHand: 10);
        product.Reserve(5);

        var result = product.SetStockOnHand(4);

        result.Error.Code.ShouldBe("Inventory.StockBelowReserved");
        product.QuantityOnHand.ShouldBe(10);
    }

    [Fact]
    public void Reservation_can_only_be_released_once()
    {
        var reservation = StockReservation.Reserve(Guid.NewGuid(), [new ReservedItem("KB-001", 1)], DateTimeOffset.UtcNow);

        reservation.Release().IsSuccess.ShouldBeTrue();
        reservation.Release().IsFailure.ShouldBeTrue();
        reservation.Status.ShouldBe(ReservationStatus.Released);
    }
}
