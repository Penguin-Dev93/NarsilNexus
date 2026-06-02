namespace NarsilNexus.Core.Tools;

public sealed class BundledToolLocator
{
    private readonly string _baseDirectory;

    public BundledToolLocator(string baseDirectory)
    {
        _baseDirectory = baseDirectory;
    }

    public string Iperf3ExecutablePath => Path.Combine(_baseDirectory, "Tools", "iperf3", "iperf3.exe");

    public bool IsIperf3Available() => File.Exists(Iperf3ExecutablePath);
}

