using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Management.Automation;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using RyTuneX.Helpers;
using Windows.ApplicationModel;
using Windows.UI.Core;
using Windows.UI.Notifications;
using CommunityToolkit.WinUI.Behaviors;
using System.Threading;
using Windows.ApplicationModel.Core;

namespace RyTuneX.Views;

public sealed partial class DebloatSystemPage : Page
{
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
    private void appTreeView_DragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs args)
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
            var numberOfInstalledApps = installedApps.Count;

            DispatcherQueue.TryEnqueue(() =>
            {
                // fetching installed apps data & hiding UI elements
                AppList.Clear();
                gettingAppsLoading.Visibility = Visibility.Visible;
                appTreeView.Visibility = Visibility.Collapsed;
                appTreeView.IsEnabled = false;
                uninstallButton.IsEnabled = false;
                installedAppsCount.Visibility = Visibility.Collapsed;
                uninstallingStatusText.Foreground = new SolidColorBrush(Colors.White);
                uninstallingStatusText.Text = "UninstallTip".GetLocalized();
                uninstallingStatusBar.Visibility = Visibility.Collapsed;
                showAll.Visibility = Visibility.Collapsed;
                uninstallButton.Visibility = Visibility.Collapsed;

                foreach (var app in installedApps)
                {
                    AppList.Add(app);
                }

                // showing the installed apps data after fetching
                installedAppsCount.Text = $"Total: {numberOfInstalledApps} Apps";
                installedAppsCount.Visibility = Visibility.Visible;
                showAll.Visibility = Visibility.Visible;
                uninstallButton.Visibility = Visibility.Visible;
                appTreeView.Visibility = Visibility.Visible;
                appTreeView.IsEnabled = true;
                uninstallButton.IsEnabled = true;
                gettingAppsLoading.Visibility = Visibility.Collapsed;
                uninstallingStatusBar.ShowError = false;
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
            // Reload the installed apps after successfull uninstall
            await LogHelper.Log("Reloading Installed Apps Data");
            LoadInstalledApps();
            // update ui elements
            uninstallingStatusBar.Visibility = Visibility.Collapsed;
            if (appTreeView.SelectedItems.Count > 1)
            {
                NotificationQueue.Show(NotificationContent("Debloat", $"{appTreeView.SelectedItems.Count} Apps uninstalled successfully", InfoBarSeverity.Success, 4000));
            }
            else
            {
                NotificationQueue.Show(NotificationContent("Debloat", "App uninstalled successfully", InfoBarSeverity.Success, 4000));
            }
            
        }
        // in case of an error
        catch (Exception ex)
        {
            await LogHelper.Log("Error Uninstalling");
            await LogHelper.LogError($"Error Uninstalling: {ex}");
            // update ui elements
            uninstallingStatusText.Text = $"Error: {ex}";
            uninstallingStatusText.Foreground = new SolidColorBrush(Colors.Crimson);
            uninstallingStatusBar.ShowError = true;
            NotificationQueue.Show(NotificationContent("Debloat", "Error uninstalling", InfoBarSeverity.Error, 5000));
            // reload
            LoadInstalledApps();
        }
    }
    private static async Task UninstallApps(string appName)
    {
        await LogHelper.Log($"Uninstalling: {appName}");
        using var script = PowerShell.Create();
        script.AddScript("Import-Module -SkipEditionCheck");
        script.AddScript($"Get-AppxPackage -AllUsers {appName} | Remove-AppxPackage");

        await script.InvokeAsync();
    }
    private void ShowAll_Checked(object sender, RoutedEventArgs e)
    {
        // Show uninstallable apps only
        LogHelper.Log("Reloading Installed Apps Data (All)");
        DispatcherQueue.TryEnqueue(() =>
        {
            gettingAppsLoading.Visibility = Visibility.Visible;
            appTreeView.Visibility = Visibility.Collapsed;
            appTreeView.IsEnabled = false;
            uninstallButton.IsEnabled = false;
            NotificationQueue.Show(NotificationContent("Debloat", "Uninstalling some apps may break your system!", InfoBarSeverity.Warning, 4000));
        });
        LoadInstalledApps(uninstallableOnly: false, cancellationTokenSource.Token);
    }
    private void ShowAll_Unchecked(object sender, RoutedEventArgs e)
    {
        // Show all apps
        LogHelper.Log("Reloading Installed Apps Data (Uninstallable Only)");
        DispatcherQueue.TryEnqueue(() =>
        {
            gettingAppsLoading.Visibility = Visibility.Visible;
            appTreeView.Visibility = Visibility.Collapsed;
            appTreeView.IsEnabled = false;
            uninstallButton.IsEnabled = false;
        });
        LoadInstalledApps(uninstallableOnly: true, cancellationTokenSource.Token);
    }
    private CommunityToolkit.WinUI.Behaviors.Notification NotificationContent(string title, string message, InfoBarSeverity severity, int duration)
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
}