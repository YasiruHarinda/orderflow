using Orders.Application.PlaceOrder;

namespace Orders.UnitTests.Application;

public class PlaceOrderCommandValidatorTests
{
    private readonly PlaceOrderCommandValidator _validator = new();

    private static PlaceOrderCommand ValidCommand() =>
        new(Guid.NewGuid(), "USD", [new PlaceOrderLine("SKU-1", "Keyboard", 49.99m, 1)]);

    [Fact]
    public void Valid_command_passes()
    {
        _validator.Validate(ValidCommand()).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("usd")]
    [InlineData("US")]
    [InlineData("DOLLAR")]
    public void Invalid_currency_fails(string currency)
    {
        var result = _validator.Validate(ValidCommand() with { Currency = currency });

        result.Errors.ShouldContain(e => e.PropertyName == nameof(PlaceOrderCommand.Currency));
    }

    [Fact]
    public void All_problems_are_reported_at_once()
    {
        var command = new PlaceOrderCommand(Guid.Empty, "usd", [new PlaceOrderLine("", "", -1m, 0)]);

        var result = _validator.Validate(command);

        // One error per bad field, so the user can fix everything in one go.
        result.Errors.Select(e => e.PropertyName).ShouldBe(
            [
                "CustomerId",
                "Currency",
                "Lines[0].Sku",
                "Lines[0].ProductName",
                "Lines[0].UnitPrice",
                "Lines[0].Quantity",
            ],
            ignoreOrder: true);
    }
}
