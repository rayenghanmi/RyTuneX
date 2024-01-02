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

namespace RyTuneX.Views;

public sealed partial class DebloatSystemPage : Page
{
    public ObservableCollection<KeyValuePair<string, string>> AppList { get; set; } = [];

    public DebloatSystemPage()
    {
        InitializeComponent();
        LoadInstalledApps();
    }

    private void appTreeView_DragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs args)
    {
        args.Cancel = true;
    }

    private async void LoadInstalledApps(bool uninstallableOnly = true)
    {
        await Task.Run(() =>
        {

            var installedApps = OptimizationOptions.GetUWPApps(uninstallableOnly);
            var numberOfInstalledApps = installedApps.Count;

            DispatcherQueue.TryEnqueue(() =>
            {
                // fetching installed apps data & hiding ui elements
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
        });
    }
    private async void UninstallSelectedApp_Click(object sender, RoutedEventArgs e)
    {
        uninstallButton.IsEnabled = false;
        appTreeView.IsEnabled = false;
        uninstallingStatusBar.Visibility = Visibility.Visible;
        try
        {
            foreach (var selectedApp in appTreeView.SelectedItems)
            {

                if (selectedApp is KeyValuePair<string, string> appInfo)
                {
                    var selectedAppInfo = appInfo;
                    uninstallingStatusText.Text = "Uninstalling".GetLocalized() + selectedAppInfo.Key.ToString();
                    await UninstallApps(appInfo.Key);
                }
            }
            // Reload the installed apps after successfull uninstall
            LoadInstalledApps();
            // update ui elements
            uninstallingStatusText.Text = "Done".GetLocalized();
            uninstallingStatusText.Foreground = new SolidColorBrush(Colors.LightGreen);
            uninstallingStatusBar.Visibility = Visibility.Collapsed;
        }
        // in case of an error
        catch (Exception ex)
        {
            // update ui elements
            uninstallingStatusText.Text = $"Error: {ex}";
            uninstallingStatusText.Foreground = new SolidColorBrush(Colors.Crimson);
            uninstallingStatusBar.ShowError = true;
            // reload
            LoadInstalledApps();
        }
    }
    private static async Task UninstallApps(string appName)
    {
        using var script = PowerShell.Create();
        script.AddScript("Import-Module -SkipEditionCheck");
        script.AddScript($"Get-AppxPackage -AllUsers {appName} | Remove-AppxPackage");

        await script.InvokeAsync();
    }
    private void ShowAll_Checked(object sender, RoutedEventArgs e)
    {
        // Show uninstallable apps only
        DispatcherQueue.TryEnqueue(() =>
        {
            gettingAppsLoading.Visibility = Visibility.Visible;
            appTreeView.Visibility = Visibility.Collapsed;
            appTreeView.IsEnabled = false;
            uninstallButton.IsEnabled = false;
        });
        LoadInstalledApps(uninstallableOnly: false);
    }
    private void ShowAll_Unchecked(object sender, RoutedEventArgs e)
    {
        // Show all apps
        DispatcherQueue.TryEnqueue(() =>
        {
            gettingAppsLoading.Visibility = Visibility.Visible;
            appTreeView.Visibility = Visibility.Collapsed;
            appTreeView.IsEnabled = false;
            uninstallButton.IsEnabled = false;
        });
        LoadInstalledApps(uninstallableOnly: true);
    }
}