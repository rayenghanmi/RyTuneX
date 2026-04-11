using System.Diagnostics;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Win32;
using RyTuneX.Helpers;

namespace RyTuneX.Views;

public sealed partial class FeaturesPage : Page
{
    private const string RegistryBaseKey = @"SOFTWARE\RyTuneX\Optimizations";
    private string? _pendingScrollTarget;
    private static readonly StringComparer ToggleKeyComparer = StringComparer.OrdinalIgnoreCase;

    public FeaturesPage()
    {
        InitializeComponent();
        LogHelper.Log("Initializing FeaturesPage");
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        Loaded += FeaturesPage_Loaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string optionTag && !string.IsNullOrEmpty(optionTag))
        {
            _pendingScrollTarget = optionTag;
        }
    }

    private async void FeaturesPage_Loaded(object sender, RoutedEventArgs e)
    {
        await InitializeToggleSwitchesAsync();

        if (!string.IsNullOrEmpty(_pendingScrollTarget))
        {
            await ScrollToElementHelper.ScrollToElementAsync(this, _pendingScrollTarget);
            _pendingScrollTarget = null;
        }
    }

    private async Task InitializeToggleSwitchesAsync()
    {
        _ = LogHelper.Log("Initializing Toggle Switches");
        try
        {
            var toggleStates = await Task.Run(ReadToggleStates);

            foreach (var toggleSwitch in FindVisualChildren<ToggleSwitch>(this))
            {
                if (toggleSwitch.Tag is string tagName)
                {
                    if (toggleStates.TryGetValue(tagName, out var currentState))
                    {
                        toggleSwitch.IsOn = currentState;
                    }

                    // Subscribe to the Toggled event
                    toggleSwitch.Toggled -= ToggleSwitch_Toggled;
                    toggleSwitch.Toggled += ToggleSwitch_Toggled;
                }
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error initializing toggle switches: {ex.Message}\nStack Trace: {ex.StackTrace}");
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
            await OptimizationOptions.XamlSwitchesAsync(toggleSwitch);
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError(ex.Message);
        }
    }

    private static Dictionary<string, bool> ReadToggleStates()
    {
        var states = new Dictionary<string, bool>(ToggleKeyComparer);
        using var key = OpenOptimizationsKey(writable: false) ?? OpenOptimizationsKey(writable: true);
        if (key == null)
        {
            return states;
        }

        foreach (var valueName in key.GetValueNames())
        {
            if (key.GetValue(valueName) is int state)
            {
                states[valueName] = state == 1;
            }
        }

        return states;
    }

    private static RegistryKey? OpenOptimizationsKey(bool writable)
    {
        var registryView = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
            ? RegistryView.Registry64
            : RegistryView.Default;

        var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView);
        return writable
            ? baseKey.CreateSubKey(RegistryBaseKey)
            : baseKey.OpenSubKey(RegistryBaseKey, writable);
    }
}