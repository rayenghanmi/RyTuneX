using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RyTuneX.Helpers;
using Windows.Storage;


namespace RyTuneX.Views;

public sealed partial class DebloatSystemPage : Page
{
    private bool uninstallableOnlyChecked = true;
    public ObservableCollection<KeyValuePair<string, string>> AppList { get; set; } = [];
    private readonly HashSet<string> selectedAppsForUninstall = [];
    private readonly CancellationTokenSource cancellationTokenSource = new();
    public DebloatSystemPage()
    {
        InitializeComponent();
        LogHelper.Log("Initializing DebloatSystemPage");
        LoadInstalledApps();
        Unloaded += DebloatSystemPage_Unloaded;
    }
    private void DebloatSystemPage_Unloaded(object sender, RoutedEventArgs e)
    {
        // Stop any background task when exiting
        LogHelper.Log("Unloading DebloatSystemPage's Tasks");
        selectedAppsForUninstall.Clear();
        cancellationTokenSource.Cancel();
    }
    private void AppTreeView_DragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs args)
    {
        args.Cancel = true;
    }

    private async void LoadInstalledApps(bool uninstallableOnly = true, CancellationToken cancellationToken = default)
    {
        try
        {
            await LogHelper.Log("Loading InstalledApps");

            // Check for cancellation request
            cancellationToken.ThrowIfCancellationRequested();

            var installedApps = await Task.Run(() => OptimizationOptions.GetUWPApps(uninstallableOnly), cancellationToken);
            var numberOfInstalledApps = installedApps.Count - 3; // removes Rayen.RyTuneX from total installed apps count
            DispatcherQueue.TryEnqueue(() =>
            {
                // fetching installed apps data & hiding UI elements
                AppList.Clear();
                gettingAppsLoading.Visibility = Visibility.Visible;
                appTreeView.Visibility = Visibility.Collapsed;
                appTreeView.IsEnabled = false;
                uninstallButton.IsEnabled = false;
                installedAppsCount.Visibility = Visibility.Collapsed;
                uninstallingStatusText.Text = "UninstallTip".GetLocalized();
                uninstallingStatusBar.Visibility = Visibility.Collapsed;
                showAll.Visibility = Visibility.Collapsed;
                uninstallButton.Visibility = Visibility.Collapsed;
                TempStackButtonTextBar.Visibility = Visibility.Collapsed;

                var isEdgeUninstalled = false;
                var settingsEdgeUninstalled = ApplicationData.Current.LocalSettings.Values["isEdgeUninstalled"];

                if (settingsEdgeUninstalled != null && settingsEdgeUninstalled is bool settingValue)
                {
                    isEdgeUninstalled = settingValue;
                }

                foreach (var app in installedApps)
                {
                    // prevent displaying Rayen.RyTuneX in AppList
                    if (app.ToString().Contains("Rayen.RyTuneX") ||
                    app.ToString().Contains("----") ||
                    app.ToString().Contains("Name") ||
                    (app.ToString().Contains("Edge") && isEdgeUninstalled))
                    {
                        Debug.WriteLine(app.ToString());
                    }
                    else
                    {
                        AppList.Add(app);
                    }
                }

                // showing the installed apps data after fetching
                installedAppsCount.Text = $"Total: {numberOfInstalledApps} Apps";
                installedAppsCount.Visibility = Visibility.Visible;
                showAll.IsEnabled = true;
                showAll.Visibility = Visibility.Visible;
                uninstallButton.Visibility = Visibility.Visible;
                appTreeView.Visibility = Visibility.Visible;
                appTreeView.IsEnabled = true;
                uninstallButton.IsEnabled = true;
                gettingAppsLoading.Visibility = Visibility.Collapsed;
                uninstallingStatusBar.ShowError = false;
                TempStackButtonTextBar.Visibility = Visibility.Visible;
            });
        }
        catch (OperationCanceledException ex)
        {
            await LogHelper.LogError(ex.ToString());
        }
    }

    private async void UninstallSelectedApp_Click(object sender, RoutedEventArgs e)
    {
        // Check if at least one item is selected
        if (appTreeView.SelectedItems.Count == 0)
        {
            return;
        }

        uninstallButton.IsEnabled = false;
        appTreeView.IsEnabled = false;
        uninstallingStatusBar.Visibility = Visibility.Visible;

        // List to store names of apps that failed to uninstall
        var failedUninstalls = new List<string>();

        try
        {
            foreach (var selectedApp in appTreeView.SelectedItems)
            {
                if (selectedApp is KeyValuePair<string, string> appInfo)
                {
                    var selectedAppName = appInfo.Key;

                    // Check if the app is already selected for uninstallation
                    if (!selectedAppsForUninstall.Contains(selectedAppName))
                    {
                        var selectedAppInfo = appInfo;
                        uninstallingStatusText.Text = "Uninstalling".GetLocalized() + " " + selectedAppInfo.Key.ToString();

                        try
                        {
                            await UninstallApps(selectedAppName);

                            // Add the app to the HashSet to mark it as selected for uninstallation
                            selectedAppsForUninstall.Add(selectedAppName);
                        }
                        catch (Exception)
                        {
                            // Add to the list of failed uninstalls
                            failedUninstalls.Add(selectedAppName);
                        }
                    }
                }
            }

            appTreeView.SelectedItems.Clear();
            // Reload the installed apps after successfull uninstall
            await LogHelper.Log("Reloading Installed Apps Data");
            if (uninstallableOnlyChecked)
            {
                LoadInstalledApps();
            }
            else
            {
                LoadInstalledApps(false);
            }

            // Show success notification if no errors, otherwise show the error notification
            uninstallingStatusBar.Visibility = Visibility.Collapsed;
            if (failedUninstalls.Count == 0)
            {
                NotificationQueue.Show(NotificationContent("Debloat", "UninstallationSuccess".GetLocalized(), InfoBarSeverity.Success, 5000));
            }
            else
            {
                var failedAppsMessage = string.Join("\n", failedUninstalls);
                NotificationQueue.Show(NotificationContent("Debloat", $"Error uninstalling the following app(s):\n{failedAppsMessage}", InfoBarSeverity.Error, 5000));
            }
        }
        catch (Exception ex)
        {
            uninstallingStatusBar.ShowError = true;
            NotificationQueue.Show(NotificationContent("Debloat".GetLocalized(), ex.ToString(), InfoBarSeverity.Error, 5000));

            if (uninstallableOnlyChecked)
            {
                LoadInstalledApps();
            }
            else
            {
                LoadInstalledApps(false);
            }
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
                await LogHelper.LogError(error);
            }
        }
    }

    private void ShowAll_Checked(object sender, RoutedEventArgs e)
    {
        // Show uninstallable apps only
        LogHelper.Log("Reloading Installed Apps Data (All)");
        DispatcherQueue.TryEnqueue(() =>
        {
            showAll.IsEnabled = false;
            gettingAppsLoading.Visibility = Visibility.Visible;
            appTreeView.Visibility = Visibility.Collapsed;
            appTreeView.IsEnabled = false;
            uninstallButton.IsEnabled = false;
            NotificationQueue.Show(NotificationContent("Debloat".GetLocalized(), "DebloatPage_NotificationBody".GetLocalized(), InfoBarSeverity.Warning, 4000));
        });
        uninstallableOnlyChecked = false;
        LoadInstalledApps(uninstallableOnly: false, cancellationTokenSource.Token);
    }
    private void ShowAll_Unchecked(object sender, RoutedEventArgs e)
    {
        // Show all apps
        LogHelper.Log("Reloading Installed Apps Data (Uninstallable Only)");
        DispatcherQueue.TryEnqueue(() =>
        {
            showAll.IsEnabled = false;
            gettingAppsLoading.Visibility = Visibility.Visible;
            appTreeView.Visibility = Visibility.Collapsed;
            appTreeView.IsEnabled = false;
            uninstallButton.IsEnabled = false;
        });
        uninstallableOnlyChecked = true;
        LoadInstalledApps(uninstallableOnly: true, cancellationTokenSource.Token);
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

            await OptimizationOptions.StartInCmd("del /F /S /Q \"C:\\*.tmp\"");
            await OptimizationOptions.StartInCmd("rd /S /Q \"%TEMP%\"");
            await OptimizationOptions.StartInCmd("del /F /S /Q \"C:\\Windows\\Temp\\*\"");
            await OptimizationOptions.StartInCmd("PowerShell.exe -NoProfile -Command \"Clear-RecycleBin -Force\"");
            await OptimizationOptions.StartInCmd("PowerShell.exe -NoProfile -Command \"wevtutil cl System\"");
            await OptimizationOptions.StartInCmd("PowerShell.exe -NoProfile -Command \"wevtutil cl Application\"");
            await OptimizationOptions.StartInCmd("del /F /S /Q \"C:\\Windows\\SoftwareDistribution\\Download\\*\"");
            await OptimizationOptions.StartInCmd("del /F /S /Q \"C:\\ProgramData\\Microsoft\\Windows\\WER\\ReportQueue\\*\"");
            await OptimizationOptions.StartInCmd("del /F /S /Q \"C:\\Windows\\SoftwareDistribution\\DeliveryOptimization\\*\"");


            TempStatusText.Text = "TempDelSucc".GetLocalized();
            TempProgress.Visibility = Visibility.Collapsed;
        }
        catch (Exception)
        {
            TempStatusText.Text = "ErrTempDel".GetLocalized();
            TempButton.Visibility = Visibility.Visible;
            TempProgress.ShowError = true;
        }
    }
}