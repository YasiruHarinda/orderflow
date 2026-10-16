using BuildingBlocks.Domain;
using FluentValidation;

namespace BuildingBlocks.Application;

/// <summary>
/// Wraps every command handler: runs all FluentValidation validators for the command first.
/// If any fail, the real handler never runs and a <see cref="ValidationError"/> is returned.
/// </summary>
public sealed class ValidationCommandDecorator<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> inner,
    IEnumerable<IValidator<TCommand>> validators)
    : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
    {
        var error = await Validation.RunAsync(command, validators, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        return await inner.Handle(command, cancellationToken);
    }
}

/// <summary>Same as <see cref="ValidationCommandDecorator{TCommand, TResponse}"/>, for queries.</summary>
public sealed class ValidationQueryDecorator<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> inner,
    IEnumerable<IValidator<TQuery>> validators)
    : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
    {
        var error = await Validation.RunAsync(query, validators, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        return await inner.Handle(query, cancellationToken);
    }
}

internal static class Validation
{
    public static async Task<ValidationError?> RunAsync<T>(
        T request,
        IEnumerable<IValidator<T>> validators,
        CancellationToken cancellationToken)
    {
        var context = new ValidationContext<T>(request);
        var errors = new List<Error>();

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(context, cancellationToken);
            errors.AddRange(result.Errors.Select(failure => new Error(failure.PropertyName, failure.ErrorMessage)));
        }

        return errors.Count == 0 ? null : new ValidationError(errors);
    }
}
