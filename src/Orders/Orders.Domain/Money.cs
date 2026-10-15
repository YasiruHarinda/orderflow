namespace Orders.Domain;

/// <summary>
/// A value object: an amount in a currency. It has no Id and never changes;
/// "adding" creates a new Money. Records give us equality by value for free,
/// so new Money(10, "USD") == new Money(10, "USD").
/// </summary>
public sealed record Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency) => new(0m, currency);

    public Money Add(Money other)
    {
        if (other.Currency != Currency)
        {
            // Mixing currencies is a programming bug, not user input, so we throw.
            throw new InvalidOperationException($"Cannot add {other.Currency} to {Currency}.");
        }

        return this with { Amount = Amount + other.Amount };
    }

    public Money Multiply(int factor) => this with { Amount = Amount * factor };
}
