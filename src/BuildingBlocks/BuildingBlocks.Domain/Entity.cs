namespace BuildingBlocks.Domain;

/// <summary>
/// Something with an identity. Two entities are "the same thing" when their Ids match,
/// even if their other values are different.
/// </summary>
public abstract class Entity<TId>
    where TId : notnull
{
    protected Entity(TId id) => Id = id;

    public TId Id { get; private init; }
}
