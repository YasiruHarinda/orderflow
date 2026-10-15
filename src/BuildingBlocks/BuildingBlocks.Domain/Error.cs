namespace BuildingBlocks.Domain;

/// <summary>
/// An expected failure, such as invalid input.
/// <see cref="Code"/> is stable and machine-readable (e.g. "Order.NoLines");
/// <see cref="Description"/> is for humans.
/// </summary>
public sealed record Error(string Code, string Description, ErrorType Type = ErrorType.Validation)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}

/// <summary>Tells the API layer which HTTP status code to return (400, 404 or 409).</summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
}
