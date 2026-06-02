namespace NarsilNexus.Core.Diagnostics;

public enum DiagnosticStatus
{
    Pending = 0,
    Running = 1,
    Pass = 2,
    Warning = 3,
    Fail = 4,
    Skipped = 5,
    Canceled = 6,
    Blocked = 7
}

