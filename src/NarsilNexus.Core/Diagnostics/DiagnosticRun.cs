using NarsilNexus.Core.Targets;

namespace NarsilNexus.Core.Diagnostics;

public sealed record DiagnosticRun
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required NormalizedTarget Target { get; init; }
    public Guid? SavedTargetId { get; init; }
    public IReadOnlyList<DiagnosticToolId> SelectedTools { get; init; } = [];
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; init; }
    public DiagnosticStatus OverallStatus { get; init; } = DiagnosticStatus.Pending;
    public IReadOnlyList<DiagnosticResult> Results { get; init; } = [];
}

