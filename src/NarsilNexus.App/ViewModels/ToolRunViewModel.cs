using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using NarsilNexus.Core.Diagnostics;

namespace NarsilNexus.App.ViewModels;

public sealed class ToolRunViewModel : INotifyPropertyChanged
{
    private bool _isSelected = true;
    private DiagnosticStatus _status = DiagnosticStatus.Pending;
    private string _summary = "Ready";
    private string? _rawOutput;

    public ToolRunViewModel(DiagnosticToolId id, string name)
    {
        Id = id;
        Name = name;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public DiagnosticToolId Id { get; }
    public string Name { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    public DiagnosticStatus Status
    {
        get => _status;
        set
        {
            if (SetField(ref _status, value))
            {
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(RunningVisibility));
                OnPropertyChanged(nameof(StatusBrush));
            }
        }
    }

    public string Summary
    {
        get => _summary;
        set => SetField(ref _summary, value);
    }

    public string? RawOutput
    {
        get => _rawOutput;
        set
        {
            if (SetField(ref _rawOutput, value))
            {
                OnPropertyChanged(nameof(RawOutputVisibility));
            }
        }
    }

    public bool IsRunning => Status == DiagnosticStatus.Running;
    public Visibility RunningVisibility => IsRunning ? Visibility.Visible : Visibility.Collapsed;
    public Visibility RawOutputVisibility => string.IsNullOrWhiteSpace(RawOutput) ? Visibility.Collapsed : Visibility.Visible;

    public Brush StatusBrush => Status switch
    {
        DiagnosticStatus.Pass => new SolidColorBrush(Color.FromRgb(22, 163, 74)),
        DiagnosticStatus.Warning => new SolidColorBrush(Color.FromRgb(217, 119, 6)),
        DiagnosticStatus.Fail or DiagnosticStatus.Blocked => new SolidColorBrush(Color.FromRgb(220, 38, 38)),
        DiagnosticStatus.Running => new SolidColorBrush(Color.FromRgb(37, 99, 235)),
        _ => new SolidColorBrush(Color.FromRgb(75, 85, 99))
    };

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
