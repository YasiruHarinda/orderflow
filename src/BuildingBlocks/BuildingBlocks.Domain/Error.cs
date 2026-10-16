namespace BuildingBlocks.Domain;

/// <summary>
/// An expected failure, such as invalid input.
/// <see cref="Code"/> is stable and machine-readable (e.g. "Order.NoLines");
/// <see cref="Description"/> is for humans.
/// </summary>
public record Error(string Code, string Description, ErrorType Type = ErrorType.Validation)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}

/// <summary>Several input problems found at once, e.g. one per invalid field.</summary>
public sealed record ValidationError(IReadOnlyList<Error> Errors)
    : Error("Validation.Failed", "One or more validation errors occurred.");

/// <summary>Tells the API layer which HTTP status code to return (400, 404 or 409).</summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
}
