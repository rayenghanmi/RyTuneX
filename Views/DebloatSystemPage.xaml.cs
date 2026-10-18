using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using RyTuneX.Helpers;
using System.Collections.ObjectModel;
using System.Text;

namespace RyTuneX.Views;

public sealed partial class DebloatSystemPage : Page
{
    public ObservableCollection<Tuple<string, string, bool>> AppList { get; set; } = new();
    private static (List<AppInfo> Apps, HashSet<string> UninstallableNames)? _cachedInstalledApps;
    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _uninstallBatchCts;
    private Guid? _uninstallBatchRegistrationId;
    private List<AppInfo> allApps = new();
    private HashSet<string> uninstallableAppNames = new(StringComparer.OrdinalIgnoreCase);
    private string? _pendingScrollTarget;
    private bool _isLoading;

    public DebloatSystemPage()
    {
        InitializeComponent();
        LogHelper.Log("Initializing DebloatSystemPage");
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Disabled;
        Loaded += DebloatSystemPage_Loaded;
        Unloaded += DebloatSystemPage_Unloaded;
    }

    private void DebloatSystemPage_Unloaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            // Cancel any ongoing operations for this page
            try { _loadCts?.Cancel(); _loadCts?.Dispose(); _loadCts = null; } catch { }

            if (_uninstallBatchCts != null)
            {
                try { _uninstallBatchCts.Cancel(); } catch { }
                _uninstallBatchCts.Dispose();
                _uninstallBatchCts = null;
            }

            AppList.Clear();
            allApps.Clear();
            uninstallableAppNames.Clear();
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogWarning($"Error during DebloatSystemPage unload cleanup: {ex.Message}");
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string optionTag && !string.IsNullOrEmpty(optionTag))
        {
            _pendingScrollTarget = optionTag;
        }
    }

    private async void DebloatSystemPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (allApps.Count == 0 && !_isLoading)
        {
            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _loadCts = new CancellationTokenSource();
            LoadInstalledApps(_loadCts.Token);
        }

        if (!string.IsNullOrEmpty(_pendingScrollTarget))
        {
            await ScrollToElementHelper.ScrollToElementAsync(this, _pendingScrollTarget);
            _pendingScrollTarget = null;
        }
    }

    private async void LoadInstalledApps(CancellationToken cancellationToken = default)
    {
        if (_isLoading) return;
        _isLoading = true;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            DispatcherQueue.TryEnqueue(() =>
            {
                gettingAppsLoading.Visibility = Visibility.Visible;
                appListView.Visibility = Visibility.Collapsed;
                installedAppsCount.Visibility = Visibility.Collapsed;
                AppSearchBox.IsEnabled = false;
                appsFilter.IsEnabled = false;
                reloadButton.IsEnabled = false;
                TempButton.IsEnabled = false;
                uninstallButton.IsEnabled = false;
                uninstallingStatusText.Text = RyTuneX.Helpers.ResourceExtensions.GetLocalized("UninstallTip");
                uninstallingStatusBar.Opacity = 0;
            });

            _ = LogHelper.Log("Loading InstalledApps");

            (List<AppInfo> installedApps, HashSet<string> fetchedUninstallableNames) data;
            if (_cachedInstalledApps.HasValue)
            {
                data = _cachedInstalledApps.Value;
            }
            else
            {
                data = await Task.Run(OptimizationOptions.GetInstalledApps).ConfigureAwait(false);
                _cachedInstalledApps = data;
            }

            var (installedApps, fetchedUninstallableNames) = data;

            DispatcherQueue.TryEnqueue(() =>
            {
                uninstallableAppNames = fetchedUninstallableNames;
                allApps = installedApps.Where(app =>
                    !app.Name.Contains("rytunex", StringComparison.CurrentCultureIgnoreCase)).ToList();

                ApplyFilter();

                installedAppsCount.Visibility = Visibility.Visible;
                appsFilter.IsEnabled = true;
                appsFilter.Visibility = Visibility.Visible;
                appsFilterText.Visibility = Visibility.Visible;
                reloadButton.Visibility = Visibility.Visible;
                reloadButton.IsEnabled = true;
                uninstallButton.Visibility = Visibility.Visible;
                appListView.Visibility = Visibility.Visible;
                appListView.IsEnabled = true;
                uninstallButton.IsEnabled = true;
                uninstallingStatusText.Visibility = Visibility.Visible;
                AppSearchBox.Visibility = Visibility.Visible;
                AppSearchBox.IsEnabled = true;
                gettingAppsLoading.Visibility = Visibility.Collapsed;
                TempStackButtonTextBar.Visibility = Visibility.Visible;
                TempButton.IsEnabled = true;
            });
        }
        catch (OperationCanceledException ex)
        {
            _ = LogHelper.Log($"Operation canceled: {ex.Message}\nStack Trace: {ex.StackTrace}");
            DispatcherQueue.TryEnqueue(() =>
            {
                gettingAppsLoading.Visibility = Visibility.Collapsed;
                AppSearchBox.IsEnabled = true;
                appsFilter.IsEnabled = true;
                reloadButton.IsEnabled = true;
                TempButton.IsEnabled = true;
            });
        }
        catch (Exception ex)
        {
            _ = LogHelper.Log($"Error loading installed apps: {ex.Message}\nStack Trace: {ex.StackTrace}");
            DispatcherQueue.TryEnqueue(() =>
            {
                gettingAppsLoading.Visibility = Visibility.Collapsed;
                AppSearchBox.IsEnabled = true;
                appsFilter.IsEnabled = true;
                reloadButton.IsEnabled = true;
                TempButton.IsEnabled = true;
            });
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void ApplyFilter()
    {
        var searchText = AppSearchBox?.Text?.Trim() ?? string.Empty;

        var filtered = appsFilter.SelectedIndex switch
        {
            0 => allApps.Where(app => app.IsWin32 || uninstallableAppNames.Contains(app.Name) || uninstallableAppNames.Contains(app.PackageId)),
            2 => allApps.Where(app => app.IsWin32),
            _ => allApps.AsEnumerable()
        };

        if (!string.IsNullOrEmpty(searchText))
        {
            filtered = filtered.Where(app => app.Name.Contains(searchText, StringComparison.CurrentCultureIgnoreCase));
        }

        var result = filtered.OrderBy(app => app.Name).ToList();

        installedAppsCount.Text = string.Format(RyTuneX.Helpers.ResourceExtensions.GetLocalized("TotalApps"), result.Count);
        noAppFoundText.Visibility = result.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        AppList.Clear();
        foreach (var app in result)
        {
            AppList.Add(app);
        }
    }

    private async void UninstallSelectedApp_Click(object sender, RoutedEventArgs e)
    {
        if (appListView.SelectedItems.Count == 0)
        {
            return;
        }

        var result = await ShowUninstallConfirmationDialog(appListView);
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        // Mark explicit operation and register a CTS for this uninstall batch so it can be cancelled via the central manager
        OperationCancellationManager.EnterOperation();
        _uninstallBatchCts?.Dispose();
        _uninstallBatchCts = new CancellationTokenSource();
        _uninstallBatchRegistrationId = OperationCancellationManager.Register(_uninstallBatchCts);
        var batchCt = _uninstallBatchCts.Token;

        uninstallButton.IsEnabled = false;
        reloadButton.IsEnabled = false;
        appsFilter.IsEnabled = false;
        appListView.IsEnabled = false;

        var failedUninstalls = new List<string>();
        var successfulUninstalls = new List<string>();

        try
        {
            // Snapshot selected items before starting
            var selectedApps = appListView.SelectedItems.OfType<AppInfo>().ToList();
            var totalApps = selectedApps.Count;
            var completedApps = 0;

            // Initialize status bar
            DispatcherQueue.TryEnqueue(() =>
            {
                uninstallingStatusBar.Value = 0;
                uninstallingStatusBar.Maximum = totalApps;
                uninstallingStatusBar.Opacity = 1;
            });

            foreach (var appInfo in selectedApps)
            {
                var selectedAppName = appInfo.Name;

                await DispatcherQueue.EnqueueAsync(() =>
                {
                    uninstallingStatusText.Text = RyTuneX.Helpers.ResourceExtensions.GetLocalized("Uninstalling") + " " + selectedAppName;
                });

                try
                {
                    if (batchCt.IsCancellationRequested)
                    {
                        // Stop processing further uninstalls
                        break;
                    }

                    await UninstallApps(appInfo);
                    successfulUninstalls.Add(selectedAppName);

                    // Remove the uninstalled app from the view and cached list
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        AppList.Remove(appInfo);
                        allApps.Remove(appInfo);
                        uninstallableAppNames.Remove(selectedAppName);
                        uninstallableAppNames.Remove(appInfo.PackageId);
                        installedAppsCount.Text = string.Format(RyTuneX.Helpers.ResourceExtensions.GetLocalized("TotalApps"), AppList.Count);
                    });
                }
                catch (Exception ex)
                {
                    _ = LogHelper.LogError($"Error uninstalling {selectedAppName}: {ex.Message}\nStack Trace: {ex.StackTrace}");
                    failedUninstalls.Add(selectedAppName);
                }

                // Update progress bar
                completedApps++;
                DispatcherQueue.TryEnqueue(() =>
                {
                    uninstallingStatusBar.Value = completedApps;
                });
            }

            _ = LogHelper.Log($"Uninstall batch complete: {successfulUninstalls.Count} succeeded, {failedUninstalls.Count} failed");

            // Show notifications
            if (successfulUninstalls.Count > 0)
            {
                var successMessage = string.Join("\n", successfulUninstalls);
                // Show success message with animation
                App.ShowNotification(
                    RyTuneX.Helpers.ResourceExtensions.GetLocalized("Debloat"),
                    RyTuneX.Helpers.ResourceExtensions.GetLocalized("UninstallationSuccess") + $":\n{successMessage}",
                    InfoBarSeverity.Success, 5000);

                ReviewPromptHelper.NotifyOptimizationCompleted(XamlRoot);
            }

            if (failedUninstalls.Count > 0)
            {
                var errorMessage = string.Join("\n", failedUninstalls);
                // Show error message with animation
                App.ShowNotification(
                    RyTuneX.Helpers.ResourceExtensions.GetLocalized("Debloat"),
                    RyTuneX.Helpers.ResourceExtensions.GetLocalized("UninstallationError") + $":\n{errorMessage}",
                    InfoBarSeverity.Error, 5000);
            }

        }
        catch (OperationCanceledException)
        {
            // Batch was cancelled
            _ = LogHelper.Log("Uninstall batch cancelled by user/system.");
        }
        catch (Exception ex)
        {
            // Log the error
            _ = LogHelper.LogError($"Error during uninstallation process: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
        finally
        {
            // Clear the selected apps for uninstall
            appListView.SelectedItems.Clear();

            // Reset UI
            DispatcherQueue.TryEnqueue(() =>
            {
                uninstallingStatusText.Text = RyTuneX.Helpers.ResourceExtensions.GetLocalized("UninstallTip");
                uninstallButton.IsEnabled = true;
                reloadButton.IsEnabled = true;
                appsFilter.IsEnabled = true;
                appListView.IsEnabled = true;
                uninstallingStatusBar.Opacity = 0;
            });

            // Cleanup uninstall batch CTS and unregister
            if (_uninstallBatchRegistrationId.HasValue)
            {
                OperationCancellationManager.Unregister(_uninstallBatchRegistrationId.Value);
                _uninstallBatchRegistrationId = null;
            }
            if (_uninstallBatchCts != null)
            {
                _uninstallBatchCts.Dispose();
                _uninstallBatchCts = null;
            }
            // Clear explicit operation marker
            OperationCancellationManager.ExitOperation();
        }
    }

    private static async Task UninstallApps(AppInfo appInfo)
    {
        _ = LogHelper.Log($"Uninstalling: {appInfo.Name}");

        if (appInfo.IsWin32)
        {
            await OptimizationOptions.UninstallWin32AppAsync(appInfo.Name).ConfigureAwait(false);
        }
        else
        {
            await OptimizationOptions.UninstallUwpAppAsync(appInfo.PackageId, appInfo.Name).ConfigureAwait(false);
        }
    }

    private void ReloadApps_Click(object sender, RoutedEventArgs e)
    {
        _ = LogHelper.Log("Reloading installed apps list");
        _cachedInstalledApps = null;
        allApps.Clear();
        AppList.Clear();
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        LoadInstalledApps(_loadCts.Token);
    }

    private void appsFilter_SelectionChanged(object sender, RoutedEventArgs e)
    {
        if (allApps.Count == 0)
        {
            return;
        }

        if (appsFilter.SelectedIndex == 1)
        {
            // Show warning message when all apps are showing
            App.ShowNotification(
                RyTuneX.Helpers.ResourceExtensions.GetLocalized("Debloat"),
                RyTuneX.Helpers.ResourceExtensions.GetLocalized("DebloatPage_NotificationBody"),
                InfoBarSeverity.Warning, 5000);
        }

        ApplyFilter();
    }

    private async void TempButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Show the deep clean dialog for scanning and category selection
            var selectedCategories = await ShowDeepCleanDialogAsync();
            if (selectedCategories == null || selectedCategories.Count == 0) return;

            _ = LogHelper.Log($"Deep clean: starting with {selectedCategories.Count} selected categories");

            // Show progress on the button
            TempButton.IsEnabled = false;
            TempButtonProgressRing.Visibility = Visibility.Visible;
            TempButtonIcon.Visibility = Visibility.Collapsed;

            // Execute cleaning for all selected categories
            var (success, bytesCleared) = await DeepCleanEngine.CleanCategoriesAsync(selectedCategories);

            // Reset button UI
            TempButton.IsEnabled = true;
            TempButtonProgressRing.Visibility = Visibility.Collapsed;
            TempButtonIcon.Visibility = Visibility.Visible;

            if (success)
            {
                var sizeStr = DeepCleanEngine.FormatBytes(bytesCleared);
                App.ShowNotification(
                    ResourceExtensions.GetLocalized("DeepClean_Title"),
                    string.Format(ResourceExtensions.GetLocalized("DeepClean_CompleteDesc"), sizeStr),
                    InfoBarSeverity.Success, 5000);
                ReviewPromptHelper.NotifyOptimizationCompleted(XamlRoot);
            }
            else
            {
                App.ShowNotification(
                    ResourceExtensions.GetLocalized("DeepClean_Title"),
                    ResourceExtensions.GetLocalized("DeepClean_Error"),
                    InfoBarSeverity.Error, 5000);
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogException(ex, "TempButton_Click");
            TempButton.IsEnabled = true;
            TempButtonProgressRing.Visibility = Visibility.Collapsed;
            TempButtonIcon.Visibility = Visibility.Visible;

            App.ShowNotification(
                ResourceExtensions.GetLocalized("DeepClean_Title"),
                ResourceExtensions.GetLocalized("DeepClean_Error"),
                InfoBarSeverity.Error, 5000);
        }
    }

    // Displays the Deep Clean dialog with categorized scanning and selection
    private async Task<List<CleanupCategory>?> ShowDeepCleanDialogAsync()
    {
        var categories = DeepCleanEngine.GetCategories();

        // Root content panel
        var mainPanel = new StackPanel { Spacing = 12, MinWidth = 480 };

        // Description text
        mainPanel.Children.Add(new TextBlock
        {
            Text = ResourceExtensions.GetLocalized("DeepClean_Description"),
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            Margin = new Thickness(0, 0, 0, 4)
        });

        // Scanning progress indicator
        var scanningRing = new ProgressRing { Width = 16, Height = 16, IsActive = true };
        var scanningText = new TextBlock
        {
            Text = ResourceExtensions.GetLocalized("DeepClean_Scanning"),
            VerticalAlignment = VerticalAlignment.Center,
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"]
        };
        var scanningPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { scanningRing, scanningText }
        };
        mainPanel.Children.Add(scanningPanel);

        // Select All / Deselect All links
        var selectAllBtn = new HyperlinkButton { Content = ResourceExtensions.GetLocalized("DeepClean_SelectAll"), Padding = new Thickness(4, 2, 4, 2) };
        var deselectAllBtn = new HyperlinkButton { Content = ResourceExtensions.GetLocalized("DeepClean_DeselectAll"), Padding = new Thickness(4, 2, 4, 2) };
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { selectAllBtn, deselectAllBtn }
        };
        mainPanel.Children.Add(buttonPanel);

        // Build the category rows
        var categoriesPanel = new StackPanel { Spacing = 2 };
        var sizeTexts = new Dictionary<string, TextBlock>();
        var checkBoxes = new Dictionary<string, CheckBox>();

        foreach (var cat in categories)
        {
            var row = BuildCategoryRow(cat, out var sizeText, out var checkBox);
            categoriesPanel.Children.Add(row);
            sizeTexts[cat.Id] = sizeText;
            checkBoxes[cat.Id] = checkBox;
        }

        var scrollViewer = new ScrollViewer
        {
            Content = categoriesPanel,
            MaxHeight = 400,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        mainPanel.Children.Add(scrollViewer);

        // Total selected size
        var totalText = new TextBlock
        {
            Text = string.Format(ResourceExtensions.GetLocalized("DeepClean_TotalSelected"), "..."),
            Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"],
            Margin = new Thickness(0, 8, 0, 0)
        };
        mainPanel.Children.Add(totalText);

        // Create the dialog
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            RequestedTheme = ActualTheme,
            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
            BorderBrush = (Brush)Application.Current.Resources["AccentAAFillColorDefaultBrush"],
            Title = ResourceExtensions.GetLocalized("DeepClean_Title"),
            Content = mainPanel,
            PrimaryButtonText = ResourceExtensions.GetLocalized("DeepClean_CleanButton"),
            CloseButtonText = ResourceExtensions.GetLocalized("Close"),
            PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"],
            IsPrimaryButtonEnabled = true
        };

        // Update total and button state based on selections
        void UpdateTotal()
        {
            var selected = categories.Where(c => c.IsSelected).ToList();
            var totalSize = selected.Where(c => c.SizeScanned).Sum(c => c.SizeBytes);
            totalText.Text = string.Format(
                ResourceExtensions.GetLocalized("DeepClean_TotalSelected"),
                DeepCleanEngine.FormatBytes(totalSize));
            dialog.IsPrimaryButtonEnabled = selected.Count > 0;
        }

        // Wire checkbox events
        foreach (var cat in categories)
        {
            var catRef = cat;
            checkBoxes[cat.Id].Checked += (s, e) => { catRef.IsSelected = true; UpdateTotal(); };
            checkBoxes[cat.Id].Unchecked += (s, e) => { catRef.IsSelected = false; UpdateTotal(); };
        }

        // Wire Select All / Deselect All
        selectAllBtn.Click += (s, e) =>
        {
            foreach (var cat in categories) { cat.IsSelected = true; checkBoxes[cat.Id].IsChecked = true; }
            UpdateTotal();
        };
        deselectAllBtn.Click += (s, e) =>
        {
            foreach (var cat in categories) { cat.IsSelected = false; checkBoxes[cat.Id].IsChecked = false; }
            UpdateTotal();
        };

        // Start scanning all categories in parallel
        _ = Task.Run(async () =>
        {
            var scanTasks = categories.Select(async cat =>
            {
                await DeepCleanEngine.ScanCategoryAsync(cat);
                DispatcherQueue.TryEnqueue(() =>
                {
                    sizeTexts[cat.Id].Text = cat.SizeBytes > 0
                        ? DeepCleanEngine.FormatBytes(cat.SizeBytes)
                        : "—";
                    UpdateTotal();
                });
            });
            await Task.WhenAll(scanTasks);
            DispatcherQueue.TryEnqueue(() => scanningPanel.Visibility = Visibility.Collapsed);
        });

        // Show dialog and return selected categories
        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary
            ? categories.Where(c => c.IsSelected).ToList()
            : null;
    }

    // Builds a single category row for the deep clean dialog
    private Border BuildCategoryRow(CleanupCategory category, out TextBlock sizeText, out CheckBox checkBox)
    {
        checkBox = new CheckBox
        {
            IsChecked = true,
            MinWidth = 0,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0)
        };

        var icon = new FontIcon
        {
            Glyph = category.Glyph,
            FontSize = 16,
            Foreground = (Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"],
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0)
        };

        var nameText = new TextBlock
        {
            Text = ResourceExtensions.GetLocalized(category.NameKey),
            Style = (Style)Application.Current.Resources["BodyTextBlockStyle"]
        };

        var descText = new TextBlock
        {
            Text = ResourceExtensions.GetLocalized(category.DescriptionKey),
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var textPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        textPanel.Children.Add(nameText);
        textPanel.Children.Add(descText);

        sizeText = new TextBlock
        {
            Text = "...",
            Style = (Style)Application.Current.Resources["BodyTextBlockStyle"],
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            MinWidth = 70,
            TextAlignment = TextAlignment.Right
        };

        var contentGrid = new Grid { VerticalAlignment = VerticalAlignment.Center };
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(checkBox, 0);
        Grid.SetColumn(icon, 1);
        Grid.SetColumn(textPanel, 2);
        Grid.SetColumn(sizeText, 3);

        contentGrid.Children.Add(checkBox);
        contentGrid.Children.Add(icon);
        contentGrid.Children.Add(textPanel);
        contentGrid.Children.Add(sizeText);

        return new Border
        {
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(12, 10, 16, 10),
            Child = contentGrid
        };
    }

    private void AppSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ApplyFilter();
        }
    }

    private void AppSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        ApplyFilter();
    }

    public async Task<ContentDialogResult> ShowUninstallConfirmationDialog(ListView appListView)
    {
        var selectedItemsText = new StringBuilder();

        foreach (var item in appListView.SelectedItems.OfType<AppInfo>())
        {
            selectedItemsText.AppendLine(item.Name);
        }

        var firstLine = RyTuneX.Helpers.ResourceExtensions.GetLocalized("ConfirmRemoveApps");
        var lastLine = RyTuneX.Helpers.ResourceExtensions.GetLocalized("ConfirmContinue");

        var firstLineTextBlock = new TextBlock
        {
            Text = firstLine,
            Margin = new Thickness(0, 10, 0, 20),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Top,
        };

        var lastLineTextBlock = new TextBlock
        {
            Text = lastLine,
            Margin = new Thickness(0, 20, 0, 10),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Bottom
        };

        var selectedAppsTextBlock = new TextBlock
        {
            Text = selectedItemsText.ToString(),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Top,
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"]
        };

        var scrollViewer = new ScrollViewer
        {
            Content = selectedAppsTextBlock,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            MaxHeight = 400
        };

        var contentStackPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            RequestedTheme = ActualTheme,
            Children = { firstLineTextBlock, scrollViewer, lastLineTextBlock }
        };

        var confirmationDialog = new ContentDialog()
        {
            XamlRoot = XamlRoot,
            RequestedTheme = ActualTheme,
            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
            BorderBrush = (SolidColorBrush)Application.Current.Resources["AccentAAFillColorDefaultBrush"],
            Title = RyTuneX.Helpers.ResourceExtensions.GetLocalized("Debloat"),
            Content = contentStackPanel,
            CloseButtonText = RyTuneX.Helpers.ResourceExtensions.GetLocalized("Close"),
            PrimaryButtonText = RyTuneX.Helpers.ResourceExtensions.GetLocalized("Continue"),
            PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"],
        };

        return await confirmationDialog.ShowAsync();
    }

}
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        // Convert null to Collapsed and non-null to Visible
        return value == null ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}