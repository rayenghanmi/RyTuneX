using Microsoft.UI.Xaml;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RyTuneX.Models;

public class WingetPackage : INotifyPropertyChanged
{
    private bool _isSelected;
    private bool _isInstalled;
    private bool _hasUpdate;
    private string _latestVersion = string.Empty;

    public string Id { get; set; } = string.Empty;

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;

    private string _version = string.Empty;
    public string Version
    {
        get => _version;
        set
        {
            if (_version != value)
            {
                _version = value;
                OnPropertyChanged();
            }
        }
    }

    // The latest available version from winget upgrade
    public string LatestVersion
    {
        get => _latestVersion;
        set
        {
            if (_latestVersion != value)
            {
                _latestVersion = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsInstalled
    {
        get => _isInstalled;
        set
        {
            if (_isInstalled != value)
            {
                _isInstalled = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(InstalledVisibility));
                OnPropertyChanged(nameof(NotInstalledVisibility));
                OnPropertyChanged(nameof(InstalledNoUpdateVisibility));
                OnPropertyChanged(nameof(ItemOpacity));
                OnPropertyChanged(nameof(ItemIsEnabled));
            }
        }
    }

    // True when winget reports a newer version is available for this installed package
    public bool HasUpdate
    {
        get => _hasUpdate;
        set
        {
            if (_hasUpdate != value)
            {
                _hasUpdate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UpdateVisibility));
                OnPropertyChanged(nameof(InstalledNoUpdateVisibility));
                OnPropertyChanged(nameof(ItemOpacity));
                OnPropertyChanged(nameof(ItemIsEnabled));
            }
        }
    }

    public Visibility InstalledNoUpdateVisibility =>
        IsInstalled && !HasUpdate ? Visibility.Visible : Visibility.Collapsed;
    public Visibility InstalledVisibility =>
        IsInstalled ? Visibility.Visible : Visibility.Collapsed;
    public Visibility NotInstalledVisibility =>
        !IsInstalled ? Visibility.Visible : Visibility.Collapsed;
    public Visibility UpdateVisibility =>
        HasUpdate ? Visibility.Visible : Visibility.Collapsed;
    public double ItemOpacity => IsInstalled && !HasUpdate ? 0.5 : 1.0;
    public bool ItemIsEnabled => !IsInstalled || HasUpdate;


    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}