using NarsilNexus.Core.Targets;

namespace NarsilNexus.Core.Configuration;

public sealed record AppConfiguration
{
    public int SchemaVersion { get; init; } = 1;
    public IReadOnlyList<SavedTarget> SavedTargets { get; init; } = [];
    public ToolDefaults ToolDefaults { get; init; } = new();
    public IReadOnlyList<SpeedTestEndpoint> SpeedTestEndpoints { get; init; } = [];
    public string Theme { get; init; } = "System";
}

