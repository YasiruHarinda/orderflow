namespace BuildingBlocks.Domain;

/// <summary>Something important that happened inside the domain.</summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
