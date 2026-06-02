namespace NarsilNexus.App.ViewModels;

public sealed record SavedTargetOption(string Name, string Target)
{
    public string DisplayName => $"{Name}  ({Target})";
}
