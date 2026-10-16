using BuildingBlocks.Domain;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Web;

public static class ResultExtensions
{
    /// <summary>
    /// Turns a domain/application error into a standard RFC 9457 "problem details" response,
    /// so every API reports errors in the same JSON shape.
    /// </summary>
    public static IResult ToProblem(this Error error)
    {
        if (error is ValidationError validation)
        {
            var errors = validation.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());

            return TypedResults.ValidationProblem(errors, title: validation.Description);
        }

        var statusCode = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };

        return TypedResults.Problem(
            title: error.Code,
            detail: error.Description,
            statusCode: statusCode);
    }
}
