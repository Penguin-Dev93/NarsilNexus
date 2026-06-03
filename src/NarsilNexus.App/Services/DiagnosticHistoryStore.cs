using System.IO;
using System.Text.Json;
using NarsilNexus.App.ViewModels;

namespace NarsilNexus.App.Services;

public sealed class DiagnosticHistoryStore
{
    private const int MaxHistoryEntries = 200;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _path;

    public DiagnosticHistoryStore(string historyDirectory)
    {
        _path = Path.Combine(historyDirectory, "diagnostic-history.json");
    }

    public IReadOnlyList<DiagnosticHistoryEntry> Load()
    {
        if (File.Exists(_path) is false)
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<List<DiagnosticHistoryEntry>>(json, SerializerOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void Save(IEnumerable<DiagnosticHistoryEntry> entries)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var normalized = entries
            .OrderByDescending(entry => entry.StartedAt)
            .Take(MaxHistoryEntries)
            .ToList();
        var json = JsonSerializer.Serialize(normalized, SerializerOptions);
        File.WriteAllText(_path, json);
    }
}
