using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using RyTuneX.Contracts.Services;
using RyTuneX.Helpers;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace RyTuneX.Views;

public sealed partial class SettingsPage : Page
{
    private static readonly HttpClient httpClient = new();
    private readonly IThemeSelectorService _themeSelectorService;

    private ElementTheme _elementTheme;
    private string _versionDescription;
    public ICommand SwitchThemeCommand
    {
        get;
    }
    public static string latestVersionString;

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
                await LogHelper.LogError("Invalid language tag");
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

    public ElementTheme ElementTheme
    {
        get
        {
            LogHelper.Log("Returning ElementTheme");
            return _elementTheme;
        }
        set
        {
            if (_elementTheme != value)
            {
                LogHelper.Log("Setting ElementTheme");
                _elementTheme = value;
            }
        }
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
            await LogHelper.LogError($"Error opening log file: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
    }

    public static async Task<bool?> CheckForUpdatesAsync(XamlRoot xaml)
    {
        try
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("RyTuneX/0.9");
            var response = await httpClient.GetAsync("https://api.github.com/repos/rayenghanmi/rytunex/releases");
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            var releases = JArray.Parse(responseString);

            // Check if any releases are available
            if (releases.Count > 0)
            {
                // Get the latest release version (e.g., "v1.0.0")
                latestVersionString = releases[0]["tag_name"].ToString();

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
                await LogHelper.Log($"Is update available: {isUpdateAvailable}");

                return isUpdateAvailable;
            }
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"HTTP error: {ex.Message}\nStack Trace: {ex.StackTrace}");
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

    private async void Button_Click(object sender, RoutedEventArgs e)
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
                    var downloadUrl = "https://github.com/rayenghanmi/rytunex/releases/latest/download/RyTuneX.Setup.zip";
                    await InstallRyTuneX(downloadUrl);
                }
            }
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"Error during update check: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
    }

    public async Task InstallRyTuneX(string downloadUrl)
    {
        var tempPath = Path.GetTempPath();
        var zipFilePath = Path.Combine(tempPath, "RyTuneX.Setup.zip");
        var extractionPath = Path.Combine(tempPath, "RyTuneX");
        var setupFilePath = Path.Combine(extractionPath, "RyTuneXSetup.exe");

        try
        {
            UpdateButton.Visibility = Visibility.Collapsed;
            UpdateStack.Visibility = Visibility.Visible;
            UpdateProgress.ShowError = false;
            UpdateProgress.Visibility = Visibility.Visible;
            UpdateStatusText.Text = "Downloading...";

            // Download the ZIP file
            using (var webClient = new WebClient())
            {
                await webClient.DownloadFileTaskAsync(new Uri(downloadUrl), zipFilePath);
                Debug.WriteLine("Download complete.");
            }

            // Extract the ZIP file
            Debug.WriteLine("Extracting files...");
            UpdateStatusText.Text = "Extracting...";
            if (Directory.Exists(extractionPath))
            {
                Directory.Delete(extractionPath, true);
            }
            ZipFile.ExtractToDirectory(zipFilePath, extractionPath);
            Debug.WriteLine("Extraction complete.");

            // Delete the ZIP file
            Debug.WriteLine("Cleaning up...");
            if (File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath);
                Debug.WriteLine("Deleted RyTuneX.Setup.zip.");
            }

            // Run the setup file with the --silent argument
            UpdateStatusText.Text = "Installing...";
            Debug.WriteLine("Running RyTuneX Setup.exe...");
            var setupProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                    ? Path.Combine(Environment.GetEnvironmentVariable("windir"), @"SysNative\cmd.exe")
                    : Path.Combine(Environment.GetEnvironmentVariable("windir"), @"System32\cmd.exe"),
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
            await LogHelper.LogError($"Error during installation: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
        finally
        {
            // Cleanup the extracted files
            if (Directory.Exists(extractionPath))
            {
                Directory.Delete(extractionPath, true);
            }
            UpdateStatusText.Text = "Done".GetLocalized();
            UpdateButton.Visibility = Visibility.Visible;
            UpdateStack.Visibility = Visibility.Collapsed;
            UpdateProgress.Visibility = Visibility.Collapsed;
        }
    }

    private async void RevertChanges_Click(object sender, RoutedEventArgs e)
    {
        var revertDialog = new ContentDialog()
        {
            XamlRoot = XamlRoot,
            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
            BorderBrush = (SolidColorBrush)Application.Current.Resources["AccentAAFillColorDefaultBrush"],
            Title = "RyTuneX",
            Content = "RevertChangesDialogText".GetLocalized(),
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
                await LogHelper.Log("Reverted all changes.");

                // Clear all local settings
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values.Clear();
                await LogHelper.Log("Cleared all local settings.");

                // Delete all registry keys
                using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                        Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                            ? RegistryView.Registry64
                            : RegistryView.Default);
                key.DeleteSubKeyTree(@"SOFTWARE\RyTuneX", throwOnMissingSubKey: false);
                await LogHelper.Log("Deleted all registry keys.");

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
        await OptimizationOptions.StartInCmd($"REG EXPORT HKLM\\SOFTWARE\\RyTuneX\\Optimizations \"{path}\\RyTuneX_Backup_{DateTime.Now:yyyy-MM-dd}.reg\"");
        await LogHelper.Log($"Exported registry settings to {path}\\RyTuneX_Backup_{DateTime.Now:yyyy-MM-dd}.reg");
        App.ShowNotification(String.Empty, "SettingsExported".GetLocalized() + $"\n{path}", InfoBarSeverity.Success, 5000);
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new DevWinUI.FilePicker(WindowNative.GetWindowHandle(App.MainWindow));
        picker.FileTypeChoices.Add("Reg File", ["*.reg"]);
        picker.DefaultFileExtension = "*.reg";
        picker.ShowAllFilesOption = false;
        picker.SuggestedStartLocation = PickerLocationId.Desktop;

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            // Import the registry file
            await OptimizationOptions.StartInCmd($"regedit.exe /s {file.Path}");
            await LogHelper.Log($"Imported registry settings from {file.Path}");
        }

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

                if (kind == RegistryValueKind.DWord && Convert.ToInt32(value) == 1)
                {
                    // Simulate the toggle being on
                    var simulatedToggle = new ToggleSwitch
                    {
                        Tag = valueName,
                        IsOn = true
                    };

                    await OptimizationOptions.XamlSwitchesAsync(simulatedToggle);
                    await LogHelper.Log($"Applied optimization: {valueName}");
                }
            }
        }
        await LogHelper.Log("Applied all optimizations from the registry key.");
        App.ShowNotification(string.Empty, "SettingsImported".GetLocalized(), InfoBarSeverity.Success, 5000);
    }
}