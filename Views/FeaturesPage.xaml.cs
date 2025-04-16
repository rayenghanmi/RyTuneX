using System.Diagnostics;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using RyTuneX.Helpers;

namespace RyTuneX.Views;

public sealed partial class FeaturesPage : Page
{
    private const string RegistryBaseKey = @"SOFTWARE\RyTuneX\FeaturesPage";

    public FeaturesPage()
    {
        InitializeComponent();
        LogHelper.Log("Initializing FeaturesPage");
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        Loaded += (sender, e) => InitializeToggleSwitchesAsync();
    }

    private async void InitializeToggleSwitchesAsync()
    {
        await LogHelper.Log("Initializing Toggle Switches");
        try
        {
            foreach (var toggleSwitch in FindVisualChildren<ToggleSwitch>(this))
            {
                if (toggleSwitch.Tag is string tagName)
                {
                    // Retrieve the state from the registry
                    using var key = Registry.CurrentUser.OpenSubKey(RegistryBaseKey);
                    if (key != null && key.GetValue(tagName) is int state)
                    {
                        toggleSwitch.IsOn = state == 1;
                    }

                    // Subscribe to the Toggled event
                    toggleSwitch.Toggled += ToggleSwitch_Toggled;
                }
            }
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

            // Save the state to the registry
            using var key = Registry.CurrentUser.CreateSubKey(RegistryBaseKey);
            key?.SetValue((string)toggleSwitch.Tag, toggleSwitch.IsOn ? 1 : 0, RegistryValueKind.DWord);

            await OptimizationOptions.XamlSwitchesAsync(toggleSwitch);
        }
        catch (Exception ex)
        {
            await LogHelper.ShowErrorMessageAndLog(ex, XamlRoot);
        }
    }
}
