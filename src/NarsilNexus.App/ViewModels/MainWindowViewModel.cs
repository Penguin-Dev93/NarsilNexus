using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using NarsilNexus.App.Services;
using NarsilNexus.Core.Diagnostics;
using NarsilNexus.Core.Storage;
using NarsilNexus.Core.Targets;

namespace NarsilNexus.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private bool _isRunning;
    private bool _isDarkMode = true;
    private CancellationTokenSource? _runCancellation;
    private string _targetName = string.Empty;
    private string _targetInput = string.Empty;
    private string _speedEndpointName = string.Empty;
    private string _speedEndpointType = "JSON Results API";
    private string _speedResultsApiUrl = string.Empty;
    private string _speedProbeDownloadUrl = string.Empty;
    private string _speedProbePingUrl = string.Empty;
    private string _speedProbeUploadUrl = string.Empty;
    private int _speedTestDurationSeconds = 10;
    private int _speedPingSamples = 10;
    private SavedTargetOption? _selectedTarget;
    private SavedEndpointOption? _selectedSpeedEndpoint;
    private readonly NarsilPaths _paths;
    private readonly SimplePdfReportWriter _pdfReportWriter = new();
    private readonly SavedTargetStore _savedTargetStore;
    private readonly SavedEndpointStore _savedEndpointStore;

    public MainWindowViewModel()
    {
        _paths = NarsilPaths.CreateDefault();
        _paths.EnsureCreated();
        _savedTargetStore = new SavedTargetStore(_paths.ConfigDirectory);
        _savedEndpointStore = new SavedEndpointStore(_paths.ConfigDirectory);
        AppDataPath = _paths.AppDataRoot;

        SavedTargets = new ObservableCollection<SavedTargetOption>(_savedTargetStore.Load());
        SavedSpeedEndpoints = new ObservableCollection<SavedEndpointOption>(_savedEndpointStore.Load());

        Tools =
        [
            new ToolRunViewModel(DiagnosticToolId.NetTestConnection, "NetTest Connection"),
            new ToolRunViewModel(DiagnosticToolId.Ping, "Ping"),
            new ToolRunViewModel(DiagnosticToolId.DnsLookup, "DNS Lookup"),
            new ToolRunViewModel(DiagnosticToolId.TcpPort, "TCP Port"),
            new ToolRunViewModel(DiagnosticToolId.Http, "HTTP/S"),
            new ToolRunViewModel(DiagnosticToolId.Traceroute, "Traceroute"),
            new ToolRunViewModel(DiagnosticToolId.Rdap, "RDAP"),
            new ToolRunViewModel(DiagnosticToolId.PathMtu, "Path MTU"),
            new ToolRunViewModel(DiagnosticToolId.Iperf3, "iperf3"),
            new ToolRunViewModel(DiagnosticToolId.SpeedTestResults, "Speed Results")
        ];

        ToggleRunCommand = new AsyncRelayCommand(ToggleRunAsync);
        SaveTargetCommand = new RelayCommand(_ => SaveTarget());
        SaveSpeedEndpointCommand = new RelayCommand(_ => SaveSpeedEndpoint());
        SelectAllToolsCommand = new RelayCommand(_ => SetToolSelection(true));
        DeselectAllToolsCommand = new RelayCommand(_ => SetToolSelection(false));
        GeneratePdfCommand = new RelayCommand(_ => GeneratePdf());
        OpenAppDataFolderCommand = new RelayCommand(_ => OpenAppDataFolder());
        ToggleThemeCommand = new RelayCommand(_ => ToggleTheme());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SavedTargetOption> SavedTargets { get; }
    public ObservableCollection<SavedEndpointOption> SavedSpeedEndpoints { get; }
    public IReadOnlyList<string> SpeedEndpointTypes { get; } =
    [
        "JSON Results API",
        "LibreSpeed",
        "HTTP Probe"
    ];
    public ObservableCollection<ToolRunViewModel> Tools { get; }
    public ICommand ToggleRunCommand { get; }
    public ICommand SaveTargetCommand { get; }
    public ICommand SaveSpeedEndpointCommand { get; }
    public ICommand SelectAllToolsCommand { get; }
    public ICommand DeselectAllToolsCommand { get; }
    public ICommand GeneratePdfCommand { get; }
    public ICommand OpenAppDataFolderCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public string AppDataPath { get; }

    public string TargetInput
    {
        get => _targetInput;
        set => SetField(ref _targetInput, value);
    }

    public string TargetName
    {
        get => _targetName;
        set => SetField(ref _targetName, value);
    }

    public string SpeedResultsApiUrl
    {
        get => _speedResultsApiUrl;
        set => SetField(ref _speedResultsApiUrl, value);
    }

    public string SpeedEndpointType
    {
        get => _speedEndpointType;
        set
        {
            if (SetField(ref _speedEndpointType, value))
            {
                if (value.Equals("LibreSpeed", StringComparison.OrdinalIgnoreCase) &&
                    string.IsNullOrWhiteSpace(SpeedProbeDownloadUrl) &&
                    string.IsNullOrWhiteSpace(SpeedResultsApiUrl) is false)
                {
                    ApplyLibreSpeedBaseUrl(SpeedResultsApiUrl);
                }
            }
        }
    }

    public string SpeedProbeDownloadUrl
    {
        get => _speedProbeDownloadUrl;
        set => SetField(ref _speedProbeDownloadUrl, value);
    }

    public string SpeedProbePingUrl
    {
        get => _speedProbePingUrl;
        set => SetField(ref _speedProbePingUrl, value);
    }

    public string SpeedProbeUploadUrl
    {
        get => _speedProbeUploadUrl;
        set => SetField(ref _speedProbeUploadUrl, value);
    }

    public int SpeedTestDurationSeconds
    {
        get => _speedTestDurationSeconds;
        set => SetField(ref _speedTestDurationSeconds, Math.Clamp(value, 3, 60));
    }

    public int SpeedPingSamples
    {
        get => _speedPingSamples;
        set => SetField(ref _speedPingSamples, Math.Clamp(value, 3, 50));
    }

    public string SpeedEndpointName
    {
        get => _speedEndpointName;
        set => SetField(ref _speedEndpointName, value);
    }

    public SavedTargetOption? SelectedTarget
    {
        get => _selectedTarget;
        set
        {
            if (SetField(ref _selectedTarget, value) && value is not null)
            {
                TargetInput = value.Target;
            }
        }
    }

    public SavedEndpointOption? SelectedSpeedEndpoint
    {
        get => _selectedSpeedEndpoint;
        set
        {
            if (SetField(ref _selectedSpeedEndpoint, value) && value is not null)
            {
                SpeedResultsApiUrl = value.Url;
            }
        }
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetField(ref _isRunning, value))
            {
                OnPropertyChanged(nameof(RunButtonText));
                OnPropertyChanged(nameof(RunStatusText));
                OnPropertyChanged(nameof(RunningVisibility));
            }
        }
    }

    public string RunButtonText => IsRunning ? "Cancel" : "Run";
    public string RunStatusText => IsRunning ? "Diagnostics are running..." : "Select tools and run diagnostics.";
    public Visibility RunningVisibility => IsRunning ? Visibility.Visible : Visibility.Collapsed;
    public string ThemeButtonText => _isDarkMode ? "Light Mode" : "Dark Mode";

    private async Task ToggleRunAsync()
    {
        if (IsRunning)
        {
            _runCancellation?.Cancel();
            return;
        }

        await RunDiagnosticsAsync();
    }

    private async Task RunDiagnosticsAsync()
    {
        var selectedTools = Tools.Where(tool => tool.IsSelected).ToList();
        if (selectedTools.Count == 0)
        {
            SetAllSelected(DiagnosticStatus.Warning, "Select at least one diagnostic tool.");
            return;
        }

        var targetInput = GetEffectiveTargetInput();
        var target = TargetNormalizer.Normalize(targetInput);
        var selectedRequiresTarget = selectedTools.Any(tool => tool.Id != DiagnosticToolId.SpeedTestResults);

        if (selectedRequiresTarget && (!target.IsValid || string.IsNullOrWhiteSpace(target.Host)))
        {
            foreach (var tool in selectedTools)
            {
                tool.Status = DiagnosticStatus.Fail;
                tool.Summary = target.ValidationError ?? "Enter a target before running this diagnostic.";
            }
            return;
        }

        _runCancellation = new CancellationTokenSource();
        IsRunning = true;

        try
        {
            foreach (var tool in selectedTools)
            {
                if (_runCancellation.IsCancellationRequested)
                {
                    tool.Status = DiagnosticStatus.Canceled;
                    tool.Summary = "Canceled";
                    continue;
                }

                tool.Status = DiagnosticStatus.Running;
                tool.Summary = "Working...";

                switch (tool.Id)
                {
                    case DiagnosticToolId.NetTestConnection:
                        await RunNetTestAsync(tool, target.Host!, _runCancellation.Token);
                        break;
                    case DiagnosticToolId.Ping:
                        await RunPingAsync(tool, target.Host!, _runCancellation.Token);
                        break;
                    case DiagnosticToolId.DnsLookup:
                        await RunDnsLookupAsync(tool, target.Host!, _runCancellation.Token);
                        break;
                    case DiagnosticToolId.TcpPort:
                        await RunTcpPortAsync(tool, target.Host!, 443, _runCancellation.Token);
                        break;
                    case DiagnosticToolId.Http:
                        await RunHttpAsync(tool, target, _runCancellation.Token);
                        break;
                    case DiagnosticToolId.Traceroute:
                        await RunTracerouteAsync(tool, target.Host!, _runCancellation.Token);
                        break;
                    case DiagnosticToolId.Rdap:
                        await RunRdapAsync(tool, target.Host!, _runCancellation.Token);
                        break;
                    case DiagnosticToolId.PathMtu:
                        await RunPathMtuAsync(tool, target.Host!, _runCancellation.Token);
                        break;
                    case DiagnosticToolId.Iperf3:
                        tool.Status = DiagnosticStatus.Skipped;
                        tool.Summary = "Bundled iperf3.exe has not been added to this build yet.";
                        break;
                    case DiagnosticToolId.SpeedTestResults:
                        await RunSpeedResultsAsync(tool, _runCancellation.Token);
                        break;
                    default:
                        tool.Status = DiagnosticStatus.Skipped;
                        tool.Summary = "Tool is not available in this version.";
                        break;
                }
            }
        }
        finally
        {
            IsRunning = false;
            _runCancellation?.Dispose();
            _runCancellation = null;
        }
    }

    private void SaveTarget()
    {
        var targetInput = GetEffectiveTargetInput();
        var target = TargetNormalizer.Normalize(targetInput);
        if (!target.IsValid || string.IsNullOrWhiteSpace(target.Host))
        {
            SetAllSelected(DiagnosticStatus.Fail, target.ValidationError ?? "Enter a valid target before saving.");
            return;
        }

        var value = target.OriginalValue.Trim();
        var existing = SavedTargets.FirstOrDefault(saved =>
            saved.Target.Equals(value, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            TargetInput = existing.Target;
            return;
        }

        var name = target.TargetKind switch
        {
            _ when string.IsNullOrWhiteSpace(TargetName) is false => TargetName.Trim(),
            TargetKind.IPv4 or TargetKind.IPv6 => value,
            TargetKind.Url => new Uri(value).Host,
            _ => value
        };

        SavedTargets.Add(new SavedTargetOption(name, value));
        _savedTargetStore.Save(SavedTargets);
        TargetName = string.Empty;
    }

    private void SetToolSelection(bool isSelected)
    {
        foreach (var tool in Tools)
        {
            tool.IsSelected = isSelected;
        }
    }

    private void SaveSpeedEndpoint()
    {
        var value = SpeedResultsApiUrl.Trim();
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) is false ||
            (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) is false &&
             uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) is false))
        {
            SetToolStatus(DiagnosticToolId.SpeedTestResults, DiagnosticStatus.Fail, "Enter a valid HTTP/S Speed API URL before saving.");
            return;
        }

        var existing = SavedSpeedEndpoints.FirstOrDefault(endpoint =>
            endpoint.Url.Equals(value, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            SpeedResultsApiUrl = existing.Url;
            return;
        }

        var name = string.IsNullOrWhiteSpace(SpeedEndpointName) ? uri.Host : SpeedEndpointName.Trim();
        SavedSpeedEndpoints.Add(new SavedEndpointOption(name, value));
        _savedEndpointStore.Save(SavedSpeedEndpoints);
        SpeedEndpointName = string.Empty;
    }

    private static async Task RunNetTestAsync(ToolRunViewModel tool, string host, CancellationToken cancellationToken)
    {
        var messages = new List<string>();

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
            messages.Add(addresses.Length == 0 ? "DNS: no addresses" : $"DNS: {addresses[0]}");

            using var tcpClient = new System.Net.Sockets.TcpClient();
            var stopwatch = Stopwatch.StartNew();
            await tcpClient.ConnectAsync(host, 443, cancellationToken);
            stopwatch.Stop();
            messages.Add($"TCP 443: connected in {stopwatch.ElapsedMilliseconds} ms");

            tool.Status = DiagnosticStatus.Pass;
            tool.Summary = string.Join("; ", messages);
        }
        catch (OperationCanceledException)
        {
            tool.Status = DiagnosticStatus.Canceled;
            tool.Summary = "Canceled";
        }
        catch (Exception ex)
        {
            messages.Add(ex.Message);
            tool.Status = messages.Any(message => message.StartsWith("DNS:", StringComparison.Ordinal))
                ? DiagnosticStatus.Warning
                : DiagnosticStatus.Fail;
            tool.Summary = string.Join("; ", messages);
        }
    }

    private static async Task RunPingAsync(ToolRunViewModel tool, string host, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, 4000);

            tool.Status = reply.Status == IPStatus.Success ? DiagnosticStatus.Pass : DiagnosticStatus.Warning;
            tool.Summary = reply.Status == IPStatus.Success
                ? $"Reply from {reply.Address}, {reply.RoundtripTime} ms"
                : $"Ping {reply.Status}";
        }
        catch (OperationCanceledException)
        {
            tool.Status = DiagnosticStatus.Canceled;
            tool.Summary = "Canceled";
        }
        catch (Exception ex)
        {
            tool.Status = DiagnosticStatus.Fail;
            tool.Summary = ex.Message;
        }
    }

    private static async Task RunDnsLookupAsync(ToolRunViewModel tool, string host, CancellationToken cancellationToken)
    {
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
            if (addresses.Length == 0)
            {
                tool.Status = DiagnosticStatus.Warning;
                tool.Summary = "No DNS addresses returned.";
                return;
            }

            tool.Status = DiagnosticStatus.Pass;
            tool.Summary = string.Join(", ", addresses.Select(address => address.ToString()));
        }
        catch (OperationCanceledException)
        {
            tool.Status = DiagnosticStatus.Canceled;
            tool.Summary = "Canceled";
        }
        catch (Exception ex)
        {
            tool.Status = DiagnosticStatus.Fail;
            tool.Summary = ex.Message;
        }
    }

    private static async Task RunTcpPortAsync(ToolRunViewModel tool, string host, int port, CancellationToken cancellationToken)
    {
        try
        {
            using var tcpClient = new System.Net.Sockets.TcpClient();
            var stopwatch = Stopwatch.StartNew();
            await tcpClient.ConnectAsync(host, port, cancellationToken);
            stopwatch.Stop();

            tool.Status = DiagnosticStatus.Pass;
            tool.Summary = $"Connected to {host}:{port} in {stopwatch.ElapsedMilliseconds} ms";
        }
        catch (OperationCanceledException)
        {
            tool.Status = DiagnosticStatus.Canceled;
            tool.Summary = "Canceled";
        }
        catch (Exception ex)
        {
            tool.Status = DiagnosticStatus.Fail;
            tool.Summary = $"TCP 443 failed: {ex.Message}";
        }
    }

    private static async Task RunHttpAsync(ToolRunViewModel tool, NormalizedTarget target, CancellationToken cancellationToken)
    {
        try
        {
            var url = target.TargetKind == TargetKind.Url
                ? target.OriginalValue
                : $"https://{target.Host}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var stopwatch = Stopwatch.StartNew();
            using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            stopwatch.Stop();

            tool.Status = response.IsSuccessStatusCode ? DiagnosticStatus.Pass : DiagnosticStatus.Warning;
            tool.Summary = $"{(int)response.StatusCode} {response.ReasonPhrase} in {stopwatch.ElapsedMilliseconds} ms";
            tool.RawOutput = response.Headers.ToString();
        }
        catch (OperationCanceledException)
        {
            tool.Status = DiagnosticStatus.Canceled;
            tool.Summary = "Canceled";
        }
        catch (Exception ex)
        {
            tool.Status = DiagnosticStatus.Fail;
            tool.Summary = ex.Message;
        }
    }

    private static async Task RunTracerouteAsync(ToolRunViewModel tool, string host, CancellationToken cancellationToken)
    {
        try
        {
            var result = await RunProcessAsync("tracert.exe", $"-d -h 30 {Quote(host)}", cancellationToken);
            tool.RawOutput = result.Output;
            tool.Status = result.ExitCode == 0 ? DiagnosticStatus.Pass : DiagnosticStatus.Warning;
            tool.Summary = SummarizeTraceroute(result.Output, result.ExitCode);
        }
        catch (OperationCanceledException)
        {
            tool.Status = DiagnosticStatus.Canceled;
            tool.Summary = "Canceled";
        }
        catch (Exception ex)
        {
            tool.Status = DiagnosticStatus.Fail;
            tool.Summary = ex.Message;
        }
    }

    private static async Task RunRdapAsync(ToolRunViewModel tool, string host, CancellationToken cancellationToken)
    {
        try
        {
            var query = IPAddress.TryParse(host, out _) ? $"ip/{host}" : $"domain/{host}";
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://rdap.org/{query}");
            request.Headers.UserAgent.ParseAdd("NarsilNexus/0.1");
            using var response = await HttpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            tool.RawOutput = body;

            if (!response.IsSuccessStatusCode)
            {
                var whois = await QueryWhoisAsync(host, cancellationToken);
                tool.RawOutput = $"RDAP returned {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{Environment.NewLine}{whois}";
                tool.Status = string.IsNullOrWhiteSpace(whois) ? DiagnosticStatus.Warning : DiagnosticStatus.Pass;
                tool.Summary = string.IsNullOrWhiteSpace(whois)
                    ? $"RDAP returned {(int)response.StatusCode} {response.ReasonPhrase}"
                    : "WHOIS fallback returned registration data";
                return;
            }

            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            var name = TryGetString(root, "name") ?? TryGetString(root, "handle") ?? host;
            var registrar = TryGetNestedString(root, "registrar", "name");

            tool.Status = DiagnosticStatus.Pass;
            tool.Summary = registrar is null ? $"RDAP record: {name}" : $"RDAP record: {name}, registrar {registrar}";
        }
        catch (OperationCanceledException)
        {
            tool.Status = DiagnosticStatus.Canceled;
            tool.Summary = "Canceled";
        }
        catch (Exception ex)
        {
            try
            {
                var whois = await QueryWhoisAsync(host, cancellationToken);
                tool.RawOutput = whois;
                tool.Status = string.IsNullOrWhiteSpace(whois) ? DiagnosticStatus.Fail : DiagnosticStatus.Pass;
                tool.Summary = string.IsNullOrWhiteSpace(whois) ? ex.Message : "WHOIS fallback returned registration data";
            }
            catch
            {
                tool.Status = DiagnosticStatus.Fail;
                tool.Summary = ex.Message;
            }
        }
    }

    private static async Task RunPathMtuAsync(ToolRunViewModel tool, string host, CancellationToken cancellationToken)
    {
        if (!IPAddress.TryParse(host, out var address) || address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            tool.Status = DiagnosticStatus.Skipped;
            tool.Summary = "Path MTU probe currently requires an IPv4 target.";
            return;
        }

        try
        {
            var low = 0;
            var high = 1472;
            var best = 0;
            var log = new StringBuilder();

            while (low <= high)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var mid = (low + high) / 2;
                var result = await RunProcessAsync("ping.exe", $"-n 1 -f -l {mid} {Quote(host)}", cancellationToken);
                var success = result.ExitCode == 0 && result.Output.Contains("TTL=", StringComparison.OrdinalIgnoreCase);
                log.AppendLine($"{mid} bytes: {(success ? "pass" : "fail")}");

                if (success)
                {
                    best = mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            tool.Status = best > 0 ? DiagnosticStatus.Pass : DiagnosticStatus.Warning;
            tool.Summary = best > 0 ? $"Estimated max payload {best} bytes, MTU {best + 28} bytes" : "Could not determine path MTU";
            tool.RawOutput = log.ToString();
        }
        catch (OperationCanceledException)
        {
            tool.Status = DiagnosticStatus.Canceled;
            tool.Summary = "Canceled";
        }
        catch (Exception ex)
        {
            tool.Status = DiagnosticStatus.Fail;
            tool.Summary = ex.Message;
        }
    }

    private async Task RunSpeedResultsAsync(ToolRunViewModel tool, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(SpeedResultsApiUrl))
        {
            tool.Status = DiagnosticStatus.Skipped;
            tool.Summary = "Enter a Speed URL before running Speed Results.";
            return;
        }

        if (SpeedEndpointType.Equals("LibreSpeed", StringComparison.OrdinalIgnoreCase))
        {
            ApplyLibreSpeedBaseUrl(SpeedResultsApiUrl);
            await RunHttpSpeedProbeAsync(tool, cancellationToken);
            return;
        }

        if (SpeedEndpointType.Equals("HTTP Probe", StringComparison.OrdinalIgnoreCase))
        {
            await RunHttpSpeedProbeAsync(tool, cancellationToken);
            return;
        }

        if (Uri.TryCreate(SpeedResultsApiUrl.Trim(), UriKind.Absolute, out var uri) is false ||
            (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) is false &&
             uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) is false))
        {
            tool.Status = DiagnosticStatus.Fail;
            tool.Summary = "Speed API URL must be an HTTP or HTTPS URL.";
            return;
        }

        try
        {
            using var response = await HttpClient.GetAsync(uri, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            tool.RawOutput = body;

            if (response.IsSuccessStatusCode is false)
            {
                tool.Status = DiagnosticStatus.Fail;
                tool.Summary = $"Speed API returned {(int)response.StatusCode} {response.ReasonPhrase}";
                return;
            }

            if (body.TrimStart().StartsWith("<", StringComparison.Ordinal))
            {
                tool.Status = DiagnosticStatus.Fail;
                tool.Summary = "Expected JSON but received HTML.";
                return;
            }

            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            var payload = root.TryGetProperty("args", out var args) && args.ValueKind == JsonValueKind.Object
                ? args
                : root;
            var download = TryGetDouble(payload, "downloadMbps", "download", "download_mbps", "download_speed");
            var upload = TryGetDouble(payload, "uploadMbps", "upload", "upload_mbps", "upload_speed");
            var latency = TryGetDouble(payload, "latencyMs", "pingMs", "ping", "latency");
            var jitter = TryGetDouble(payload, "jitterMs", "jitter");
            var server = TryGetString(payload, "server") ?? TryGetString(payload, "serverName") ?? TryGetString(payload, "name");
            var timestamp = TryGetString(payload, "timestamp") ?? TryGetString(payload, "time") ?? TryGetString(payload, "date");

            if (download is null && upload is null && latency is null)
            {
                tool.Status = DiagnosticStatus.Warning;
                tool.Summary = "JSON received, but no download/upload/latency fields were found.";
                return;
            }

            var parts = new List<string>();
            if (download is not null)
            {
                parts.Add($"Download {download:0.##} Mbps");
            }

            if (upload is not null)
            {
                parts.Add($"Upload {upload:0.##} Mbps");
            }

            if (latency is not null)
            {
                parts.Add($"Latency {latency:0.##} ms");
            }

            tool.Status = DiagnosticStatus.Pass;
            tool.Summary = string.Join(", ", parts);
            tool.RawOutput = BuildSpeedDetails(uri, download, upload, latency, jitter, server, timestamp, body);
        }
        catch (JsonException ex)
        {
            tool.Status = DiagnosticStatus.Fail;
            tool.Summary = $"Invalid JSON: {ex.Message}";
        }
        catch (OperationCanceledException)
        {
            tool.Status = DiagnosticStatus.Canceled;
            tool.Summary = "Canceled";
        }
        catch (Exception ex)
        {
            tool.Status = DiagnosticStatus.Fail;
            tool.Summary = ex.Message;
        }
    }

    private async Task RunHttpSpeedProbeAsync(ToolRunViewModel tool, CancellationToken cancellationToken)
    {
        var pingUrl = string.IsNullOrWhiteSpace(SpeedProbePingUrl) ? SpeedResultsApiUrl : SpeedProbePingUrl;
        var downloadUrl = string.IsNullOrWhiteSpace(SpeedProbeDownloadUrl) ? SpeedResultsApiUrl : SpeedProbeDownloadUrl;
        var uploadUrl = SpeedProbeUploadUrl.Trim();

        if (Uri.TryCreate(pingUrl.Trim(), UriKind.Absolute, out var pingUri) is false ||
            Uri.TryCreate(downloadUrl.Trim(), UriKind.Absolute, out var downloadUri) is false)
        {
            tool.Status = DiagnosticStatus.Fail;
            tool.Summary = "HTTP Probe requires valid ping and download URLs.";
            return;
        }

        try
        {
            var latencySamples = new List<double>();
            for (var i = 0; i < SpeedPingSamples; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var request = new HttpRequestMessage(HttpMethod.Get, AddCacheBust(pingUri));
                var stopwatch = Stopwatch.StartNew();
                using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                stopwatch.Stop();
                response.EnsureSuccessStatusCode();
                latencySamples.Add(stopwatch.Elapsed.TotalMilliseconds);
            }

            var download = await RunTimedDownloadAsync(downloadUri, TimeSpan.FromSeconds(SpeedTestDurationSeconds), cancellationToken);
            var latency = latencySamples.Average();
            var jitter = CalculateJitter(latencySamples);

            SpeedMeasurement? upload = null;
            if (string.IsNullOrWhiteSpace(uploadUrl) is false &&
                Uri.TryCreate(uploadUrl, UriKind.Absolute, out var uploadUri))
            {
                upload = await RunTimedUploadAsync(uploadUri, TimeSpan.FromSeconds(SpeedTestDurationSeconds), cancellationToken);
            }

            tool.Status = DiagnosticStatus.Pass;
            tool.Summary = upload is null
                ? $"Download {download.Mbps:0.##} Mbps, Latency {latency:0.##} ms, Jitter {jitter:0.##} ms"
                : $"Download {download.Mbps:0.##} Mbps, Upload {upload.Mbps:0.##} Mbps, Latency {latency:0.##} ms, Jitter {jitter:0.##} ms";
            tool.RawOutput = string.Join(Environment.NewLine,
                $"Mode: {SpeedEndpointType}",
                $"Duration: {SpeedTestDurationSeconds} sec",
                $"Ping URL: {pingUri}",
                $"Download URL: {downloadUri}",
                string.IsNullOrWhiteSpace(uploadUrl) ? "Upload URL: not configured" : $"Upload URL: {uploadUrl}",
                $"Bytes downloaded: {download.Bytes:N0}",
                $"Download duration: {download.Elapsed.TotalSeconds:0.###} sec",
                $"Download: {download.Mbps:0.##} Mbps",
                upload is null ? "Upload: not run" : $"Bytes uploaded: {upload.Bytes:N0}",
                upload is null ? string.Empty : $"Upload duration: {upload.Elapsed.TotalSeconds:0.###} sec",
                upload is null ? string.Empty : $"Upload: {upload.Mbps:0.##} Mbps",
                $"Latency samples: {string.Join(", ", latencySamples.Select(sample => $"{sample:0.##} ms"))}",
                $"Average latency: {latency:0.##} ms",
                $"Jitter: {jitter:0.##} ms");
        }
        catch (OperationCanceledException)
        {
            tool.Status = DiagnosticStatus.Canceled;
            tool.Summary = "Canceled";
        }
        catch (Exception ex)
        {
            tool.Status = DiagnosticStatus.Fail;
            tool.Summary = ex.Message;
        }
    }

    private void GeneratePdf()
    {
        var effectiveTarget = GetEffectiveTargetInput();
        var target = string.IsNullOrWhiteSpace(effectiveTarget) ? "No target entered" : effectiveTarget.Trim();
        var reportPath = _pdfReportWriter.WriteReport(_paths.ReportsDirectory, target, Tools);
        foreach (var tool in Tools.Where(tool => tool.Id == DiagnosticToolId.SpeedTestResults))
        {
            tool.Status = DiagnosticStatus.Pass;
            tool.Summary = $"Report saved: {reportPath}";
            break;
        }
    }

    private string GetEffectiveTargetInput()
    {
        if (SelectedTarget is not null)
        {
            return SelectedTarget.Target;
        }

        var value = TargetInput.Trim();
        var savedTarget = SavedTargets.FirstOrDefault(target =>
            target.DisplayName.Equals(value, StringComparison.OrdinalIgnoreCase) ||
            target.Target.Equals(value, StringComparison.OrdinalIgnoreCase));

        if (savedTarget is not null)
        {
            return savedTarget.Target;
        }

        var openParen = value.LastIndexOf('(');
        var closeParen = value.LastIndexOf(')');
        if (openParen >= 0 && closeParen > openParen)
        {
            var possibleTarget = value[(openParen + 1)..closeParen].Trim();
            var normalized = TargetNormalizer.Normalize(possibleTarget);
            if (normalized.IsValid)
            {
                return possibleTarget;
            }
        }

        return value;
    }

    private static async Task<ProcessResult> RunProcessAsync(string fileName, string arguments, CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var output = new StringBuilder();
        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                output.AppendLine(args.Data);
            }
        };
        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                output.AppendLine(args.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(cancellationToken);

        return new ProcessResult(process.ExitCode, output.ToString());
    }

    private static string SummarizeTraceroute(string output, int exitCode)
    {
        var hopCount = output.Split(Environment.NewLine)
            .Count(line => line.TrimStart().Length > 0 && char.IsDigit(line.TrimStart()[0]));

        if (hopCount > 0)
        {
            return $"Completed with {hopCount} visible hops";
        }

        return exitCode == 0 ? "Traceroute completed" : "Traceroute returned warnings";
    }

    private static string Quote(string value)
    {
        return $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static string? TryGetNestedString(JsonElement element, string objectName, string propertyName)
    {
        return element.TryGetProperty(objectName, out var nested)
            ? TryGetString(nested, propertyName)
            : null;
    }

    private static double? TryGetDouble(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out var property) is false)
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var number))
            {
                return number;
            }

            if (property.ValueKind == JsonValueKind.String &&
                double.TryParse(property.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static string BuildSpeedDetails(
        Uri source,
        double? download,
        double? upload,
        double? latency,
        double? jitter,
        string? server,
        string? timestamp,
        string rawJson)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Source: {source}");
        if (server is not null) builder.AppendLine($"Server: {server}");
        if (timestamp is not null) builder.AppendLine($"Timestamp: {timestamp}");
        if (download is not null) builder.AppendLine($"Download: {download:0.##} Mbps");
        if (upload is not null) builder.AppendLine($"Upload: {upload:0.##} Mbps");
        if (latency is not null) builder.AppendLine($"Latency: {latency:0.##} ms");
        if (jitter is not null) builder.AppendLine($"Jitter: {jitter:0.##} ms");
        builder.AppendLine();
        builder.AppendLine("Raw JSON:");
        builder.AppendLine(rawJson);
        return builder.ToString();
    }

    private void ApplyLibreSpeedBaseUrl(string baseUrl)
    {
        if (Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var uri) is false)
        {
            return;
        }

        var baseText = uri.ToString().TrimEnd('/') + "/";
        SpeedProbePingUrl = new Uri(new Uri(baseText), "empty.php").ToString();
        SpeedProbeDownloadUrl = new Uri(new Uri(baseText), "garbage.php?ckSize=100").ToString();
        SpeedProbeUploadUrl = new Uri(new Uri(baseText), "empty.php").ToString();
    }

    private static async Task<SpeedMeasurement> RunTimedDownloadAsync(Uri uri, TimeSpan duration, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + duration;
        var stopwatch = Stopwatch.StartNew();
        var buffer = new byte[128 * 1024];
        long bytesRead = 0;

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var stream = await HttpClient.GetStreamAsync(AddCacheBust(uri), cancellationToken);
            int read;
            while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                bytesRead += read;
                if (DateTimeOffset.UtcNow >= deadline)
                {
                    break;
                }
            }
        }

        stopwatch.Stop();
        return SpeedMeasurement.FromBytes(bytesRead, stopwatch.Elapsed);
    }

    private static async Task<SpeedMeasurement> RunTimedUploadAsync(Uri uri, TimeSpan duration, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + duration;
        var payload = new byte[1024 * 1024];
        Random.Shared.NextBytes(payload);
        var stopwatch = Stopwatch.StartNew();
        long bytesSent = 0;

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var content = new ByteArrayContent(payload);
            using var response = await HttpClient.PostAsync(AddCacheBust(uri), content, cancellationToken);
            response.EnsureSuccessStatusCode();
            bytesSent += payload.Length;
        }

        stopwatch.Stop();
        return SpeedMeasurement.FromBytes(bytesSent, stopwatch.Elapsed);
    }

    private static Uri AddCacheBust(Uri uri)
    {
        var separator = string.IsNullOrEmpty(uri.Query) ? "?" : "&";
        return new Uri($"{uri}{separator}narsil={Guid.NewGuid():N}");
    }

    private static double CalculateJitter(IReadOnlyList<double> samples)
    {
        if (samples.Count < 2)
        {
            return 0;
        }

        var deltas = samples.Zip(samples.Skip(1), (left, right) => Math.Abs(right - left));
        return deltas.Average();
    }

    private sealed record SpeedMeasurement(long Bytes, TimeSpan Elapsed, double Mbps)
    {
        public static SpeedMeasurement FromBytes(long bytes, TimeSpan elapsed)
        {
            var mbps = elapsed.TotalSeconds <= 0 ? 0 : bytes * 8d / elapsed.TotalSeconds / 1_000_000d;
            return new SpeedMeasurement(bytes, elapsed, mbps);
        }
    }

    private static async Task<string> QueryWhoisAsync(string query, CancellationToken cancellationToken)
    {
        var first = await QueryWhoisServerAsync("whois.iana.org", query, cancellationToken);
        var referral = first.Split(Environment.NewLine)
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.StartsWith("refer:", StringComparison.OrdinalIgnoreCase));

        if (referral is null)
        {
            return first;
        }

        var server = referral.Split(':', 2)[1].Trim();
        if (string.IsNullOrWhiteSpace(server))
        {
            return first;
        }

        var second = await QueryWhoisServerAsync(server, query, cancellationToken);
        return $"{first}{Environment.NewLine}{Environment.NewLine}--- {server} ---{Environment.NewLine}{second}";
    }

    private static async Task<string> QueryWhoisServerAsync(string server, string query, CancellationToken cancellationToken)
    {
        using var tcpClient = new System.Net.Sockets.TcpClient();
        await tcpClient.ConnectAsync(server, 43, cancellationToken);
        await using var stream = tcpClient.GetStream();
        var request = Encoding.ASCII.GetBytes($"{query}\r\n");
        await stream.WriteAsync(request, cancellationToken);

        using var reader = new StreamReader(stream, Encoding.ASCII);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private sealed record ProcessResult(int ExitCode, string Output);

    private void OpenAppDataFolder()
    {
        _paths.EnsureCreated();
        Process.Start(new ProcessStartInfo
        {
            FileName = _paths.AppDataRoot,
            UseShellExecute = true
        });
    }

    private void ToggleTheme()
    {
        _isDarkMode = !_isDarkMode;
        var themePath = _isDarkMode ? "Resources/Themes/Dark.xaml" : "Resources/Themes/Light.xaml";
        Application.Current.Resources.MergedDictionaries.Clear();
        Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(themePath, UriKind.Relative)
        });
        OnPropertyChanged(nameof(ThemeButtonText));
    }

    private void SetAllSelected(DiagnosticStatus status, string summary)
    {
        foreach (var tool in Tools.Where(tool => tool.IsSelected))
        {
            tool.Status = status;
            tool.Summary = summary;
        }
    }

    private void SetToolStatus(DiagnosticToolId toolId, DiagnosticStatus status, string summary)
    {
        var tool = Tools.FirstOrDefault(candidate => candidate.Id == toolId);
        if (tool is null)
        {
            return;
        }

        tool.Status = status;
        tool.Summary = summary;
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
