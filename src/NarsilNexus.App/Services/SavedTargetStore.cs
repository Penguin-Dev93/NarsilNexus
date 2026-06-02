using System.IO;
using System.Text.Json;
using NarsilNexus.App.ViewModels;

namespace NarsilNexus.App.Services;

public sealed class SavedTargetStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _path;

    public SavedTargetStore(string configDirectory)
    {
        _path = Path.Combine(configDirectory, "saved-targets.json");
    }

    public IReadOnlyList<SavedTargetOption> Load()
    {
        if (File.Exists(_path) is false)
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<List<SavedTargetOption>>(json, SerializerOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void Save(IEnumerable<SavedTargetOption> targets)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var json = JsonSerializer.Serialize(targets, SerializerOptions);
        File.WriteAllText(_path, json);
    }
}
