using NarsilNexus.Core.Diagnostics;

namespace NarsilNexus.App.ViewModels;

public sealed record DiagnosticHistoryEntry(
    Guid Id,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    string Target,
    string SavedTargetName,
    string DnsServer,
    string SpeedEndpointName,
    string SpeedEndpointType,
    string SpeedEndpointUrl,
    string PdfPath,
    IReadOnlyList<HistoryToolResult> Results)
{
    public DiagnosticStatus OverallStatus
    {
        get
        {
            if (Results.Any(result => result.Status is DiagnosticStatus.Fail or DiagnosticStatus.Blocked))
            {
                return DiagnosticStatus.Fail;
            }

            if (Results.Any(result => result.Status == DiagnosticStatus.Warning))
            {
                return DiagnosticStatus.Warning;
            }

            if (Results.Any(result => result.Status == DiagnosticStatus.Canceled))
            {
                return DiagnosticStatus.Canceled;
            }

            return Results.Any(result => result.Status == DiagnosticStatus.Pass)
                ? DiagnosticStatus.Pass
                : DiagnosticStatus.Skipped;
        }
    }
}
