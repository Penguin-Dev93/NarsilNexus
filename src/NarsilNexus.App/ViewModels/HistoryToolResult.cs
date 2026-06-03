using NarsilNexus.Core.Diagnostics;

namespace NarsilNexus.App.ViewModels;

public sealed record HistoryToolResult(
    DiagnosticToolId Id,
    string Name,
    DiagnosticStatus Status,
    string Summary,
    string RawOutput);
