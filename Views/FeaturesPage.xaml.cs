using Microsoft.UI.Xaml;
using System.Diagnostics;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Microsoft.UI.Xaml.Media;
using RyTuneX.Helpers;
using Windows.UI.Popups;
using Json.Schema;
using System;
using CommunityToolkit.WinUI.Controls;

namespace RyTuneX.Views;

public sealed partial class FeaturesPage : Page
{

    private readonly bool isInitialSetup = true;

    public FeaturesPage()
    {
        InitializeComponent();
        LogHelper.Log("Initializing FeaturesPage");
        Loaded += (sender, e) => InitializeToggleSwitches();
        isInitialSetup = false;
    }
    private void InitializeToggleSwitches()
    {
        LogHelper.Log("Initializing Toggle Switches");
        foreach (var control in FindVisualChildren<ToggleSwitch>(this))
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
    private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!isInitialSetup)
            {
                var toggleSwitch = (ToggleSwitch)sender;
                LogHelper.Log($"ToggleSwitch Tag: {toggleSwitch.Tag}, IsOn: {toggleSwitch.IsOn}");
                if (toggleSwitch != null && toggleSwitch.Tag != null)
                {
                    switch (toggleSwitch.Tag)
                    {
                        case "TaskbarColor":
                        case "Hibernation":
                        case "HomeGroup":
                        case "PrintService":
                        case "CompatibilityAssistant":
                        case "Search":
                        case "ErrorReporting":
                        case "GameBar":
                        case "QuickAccessHistory":
                        case "StartMenuAds":
                        case "MyPeople":
                        case "SensorServices":
                        case "WindowsInk":
                        case "SpellingAndTypingFeatures":
                        case "FaxService":
                        case "InsiderService":
                        case "CloudClipboard":
                        case "StickyKeys":
                        case "CastToDevice":
                        case "VBS":
                        case "TaskbarToLeft":
                        case "SnapAssist":
                        case "Widgets":
                        case "Chat":
                        case "ContextMenu":
                        case "LegacyVolumeSlider":
                        case "ShowMoreOptions":
                        case "TPMCheck":
                        case "FilesCompactMode":
                        case "Stickers":
                        case "EdgeDiscoverBar":
                        case "CoPilotAI":
                            var methodName = toggleSwitch.IsOn ? $"Enable{toggleSwitch.Tag}" : $"Disable{toggleSwitch.Tag}";
                            typeof(OptimizeSystemHelper).GetMethod(methodName)?.Invoke(null, null);
                            ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = toggleSwitch.IsOn;
                            break;

                        default:
                            break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogHelper.ShowErrorMessageAndLog(ex, this.XamlRoot);
        }
    }
}