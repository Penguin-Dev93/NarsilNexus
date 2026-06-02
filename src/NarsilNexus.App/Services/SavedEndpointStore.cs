using System.IO;
using System.Text.Json;
using NarsilNexus.App.ViewModels;

namespace NarsilNexus.App.Services;

public sealed class SavedEndpointStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _path;

    public SavedEndpointStore(string configDirectory)
    {
        _path = Path.Combine(configDirectory, "speed-endpoints.json");
    }

    public IReadOnlyList<SavedEndpointOption> Load()
    {
        if (File.Exists(_path) is false)
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<List<SavedEndpointOption>>(json, SerializerOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void Save(IEnumerable<SavedEndpointOption> endpoints)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var json = JsonSerializer.Serialize(endpoints, SerializerOptions);
        File.WriteAllText(_path, json);
    }
}
