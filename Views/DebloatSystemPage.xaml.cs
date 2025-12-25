using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Win32;
using RyTuneX.Helpers;

namespace RyTuneX.Views;

public sealed partial class DebloatSystemPage : Page
{
    public ObservableCollection<Tuple<string, string, bool>> AppList { get; set; } = new();
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private List<Tuple<string, string, bool>> allApps = new();
    private string? _pendingScrollTarget;

    public DebloatSystemPage()
    {
        InitializeComponent();
        LogHelper.Log("Initializing DebloatSystemPage");
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        Loaded += DebloatSystemPage_Loaded;
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
        if (!string.IsNullOrEmpty(_pendingScrollTarget))
        {
            await ScrollToElementHelper.ScrollToElementAsync(this, _pendingScrollTarget);
            _pendingScrollTarget = null;
        }
    }

    private void AppTreeView_DragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs args)
    {
        args.Cancel = true;
    }

    // Select the treeview item when pressed
    private void appTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is Tuple<string, string, bool> app)
        {
            if (sender.SelectedItems.Contains(app))
            {
                // If the item is already selected, remove it
                sender.SelectedItems.Remove(app);
            }
            else
            {
                // If the item is not selected, add it
                sender.SelectedItems.Add(app);
            }
        }
    }

    private async void LoadInstalledApps(bool uninstallableOnly = true, bool win32Only = false, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            DispatcherQueue.TryEnqueue(() =>
            {
                gettingAppsLoading.Visibility = Visibility.Visible;
                appTreeView.Visibility = Visibility.Collapsed;
                uninstallButton.IsEnabled = false;
                uninstallingStatusText.Text = RyTuneX.Helpers.ResourceExtensions.GetLocalized("UninstallTip");
                uninstallingStatusBar.Opacity = 0;
                appsFilter.IsEnabled = false;
            });

            await LogHelper.Log("Loading InstalledApps");

            List<Tuple<string, string, bool>> installedApps;
            if (win32Only)
            {
                installedApps = await Task.Run(OptimizationOptions.GetWin32Apps);
            }
            else
            {
                installedApps = await Task.Run(() => OptimizationOptions.GetInstalledApps(uninstallableOnly));
            }

            DispatcherQueue.TryEnqueue(() =>
            {
                AppList.Clear();

                allApps = installedApps.AsParallel().Where(app =>
                !app.Item1.Contains("rytunex", StringComparison.CurrentCultureIgnoreCase)).ToList();

                foreach (var app in allApps)
                {
                    AppList.Add(app);
                }

                installedAppsCount.Text = string.Format(RyTuneX.Helpers.ResourceExtensions.GetLocalized("TotalApps"), AppList.Count);
                installedAppsCount.Visibility = Visibility.Visible;
                appsFilter.IsEnabled = true;
                appsFilter.Visibility = Visibility.Visible;
                appsFilterText.Visibility = Visibility.Visible;
                uninstallButton.Visibility = Visibility.Visible;
                appTreeView.Visibility = Visibility.Visible;
                appTreeView.IsEnabled = true;
                uninstallButton.IsEnabled = true;
                uninstallingStatusText.Visibility = Visibility.Visible;
                AppSearchBox.Visibility = Visibility.Visible;
                gettingAppsLoading.Visibility = Visibility.Collapsed;
                TempStackButtonTextBar.Visibility = Visibility.Visible;
            });
        }
        catch (OperationCanceledException ex)
        {
            await LogHelper.Log($"Operation canceled: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
        catch (Exception ex)
        {
            await LogHelper.Log($"Error loading installed apps: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
    }

    private async void UninstallSelectedApp_Click(object sender, RoutedEventArgs e)
    {
        if (appTreeView.SelectedItems.Count == 0)
        {
            return;
        }

        var result = await ShowUninstallConfirmationDialog(appTreeView);
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        uninstallButton.IsEnabled = false;
        appsFilter.IsEnabled = false;
        appTreeView.IsEnabled = false;

        var failedUninstalls = new List<string>();
        var successfulUninstalls = new List<string>();

        try
        {
            var totalApps = appTreeView.SelectedItems.Count;
            var completedApps = 0;

            // Initialize status bar
            DispatcherQueue.TryEnqueue(() =>
            {
                uninstallingStatusBar.Value = 0;
                uninstallingStatusBar.Maximum = totalApps;
                uninstallingStatusBar.Opacity = 1;
            });

            foreach (var appInfo in appTreeView.SelectedItems.OfType<Tuple<string, string, bool>>())
            {
                var selectedAppName = appInfo.Item1;
                var isWin32App = appInfo.Item3;

                await DispatcherQueue.EnqueueAsync(() =>
                {
                    uninstallingStatusText.Text = RyTuneX.Helpers.ResourceExtensions.GetLocalized("Uninstalling") + " " + selectedAppName;
                });

                try
                {
                    await UninstallApps(selectedAppName, isWin32App);
                    successfulUninstalls.Add(selectedAppName);
                }
                catch (Exception ex)
                {
                    await LogHelper.LogError($"Error uninstalling {selectedAppName}: {ex.Message}\nStack Trace: {ex.StackTrace}");
                    failedUninstalls.Add(selectedAppName);
                }

                // Update progress bar
                completedApps++;
                DispatcherQueue.TryEnqueue(() =>
                {
                    uninstallingStatusBar.Value = completedApps;
                });
            }

            // Show notifications
            if (successfulUninstalls.Count > 0)
            {
                var successMessage = string.Join("\n", successfulUninstalls);
                // Show success message with animation
                App.ShowNotification(
                    RyTuneX.Helpers.ResourceExtensions.GetLocalized("Debloat"),
                    RyTuneX.Helpers.ResourceExtensions.GetLocalized("UninstallationSuccess") + $":\n{successMessage}",
                    InfoBarSeverity.Success, 5000);
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

            // Reload installed apps after uninstallation and keep the list filtered
            appsFilter_SelectionChanged(appsFilter, e);

        }
        catch (Exception ex)
        {
            // Log the error
            await LogHelper.LogError($"Error during uninstallation process: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
        finally
        {
            // Clear the selected apps for uninstall
            appTreeView.SelectedItems.Clear();

            // Reset UI
            DispatcherQueue.TryEnqueue(() =>
            {
                uninstallingStatusText.Text = RyTuneX.Helpers.ResourceExtensions.GetLocalized("UninstallTip");
                uninstallButton.IsEnabled = true;
                appsFilter.IsEnabled = true;
                appTreeView.IsEnabled = true;
            });
        }
    }

    private static async Task UninstallApps(string appName, bool isWin32App)
    {
        await LogHelper.Log($"Uninstalling: {appName}");

        if (!isWin32App)
        {
            if (!appName.Contains("microsoft.edge.stable", StringComparison.CurrentCultureIgnoreCase))
            {
                var cmdCommandRemoveProvisioned = $"powershell -Command \"Get-AppxProvisionedPackage -Online | Where-Object {{ $_.DisplayName -eq '{appName}' }} | ForEach-Object {{ Remove-AppxProvisionedPackage -Online -PackageName $_.PackageName }}\"";
                var cmdCommandRemoveAppxPackage = $"powershell -Command \"Get-AppxPackage -AllUsers | Where-Object {{ $_.Name -eq '{appName}' }} | Remove-AppxPackage\"";

                // Create the process to try running Remove-AppxProvisionedPackage first
                var processInfoProvisioned = new ProcessStartInfo(Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                        ? Path.Combine(Environment.GetEnvironmentVariable("windir"), @"SysNative\cmd.exe")
                        : Path.Combine(Environment.GetEnvironmentVariable("windir"), @"System32\cmd.exe"), $"/c {cmdCommandRemoveProvisioned}")
                {
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var processProvisioned = new Process { StartInfo = processInfoProvisioned };
                {
                    processProvisioned.Start();

                    var errorProvisioned = await processProvisioned.StandardError.ReadToEndAsync();

                    // Log errors but ignore them and proceed to the second command
                    if (!string.IsNullOrEmpty(errorProvisioned))
                    {
                        await LogHelper.LogError(errorProvisioned);
                    }
                }

                // Run Remove-AppxPackage
                var processInfoAppxPackage = new ProcessStartInfo(Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                        ? Path.Combine(Environment.GetEnvironmentVariable("windir"), @"SysNative\cmd.exe")
                        : Path.Combine(Environment.GetEnvironmentVariable("windir"), @"System32\cmd.exe"), $"/c {cmdCommandRemoveAppxPackage}")
                {
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var processAppxPackage = new Process { StartInfo = processInfoAppxPackage };
                {
                    processAppxPackage.Start();

                    var errorAppxPackage = await processAppxPackage.StandardError.ReadToEndAsync();

                    if (!string.IsNullOrEmpty(errorAppxPackage))
                    {
                        await LogHelper.LogError(errorAppxPackage);
                        throw new Exception($"Failed to remove Appx package for {appName}: {errorAppxPackage}");
                    }
                }
            }
            else
            {
                // Remove Edge using @he3als EdgeRemover script
                // Link: https://github.com/he3als/EdgeRemover/blob/main/RemoveEdge.ps1

                var scriptFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "RemoveEdge.ps1");

                var cmdCommand = $"powershell.exe -ExecutionPolicy Bypass -File \"{scriptFilePath}\" -UninstallEdge -RemoveEdgeData -NonInteractive";

                var processInfo = new ProcessStartInfo(Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                        ? Path.Combine(Environment.GetEnvironmentVariable("windir"), @"SysNative\cmd.exe")
                        : Path.Combine(Environment.GetEnvironmentVariable("windir"), @"System32\cmd.exe"), $"/c {cmdCommand}")
                {
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                {
                    process.Start();

                    var error = await process.StandardError.ReadToEndAsync();

                    if (!string.IsNullOrEmpty(error))
                    {
                        await LogHelper.LogError(error);
                    }
                }
            }
        }
        else
        {
            try
            {
                // Define the registry paths for installed programs
                var registryKeys = new[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",            // 64-bit on 64-bit systems, 32-bit on 32-bit systems
                    @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall" // 32-bit on 64-bit systems
                };

                string? uninstallString = null;
                foreach (var registryKey in registryKeys)
                {
                    // Open the registry keys from both LocalMachine and CurrentUser
                    using (var keyLocalMachine = Registry.LocalMachine.OpenSubKey(registryKey))
                    using (var keyCurrentUser = Registry.CurrentUser.OpenSubKey(registryKey))
                    {
                        if (keyLocalMachine != null || keyCurrentUser != null)
                        {
                            var subKeyNames = keyLocalMachine?.GetSubKeyNames().Concat(keyCurrentUser?.GetSubKeyNames() ?? Enumerable.Empty<string>()) ?? Enumerable.Empty<string>();

                            // Loop through the combined subkeys
                            foreach (var subKeyName in subKeyNames)
                            {
                                using var subKey = keyLocalMachine?.OpenSubKey(subKeyName) ?? keyCurrentUser?.OpenSubKey(subKeyName);
                                var displayName = subKey?.GetValue("DisplayName")?.ToString();

                                // Check if the display name matches the app name
                                if (!string.IsNullOrEmpty(displayName) && displayName.Equals(appName, StringComparison.OrdinalIgnoreCase))
                                {
                                    uninstallString = subKey?.GetValue("QuietUninstallString") as string;

                                    // If no QuietUninstallString found, try UninstallString
                                    if (string.IsNullOrEmpty(uninstallString))
                                    {
                                        uninstallString = subKey?.GetValue("UninstallString") as string;
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    // If the uninstall string is found, break out of the loop
                    if (!string.IsNullOrEmpty(uninstallString)) break;
                }

                if (string.IsNullOrEmpty(uninstallString))
                {
                    await LogHelper.LogError($"Uninstall string for {appName} not found in registry.");
                }

                // If the uninstall string contains spaces, ensure it's quoted properly
                if (!uninstallString.StartsWith("\"") && !uninstallString.EndsWith("\""))
                {
                    uninstallString = $"\"{uninstallString}\"";
                }

                // Execute the uninstall command using cmd
                var processInfo = new ProcessStartInfo(Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                        ? Path.Combine(Environment.GetEnvironmentVariable("windir"), @"SysNative\cmd.exe")
                        : Path.Combine(Environment.GetEnvironmentVariable("windir"), @"System32\cmd.exe"),
                        $"/c {uninstallString}")
                {
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();

                // Read the error stream asynchronously
                var error = await process.StandardError.ReadToEndAsync();

                if (!string.IsNullOrEmpty(error))
                {
                    await LogHelper.LogError(error);
                }

                if (process.ExitCode != 0)
                {
                    await LogHelper.LogError($"Uninstallation failed with exit code: {process.ExitCode}");
                }

                await LogHelper.Log($"Successfully uninstalled {appName}");
            }
            catch (Exception ex)
            {
                await LogHelper.LogError($"Error uninstalling {appName}: {ex.Message}");
            }
        }
    }

    private void appsFilter_SelectionChanged(object sender, RoutedEventArgs e)
    {
        switch (appsFilter.SelectedIndex)
        {
            case 0:
                LoadInstalledApps(true, false, cancellationTokenSource.Token);
                break;
            case 1:
                // Show warning message when all apps are showing in the NotificationQueue
                App.ShowNotification(
                    RyTuneX.Helpers.ResourceExtensions.GetLocalized("Debloat"),
                    RyTuneX.Helpers.ResourceExtensions.GetLocalized("DebloatPage_NotificationBody"),
                    InfoBarSeverity.Warning, 5000);

                // Show all apps
                LoadInstalledApps(false, false, cancellationTokenSource.Token);
                break;
            case 2:
                LoadInstalledApps(false, true, cancellationTokenSource.Token);
                break;
        }
    }

    private async void TempButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Update UI to show progress
            TempStack.Visibility = Visibility.Visible;
            TempProgress.Visibility = Visibility.Visible;
            TempButtonStack.Visibility = Visibility.Collapsed;
            TempStatusText.Text = RyTuneX.Helpers.ResourceExtensions.GetLocalized("DeligTemp") + "...";

            // Execute temp removal commands
            var result = await OptimizeSystemHelper.RemoveTempFiles();

            // Reset UI after task completion
            TempStack.Visibility = Visibility.Collapsed;
            TempProgress.Visibility = Visibility.Collapsed;
            TempButtonStack.Visibility = Visibility.Visible;

            if (result)
            {
                // Show success notification
                App.ShowNotification(
                    RyTuneX.Helpers.ResourceExtensions.GetLocalized("Debloat"),
                    RyTuneX.Helpers.ResourceExtensions.GetLocalized("TempDelSucc"),
                    InfoBarSeverity.Success, 5000);
            }
            else
            {
                // Show error notification
                App.ShowNotification(
                    RyTuneX.Helpers.ResourceExtensions.GetLocalized("Debloat"),
                    RyTuneX.Helpers.ResourceExtensions.GetLocalized("ErrTempDel"),
                    InfoBarSeverity.Error, 5000);
            }
        }
        catch (Exception)
        {
            // Restore UI in case of unexpected error
            TempStack.Visibility = Visibility.Collapsed;
            TempProgress.Visibility = Visibility.Collapsed;
            TempButtonStack.Visibility = Visibility.Visible;

            // Show error notification
            App.ShowNotification(
                RyTuneX.Helpers.ResourceExtensions.GetLocalized("Debloat"),
                RyTuneX.Helpers.ResourceExtensions.GetLocalized("ErrTempDel"),
                InfoBarSeverity.Error, 5000);
        }
    }

    private void SearchApps(string query)
    {
        noAppFoundText.Visibility = Visibility.Collapsed;

        // Search and sort apps
        var filteredApps = allApps.AsParallel()
                                  .Where(app => app.Item1.Contains(query, StringComparison.CurrentCultureIgnoreCase))
                                  .OrderBy(app => app.Item1)
                                  .ToList();

        // Update the AppList
        AppList.Clear();
        foreach (var app in filteredApps)
        {
            AppList.Add(app);
        }

        // Show "no app found" text if no results
        if (AppList.Count == 0)
        {
            noAppFoundText.Visibility = Visibility.Visible;
        }

        // Update the installed apps count
        installedAppsCount.Text = string.Format(RyTuneX.Helpers.ResourceExtensions.GetLocalized("TotalApps"), AppList.Count);
    }

    private void AppSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            SearchApps(sender.Text.ToLower());
        }
    }

    private void AppSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        SearchApps(args.QueryText.ToLower());
    }

    public async Task<ContentDialogResult> ShowUninstallConfirmationDialog(TreeView appTreeView)
    {
        var selectedItemsText = new StringBuilder();

        foreach (var item in appTreeView.SelectedItems.OfType<Tuple<string, string, bool>>())
        {
            selectedItemsText.AppendLine(item.Item1);
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
            Children = { firstLineTextBlock, scrollViewer, lastLineTextBlock }
        };

        var confirmationDialog = new ContentDialog()
        {
            XamlRoot = XamlRoot,
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