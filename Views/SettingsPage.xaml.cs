using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RyTuneX.Contracts.Services;
using RyTuneX.Helpers;
using Windows.ApplicationModel;
using Windows.Storage;

namespace RyTuneX.Views;

public sealed partial class SettingsPage : Page
{
    private readonly IThemeSelectorService _themeSelectorService;

    private ElementTheme _elementTheme;
    private string _versionDescription;
    public ICommand SwitchThemeCommand
    {
        get;
    }

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
}
