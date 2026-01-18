using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Win32;
using Microsoft.Windows.Storage.Pickers;
using RyTuneX.Contracts.Services;
using RyTuneX.Helpers;
using Windows.ApplicationModel;
using Windows.Storage;

namespace RyTuneX.Views;

[JsonSerializable(typeof(GitHubRelease))]
internal partial class UpdateJsonContext : JsonSerializerContext
{
}
public record GitHubRelease(
    [property: JsonPropertyName("tag_name")] string TagName
);

public sealed partial class SettingsPage : Page, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private ElementTheme _elementTheme;
    public ElementTheme ElementTheme
    {
        get => _elementTheme;
        set
        {
            if (_elementTheme != value)
            {
                _elementTheme = value;
                OnPropertyChanged();
            }
        }
    }
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static readonly HttpClient httpClient = new();
    private readonly IThemeSelectorService _themeSelectorService;

    private string _versionDescription;
    private string? _pendingScrollTarget;

    public ICommand SwitchThemeCommand
    {
        get;
    }
    public static string latestVersionString = string.Empty;

    public SettingsPage()
    {
        InitializeComponent();
        LogHelper.Log("Initializing SettingsPage");
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;

        _themeSelectorService = App.GetService<IThemeSelectorService>();

        // Set the default language based on the stored setting or the system if not set explicitly
        SetDefaultLanguageBasedOnSystem();

        _elementTheme = _themeSelectorService.Theme;
        _versionDescription = "Version".GetLocalized() + " " + GetVersionDescription();

        SwitchThemeCommand = new RelayCommand<ElementTheme>(
            async (param) =>
            {
                if (ElementTheme != param)
                {
                    ElementTheme = param;
                    await _themeSelectorService.SetThemeAsync(param);
                }
            });

        Loaded += SettingsPage_Loaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string optionTag && !string.IsNullOrEmpty(optionTag))
        {
            _pendingScrollTarget = optionTag;
        }
    }

    private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_pendingScrollTarget))
        {
            await ScrollToElementHelper.ScrollToElementAsync(this, _pendingScrollTarget);
            _pendingScrollTarget = null;
        }
    }

    private async void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedLanguage = (ComboBoxItem)LanguageComboBox.SelectedItem;

        if (selectedLanguage != null)
        {
            var languageTag = selectedLanguage.Tag as string;

            if (!string.IsNullOrEmpty(languageTag))
            {
                // Set the primary language override
                Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = languageTag;

                // Save the selected language to local settings for the next app session
                ApplicationData.Current.LocalSettings.Values["SelectedLanguage"] = languageTag;
            }
            else
            {
                _ = LogHelper.LogError("Invalid language tag");
                throw new Exception($"Invalid language tag");
            }
        }
    }

    private void SetDefaultLanguageBasedOnSystem()
    {
        // Check if the user has previously selected a language
        if (ApplicationData.Current.LocalSettings.Values.ContainsKey("SelectedLanguage"))
        {
            // Retrieve the stored language
            var storedLanguage = ApplicationData.Current.LocalSettings.Values["SelectedLanguage"] as string;

            // Set the default language in the ComboBox based on the stored language
            SetDefaultLanguage(storedLanguage);
        }
        else
        {
            // Determine the current system language
            var currentLanguage = Windows.Globalization.ApplicationLanguages.Languages[0];

            // Set the default language in the ComboBox based on the system language
            SetDefaultLanguage(currentLanguage);
        }
    }

    private void SetDefaultLanguage(string tag)
    {
        LogHelper.Log($"Setting Language: {tag}");
        foreach (ComboBoxItem item in LanguageComboBox.Items)
        {
            if (item.Tag as string == tag)
            {
                LanguageComboBox.SelectedItem = item;
                break;
            }
        }
    }

    public static string GetVersionDescription()
    {
        Version version;

        if (RuntimeHelper.IsMSIX)
        {
            var packageVersion = Package.Current.Id.Version;

            version = new(packageVersion.Major, packageVersion.Minor, packageVersion.Build);
        }
        else
        {
            version = typeof(SettingsPage).Assembly.GetName().Version!;
        }
        return $"{version.Major}.{version.Minor}.{version.Build}";
    }

    public string VersionDescription
    {
        get
        {
            LogHelper.Log("Getting VersionDescription");
            return _versionDescription;
        }
        set
        {
            if (_versionDescription != value)
            {
                LogHelper.Log("Setting VersionDescription");
                _versionDescription = value;
            }
        }
    }

    private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
    {
        await OpenLogFile();
    }

    private async Task OpenLogFile()
    {
        try
        {
            var tempFolder = ApplicationData.Current.TemporaryFolder;
            var logFile = await tempFolder.GetFileAsync($"Logs_{DateTime.Now:yyyy-MM-dd}.txt");

            if (logFile != null)
            {
                var options = new Windows.System.LauncherOptions();
                options.DisplayApplicationPicker = false;
                await Windows.System.Launcher.LaunchFileAsync(logFile, options);
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error opening log file: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
    }

    public static async Task<bool?> CheckForUpdatesAsync(XamlRoot xaml)
    {
        try
        {
            // Only fetch the latest release
            var requestUri = "https://api.github.com/repos/rayenghanmi/rytunex/releases/latest";

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.UserAgent.ParseAdd("RyTuneX/1.0");

            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            // Stream the response directly into the deserializer
            using var stream = await response.Content.ReadAsStreamAsync();

            // Use Source Generation context for near-instant parsing
            var release = await JsonSerializer.DeserializeAsync(stream, UpdateJsonContext.Default.GitHubRelease);

            if (release?.TagName != null)
            {
                // Get the latest release version (e.g., "v1.0.0")
                latestVersionString = release.TagName;

                // Remove leading 'v'
                if (latestVersionString.StartsWith("v"))
                {
                    latestVersionString = latestVersionString.Substring(1);
                }

                // Get the current assembly version
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

                // Parse the latest version string into a Version object
                var latestVersion = new Version(latestVersionString);

                // Compare versions: check if the latest version is greater than the current version
                var isUpdateAvailable = latestVersion > currentVersion;

                Debug.WriteLine($"Is update available: {isUpdateAvailable}");
                _ = LogHelper.Log($"Is update available: {isUpdateAvailable}");

                return isUpdateAvailable;
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Update Check Failed: {ex.Message}");
            var networkError = new ContentDialog()
            {
                XamlRoot = xaml,
                Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
                BorderBrush = (SolidColorBrush)Application.Current.Resources["AccentAAFillColorDefaultBrush"],
                Title = "UpdateTitle".GetLocalized(),
                Content = "NetworkError".GetLocalized(),
                CloseButtonText = "Close".GetLocalized()
            };
            await networkError.ShowAsync();
        }

        return null;
    }

    private async void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var res = await CheckForUpdatesAsync(XamlRoot);
            if (res == false)
            {
                var updateUnavailable = new ContentDialog()
                {
                    XamlRoot = XamlRoot,
                    Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
                    BorderBrush = (SolidColorBrush)Application.Current.Resources["AccentAAFillColorDefaultBrush"],
                    Title = "UpdateTitle".GetLocalized(),
                    Content = "UnavailableUpdate0".GetLocalized() + latestVersionString + "UnavailableUpdate1".GetLocalized(),
                    CloseButtonText = "Close".GetLocalized()
                };
                await updateUnavailable.ShowAsync();
            }
            if (res == true)
            {
                var updateAvailable = new ContentDialog()
                {
                    XamlRoot = XamlRoot,
                    Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
                    BorderBrush = (SolidColorBrush)Application.Current.Resources["AccentAAFillColorDefaultBrush"],
                    Title = "UpdateTitle".GetLocalized(),
                    Content = "AvailableUpdateContent0".GetLocalized() + latestVersionString + "AvailableUpdateContent1".GetLocalized(),
                    CloseButtonText = "Close".GetLocalized(),
                    PrimaryButtonText = "Update".GetLocalized(),
                    PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"]
                };
                // Show the dialog and await the result
                var result = await updateAvailable.ShowAsync();

                // Check if the "Update" button was clicked
                if (result == ContentDialogResult.Primary)
                {
                    // Run the installation module
                    var downloadUrl = "https://github.com/rayenghanmi/rytunex/releases/latest/download/RyTuneXSetup.exe";
                    await InstallRyTuneX(downloadUrl);
                }
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error during update check: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
    }

    public async Task InstallRyTuneX(string downloadUrl)
    {
        var tempPath = Path.GetTempPath();
        var setupFilePath = Path.Combine(tempPath, "RyTuneXSetup.exe");

        try
        {
            UpdateButton.Visibility = Visibility.Collapsed;
            UpdateStack.Visibility = Visibility.Visible;
            UpdateProgress.ShowError = false;
            UpdateProgress.Visibility = Visibility.Visible;
            UpdateStatusText.Text = "Downloading...";

            // Download the setup file
            using (var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                using var fileStream = new FileStream(setupFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                await response.Content.CopyToAsync(fileStream);
            }

            // Run the setup file with the --silent argument
            UpdateStatusText.Text = "Installing...";
            Debug.WriteLine("Running RyTuneXSetup.exe...");
            var setupProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SysNative", "cmd.exe")
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "cmd.exe"),
                    Arguments = $"/c \"{setupFilePath} --silent\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas"
                }
            };
            setupProcess.Start();
            await setupProcess.WaitForExitAsync();
            Debug.WriteLine("RyTuneX Setup.exe has finished execution.");
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = "UnexpectedError".GetLocalized();
            UpdateProgress.ShowError = true;
            _ = LogHelper.LogError($"Error during installation: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
        finally
        {
            // Cleanup the setup file
            if (File.Exists(setupFilePath))
            {
                try
                {
                    File.Delete(setupFilePath);
                    Debug.WriteLine("Deleted RyTuneXSetup.exe.");
                }
                catch (Exception ex)
                {
                    _ = LogHelper.LogError($"Error deleting setup file: {ex.Message}");
                }
            }
            UpdateStatusText.Text = "Done".GetLocalized();
            UpdateButton.Visibility = Visibility.Visible;
            UpdateStack.Visibility = Visibility.Collapsed;
            UpdateProgress.Visibility = Visibility.Collapsed;
        }
    }

    private async void RevertChanges_Click(object sender, RoutedEventArgs e)
    {
        // Checkbox for keeping app data
        var keepAppDataCheckBox = new CheckBox
        {
            Content = "KeepAppData".GetLocalized(),
            IsChecked = true, // Checked by default
            Margin = new Thickness(0, 10, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left
        };

        // Stack panel to hold the original text and the checkbox
        var contentPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Children =
            {
                new TextBlock
                {
                    Text = "RevertChangesDialogText".GetLocalized(),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                },
                keepAppDataCheckBox
            }
        };

        var revertDialog = new ContentDialog()
        {
            XamlRoot = XamlRoot,
            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
            BorderBrush = (SolidColorBrush)Application.Current.Resources["AccentAAFillColorDefaultBrush"],
            Title = "RyTuneX",
            Content = contentPanel,
            CloseButtonText = "Close".GetLocalized(),
            PrimaryButtonText = "Continue".GetLocalized(),
            PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"]
        };
        var result = await revertDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            try
            {
                var progressRing = new ProgressRing
                {
                    IsActive = true,
                    Width = 50,
                    Height = 50
                };

                var textBlock = new TextBlock
                {
                    Text = "RevertingChanges".GetLocalized(),
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10)
                };

                var currentDialog = new ContentDialog()
                {
                    XamlRoot = XamlRoot,
                    Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
                    Title = "RyTuneX",
                    Content = new StackPanel
                    {
                        Children = { progressRing, textBlock },
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Spacing = 20
                    },
                    IsPrimaryButtonEnabled = false,
                };

                // Show the ContentDialog asynchronously (non-blocking)
                var dialogTask = currentDialog.ShowAsync();

                // Execute the revert operation
                await OptimizationOptions.RevertAllChanges();
                _ = LogHelper.Log("Reverted all changes.");

                // Clear local settings if the checkbox is not checked
                if (keepAppDataCheckBox.IsChecked != true)
                {
                    // Clear all local settings
                    var localSettings = ApplicationData.Current.LocalSettings;
                    localSettings.Values.Clear();
                    _ = LogHelper.Log("Cleared all local settings.");
                }
                else
                {
                    _ = LogHelper.Log("Kept app data as requested by user.");
                }

                // Delete all registry keys
                using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                        Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                            ? RegistryView.Registry64
                            : RegistryView.Default);
                key.DeleteSubKeyTree(@"SOFTWARE\RyTuneX", throwOnMissingSubKey: false);
                _ = LogHelper.Log("Deleted all registry keys.");

                // Wait for a small delay for the operations to complete
                await Task.Delay(1000);

                // Close the application after the operations are completed
                Application.Current.Exit();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var exportFilePath = Path.Combine(path, $"RyTuneX_Backup_{DateTime.Now:yyyy-MM-dd}.reg");

        try
        {
            var regFlag = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess ? " /reg:64" : "";
            var exitCode = await OptimizationOptions.StartInCmd($"REG EXPORT \"HKLM\\SOFTWARE\\RyTuneX\\Optimizations\" \"{exportFilePath}\" /y{regFlag}");

            if (exitCode == 0 && File.Exists(exportFilePath))
            {
                _ = LogHelper.Log($"Exported registry settings to {exportFilePath}");
                App.ShowNotification(string.Empty, "SettingsExported".GetLocalized() + $"\n{path}", InfoBarSeverity.Success, 5000);
            }
            else
            {
                _ = LogHelper.LogError($"Failed to export registry settings. Exit code: {exitCode}");
                App.ShowNotification(string.Empty, "UnexpectedError".GetLocalized(), InfoBarSeverity.Error, 5000);
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error exporting registry settings: {ex.Message}\nStack Trace: {ex.StackTrace}");
            App.ShowNotification(string.Empty, "UnexpectedError".GetLocalized(), InfoBarSeverity.Error, 5000);
        }
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker(App.MainWindow.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.Desktop
        };
        picker.FileTypeFilter.Add(".reg");

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            // Import the registry file
            await OptimizationOptions.StartInCmd($"regedit.exe /s {file.Path}");
            _ = LogHelper.Log($"Imported registry settings from {file.Path}");
            // Apply all the optimizations present in the registry key
            using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                    Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                        ? RegistryView.Registry64
                        : RegistryView.Default).CreateSubKey(@"SOFTWARE\RyTuneX\Optimizations");

            if (key != null)
            {
                foreach (var valueName in key.GetValueNames())
                {
                    var value = key.GetValue(valueName);
                    var kind = key.GetValueKind(valueName);

                    // Handle Windows Updates mode separately
                    if (valueName == "WindowsUpdatesMode" && kind == RegistryValueKind.String)
                    {
                        var mode = value as string;
                        if (!string.IsNullOrEmpty(mode))
                        {
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
                            _ = LogHelper.Log($"Applied Windows Updates mode: {mode}");
                        }
                        continue;
                    }

                    if (kind == RegistryValueKind.DWord && Convert.ToInt32(value) == 1)
                    {
                        // Simulate the toggle being on
                        var simulatedToggle = new ToggleSwitch
                        {
                            Tag = valueName,
                            IsOn = true
                        };

                        await OptimizationOptions.XamlSwitchesAsync(simulatedToggle);
                        _ = LogHelper.Log($"Applied optimization: {valueName}");
                    }
                }
            }
            _ = LogHelper.Log("Applied all optimizations from the registry key.");
            App.ShowNotification(string.Empty, "SettingsImported".GetLocalized(), InfoBarSeverity.Success, 5000);
        }
    }
}