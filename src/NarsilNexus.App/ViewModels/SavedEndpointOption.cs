namespace NarsilNexus.App.ViewModels;

public sealed record SavedEndpointOption(string Name, string Url)
{
    public string DisplayName => $"{Name}  ({Url})";
}
