using Microsoft.Win32;
using RyTuneX.Helpers;
using System.ComponentModel;

namespace RyTuneX.Models;

public enum StartupLocationType
{
    HKCU_Run,
    HKCU_RunOnce,
    HKLM_Run,
    HKLM_RunOnce,
    UserStartupFolder,
    CommonStartupFolder,
    ScheduledTask
}

public enum StartupImpact
{
    Low,
    Medium,
    High,
    Broken
}

public class StartupItem : INotifyPropertyChanged
{
    private bool _isEnabled;

    public event PropertyChangedEventHandler? PropertyChanged;

    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Command { get; set; }
    public required string ExecutablePath { get; set; }
    public string Publisher { get; set; } = "Unknown";
    public string Description { get; set; } = string.Empty;
    public required string Location { get; set; }
    public StartupLocationType LocationType { get; set; }
    public RegistryHive? Hive { get; set; }
    public RegistryView View { get; set; } = RegistryView.Default;
    public string? RegistryPath { get; set; }
    public string? ValueName { get; set; }
    public string? FilePath { get; set; }
    public string? TaskName { get; set; }
    public string? TaskPath { get; set; }
    public bool IsValid { get; set; } = true;
    public StartupImpact Impact { get; set; } = StartupImpact.Low;
    public long FileSizeBytes { get; set; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusGlyph));
                OnPropertyChanged(nameof(StatusColorKey));
            }
        }
    }

    public string StatusText => IsEnabled
        ? "StartupPage_EnabledApps/Text".TryGetLocalized() ?? "Enabled"
        : "StartupPage_DisabledApps/Text".TryGetLocalized() ?? "Disabled";

    public string StatusGlyph => IsEnabled ? "\uE768" : "\uE71A";

    public string StatusColorKey => IsEnabled ? "SystemFillColorSuccessBrush" : "SystemFillColorCautionBrush";

    public string ImpactText => Impact switch
    {
        StartupImpact.High => "StartupPage_Impact_High".TryGetLocalized() ?? "High",
        StartupImpact.Medium => "StartupPage_Impact_Medium".TryGetLocalized() ?? "Medium",
        StartupImpact.Low => "StartupPage_Impact_Low".TryGetLocalized() ?? "Low",
        StartupImpact.Broken => "StartupPage_Impact_Broken".TryGetLocalized() ?? "Missing File",
        _ => "Low"
    };

    public string ImpactGlyph => Impact switch
    {
        StartupImpact.High => "\uE7BA",
        StartupImpact.Medium => "\uE7C8",
        StartupImpact.Low => "\uE73E",
        StartupImpact.Broken => "\uE783",
        _ => "\uE73E"
    };

    public string LocationDisplay => LocationType switch
    {
        StartupLocationType.HKCU_Run => "Registry (HKCU)",
        StartupLocationType.HKCU_RunOnce => "Registry (HKCU RunOnce)",
        StartupLocationType.HKLM_Run => "Registry (HKLM)",
        StartupLocationType.HKLM_RunOnce => "Registry (HKLM RunOnce)",
        StartupLocationType.UserStartupFolder => "Startup Folder (User)",
        StartupLocationType.CommonStartupFolder => "Startup Folder (Common)",
        StartupLocationType.ScheduledTask => "Scheduled Task",
        _ => Location
    };

    public bool IsUserScope => LocationType is StartupLocationType.HKCU_Run or StartupLocationType.HKCU_RunOnce or StartupLocationType.UserStartupFolder;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
