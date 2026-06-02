namespace NarsilNexus.Core.Targets;

public sealed record NormalizedTarget(
    string OriginalValue,
    TargetKind TargetKind,
    string? Host,
    string? ValidationError)
{
    public bool IsValid => string.IsNullOrWhiteSpace(ValidationError) is false ? false : TargetKind != TargetKind.Unknown;
}

