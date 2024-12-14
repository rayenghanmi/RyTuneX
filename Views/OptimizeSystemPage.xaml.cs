using System.Diagnostics;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using RyTuneX.Helpers;
using Windows.Storage;

namespace RyTuneX.Views;

public sealed partial class OptimizeSystemPage : Page
{
    private bool isInitialLoad = true;

    public OptimizeSystemPage()
    {
        InitializeComponent();
        LogHelper.Log("Initializing OptimizeSystemPage");
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        Loaded += (sender, e) => InitializeToggleSwitchesAsync();
    }
    private async void InitializeToggleSwitchesAsync()
    {
        await LogHelper.Log("Initializing Toggle Switches");
        try
        {
            var tasks = FindVisualChildren<ToggleSwitch>(this).Select(async control =>
            {
                if (control.Tag != null && control.Tag is string tagName)
                {
                    // Set the initial state based on the stored value in LocalSettings
                    var settingValueObj = ApplicationData.Current.LocalSettings.Values[tagName];

                    if (settingValueObj != null && settingValueObj is bool settingValue)
                    {
                        control.IsOn = settingValue;
                    }
                }
            });
            await Task.WhenAll(tasks);
            isInitialLoad = false;
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"Error initializing toggle switches: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
    }
    // Helper method to find all children of a specific type in the visual tree
    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
    {
        if (depObj != null)
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T typedChild)
                {
                    yield return typedChild;
                }
                if (child is SettingsCard settingsCard)
                {
                    foreach (var childOfSettingsCard in FindVisualChildren<T>(settingsCard))
                    {
                        yield return childOfSettingsCard;
                    }
                }
                else
                {
                    foreach (var childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }
    private async void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        try
        {
            var toggleSwitch = (ToggleSwitch)sender;
            Debug.WriteLine($"ToggleSwitch Tag: {toggleSwitch.Tag}, IsOn: {toggleSwitch.IsOn}");
            await LogHelper.Log($"ToggleSwitch Toggled: Tag={toggleSwitch.Tag}, IsOn={toggleSwitch.IsOn}");
            if (toggleSwitch.IsOn)
            {
                ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
            }
            else
            {
                ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
            }
            if (isInitialLoad)
            {
                OptimizationOptions.XamlSwitches(toggleSwitch);
            }
            else
            {
                OptimizationOptions.XamlSwitches(toggleSwitch, false);
            }
        }
        catch (Exception ex)
        {
            await LogHelper.ShowErrorMessageAndLog(ex, XamlRoot);
        }
    }
}