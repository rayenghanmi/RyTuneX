using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Win32;
using RyTuneX.Helpers;
using RyTuneX.Services;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace RyTuneX.Views;

public sealed partial class OptimizeSystemPage : Page
{
    private const string RegistryBaseKey = @"SOFTWARE\RyTuneX\Optimizations";
    private static List<(string Guid, string Name)>? _cachedPowerPlans;
    private static string? _cachedActivePowerPlanGuid;
    private string? _pendingScrollTarget;
    private bool _isInitialized;
    private bool _isInitializingPowerMode;
    private bool _isInitializingWindowsUpdates;

    public OptimizeSystemPage()
    {
        InitializeComponent();
        LogHelper.Log("Initializing OptimizeSystemPage");
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        Loaded += OptimizeSystemPage_Loaded;
        Unloaded += OptimizeSystemPage_Unloaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Store the navigation parameter for scrolling after the page is loaded
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

    // Start initialization in the background so navigation UI is not blocked
    private void OptimizeSystemPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized)
        {
            _ = InitializeAsync();
        }
        else if (!string.IsNullOrEmpty(_pendingScrollTarget))
        {
            _ = ScrollToElementHelper.ScrollToElementAsync(this, _pendingScrollTarget);
            _pendingScrollTarget = null;
        }
    }

    // Perform the heavier initialization work asynchronously without blocking the UI navigation
    private async Task InitializeAsync()
    {
        try
        {
            await InitializeToggleSwitchesAsync();
            _ = InitializePowerModeAsync();
            _ = InitializeWindowsUpdatesAsync();
            IntelligentCardEnhancer.EnhancePage(this);
            _isInitialized = true;

            // Scroll to the target element if there's a pending scroll target
            if (!string.IsNullOrEmpty(_pendingScrollTarget))
            {
                await ScrollToElementHelper.ScrollToElementAsync(this, _pendingScrollTarget);
                _pendingScrollTarget = null;
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error during page initialization: {ex.Message}");
        }
    }

    private async Task InitializePowerModeAsync()
    {
        _isInitializingPowerMode = true;
        try
        {
            if (_cachedPowerPlans == null || _cachedActivePowerPlanGuid == null)
            {
                var plans = await Task.Run(async () =>
                {
                    var p = await GetAvailablePowerPlansAsync();
                    var a = await GetActivePowerPlanGuidAsync();
                    return (p, a);
                });
                _cachedPowerPlans = plans.p;
                _cachedActivePowerPlanGuid = plans.a;
            }

            PowerModeComboBox.Items.Clear();

            foreach (var (guid, name) in _cachedPowerPlans)
            {
                var item = new ComboBoxItem
                {
                    Content = name,
                    Tag = guid
                };
                PowerModeComboBox.Items.Add(item);

                // Select the active plan
                if (!string.IsNullOrEmpty(_cachedActivePowerPlanGuid) && guid.Equals(_cachedActivePowerPlanGuid, StringComparison.OrdinalIgnoreCase))
                {
                    PowerModeComboBox.SelectedItem = item;
                }
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error initializing power mode: {ex.Message}");
        }
        finally
        {
            _isInitializingPowerMode = false;
        }
    }

    // Gets all available power plans from the system using powercfg.
    private async Task<List<(string Guid, string Name)>> GetAvailablePowerPlansAsync()
    {
        var powerPlans = new List<(string Guid, string Name)>();
        try
        {
            var output = await StartTaskAsync("powercfg /list");
            // Output format per line: "Power Scheme GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (Balanced) *"
            // The asterisk (*) indicates the active plan
            var matches = Regex.Matches(output, @"([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\s+\(([^)]+)\)");
            foreach (Match match in matches)
            {
                var guid = match.Groups[1].Value.ToLowerInvariant();
                var name = match.Groups[2].Value.Trim();
                powerPlans.Add((guid, name));
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error getting available power plans: {ex.Message}");
        }
        return powerPlans;
    }

    // Gets the GUID of the currently active power plan using powercfg.
    private async Task<string?> GetActivePowerPlanGuidAsync()
    {
        try
        {
            var output = await StartTaskAsync("powercfg /getactivescheme");
            // Output format: "Power Scheme GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (Balanced)"
            var match = Regex.Match(output, @"([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})");
            if (match.Success)
            {
                return match.Groups[1].Value.ToLowerInvariant();
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error getting active power plan: {ex.Message}");
        }
        return null;
    }

    // Sets the active power plan using powercfg.
    private async Task SetPowerPlanAsync(string guid)
    {
        try
        {
            await StartTaskAsync($"powercfg /setactive {guid}");
            _ = LogHelper.Log($"Power plan set to: {guid}");
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error setting power plan: {ex.Message}");
        }
    }

    private async void PowerModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Skip if we're initializing the control
        if (_isInitializingPowerMode)
            return;

        if (PowerModeComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string guid)
        {
            _cachedActivePowerPlanGuid = guid;
            await SetPowerPlanAsync(guid);
        }
    }

    private async Task InitializeWindowsUpdatesAsync()
    {
        _isInitializingWindowsUpdates = true;
        try
        {
            // Retrieve the saved state from the registry
            using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                    ? RegistryView.Registry64
                    : RegistryView.Default).OpenSubKey(RegistryBaseKey);

            var savedMode = key?.GetValue("WindowsUpdatesMode") as string ?? "Default";

            // Select the corresponding item in the ComboBox
            foreach (ComboBoxItem item in WindowsUpdatesComboBox.Items)
            {
                if (item.Tag is string tag && tag.Equals(savedMode, StringComparison.OrdinalIgnoreCase))
                {
                    WindowsUpdatesComboBox.SelectedItem = item;
                    break;
                }
            }

            // If no selection was made, default to first item
            if (WindowsUpdatesComboBox.SelectedItem == null && WindowsUpdatesComboBox.Items.Count > 0)
            {
                WindowsUpdatesComboBox.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error initializing automatic updates: {ex.Message}");
            // Default to first item on error
            if (WindowsUpdatesComboBox.Items.Count > 0)
            {
                WindowsUpdatesComboBox.SelectedIndex = 0;
            }
        }
        finally
        {
            _isInitializingWindowsUpdates = false;
        }
    }

    private async void WindowsUpdatesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Skip if we're initializing the control
        if (_isInitializingWindowsUpdates)
            return;

        if (WindowsUpdatesComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string mode)
        {
            try
            {
                // Save the selection to the registry
                using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                    Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                        ? RegistryView.Registry64
                        : RegistryView.Default).CreateSubKey(RegistryBaseKey);

                key?.SetValue("WindowsUpdatesMode", mode, RegistryValueKind.String);

                // Apply the selected mode
                switch (mode)
                {
                    case "Default":
                        await OptimizeSystemHelper.SetWindowsUpdatesDefault();
                        break;
                    case "Security":
                        await OptimizeSystemHelper.SetWindowsUpdatesSecurityOnly();
                        break;
                    case "Manually":
                        await OptimizeSystemHelper.SetWindowsUpdatesManually();
                        break;
                    case "Disabled":
                        await OptimizeSystemHelper.SetWindowsUpdatesDisabled();
                        break;
                }

                _ = LogHelper.Log($"Automatic updates mode set to: {mode}");
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Error setting automatic updates mode: {ex.Message}");
            }
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
    private void OptimizeSystemPage_Unloaded(object sender, RoutedEventArgs e)
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
    private readonly Lazy<string> _cmdFullPath = new Lazy<string>(() =>
    {
        return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                    Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess ? "SysNative" : "System32",
                    "cmd.exe"
                    );
    });
    private async Task<string> StartTaskAsync(string command, Encoding? encoding = null)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _cmdFullPath.Value,
                Arguments = $"/C \"{command}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = encoding ?? GetOEMConsoleEncoding(),
            }
        };

        process.Start();

        // Read output asynchronously
        var output = await process.StandardOutput.ReadToEndAsync();

        // Wait for the process to exit asynchronously
        await process.WaitForExitAsync();

        return output;
    }

    private async void CompressOSButton_Click(object sender, RoutedEventArgs e)
    {
        // Get the current compression status
        var status = await StartTaskAsync("compact.exe /compactos:query");

        // Create a dialog to show the compression status and options
        var compressDialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            RequestedTheme = ActualTheme,
            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
            BorderBrush = (SolidColorBrush)Application.Current.Resources["AccentAAFillColorDefaultBrush"],
            PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"],
            SecondaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"],
            Title = "SystemCompressionTitle".GetLocalized(),
            Content = status,
            PrimaryButtonText = "Compress".GetLocalized(),
            SecondaryButtonText = "Decompress".GetLocalized(),
            CloseButtonText = "Cancel".GetLocalized()
        };

        // Handle the Compress button click by setting UI elements and running the command
        compressDialog.PrimaryButtonClick += async (sender, args) =>
        {
            _ = LogHelper.Log("Starting OS compression");
            CompressOSButton.Visibility = Visibility.Collapsed;
            CompressOSProgressRing.Visibility = Visibility.Visible;
            CompressOSProgressText.Text = "Compressing".GetLocalized();
            try
            {
                var result = await StartTaskAsync("compact.exe /compactos:always");
                App.ShowNotification("SystemCompressionTitle".GetLocalized(), result, InfoBarSeverity.Success, 5000);
                CompressOSProgressText.Text = string.Empty;
            }
            finally
            {
                CompressOSButton.Visibility = Visibility.Visible;
                CompressOSProgressRing.Visibility = Visibility.Collapsed;
            }
        };

        // Handle the Decompress button click by setting UI elements and running the command
        compressDialog.SecondaryButtonClick += async (sender, args) =>
        {
            _ = LogHelper.Log("Starting OS decompression");
            CompressOSButton.Visibility = Visibility.Collapsed;
            CompressOSProgressRing.Visibility = Visibility.Visible;
            CompressOSProgressText.Text = "Decompressing".GetLocalized();
            try
            {
                var result = await StartTaskAsync("compact.exe /compactos:never");
                App.ShowNotification("SystemCompressionTitle".GetLocalized(), result, InfoBarSeverity.Success, 5000);
                CompressOSProgressText.Text = string.Empty;
            }
            finally
            {
                CompressOSButton.Visibility = Visibility.Visible;
                CompressOSProgressRing.Visibility = Visibility.Collapsed;
            }
        };
        await compressDialog.ShowAsync();
    }

    private async void AddUltimatePowerPlanButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            AddUltimatePowerPlanButton.IsEnabled = false;

            // Check if Ultimate Performance plan already exists
            var powerPlans = await GetAvailablePowerPlansAsync();
            var ultimateExists = powerPlans.Any(p =>
                p.Guid.Equals("e9a42b02-d5df-448d-aa00-03f14749eb61", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("Ultimate", StringComparison.OrdinalIgnoreCase));

            var title = "AddUltimatePowerPlanTitle".GetLocalized();

            if (ultimateExists)
            {
                App.ShowNotification(
                    title,
                    "UltimatePowerPlanExists".GetLocalized(),
                    InfoBarSeverity.Success,
                    3000);
            }
            else
            {
                // Add the Ultimate Performance power plan using powercfg
                await StartTaskAsync("powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61");
                _ = LogHelper.Log("Added Ultimate Performance power plan");

                // Refresh the power plans list
                await InitializePowerModeAsync();

                App.ShowNotification(
                    title,
                    "UltimatePowerPlanAdded".GetLocalized(),
                    InfoBarSeverity.Success,
                    3000);
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error adding Ultimate Performance power plan: {ex.Message}");
            App.ShowNotification(
                "AddUltimatePowerPlanTitle".GetLocalized(),
                "UnexpectedError".GetLocalized(),
                InfoBarSeverity.Error,
                3000);
        }
        finally
        {
            AddUltimatePowerPlanButton.IsEnabled = true;
        }
    }

    private async void CreatePowerPlanButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Create input for power plan name
            var nameTextBox = new TextBox
            {
                PlaceholderText = "PowerPlanNamePlaceholder".GetLocalized(),
                MaxLength = 50,
                Margin = new Thickness(0, 8, 0, 16)
            };

            // Get available power plans for base plan selection
            var powerPlans = await GetAvailablePowerPlansAsync();
            var basePlanComboBox = new ComboBox
            {
                MinWidth = 250,
                Margin = new Thickness(0, 8, 0, 0)
            };

            foreach (var (guid, name) in powerPlans)
            {
                basePlanComboBox.Items.Add(new ComboBoxItem
                {
                    Content = name,
                    Tag = guid
                });
            }

            if (basePlanComboBox.Items.Count > 0)
            {
                basePlanComboBox.SelectedIndex = 0;
            }

            var contentPanel = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = "PowerPlanNameLabel".GetLocalized(),
                        Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"]
                    },
                    nameTextBox,
                    new TextBlock
                    {
                        Text = "BasePowerPlanLabel".GetLocalized(),
                        Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"]
                    },
                    basePlanComboBox
                }
            };

            contentPanel.RequestedTheme = ActualTheme;

            var createDialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                RequestedTheme = ActualTheme,
                Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
                BorderBrush = (SolidColorBrush)Application.Current.Resources["AccentAAFillColorDefaultBrush"],
                Title = "CreatePowerPlanTitle".GetLocalized(),
                Content = contentPanel,
                PrimaryButtonText = "Create".GetLocalized(),
                CloseButtonText = "Cancel".GetLocalized(),
                PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"],
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await createDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var planName = nameTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(planName))
                {
                    App.ShowNotification(
                        "CreatePowerPlanTitle".GetLocalized(),
                        "PowerPlanNameRequired".GetLocalized(),
                        InfoBarSeverity.Warning,
                        3000);
                    return;
                }

                if (basePlanComboBox.SelectedItem is not ComboBoxItem selectedItem || selectedItem.Tag is not string baseGuid)
                {
                    App.ShowNotification(
                        "CreatePowerPlanTitle".GetLocalized(),
                        "BasePowerPlanRequired".GetLocalized(),
                        InfoBarSeverity.Warning,
                        3000);
                    return;
                }

                // Create the new power plan by duplicating the selected base plan
                var createOutput = await StartTaskAsync($"powercfg /duplicatescheme {baseGuid}");

                // Parse the new GUID from the output
                var match = System.Text.RegularExpressions.Regex.Match(createOutput,
                    @"([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})");

                if (match.Success)
                {
                    var newGuid = match.Groups[1].Value;

                    // Rename the new power plan
                    await StartTaskAsync($"powercfg /changename {newGuid} \"{planName}\"");

                    _ = LogHelper.Log($"Created new power plan '{planName}' with GUID: {newGuid}");

                    // Refresh the power plans list
                    await InitializePowerModeAsync();

                    App.ShowNotification(
                        "CreatePowerPlanTitle".GetLocalized(),
                        "PowerPlanCreated".GetLocalized(),
                        InfoBarSeverity.Success,
                        3000);
                }
                else
                {
                    _ = LogHelper.LogError($"Failed to parse new power plan GUID from output: {createOutput}");
                    App.ShowNotification(
                        "CreatePowerPlanTitle".GetLocalized(),
                        "UnexpectedError".GetLocalized(),
                        InfoBarSeverity.Error,
                        3000);
                }
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error creating power plan: {ex.Message}");
            App.ShowNotification(
                "CreatePowerPlanTitle".GetLocalized(),
                "UnexpectedError".GetLocalized(),
                InfoBarSeverity.Error,
                3000);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint GetConsoleOutputCP();


    private static int GetFallbackOEMCodePage()
    {
        int ansiCodePage = Console.OutputEncoding.CodePage;

        return ansiCodePage switch
        {
            1251 => 866,
            1252 => 437,
            1250 => 852,
            1253 => 737,
            1254 => 857,
            1255 => 862,
            1256 => 708,
            1257 => 775,
            1258 => 869,
            932 => 932,
            936 => 936,
            949 => 949,
            950 => 950,
            _ => 437
        };
    }

    private Encoding? _OEMConsoleEncoding;
    private Encoding GetOEMConsoleEncoding()
    {
        if (_OEMConsoleEncoding == null)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            int code_page = (int)GetConsoleOutputCP();
            if (code_page == 0)
            {
                code_page = System.Globalization.CultureInfo.CurrentCulture.TextInfo.OEMCodePage;
            }
            if (code_page == 0)
                code_page = GetFallbackOEMCodePage();

            try
            {
                _OEMConsoleEncoding = Encoding.GetEncoding(code_page);
            }
            catch
            {
                _OEMConsoleEncoding = Encoding.UTF8;
            }
        }
        return _OEMConsoleEncoding!;
    }
}