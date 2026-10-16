using FluentValidation;
using Orders.Domain;

namespace Orders.Application.PlaceOrder;

/// <summary>
/// Checks the SHAPE of the input (required fields, lengths, formats) and reports every problem at once.
/// Business rules such as "no duplicate SKUs" stay in the Order aggregate.
/// </summary>
internal sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(c => c.CustomerId).NotEmpty();

        RuleFor(c => c.Currency)
            .NotEmpty()
            .Matches("^[A-Z]{3}$").WithMessage("Currency must be a 3-letter ISO code such as USD.");

        RuleFor(c => c.Lines)
            .NotEmpty()
            .Must(lines => lines is null || lines.Count <= Order.MaxLines)
            .WithMessage($"An order can contain at most {Order.MaxLines} lines.");

        RuleForEach(c => c.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Sku).NotEmpty().MaximumLength(32);
            line.RuleFor(l => l.ProductName).NotEmpty().MaximumLength(200);
            line.RuleFor(l => l.UnitPrice).GreaterThan(0);
            line.RuleFor(l => l.Quantity).InclusiveBetween(1, Order.MaxQuantityPerLine);
        });
    }
}
