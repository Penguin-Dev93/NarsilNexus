namespace NarsilNexus.Core.Configuration;

public sealed record ToolDefaults
{
    public string? CustomDnsServer { get; init; }
    public int DefaultTcpPort { get; init; } = 443;
    public int HttpTimeoutSeconds { get; init; } = 10;
    public int IperfPort { get; init; } = 5201;
    public int IperfDurationSeconds { get; init; } = 10;
    public int IperfParallelStreams { get; init; } = 1;
    public bool IperfReverseMode { get; init; }
}

