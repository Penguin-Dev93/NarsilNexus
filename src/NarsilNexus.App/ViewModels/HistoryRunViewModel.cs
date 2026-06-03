using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using NarsilNexus.Core.Diagnostics;

namespace NarsilNexus.App.ViewModels;

public sealed class HistoryRunViewModel : INotifyPropertyChanged
{
    private string _pdfPath;

    public HistoryRunViewModel(DiagnosticHistoryEntry entry)
    {
        Entry = entry;
        _pdfPath = entry.PdfPath;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public DiagnosticHistoryEntry Entry { get; private set; }
    public Guid Id => Entry.Id;
    public string Target => Entry.Target;
    public string StartedAtText => Entry.StartedAt.LocalDateTime.ToString("g");
    public string ToolCountText => $"{Entry.Results.Count} tools";
    public string OverallStatusText => Entry.OverallStatus.ToString();
    public string SummaryText => $"{StartedAtText} - {ToolCountText}";
    public string SpeedEndpointText => string.IsNullOrWhiteSpace(Entry.SpeedEndpointName)
        ? Entry.SpeedEndpointType
        : $"{Entry.SpeedEndpointName} [{Entry.SpeedEndpointType}]";

    public string PdfPath
    {
        get => _pdfPath;
        private set
        {
            if (_pdfPath == value)
            {
                return;
            }

            _pdfPath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasPdfText));
        }
    }

    public string HasPdfText => string.IsNullOrWhiteSpace(PdfPath) ? "No PDF" : "PDF Ready";

    public Brush StatusBrush => Entry.OverallStatus switch
    {
        DiagnosticStatus.Pass => new SolidColorBrush(Color.FromRgb(22, 163, 74)),
        DiagnosticStatus.Warning => new SolidColorBrush(Color.FromRgb(217, 119, 6)),
        DiagnosticStatus.Fail or DiagnosticStatus.Blocked => new SolidColorBrush(Color.FromRgb(220, 38, 38)),
        DiagnosticStatus.Canceled => new SolidColorBrush(Color.FromRgb(75, 85, 99)),
        _ => new SolidColorBrush(Color.FromRgb(75, 85, 99))
    };

    public void UpdatePdfPath(string pdfPath)
    {
        Entry = Entry with { PdfPath = pdfPath };
        PdfPath = pdfPath;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
