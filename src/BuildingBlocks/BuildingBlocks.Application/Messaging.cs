using BuildingBlocks.Domain;

namespace BuildingBlocks.Application;

/// <summary>A request to CHANGE something. TResponse is what comes back on success (often an Id).</summary>
public interface ICommand<TResponse>;

/// <summary>A request to READ something. It must never change data.</summary>
public interface IQuery<TResponse>;

/// <summary>The one class that carries out a specific command.</summary>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
}

/// <summary>The one class that answers a specific query.</summary>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
}
