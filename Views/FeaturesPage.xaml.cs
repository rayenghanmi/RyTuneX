using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Win32;
using RyTuneX.Helpers;
using RyTuneX.Services;

namespace RyTuneX.Views;

public sealed partial class FeaturesPage : Page
{
    private const string RegistryBaseKey = @"SOFTWARE\RyTuneX\Optimizations";
    private string? _pendingScrollTarget;
    private bool _isInitialized;

    public FeaturesPage()
    {
        InitializeComponent();
        LogHelper.Log("Initializing FeaturesPage");
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        Loaded += FeaturesPage_Loaded;
        Unloaded += FeaturesPage_Unloaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string optionTag && !string.IsNullOrEmpty(optionTag))
        {
            _pendingScrollTarget = optionTag;
            if (_isInitialized)
            {
                _ = ScrollToElementHelper.ScrollToElementAsync(this, _pendingScrollTarget);
                _pendingScrollTarget = null;
            }
        }
    }

    private async void FeaturesPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized)
        {
            await InitializeToggleSwitchesAsync();
            IntelligentCardEnhancer.EnhancePage(this);
            _isInitialized = true;
        }

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
            var toggleSwitches = IntelligentCardEnhancer.GetAllToggleSwitches(this);
            var tags = toggleSwitches
                .Select(t => (t.Tag as string) ?? t.Name)
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var states = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var missingTags = new List<string>();

            foreach (var tagName in tags)
            {
                if (IntelligentOptimizationEngine.TryGetCachedState(tagName, out bool cached))
                {
                    states[tagName] = cached;
                }
                else
                {
                    missingTags.Add(tagName);
                }
            }

            if (missingTags.Count > 0)
            {
                var detected = await Task.Run(() =>
                {
                    var dict = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                    var regView = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                        ? RegistryView.Registry64
                        : RegistryView.Default;
                    using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, regView);
                    using var key = baseKey.CreateSubKey(RegistryBaseKey);

                    foreach (var tagName in missingTags)
                    {
                        var liveState = SystemStateDetector.DetectState(tagName);
                        bool targetState;
                        if (liveState.HasValue)
                        {
                            targetState = liveState.Value;
                            try { key?.SetValue(tagName, targetState ? 1 : 0, RegistryValueKind.DWord); } catch { }
                        }
                        else
                        {
                            targetState = key != null && key.GetValue(tagName) is int state && state == 1;
                        }

                        IntelligentOptimizationEngine.SetCachedState(tagName, targetState);
                        dict[tagName] = targetState;
                    }

                    return dict;
                });

                foreach (var kvp in detected)
                {
                    states[kvp.Key] = kvp.Value;
                }
            }

            foreach (var toggleSwitch in toggleSwitches)
            {
                var tagName = (toggleSwitch.Tag as string) ?? toggleSwitch.Name;
                if (states.TryGetValue(tagName, out bool targetState))
                {
                    IntelligentCardEnhancer.SetToggleIsOnSilently(toggleSwitch, targetState);
                }

                toggleSwitch.Toggled -= ToggleSwitch_Toggled;
                toggleSwitch.Toggled += ToggleSwitch_Toggled;
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
    private void FeaturesPage_Unloaded(object sender, RoutedEventArgs e)
    {
        // When cached (NavigationCacheMode.Required), keep event subscriptions active
    }

    private async void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        try
        {
            var toggleSwitch = (ToggleSwitch)sender;
            if (IntelligentCardEnhancer.IsSuppressed(toggleSwitch))
            {
                return;
            }

            var tagName = (toggleSwitch.Tag as string) ?? toggleSwitch.Name;
            _ = LogHelper.Log($"ToggleSwitch Tag: {tagName}, IsOn: {toggleSwitch.IsOn}");
            IntelligentOptimizationEngine.SetCachedState(tagName, toggleSwitch.IsOn);
            await OptimizationOptions.XamlSwitchesAsync(toggleSwitch);
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError(ex.Message);
        }
    }
}