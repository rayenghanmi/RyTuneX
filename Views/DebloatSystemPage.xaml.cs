using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management.Automation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
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
            var numberOfInstalledApps = installedApps.Count - 1; // removes Rayen.RyTuneX from total installed apps count
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

                foreach (var app in installedApps)
                {
                    // prevent displaying Rayen.RyTuneX in AppList
                    if (!app.ToString().Contains("Rayen.RyTuneX"))
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

                        await UninstallApps(selectedAppName);

                        // Add the app to the HashSet to mark it as selected for uninstallation
                        selectedAppsForUninstall.Add(selectedAppName);
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

            // update ui elements
            uninstallingStatusBar.Visibility = Visibility.Collapsed;
            NotificationQueue.Show(NotificationContent("Debloat", "UninstallationSuccess".GetLocalized(), InfoBarSeverity.Success, 4000));
        }
        // in case of an error
        catch (Exception ex)
        {
            // update ui elements
            uninstallingStatusText.Text = ex.ToString();
            uninstallingStatusBar.ShowError = true;
            NotificationQueue.Show(NotificationContent("Debloat".GetLocalized(), "UninstallationError".GetLocalized(), InfoBarSeverity.Error, 5000));

            var uninstallationFailed = new ContentDialog()
            {
                XamlRoot = XamlRoot,
                Title = "UninstallationError".GetLocalized(),
                Content = ex.Message,
                CloseButtonText = "View logs"
            };
            await uninstallationFailed.ShowAsync();
            var tempFolder = ApplicationData.Current.TemporaryFolder;
            var file = await tempFolder.GetFileAsync($"Logs_{DateTime.Now:yyyy-MM-dd}.txt");

            // Use Process.Start to open the file with the default application
            Process.Start(new ProcessStartInfo(file.Path) { UseShellExecute = true });

            // reload after error

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
        /*if (!appName.Contains("Microsoft.MicrosoftEdge"))
        {*/
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
            
        // Edge removal (will be added soon)

        /*}
        else
        {
            var scriptFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "RemoveEdge.ps1");

            var scriptContent = File.ReadAllText(scriptFilePath);

            await LogHelper.Log($"Uninstalling: {appName}");

            using var PowerShellInstance = PowerShell.Create();
            PowerShellInstance.AddScript(scriptContent)
                .AddArgument("-Set-ExecutionPolicy Unrestricted");

            var result = PowerShellInstance.InvokeAsync();
            if (PowerShellInstance.HadErrors)
            {
                var errorMessage = string.Join(Environment.NewLine, PowerShellInstance.Streams.Error.Select(err => err.ToString()));
                await LogHelper.LogError(errorMessage);
                throw new Exception(errorMessage);
            }
        }*/
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

            var exitCode1 = await OptimizationOptions.StartInCmd("del /F /S /Q \"C:\\*.tmp\"");
            var exitCode2 = await OptimizationOptions.StartInCmd("del /q/f/s %TEMP%\\*");

            // Check for successful execution
            if (exitCode1 == 0 && exitCode2 == 0)
            {
                TempStatusText.Text = "TempDelSucc".GetLocalized();
                TempProgress.Visibility = Visibility.Collapsed;
            }
            else
            {
                TempStatusText.Text = "ErrTempDel".GetLocalized();
                TempProgress.ShowError = true;
            }
        }
        catch (Exception ex)
        {
            TempStatusText.Text = "Error: " + ex.Message;
            TempButton.Visibility = Visibility.Visible;
            TempProgress.Visibility = Visibility.Collapsed;
            TempProgress.ShowError = true;
        }
    }
    private void TextBlock_Loaded(object sender, RoutedEventArgs e)
    {
        var textBlock = sender as TextBlock;
        if (textBlock != null)
        {
            var storyboard = new Storyboard();
            var fadeInAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromSeconds(0.5))
            };
            Storyboard.SetTarget(fadeInAnimation, textBlock);
            Storyboard.SetTargetProperty(fadeInAnimation, "Opacity");
            storyboard.Children.Add(fadeInAnimation);
            storyboard.Begin();
        }
    }

}