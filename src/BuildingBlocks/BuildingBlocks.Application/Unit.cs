namespace BuildingBlocks.Application;

/// <summary>"Nothing" as a value, for commands that succeed without returning data: ICommand&lt;Unit&gt;.</summary>
public readonly record struct Unit
{
    public static readonly Unit Value;
}
