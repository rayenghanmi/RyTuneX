using System.Diagnostics;
using System.Text.RegularExpressions;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Win32;
using RyTuneX.Helpers;

namespace RyTuneX.Views;

public sealed partial class OptimizeSystemPage : Page
{
    private const string RegistryBaseKey = @"SOFTWARE\RyTuneX\Optimizations";
    private string? _pendingScrollTarget;
    private bool _isInitializingPowerMode;
    private bool _isInitializingWindowsUpdates;

    public OptimizeSystemPage()
    {
        InitializeComponent();
        LogHelper.Log("Initializing OptimizeSystemPage");
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        Loaded += OptimizeSystemPage_Loaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Store the navigation parameter for scrolling after the page is loaded
        if (e.Parameter is string optionTag && !string.IsNullOrEmpty(optionTag))
        {
            _pendingScrollTarget = optionTag;
        }
    }

    // Start initialization in the background so navigation UI is not blocked
    private void OptimizeSystemPage_Loaded(object sender, RoutedEventArgs e)
    {
        _ = InitializeAsync();
    }

    // Perform the heavier initialization work asynchronously without blocking the UI navigation
    private async Task InitializeAsync()
    {
        // Allow the UI to finish rendering and release the pressed state on the menu
        await Task.Yield();

        try
        {
            await InitializeToggleSwitchesAsync();
            await InitializePowerModeAsync();
            await InitializeWindowsUpdatesAsync();

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
            // Get all available power plans from the system
            var powerPlans = await GetAvailablePowerPlansAsync();
            var activePlanGuid = await GetActivePowerPlanGuidAsync();

            PowerModeComboBox.Items.Clear();

            foreach (var (guid, name) in powerPlans)
            {
                var item = new ComboBoxItem
                {
                    Content = name,
                    Tag = guid
                };
                PowerModeComboBox.Items.Add(item);

                // Select the active plan
                if (!string.IsNullOrEmpty(activePlanGuid) && guid.Equals(activePlanGuid, StringComparison.OrdinalIgnoreCase))
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
            foreach (var toggleSwitch in FindVisualChildren<ToggleSwitch>(this))
            {
                if (toggleSwitch.Tag is string tagName)
                {
                    // Retrieve the state from the 64-bit registry with 32-bit app
                    using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                        Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                            ? RegistryView.Registry64
                            : RegistryView.Default).CreateSubKey(RegistryBaseKey);
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
    private async Task<string> StartTaskAsync(string command)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SysNative", "cmd.exe")
                            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "cmd.exe"),
                Arguments = $"/C \"{command}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        // Read output asynchronously
        var output = await process.StandardOutput.ReadToEndAsync();

        // Wait for the process to exit asynchronously
        await process.WaitForExitAsync();

        return output;
    }

    private async Task<string> StartTask(string command)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SysNative", "cmd.exe")
                            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "cmd.exe"),
                Arguments = $"/C \"{command}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
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
        var status = await StartTask("compact.exe /compactos:query");

        // Create a dialog to show the compression status and options
        var compressDialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
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
            CompressOSButton.Visibility = Visibility.Collapsed;
            CompressOSProgressRing.Visibility = Visibility.Visible;
            CompressOSProgressText.Text = "Compressing".GetLocalized();
            var result = await StartTask("compact.exe /compactos:always");
            App.ShowNotification("SystemCompressionTitle".GetLocalized(), result, InfoBarSeverity.Success, 5000);
            CompressOSButton.Visibility = Visibility.Visible;
            CompressOSProgressRing.Visibility = Visibility.Collapsed;
            CompressOSProgressText.Text = string.Empty;
        };

        // Handle the Decompress button click by setting UI elements and running the command
        compressDialog.SecondaryButtonClick += async (sender, args) =>
        {
            CompressOSButton.Visibility = Visibility.Collapsed;
            CompressOSProgressRing.Visibility = Visibility.Visible;
            CompressOSProgressText.Text = "Decompressing".GetLocalized();
            var result = await StartTask("compact.exe /compactos:never");
            App.ShowNotification("SystemCompressionTitle".GetLocalized(), result, InfoBarSeverity.Success, 5000);
            CompressOSButton.Visibility = Visibility.Visible;
            CompressOSProgressRing.Visibility = Visibility.Collapsed;
            CompressOSProgressText.Text = string.Empty;
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

            var createDialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
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
}