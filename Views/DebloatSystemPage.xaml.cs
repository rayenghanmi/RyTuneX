using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RyTuneX.Helpers;
using Windows.Storage;

namespace RyTuneX.Views;

public sealed partial class DebloatSystemPage : Page
{
    private bool uninstallableOnly = true;
    public ObservableCollection<KeyValuePair<string, string>> AppList { get; set; } = new();
    private readonly HashSet<string> selectedAppsForUninstall = new();
    private List<KeyValuePair<string, string>> allApps = new();
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

    private async void LoadInstalledApps(bool uninstallableOnly = true)
    {
        try
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                // hiding UI elements
                gettingAppsLoading.Visibility = Visibility.Visible;
                appTreeView.Visibility = Visibility.Collapsed;
                uninstallButton.IsEnabled = false;
                uninstallingStatusText.Text = "UninstallTip".GetLocalized();
                uninstallingStatusBar.Opacity = 0;
                showAll.IsEnabled = false;
            });

            await LogHelper.Log("Loading InstalledApps");

            // fetching installed apps data
            var installedApps = await Task.Run(() => OptimizationOptions.GetUWPApps(uninstallableOnly));
            var numberOfInstalledApps = installedApps.Count - 3;

            var filteredApps = installedApps.Where(app =>
                !app.ToString().Contains("Rayen.RyTuneX") &&
                !app.ToString().Contains("----") &&
                !app.ToString().Contains("Name") &&
                !(app.ToString().Contains("Edge") && IsEdgeUninstalled())).ToList();

            DispatcherQueue.TryEnqueue(() =>
            {
                // Clear previous data
                AppList.Clear();
                allApps = filteredApps;
                foreach (var app in filteredApps)
                {
                    AppList.Add(app);
                }

                installedAppsCount.Text = string.Format("TotalApps".GetLocalized(), numberOfInstalledApps);
                installedAppsCount.Visibility = Visibility.Visible;
                showAll.IsEnabled = true;
                showAll.Visibility = Visibility.Visible;
                uninstallButton.Visibility = Visibility.Visible;
                appTreeView.Visibility = Visibility.Visible;
                appTreeView.IsEnabled = true;
                uninstallButton.IsEnabled = true;
                gettingAppsLoading.Visibility = Visibility.Collapsed;
                TempStackButtonTextBar.Visibility = Visibility.Visible;
            });
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
        // Check if at least one item is selected
        if (appTreeView.SelectedItems.Count == 0)
        {
            return;
        }

        uninstallButton.IsEnabled = false;
        showAll.IsEnabled = false;
        appTreeView.IsEnabled = false;

        // List to store names of apps that failed or succeeded to uninstall
        var failedUninstalls = new List<string>();
        var successfulUninstalls = new List<string>();

        try
        {
            var totalApps = appTreeView.SelectedItems.Count;
            var completedApps = 0;

            // Initialize status bar
            uninstallingStatusBar.Value = 0;
            uninstallingStatusBar.Maximum = totalApps;
            uninstallingStatusBar.Opacity = 1;

            foreach (var appInfo in appTreeView.SelectedItems.OfType<KeyValuePair<string, string>>())
            {
                var selectedAppName = appInfo.Key;
                DispatcherQueue.TryEnqueue(() =>
                {
                    uninstallingStatusText.Text = "Uninstalling".GetLocalized() + " " + selectedAppName;
                });

                try
                {
                    await UninstallApps(selectedAppName);
                    successfulUninstalls.Add(selectedAppName);
                    selectedAppsForUninstall.Add(selectedAppName);
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

            uninstallingStatusText.Text = "UninstallTip".GetLocalized();

            appTreeView.SelectedItems.Clear();
            // Reload the installed apps after successful uninstall
            await LogHelper.Log("Reloading Installed Apps Data");
            LoadInstalledApps(uninstallableOnly);

            // Show notifications for succeeded apps
            if (successfulUninstalls.Count > 0)
            {
                var successfulAppsMessage = string.Join("\n", successfulUninstalls);
                NotificationQueue.Show(NotificationContent("Debloat".GetLocalized(),
                    "UninstallationSuccess".GetLocalized() + $":\n{successfulAppsMessage}",
                    InfoBarSeverity.Success, 5000));
            }

            // Show notifications for failed apps
            if (failedUninstalls.Count > 0)
            {
                var failedAppsMessage = string.Join("\n", failedUninstalls);
                NotificationQueue.Show(NotificationContent("Debloat".GetLocalized(),
                    "UninstallationError".GetLocalized() + $":\n{failedAppsMessage}",
                    InfoBarSeverity.Error, 5000));
            }
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"Error during uninstallation process: {ex.Message}\nStack Trace: {ex.StackTrace}");
            uninstallingStatusBar.ShowError = true;
            NotificationQueue.Show(NotificationContent("Debloat".GetLocalized(), "UnexpectedError".GetLocalized(), InfoBarSeverity.Error, 5000));
            LoadInstalledApps(uninstallableOnly);
        }
    }

    private static async Task UninstallApps(string appName)
    {
        if (!appName.Contains("MicrosoftEdge"))
        {
            // uwp apps removal
            await LogHelper.Log($"Uninstalling: {appName}");

            var cmdCommand = $"powershell -Command \"Get-AppxPackage -AllUsers | Where-Object {{ $_.Name -eq '{appName}' }} | Remove-AppxPackage\"";

            var processInfo = new ProcessStartInfo("cmd.exe", $"/c {cmdCommand}")
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

                await LogHelper.Log(output);

                if (!string.IsNullOrEmpty(error))
                {
                    await LogHelper.LogError(error);
                    throw new Exception(error);
                }
            }
        }
        else
        {
            // edge removal process
            var scriptFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "RemoveEdge.ps1");

            await LogHelper.Log($"Uninstalling: {appName}");

            var cmdCommand = $"powershell.exe -ExecutionPolicy Bypass -File \"{scriptFilePath}\"";

            var processInfo = new ProcessStartInfo("cmd.exe", $"/c {cmdCommand}")
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

                // setting edge installation state to uninstalled
                ApplicationData.Current.LocalSettings.Values["isEdgeUninstalled"] = true;

                // starting explorer
                OptimizationOptions.RestartExplorer();

                // writing output log
                await LogHelper.Log(output);
                if (!string.IsNullOrEmpty(error))
                {
                    await LogHelper.LogError(error);
                    throw new Exception(error);
                }
            }
        }
    }

    private void ShowAll_Checked(object sender, RoutedEventArgs e)
    {
        uninstallableOnly = false;
        LoadInstalledApps(uninstallableOnly);
    }
    private void ShowAll_Unchecked(object sender, RoutedEventArgs e)
    {
        uninstallableOnly = true;
        LoadInstalledApps(uninstallableOnly);
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
            TempStatusText.Text = "DeligTemp".GetLocalized() + "...";

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

            var tempTasks = tempCommands.Select(cmd => OptimizationOptions.StartInCmd(cmd));
            await Task.WhenAll(tempTasks);

            TempStatusText.Text = "TempDelSucc".GetLocalized();
            TempProgress.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"Error removing temporary files: {ex.Message}\nStack Trace: {ex.StackTrace}");
            TempStatusText.Text = "ErrTempDel".GetLocalized();
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
            var filteredApps = allApps.Where(app => app.Key.ToLower().Contains(query)).OrderBy(app => app.Key).ToList();
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
        var filteredApps = allApps.Where(app => app.Key.ToLower().Contains(query)).OrderBy(app => app.Key).ToList();
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
}
