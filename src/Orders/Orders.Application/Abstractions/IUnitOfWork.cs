namespace Orders.Application.Abstractions;

/// <summary>Saves every pending change in ONE database transaction: all or nothing.</summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
