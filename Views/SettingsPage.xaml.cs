using System.Diagnostics;
using System.Reflection;
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
            Console.WriteLine($"Error opening log file: {ex.Message}");
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
            Debug.WriteLine($"HTTP error: {ex.Message}");
            var networkError = new ContentDialog()
            {
                XamlRoot = xaml,
                Title = "UpdateTitle".GetLocalized(),
                Content = "NetworkError".GetLocalized(),
                CloseButtonText = "Close".GetLocalized()
            };
            await networkError.ShowAsync();
            await LogHelper.LogError($"HTTP error: {ex}");
        }

        return null;
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
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
            await updateAvailable.ShowAsync();
        }
    }
}
