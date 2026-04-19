using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System.ServiceProcess;

namespace RyTuneX.Views;

public sealed partial class ServicesPage : Page
{
    private List<ServiceInfoItem> _allServices = [];
    private List<ServiceInfoItem> _filteredServices = [];
    private string _currentSort = "Name";
    private bool _sortAscending = true;
    private string _currentFilter = "All";
    private bool _isLoaded;
    private bool _isUpdatingStartupType;
    private HashSet<ComboBox> _userInteractedComboBoxes = [];

    public ServicesPage()
    {
        InitializeComponent();
        LogHelper.Log("Initializing ServicesPage");
        Loaded += ServicesPage_Loaded;
    }

    private async void ServicesPage_Loaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = true;
        await LoadServicesAsync();
    }

    private async Task LoadServicesAsync()
    {
        LoadingRing.IsActive = true;
        LoadingRing.Visibility = Visibility.Visible;
        ServicesListView.Visibility = Visibility.Collapsed;

        try
        {
            _allServices = await Task.Run(() =>
            {
                return ServiceController.GetServices()
                    .Select(s =>
                    {
                        var startType = GetServiceStartType(s.ServiceName);
                        var isRunning = s.Status == ServiceControllerStatus.Running;
                        var canStop = s.Status == ServiceControllerStatus.Running && s.CanStop;

                        return new ServiceInfoItem
                        {
                            Name = s.ServiceName,
                            DisplayName = s.DisplayName,
                            Status = s.Status.ToString(),
                            StartType = startType,
                            CanStart = !isRunning && startType != "Disabled",
                            CanStop = canStop
                        };
                    })
                    .OrderBy(s => s.DisplayName)
                    .ToList();
            });

            UpdateSummary();
            ApplyFilterAndSort();
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error loading services: {ex.Message}");
        }
        finally
        {
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
            ServicesListView.Visibility = Visibility.Visible;
        }
    }

    private void UpdateSummary()
    {
        var runningCount = _allServices.Count(s => s.Status == "Running");
        var stoppedCount = _allServices.Count(s => s.Status == "Stopped");

        TotalServicesText.Text = _allServices.Count.ToString();
        RunningServicesText.Text = runningCount.ToString();
        StoppedServicesText.Text = stoppedCount.ToString();
    }

    private void ApplyFilterAndSort()
    {
        if (!_isLoaded) return;

        var query = SearchBox.Text?.ToLowerInvariant() ?? "";

        _filteredServices = _allServices.Where(s =>
        {
            var matchesFilter = _currentFilter switch
            {
                "Running" => s.Status == "Running",
                "Stopped" => s.Status == "Stopped",
                "Automatic" => s.StartType == "Automatic",
                "Manual" => s.StartType == "Manual",
                "Disabled" => s.StartType == "Disabled",
                _ => true
            };

            var matchesSearch = string.IsNullOrEmpty(query) ||
                s.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                s.Name.Contains(query, StringComparison.OrdinalIgnoreCase);

            return matchesFilter && matchesSearch;
        }).ToList();

        SortServices();

        ServicesListView.ItemsSource = _filteredServices;
        ResultsText.Text = $"Showing {_filteredServices.Count} of {_allServices.Count} services";
    }

    private void SortServices()
    {
        _filteredServices = _currentSort switch
        {
            "Name" => _sortAscending
                ? [.. _filteredServices.OrderBy(s => s.DisplayName)]
                : [.. _filteredServices.OrderByDescending(s => s.DisplayName)],
            "Status" => _sortAscending
                ? [.. _filteredServices.OrderBy(s => s.Status)]
                : [.. _filteredServices.OrderByDescending(s => s.Status)],
            "StartType" => _sortAscending
                ? [.. _filteredServices.OrderBy(s => s.StartType)]
                : [.. _filteredServices.OrderByDescending(s => s.StartType)],
            _ => _filteredServices
        };
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ApplyFilterAndSort();
        }
    }

    private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded) return;

        if (FilterComboBox.SelectedItem is ComboBoxItem item)
        {
            _currentFilter = item.Content?.ToString() ?? "All";
            ApplyFilterAndSort();
        }
    }

    private void SortHeader_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string column)
        {
            _sortAscending = _currentSort != column || !_sortAscending;
            _currentSort = column;
            ApplyFilterAndSort();
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        _ = LogHelper.Log("Manually refreshing services list");
        await LoadServicesAsync();
    }

    private async void StartService_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string serviceName)
        {
            await ControlServiceAsync(serviceName, ServiceControlAction.Start);
        }
    }

    private async void StopService_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string serviceName)
        {
            await ControlServiceAsync(serviceName, ServiceControlAction.Stop);
        }
    }

    private async void RestartService_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string serviceName)
        {
            await ControlServiceAsync(serviceName, ServiceControlAction.Restart);
        }
    }

    private async void StartupType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingStartupType) return;

        // Skip if no items were added (initial load scenario)
        if (e.AddedItems.Count == 0) return;

        if (sender is ComboBox comboBox &&
            comboBox.Tag is string serviceName &&
            comboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            // Only process if user explicitly interacted with this ComboBox
            if (!_userInteractedComboBoxes.Remove(comboBox)) return;

            var startupType = selectedItem.Content?.ToString();
            if (string.IsNullOrEmpty(startupType)) return;

            // Find the current service to check if the startup type actually changed
            var service = _allServices.FirstOrDefault(s => s.Name == serviceName);
            if (service == null || service.StartType == startupType) return;

            await ChangeStartupTypeAsync(serviceName, startupType);
        }
    }

    private void StartupType_DropDownOpened(object sender, object e)
    {
        if (sender is ComboBox comboBox)
        {
            _userInteractedComboBoxes.Add(comboBox);
        }
    }

    private void StartupType_DropDownClosed(object sender, object e)
    {
        // Clean up if user closed dropdown without making a selection change
        // The SelectionChanged handler will remove it if a change was made
        if (sender is ComboBox comboBox)
        {
            _userInteractedComboBoxes.Remove(comboBox);
        }
    }

    private async Task ControlServiceAsync(string serviceName, ServiceControlAction action)
    {
        try
        {
            var actionText = action switch
            {
                ServiceControlAction.Start => "Starting",
                ServiceControlAction.Stop => "Stopping",
                ServiceControlAction.Restart => "Restarting",
                _ => "Processing"
            };

            _ = LogHelper.Log($"{actionText} service: {serviceName}");

            await Task.Run(() =>
            {
                using var service = new ServiceController(serviceName);
                var timeout = TimeSpan.FromSeconds(30);

                switch (action)
                {
                    case ServiceControlAction.Start:
                        if (service.Status != ServiceControllerStatus.Running)
                        {
                            service.Start();
                            service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                        }
                        break;

                    case ServiceControlAction.Stop:
                        if (service.Status == ServiceControllerStatus.Running && service.CanStop)
                        {
                            service.Stop();
                            service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                        }
                        break;

                    case ServiceControlAction.Restart:
                        if (service.Status == ServiceControllerStatus.Running && service.CanStop)
                        {
                            service.Stop();
                            service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                        }
                        service.Start();
                        service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                        break;
                }
            });

            var successText = action switch
            {
                ServiceControlAction.Start => "started",
                ServiceControlAction.Stop => "stopped",
                ServiceControlAction.Restart => "restarted",
                _ => "processed"
            };

            App.ShowNotification("Service Control", $"Service '{serviceName}' {successText} successfully.", InfoBarSeverity.Success, 3000);
            await LoadServicesAsync();
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error controlling service {serviceName}: {ex.Message}");
            App.ShowNotification("Service Control Error", $"Failed to control service: {ex.Message}", InfoBarSeverity.Error, 5000);
        }
    }

    private async Task ChangeStartupTypeAsync(string serviceName, string startupType)
    {
        try
        {
            _ = LogHelper.Log($"Changing startup type for service {serviceName} to {startupType}");

            var startValue = startupType switch
            {
                "Automatic" => 2,
                "Manual" => 3,
                "Disabled" => 4,
                _ => 3
            };

            await Task.Run(() =>
            {
                using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}", true);
                key?.SetValue("Start", startValue, RegistryValueKind.DWord);
            });

            App.ShowNotification("Startup Type Changed", $"Service '{serviceName}' startup type set to {startupType}.", InfoBarSeverity.Success, 3000);

            // Update the local data without full reload
            var service = _allServices.FirstOrDefault(s => s.Name == serviceName);
            if (service != null)
            {
                service.StartType = startupType;
                service.CanStart = service.Status != "Running" && startupType != "Disabled";
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error changing startup type for {serviceName}: {ex.Message}");
            App.ShowNotification("Error", $"Failed to change startup type: {ex.Message}", InfoBarSeverity.Error, 5000);

            // Reload to reset the combobox
            _isUpdatingStartupType = true;
            await LoadServicesAsync();
            _isUpdatingStartupType = false;
        }
    }

    private static string GetServiceStartType(string serviceName)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}");
            if (key?.GetValue("Start") is int startType)
            {
                return startType switch
                {
                    0 => "Boot",
                    1 => "System",
                    2 => "Automatic",
                    3 => "Manual",
                    4 => "Disabled",
                    _ => "Unknown"
                };
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogWarning($"Error reading start type for service {serviceName}: {ex.Message}");
        }
        return "Unknown";
    }
}

internal enum ServiceControlAction
{
    Start,
    Stop,
    Restart
}

internal class StatusInfo
{
    public string Glyph { get; set; } = string.Empty;
    public Brush Color { get; set; } = null!;
}

internal class ServiceInfoItem
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StartType { get; set; } = string.Empty;
    public bool CanStart
    {
        get; set;
    }
    public bool CanStop
    {
        get; set;
    }

    public StatusInfo StatusDisplay => Status switch
    {
        "Running" => new StatusInfo
        {
            Glyph = "\uE768",
            Color = (Brush)App.Current.Resources["SystemFillColorSuccessBrush"]
        },
        "Stopped" => new StatusInfo
        {
            Glyph = "\uE71A",
            Color = (Brush)App.Current.Resources["SystemFillColorCautionBrush"]
        },
        _ => new StatusInfo
        {
            Glyph = "\uE7BA",
            Color = (Brush)App.Current.Resources["SystemFillColorBaseMediumBrush"]
        }
    };

    // Gets the index for the startup type ComboBox.
    public int StartTypeIndex => StartType switch
    {
        "Automatic" => 0,
        "Manual" => 1,
        "Disabled" => 2,
        _ => 1 // Default to Manual for unknown types (Boot, System, Unknown)
    };
}
