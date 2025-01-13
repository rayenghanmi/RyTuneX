using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Win32;
using RyTuneX.Helpers;
using Windows.Storage;

namespace RyTuneX.Views;

public sealed partial class DebloatSystemPage : Page
{
    private bool uninstallableOnly = true;
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

    private async void LoadInstalledApps(bool uninstallableOnly = true, CancellationToken cancellationToken = default)
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

            var installedApps = await Task.Run(() => OptimizationOptions.GetInstalledApps(uninstallableOnly));
            var numberOfInstalledApps = installedApps.Count - 3;

            var filteredApps = installedApps.AsParallel().Where(app =>
                !app.Item1.Contains("Rayen.RyTuneX") &&
                !app.Item1.Contains("----") &&
                !app.Item1.Contains("Name") &&
                !(app.Item1.Contains("Edge") && IsEdgeUninstalled())).ToList();

            DispatcherQueue.TryEnqueue(() =>
            {
                AppList.Clear();
                allApps = filteredApps;
                foreach (var app in filteredApps)
                {
                    AppList.Add(app);
                }

                installedAppsCount.Text = string.Format(RyTuneX.Helpers.ResourceExtensions.GetLocalized("TotalApps"), numberOfInstalledApps);
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

            // Reload installed apps after successful uninstallation
            LoadInstalledApps(uninstallableOnly);

            // Show notifications
            if (successfulUninstalls.Count > 0)
            {
                var successMessage = string.Join("\n", successfulUninstalls);
                // Show success message with animation
                NotificationQueue.Show(NotificationContent(
                    RyTuneX.Helpers.ResourceExtensions.GetLocalized("Debloat"),
                    RyTuneX.Helpers.ResourceExtensions.GetLocalized("UninstallationSuccess") + $":\n{successMessage}",
                    InfoBarSeverity.Success, 5000));

                // Trigger animation for showing the notification
                var showStoryboard = (Storyboard)infoBar.Resources["ShowNotificationStoryboard"];
                showStoryboard.Begin();
            }

            if (failedUninstalls.Count > 0)
            {
                var errorMessage = string.Join("\n", failedUninstalls);
                // Show error message with animation
                NotificationQueue.Show(NotificationContent(
                    RyTuneX.Helpers.ResourceExtensions.GetLocalized("Debloat"),
                    RyTuneX.Helpers.ResourceExtensions.GetLocalized("UninstallationError") + $":\n{errorMessage}",
                    InfoBarSeverity.Error, 5000));

                // Trigger animation for showing the notification
                var showStoryboard = (Storyboard)infoBar.Resources["ShowNotificationStoryboard"];
                showStoryboard.Begin();
            }

        }
        catch (Exception ex)
        {
            // Log the error
            await LogHelper.LogError($"Error during uninstallation process: {ex.Message}\nStack Trace: {ex.StackTrace}");

            // Show error message in the NotificationQueue
            NotificationQueue.Show(NotificationContent(
                RyTuneX.Helpers.ResourceExtensions.GetLocalized("Debloat"),
                RyTuneX.Helpers.ResourceExtensions.GetLocalized("UnexpectedError"),
                InfoBarSeverity.Error, 5000));

            // Trigger the animation for showing the error notification
            var showStoryboard = (Storyboard)infoBar.Resources["ShowNotificationStoryboard"];
            showStoryboard.Begin();
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
                // Define the registry key to query for installed programs
                var registryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

                // Query the registry for the uninstall string
                string? uninstallString = null;

                using (var registryKeyObj = Registry.LocalMachine.OpenSubKey(registryKey))
                {
                    if (registryKeyObj != null)
                    {
                        // Loop through subkeys to find the app
                        foreach (var subKeyName in registryKeyObj.GetSubKeyNames())
                        {
                            using var subKey = registryKeyObj.OpenSubKey(subKeyName);
                            if (subKey?.GetValue("DisplayName") is string displayName && displayName.Equals(appName, StringComparison.OrdinalIgnoreCase))
                            {
                                uninstallString = subKey.GetValue("UninstallString") as string;
                                break;
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(uninstallString))
                {
                    throw new Exception($"Uninstall string for {appName} not found in registry.");
                }

                // Start the process to run the uninstallation command
                var processInfo = new ProcessStartInfo(uninstallString)
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
                uninstallableOnly = true;
                LoadInstalledApps(uninstallableOnly, cancellationTokenSource.Token);
                break;
            case 1:
                uninstallableOnly = false;
                LoadInstalledApps(uninstallableOnly, cancellationTokenSource.Token);
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
            TempProgress.ShowError = false;
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
                "del /F /S /Q \"C:\\Windows\\SoftwareDistribution\\DeliveryOptimization\\*\""
            };

            var tempTasks = tempCommands.AsParallel().Select(cmd => OptimizationOptions.StartInCmd(cmd));
            await Task.WhenAll(tempTasks);

            TempStatusText.Text = RyTuneX.Helpers.ResourceExtensions.GetLocalized("TempDelSucc");
            TempProgress.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"Error removing temporary files: {ex.Message}\nStack Trace: {ex.StackTrace}");
            TempStatusText.Text = RyTuneX.Helpers.ResourceExtensions.GetLocalized("ErrTempDel");
            TempButton.Visibility = Visibility.Visible;
            TempProgress.ShowError = true;
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
