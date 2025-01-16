using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using RyTuneX.Helpers;
using Windows.Storage;

namespace RyTuneX.Views;

public sealed partial class DebloatSystemPage : Page
{
    public ObservableCollection<Tuple<string, string, bool>> AppList { get; set; } = new();
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private List<Tuple<string, string, bool>> allApps = new();

    public DebloatSystemPage()
    {
        InitializeComponent();
        LogHelper.Log("Initializing DebloatSystemPage");
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        LoadInstalledApps();
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

            var filteredApps = installedApps.AsParallel().Where(app =>
                !app.Item1.Contains("Rayen.RyTuneX") &&
                !(app.Item1.Contains("Edge") && IsEdgeUninstalled())).ToList();

            DispatcherQueue.TryEnqueue(() =>
            {
                AppList.Clear();
                allApps = filteredApps;
                foreach (var app in filteredApps)
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
            await LogHelper.LogError($"Operation canceled: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"Error loading installed apps: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
    }

    private bool IsEdgeUninstalled()
    {
        var settingsEdgeUninstalled = ApplicationData.Current.LocalSettings.Values["isEdgeUninstalled"];
        return settingsEdgeUninstalled != null && settingsEdgeUninstalled is bool settingValue && settingValue;
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

            // Reload installed apps after successful uninstallation and keep the list filtered
            appsFilter_SelectionChanged(appsFilter, e);

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

        }
        catch (Exception ex)
        {
            // Log the error
            await LogHelper.LogError($"Error during uninstallation process: {ex.Message}\nStack Trace: {ex.StackTrace}");

            // Show error message in the NotificationQueue
            App.ShowNotification(
                RyTuneX.Helpers.ResourceExtensions.GetLocalized("Debloat"),
                RyTuneX.Helpers.ResourceExtensions.GetLocalized("UnexpectedError"),
                InfoBarSeverity.Error, 5000);
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
            if (!appName.Contains("MicrosoftEdge"))
            {
                var cmdCommand = $"powershell -Command \"Get-AppxPackage -AllUsers | Where-Object {{ $_.Name -eq '{appName}' }} | Remove-AppxPackage\"";

                var processInfo = new ProcessStartInfo(Environment.Is64BitOperatingSystem
                        ? Path.Combine(Environment.GetEnvironmentVariable("windir"), @"SysNative\cmd.exe")
                        : Path.Combine(Environment.GetEnvironmentVariable("windir"), @"System32\cmd.exe"), $"/c {cmdCommand}")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                {
                    process.Start();

                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();

                    if (!string.IsNullOrEmpty(error))
                    {
                        await LogHelper.LogError(error);
                        throw new Exception(error);
                    }
                }
            }
            else
            {
                var scriptFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "RemoveEdge.ps1");

                var cmdCommand = $"powershell.exe -ExecutionPolicy Bypass -File \"{scriptFilePath}\"";

                var processInfo = new ProcessStartInfo(Environment.Is64BitOperatingSystem
                        ? Path.Combine(Environment.GetEnvironmentVariable("windir"), @"SysNative\cmd.exe")
                        : Path.Combine(Environment.GetEnvironmentVariable("windir"), @"System32\cmd.exe"), $"/c {cmdCommand}")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                {
                    process.Start();

                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();

                    ApplicationData.Current.LocalSettings.Values["isEdgeUninstalled"] = true;
                    await OptimizationOptions.StartInCmd("taskkill /F /IM explorer.exe & start explorer");

                    if (!string.IsNullOrEmpty(error))
                    {
                        await LogHelper.LogError(error);
                        throw new Exception(error);
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
                    throw new Exception($"Uninstall string for {appName} not found in registry.");
                }

                // If the uninstall string contains spaces, ensure it's quoted properly
                if (!uninstallString.StartsWith("\"") && !uninstallString.EndsWith("\""))
                {
                    uninstallString = $"\"{uninstallString}\"";
                }

                // Execute the uninstall command using cmd
                var processInfo = new ProcessStartInfo(Environment.Is64BitOperatingSystem
                        ? Path.Combine(Environment.GetEnvironmentVariable("windir"), @"SysNative\cmd.exe")
                        : Path.Combine(Environment.GetEnvironmentVariable("windir"), @"System32\cmd.exe"),
                        $"/c {uninstallString}")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();

                // Read the output and error streams asynchronously
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                if (!string.IsNullOrEmpty(error))
                {
                    await LogHelper.LogError(error);
                    throw new Exception(error);
                }

                if (process.ExitCode != 0)
                {
                    await LogHelper.LogError($"Uninstallation failed with exit code: {process.ExitCode}");
                    throw new Exception($"Uninstallation failed with exit code: {process.ExitCode}");
                }

                await LogHelper.Log($"Successfully uninstalled {appName}");
            }
            catch (Exception ex)
            {
                await LogHelper.LogError($"Error uninstalling {appName}: {ex.Message}");
                throw;
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
    private static CommunityToolkit.WinUI.Behaviors.Notification NotificationContent(string title, string message, InfoBarSeverity severity, int duration)
    {
        var notification = new CommunityToolkit.WinUI.Behaviors.Notification
        {
            Title = title,
            Message = message,
            Severity = severity,
            Duration = TimeSpan.FromMilliseconds(duration)
        };
        LogHelper.Log("Returning Debloat Notification");
        return notification;
    }

    private async void RemoveTempFiles(object sender, RoutedEventArgs e)
    {
        try
        {
            TempStack.Visibility = Visibility.Visible;
            TempProgress.Visibility = Visibility.Visible;
            TempButton.Visibility = Visibility.Collapsed;
            TempStatusText.Text = RyTuneX.Helpers.ResourceExtensions.GetLocalized("DeligTemp") + "...";

            var tempCommands = new[]
            {
                "del /F /S /Q \"C:\\*.tmp\"",
                "rd /S /Q \"%TEMP%\"",
                "del /F /S /Q \"C:\\Windows\\Temp\\*\"",
                "PowerShell.exe -NoProfile -Command \"Clear-RecycleBin -Force\"",
                "PowerShell.exe -NoProfile -Command \"wevtutil cl System\"",
                "PowerShell.exe -NoProfile -Command \"wevtutil cl Application\"",
                "del /F /S /Q \"C:\\Windows\\SoftwareDistribution\\Download\\*\"",
                "del /F /S /Q \"C:\\ProgramData\\Microsoft\\Windows\\WER\\ReportQueue\\*\"",
                "del /F /S /Q \"C:\\Windows\\SoftwareDistribution\\DeliveryOptimization\\*\"",
                "del /F /S /Q \"C:\\Windows\\Prefetch\\*\"",
                "del /F /S /Q \"C:\\Windows\\Logs\\CBS\\*\"",
                "del /F /S /Q \"C:\\Windows\\Temp\\WindowsUpdate.log\"",
                "del /F /S /Q \"C:\\Users\\%USERNAME%\\AppData\\Local\\Temp\\*\"",
                "del /F /S /Q \"C:\\Users\\%USERNAME%\\AppData\\Local\\Microsoft\\Windows\\WER\\ReportArchive\\*\"",
                "del /F /S /Q \"C:\\Users\\%USERNAME%\\AppData\\Local\\Microsoft\\Windows\\INetCache\\*\"",
                "PowerShell.exe -NoProfile -Command \"Remove-Item -Path 'C:\\Users\\%USERNAME%\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Cache\\*' -Recurse -Force\"",
                "PowerShell.exe -NoProfile -Command \"Remove-Item -Path 'C:\\Users\\%USERNAME%\\AppData\\Local\\Microsoft\\Edge\\User Data\\Default\\Cache\\*' -Recurse -Force\"",
                "PowerShell.exe -NoProfile -Command \"Remove-Item -Path 'C:\\Users\\%USERNAME%\\AppData\\Local\\Mozilla\\Firefox\\Profiles\\*\\cache2\\*' -Recurse -Force\"",
                "CLEANMGR /verylowdisk",
            };

            var tempTasks = tempCommands.AsParallel().Select(cmd => OptimizationOptions.StartInCmd(cmd));
            await Task.WhenAll(tempTasks);

            TempStack.Visibility = Visibility.Collapsed;
            TempProgress.Visibility = Visibility.Collapsed;
            TempButton.Visibility = Visibility.Visible;

            // Show success message when temp deletion succeed
            App.ShowNotification(
                RyTuneX.Helpers.ResourceExtensions.GetLocalized("Debloat"),
                RyTuneX.Helpers.ResourceExtensions.GetLocalized("TempDelSucc"),
                InfoBarSeverity.Success, 5000);
        }
        catch (Exception)
        {
            TempStack.Visibility = Visibility.Collapsed;
            TempProgress.Visibility = Visibility.Collapsed;
            TempButton.Visibility = Visibility.Visible;

            // Show error message when temp deletion fail
            App.ShowNotification(
                RyTuneX.Helpers.ResourceExtensions.GetLocalized("Debloat"),
                RyTuneX.Helpers.ResourceExtensions.GetLocalized("ErrTempDel"),
                InfoBarSeverity.Error, 5000);
        }
    }
    private void AppSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            noAppFoundText.Visibility = Visibility.Collapsed;
            var query = sender.Text.ToLower();
            var filteredApps = allApps.AsParallel().Where(app => app.Item1.ToLower().Contains(query)).OrderBy(app => app.Item1).ToList();
            AppList.Clear();
            foreach (var app in filteredApps)
            {
                AppList.Add(app);
            }
        }
        if (AppList.Count == 0)
        {
            noAppFoundText.Visibility = Visibility.Visible;
        }
        installedAppsCount.Text = string.Format(RyTuneX.Helpers.ResourceExtensions.GetLocalized("TotalApps"), AppList.Count);
    }

    private void AppSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        noAppFoundText.Visibility = Visibility.Collapsed;
        var query = args.QueryText.ToLower();
        var filteredApps = allApps.AsParallel().Where(app => app.Item1.ToLower().Contains(query)).OrderBy(app => app.Item1).ToList();
        AppList.Clear();
        foreach (var app in filteredApps)
        {
            AppList.Add(app);
        }
        if (AppList.Count == 0)
        {
            noAppFoundText.Visibility = Visibility.Visible;
        }
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