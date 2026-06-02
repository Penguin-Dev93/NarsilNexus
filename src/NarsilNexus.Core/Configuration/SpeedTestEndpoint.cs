namespace NarsilNexus.Core.Configuration;

public sealed record SpeedTestEndpoint
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public required Uri Url { get; init; }
}

