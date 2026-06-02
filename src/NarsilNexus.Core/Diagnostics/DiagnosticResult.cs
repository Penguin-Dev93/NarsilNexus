namespace NarsilNexus.Core.Diagnostics;

public sealed record DiagnosticResult
{
    public required DiagnosticToolId ToolId { get; init; }
    public DiagnosticStatus Status { get; init; } = DiagnosticStatus.Pending;
    public long? DurationMs { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string? Error { get; init; }
    public object? Details { get; init; }
    public string? RawOutput { get; init; }
}

