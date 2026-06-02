using System.Net;

namespace NarsilNexus.Core.Targets;

public static class TargetNormalizer
{
    public static NormalizedTarget Normalize(string value)
    {
        var trimmed = value.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return new NormalizedTarget(trimmed, TargetKind.Unknown, null, "Target is required.");
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) &&
            (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
             uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            return new NormalizedTarget(trimmed, TargetKind.Url, uri.Host, null);
        }

        if (IPAddress.TryParse(trimmed, out var address))
        {
            var kind = address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                ? TargetKind.IPv4
                : TargetKind.IPv6;

            return new NormalizedTarget(trimmed, kind, trimmed, null);
        }

        if (trimmed.Contains('.', StringComparison.Ordinal))
        {
            return new NormalizedTarget(trimmed, TargetKind.Domain, trimmed, null);
        }

        return new NormalizedTarget(trimmed, TargetKind.Hostname, trimmed, null);
    }
}

