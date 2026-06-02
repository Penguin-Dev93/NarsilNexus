namespace NarsilNexus.Core.Storage;

public sealed class NarsilPaths
{
    public NarsilPaths(string appDataRoot)
    {
        AppDataRoot = appDataRoot;
        ConfigDirectory = Path.Combine(appDataRoot, "Config");
        HistoryDirectory = Path.Combine(appDataRoot, "History");
        ReportsDirectory = Path.Combine(appDataRoot, "Reports");
        LogsDirectory = Path.Combine(appDataRoot, "Logs");
    }

    public string AppDataRoot { get; }
    public string ConfigDirectory { get; }
    public string HistoryDirectory { get; }
    public string ReportsDirectory { get; }
    public string LogsDirectory { get; }

    public string AppConfigurationFile => Path.Combine(ConfigDirectory, "appsettings.json");

    public static NarsilPaths CreateDefault()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return new NarsilPaths(Path.Combine(appData, "NarsilNexus"));
    }

    public void EnsureCreated()
    {
        Directory.CreateDirectory(AppDataRoot);
        Directory.CreateDirectory(ConfigDirectory);
        Directory.CreateDirectory(HistoryDirectory);
        Directory.CreateDirectory(ReportsDirectory);
        Directory.CreateDirectory(LogsDirectory);
    }
}

