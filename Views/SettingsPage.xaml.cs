using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using RyTuneX.Contracts.Services;
using RyTuneX.Helpers;
using Windows.ApplicationModel;
using Windows.Storage;

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
        _versionDescription = "Version " + GetVersionDescription();

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

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
                LogHelper.Log("Invalid language tag");
                LogHelper.LogError("Invalid language tag");
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
            StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
            StorageFile logFile = await tempFolder.GetFileAsync($"Logs_{DateTime.Now:yyyy-MM-dd}.txt");

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

                // Log both versions for debugging
                Debug.WriteLine($"Current version: {currentVersion}");
                Debug.WriteLine($"Parsed latest version: {latestVersion}");

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
                    ApplicationData.Current.LocalSettings.Values["JustUpdated"] = true;
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

    static string ExtractLatestVersionChanges(string changelog)
    {
        try
        {
            // Regex to match the latest version section
            var match = Regex.Match(changelog, @"## (\d+\.\d+\.\d+) - Released\n((.|\n)*?)(?=\n## |$)");
            if (match.Success)
            {
                var latestChanges = match.Groups[2].Value.Trim();
                latestChanges = Regex.Replace(latestChanges, @"^###\s+", "", RegexOptions.Multiline);
                latestChanges = Regex.Replace(latestChanges, @"^>\s+", "", RegexOptions.Multiline);
                latestChanges = Regex.Replace(latestChanges, @"\[\!(.*?)\]", match => match.Groups[1].Value);

                return latestChanges;
            }
            else
            {
                return "No notable changes found.";
            }
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"Error extracting latest version changes: {ex.Message}\nStack Trace: {ex.StackTrace}");
            return "Error extracting latest version changes.";
        }
    }

    public async Task InstallRyTuneX(string downloadUrl)
    {
        var tempPath = Path.GetTempPath();
        var zipFilePath = Path.Combine(tempPath, "RyTuneX.Setup.zip");
        var extractionPath = Path.Combine(tempPath, "RyTuneX");
        var setupFilePath = Path.Combine(extractionPath, "RyTuneXSetup.exe");
        var changelogUrl = "https://raw.githubusercontent.com/rayenghanmi/RyTuneX/refs/heads/main/CHANGELOG.md";

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

            string changelogContent;
            using (var webClient = new WebClient())
            {
                changelogContent = await webClient.DownloadStringTaskAsync(new Uri(changelogUrl));
                Debug.WriteLine("Changelog download complete.");
            }

            ApplicationData.Current.LocalSettings.Values["latestChanges"] = ExtractLatestVersionChanges(changelogContent);

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
            Process setupProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
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
            UpdateStatusText.Text = "Error has occurred";
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
            ApplicationData.Current.LocalSettings.Values["DoneUpdating"] = true;
            UpdateStatusText.Text = "Done";
            UpdateButton.Visibility = Visibility.Visible;
            UpdateStack.Visibility = Visibility.Collapsed;
            UpdateProgress.Visibility = Visibility.Collapsed;
        }
    }
}