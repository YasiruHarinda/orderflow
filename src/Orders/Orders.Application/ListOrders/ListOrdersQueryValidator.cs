using FluentValidation;

namespace Orders.Application.ListOrders;

internal sealed class ListOrdersQueryValidator : AbstractValidator<ListOrdersQuery>
{
    public const int MaxPageSize = 100;

    public ListOrdersQueryValidator()
    {
        RuleFor(q => q.CustomerId).NotEmpty();
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);

        // A cap stops one request from asking for a million rows.
        RuleFor(q => q.PageSize).InclusiveBetween(1, MaxPageSize);
    }
}
