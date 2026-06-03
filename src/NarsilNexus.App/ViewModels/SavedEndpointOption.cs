namespace NarsilNexus.App.ViewModels;

public sealed record SavedEndpointOption(
    string Name = "",
    string Url = "",
    string EndpointType = "JSON Results API",
    string PingUrl = "",
    string DownloadUrl = "",
    string UploadUrl = "",
    int DurationSeconds = 10,
    int PingSamples = 10)
{
    public string DisplayName => string.IsNullOrWhiteSpace(EndpointType)
        ? $"{Name}  ({Url})"
        : $"{Name}  [{EndpointType}]";
}
