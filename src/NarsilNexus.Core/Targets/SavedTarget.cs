namespace NarsilNexus.Core.Targets;

public sealed record SavedTarget
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public required string Target { get; init; }
    public TargetKind TargetKind { get; init; }
    public string? Notes { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}

