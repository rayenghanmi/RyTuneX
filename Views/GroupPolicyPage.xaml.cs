using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using RyTuneX.Helpers;

namespace RyTuneX.Views;

public sealed partial class GroupPolicyPage : Page
{
    private CancellationTokenSource? _cancellationTokenSource;
    private Guid? _cancellationRegistrationId;
    private IReadOnlyList<GroupPolicyHelper.PolicyState>? _policyStates;
    private string? _pendingScrollTarget;

    public GroupPolicyPage()
    {
        InitializeComponent();
        LogHelper.Log("Initializing GroupPolicyPage");
        NavigationCacheMode = NavigationCacheMode.Required;
        Loaded += GroupPolicyPage_Loaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string optionTag && !string.IsNullOrEmpty(optionTag))
        {
            _pendingScrollTarget = optionTag;
        }
    }

    private async void GroupPolicyPage_Loaded(object sender, RoutedEventArgs e)
    {
        // Subscribe to selection changed after page is loaded
        ConfiguredPoliciesListView.SelectionChanged += ConfiguredPoliciesListView_SelectionChanged;

        await ScanPoliciesAsync();

        if (!string.IsNullOrEmpty(_pendingScrollTarget))
        {
            await ScrollToElementHelper.ScrollToElementAsync(this, _pendingScrollTarget);
            _pendingScrollTarget = null;
        }
    }

    private void ConfiguredPoliciesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RemoveSelectedButton.IsEnabled = ConfiguredPoliciesListView.SelectedItems.Count > 0;
    }

    private async Task ScanPoliciesAsync()
    {
        _cancellationTokenSource?.Cancel();
        if (_cancellationRegistrationId.HasValue)
        {
            OperationCancellationManager.Unregister(_cancellationRegistrationId.Value);
            _cancellationRegistrationId = null;
        }
        _cancellationTokenSource = new CancellationTokenSource();
        _cancellationRegistrationId = OperationCancellationManager.Register(_cancellationTokenSource);

        try
        {
            // Show scanning state
            ScanProgressRing.Visibility = Visibility.Visible;
            ScanProgressRing.IsActive = true;
            SummaryText.Text = "GroupPolicyPage_ScanningPolicies".GetLocalized();
            RefreshButton.IsEnabled = false;
            RemoveAllButton.IsEnabled = false;

            // Detect policy states
            _policyStates = await GroupPolicyHelper.DetectPolicyStatesAsync(_cancellationTokenSource.Token);

            // Update UI on dispatcher thread
            DispatcherQueue.TryEnqueue(() =>
            {
                UpdateSummary();
                UpdateCategorySummary();
                UpdateConfiguredPoliciesList();
            });
        }
        catch (OperationCanceledException)
        {
            // Scan was cancelled
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error scanning policies: {ex.Message}");
            DispatcherQueue.TryEnqueue(() =>
            {
                SummaryText.Text = "GroupPolicyPage_ScanError".GetLocalized();
            });
        }
        finally
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                ScanProgressRing.Visibility = Visibility.Collapsed;
                ScanProgressRing.IsActive = false;
                RefreshButton.IsEnabled = true;
            });
            if (_cancellationRegistrationId.HasValue)
            {
                OperationCancellationManager.Unregister(_cancellationRegistrationId.Value);
                _cancellationRegistrationId = null;
            }
        }
    }

    private void UpdateSummary()
    {
        if (_policyStates == null)
            return;

        var configuredCount = _policyStates.Count(s => s.IsConfigured);
        var totalCount = _policyStates.Count;

        if (configuredCount == 0)
        {
            SummaryText.Text = "GroupPolicyPage_NoPoliciesDetected".GetLocalized();
            RemoveAllButton.IsEnabled = false;
        }
        else
        {
            SummaryText.Text = string.Format(
                "GroupPolicyPage_ConfiguredPoliciesCount".GetLocalized(),
                configuredCount,
                totalCount);
            RemoveAllButton.IsEnabled = true;
        }
    }

    private void UpdateCategorySummary()
    {
        if (_policyStates == null)
            return;

        var categoryGroups = _policyStates
            .GroupBy(s => s.Policy.Category)
            .Select(g => new CategorySummaryItem
            {
                Category = g.Key,
                TotalCount = g.Count(),
                ConfiguredCount = g.Count(s => s.IsConfigured),
                IconGlyph = GetCategoryIcon(g.Key)
            })
            .OrderByDescending(c => c.ConfiguredCount)
            .ThenBy(c => c.Category)
            .ToList();

        CategorySummaryRepeater.ItemsSource = categoryGroups;
    }

    private void UpdateConfiguredPoliciesList()
    {
        if (_policyStates == null)
            return;

        var configuredPolicies = _policyStates
            .Where(s => s.IsConfigured)
            .Select(s => new PolicyStateViewModel(s))
            .OrderBy(p => p.Policy.Category)
            .ThenBy(p => p.Policy.Name)
            .ToList();

        if (configuredPolicies.Count == 0)
        {
            ConfiguredPoliciesListView.Visibility = Visibility.Collapsed;
            NoPoliciesPanel.Visibility = Visibility.Visible;
        }
        else
        {
            ConfiguredPoliciesListView.Visibility = Visibility.Visible;
            NoPoliciesPanel.Visibility = Visibility.Collapsed;
            ConfiguredPoliciesListView.ItemsSource = configuredPolicies;
        }
    }

    private static string GetCategoryIcon(string category)
    {
        return category switch
        {
            "Windows Update" => "\uE777",
            "Privacy & Telemetry" => "\uE72E",
            "Cortana & Search" => "\uE721",
            "Windows Store" => "\uE719",
            "OneDrive" => "\uE753",
            "Security" => "\uE72E",
            "Error Reporting" => "\uE783",
            "System Restore" => "\uE777",
            "Windows Insider" => "\uF1AD",
            "Input & Privacy" => "\uE765",
            "App Privacy" => "\uE71D",
            "Windows Ink" => "\uE929",
            "Biometrics" => "\uE928",
            "Location" => "\uE81D",
            "Find My Device" => "\uE707",
            "Messaging" => "\uE715",
            "Clipboard" => "\uE77F",
            "Speech" => "\uE720",
            "Activity History" => "\uE823",
            "Gaming" => "\uE7FC",
            "Widgets & Feeds" => "\uE71B",
            "Copilot" => "\uE946",
            "Windows Recall" => "\uE946",
            "Microsoft Edge" => "\uE774",
            "File History" => "\uE8F1",
            "Search" => "\uE721",
            "Start Menu" => "\uE80F",
            "Windows Defender" => "\uE83D",
            "Windows Firewall" => "\uE785",
            "Remote Desktop" => "\uE8AF",
            "Remote Assistance" => "\uE8AF",
            "Network" => "\uE839",
            "Power Management" => "\uE945",
            "Device Installation" => "\uE772",
            "Lock Screen" => "\uE72E",
            "User Account Control" => "\uE7EF",
            "Delivery Optimization" => "\uE896",
            "Notifications" => "\uEA8F",
            "Windows Spotlight" => "\uE7E8",
            "Autoplay" => "\uE768",
            "Offline Maps" => "\uE909",
            "Print Spooler" => "\uE749",
            "Scheduled Maintenance" => "\uE916",
            "OEM & Preinstall" => "\uE770",
            "Microsoft Office" => "\uE8A5",
            "Google Chrome" => "\uE774",
            "Mozilla Firefox" => "\uE774",
            "Adobe" => "\uE8A5",
            "Microsoft Teams" => "\uE8F2",
            "BitLocker" => "\uE72E",
            "Credentials" => "\uE8D7",
            "Startup" => "\uE7E8",
            "Explorer" => "\uED43",
            "Multimedia" => "\uE8B2",
            _ => "\uE713"
        };
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        _ = LogHelper.Log("Manually refreshing group policy scan");
        await ScanPoliciesAsync();
    }

    private async void RemoveAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (_policyStates == null)
            return;

        var configuredPolicies = _policyStates.Where(s => s.IsConfigured).ToList();
        if (configuredPolicies.Count == 0)
            return;

        // Show confirmation dialog
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
            BorderBrush = (SolidColorBrush)Application.Current.Resources["AccentAAFillColorDefaultBrush"],
            Title = "GroupPolicyPage_ConfirmRemoveAllTitle".GetLocalized(),
            Content = string.Format(
                "GroupPolicyPage_ConfirmRemoveAllContent".GetLocalized(),
                configuredPolicies.Count),
            PrimaryButtonText = "GroupPolicyPage_Remove".GetLocalized(),
            PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"],
            CloseButtonText = "Cancel".GetLocalized()
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return;

        await RemovePoliciesAsync(configuredPolicies.Select(s => s.Policy));
    }

    private async void CategoryRemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string category)
            return;

        if (_policyStates == null)
            return;

        var categoryPolicies = _policyStates
            .Where(s => s.IsConfigured && s.Policy.Category == category)
            .ToList();

        if (categoryPolicies.Count == 0)
            return;

        // Show confirmation dialog
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
            BorderBrush = (SolidColorBrush)Application.Current.Resources["AccentAAFillColorDefaultBrush"],
            Title = "GroupPolicyPage_ConfirmRemoveCategoryTitle".GetLocalized(),
            Content = string.Format(
                "GroupPolicyPage_ConfirmRemoveCategoryContent".GetLocalized(),
                categoryPolicies.Count,
                category),
            PrimaryButtonText = "GroupPolicyPage_Remove".GetLocalized(),
            PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"],
            CloseButtonText = "Cancel".GetLocalized()
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return;

        await RemovePoliciesAsync(categoryPolicies.Select(s => s.Policy));
    }

    private async void PolicyRemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string policyId)
            return;

        if (_policyStates == null)
            return;

        var policy = _policyStates.FirstOrDefault(s => s.Policy.Id == policyId);
        if (policy == null)
            return;

        await RemovePoliciesAsync([policy.Policy]);
    }

    private async void RemoveSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedItems = ConfiguredPoliciesListView.SelectedItems
            .OfType<PolicyStateViewModel>()
            .ToList();

        if (selectedItems.Count == 0)
            return;

        // Show confirmation dialog
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
            BorderBrush = (SolidColorBrush)Application.Current.Resources["AccentAAFillColorDefaultBrush"],
            Title = "GroupPolicyPage_ConfirmRemoveSelectedTitle".GetLocalized(),
            Content = string.Format(
                "GroupPolicyPage_ConfirmRemoveSelectedContent".GetLocalized(),
                selectedItems.Count),
            PrimaryButtonText = "GroupPolicyPage_Remove".GetLocalized(),
            PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"],
            CloseButtonText = "Cancel".GetLocalized()
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return;

        await RemovePoliciesAsync(selectedItems.Select(s => s.Policy));
    }

    private async Task RemovePoliciesAsync(IEnumerable<GroupPolicyHelper.PolicyEntry> policies)
    {
        var policyList = policies.ToList();
        if (policyList.Count == 0)
            return;

        try
        {
            // Show progress
            ScanProgressRing.Visibility = Visibility.Visible;
            ScanProgressRing.IsActive = true;
            SummaryText.Text = "GroupPolicyPage_RemovingPolicies".GetLocalized();
            RefreshButton.IsEnabled = false;
            RemoveAllButton.IsEnabled = false;

            _ = LogHelper.Log($"Removing {policyList.Count} group policy overrides");
            var (succeeded, failed) = await GroupPolicyHelper.RemovePolicyOverridesAsync(policyList);
            _ = LogHelper.Log($"Policy removal complete: {succeeded} succeeded, {failed} failed");

            // Show result notification
            if (failed == 0)
            {
                App.ShowNotification(
                    "GroupPolicyPage_RemoveSuccessTitle".GetLocalized(),
                    string.Format("GroupPolicyPage_RemoveSuccessContent".GetLocalized(), succeeded),
                    InfoBarSeverity.Success,
                    5000);
            }
            else
            {
                App.ShowNotification(
                    "GroupPolicyPage_RemovePartialTitle".GetLocalized(),
                    string.Format("GroupPolicyPage_RemovePartialContent".GetLocalized(), succeeded, failed),
                    InfoBarSeverity.Warning,
                    5000);
            }

            // Ask if user wants to restart Explorer
            if (succeeded > 0)
            {
                var restartDialog = new ContentDialog
                {
                    XamlRoot = XamlRoot,
                    Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
                    BorderBrush = (SolidColorBrush)Application.Current.Resources["AccentAAFillColorDefaultBrush"],
                    Title = "GroupPolicyPage_RestartExplorerTitle".GetLocalized(),
                    Content = "GroupPolicyPage_RestartExplorerContent".GetLocalized(),
                    PrimaryButtonText = "GroupPolicyPage_RestartNow".GetLocalized(),
                    PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"],
                    CloseButtonText = "GroupPolicyPage_Later".GetLocalized()
                };

                var restartResult = await restartDialog.ShowAsync();
                if (restartResult == ContentDialogResult.Primary)
                {
                    await GroupPolicyHelper.RestartExplorerAsync();
                }
            }

            // Rescan policies
            await ScanPoliciesAsync();
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error removing policies: {ex.Message}");
            App.ShowNotification(
                "GroupPolicyPage_RemoveErrorTitle".GetLocalized(),
                "GroupPolicyPage_RemoveErrorContent".GetLocalized(),
                InfoBarSeverity.Error,
                5000);
        }
        finally
        {
            ScanProgressRing.Visibility = Visibility.Collapsed;
            ScanProgressRing.IsActive = false;
            RefreshButton.IsEnabled = true;
        }
    }
}

// View model for category summary display.
internal sealed class CategorySummaryItem
{
    public required string Category
    {
        get; init;
    }
    public required int TotalCount
    {
        get; init;
    }
    public required int ConfiguredCount
    {
        get; init;
    }
    public required string IconGlyph
    {
        get; init;
    }

    public string StatusText => ConfiguredCount == 0
        ? "GroupPolicyPage_NotConfigured".GetLocalized()
        : string.Format("GroupPolicyPage_ConfiguredCount".GetLocalized(), ConfiguredCount);

    public SolidColorBrush StatusColor => ConfiguredCount == 0
        ? new SolidColorBrush(Microsoft.UI.Colors.Green)
        : (SolidColorBrush)Application.Current.Resources["SystemFillColorCautionBrush"];

    public bool HasConfiguredPolicies => ConfiguredCount > 0;

    public string ButtonText => "GroupPolicyPage_RemoveOverrides".GetLocalized();
}

// View model for policy state display.
internal sealed class PolicyStateViewModel
{
    private readonly GroupPolicyHelper.PolicyState _state;

    public PolicyStateViewModel(GroupPolicyHelper.PolicyState state)
    {
        _state = state;
    }

    public GroupPolicyHelper.PolicyEntry Policy => _state.Policy;

    public string HiveDisplay => _state.Policy.Hive switch
    {
        Microsoft.Win32.RegistryHive.LocalMachine => "HKLM",
        Microsoft.Win32.RegistryHive.CurrentUser => "HKCU",
        _ => _state.Policy.Hive.ToString()
    };

    public string CurrentValueDisplay
    {
        get
        {
            if (_state.CurrentValue == null)
                return "Not set";

            return _state.ActualValueKind switch
            {
                Microsoft.Win32.RegistryValueKind.DWord => $"Value: {_state.CurrentValue}",
                Microsoft.Win32.RegistryValueKind.String => $"Value: \"{_state.CurrentValue}\"",
                _ => $"Value: {_state.CurrentValue}"
            };
        }
    }
}
