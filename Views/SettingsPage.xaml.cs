using System.Diagnostics;
using CommunityToolkit.WinUI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using RyTuneX.ViewModels;
using Windows.ApplicationModel.Core;
using Windows.Globalization;
using Windows.Storage;
using Windows.UI.Core;

namespace RyTuneX.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel
    {
        get;
    }

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();

        // Set the default language based on the stored setting or the system if not set explicitly
        SetDefaultLanguageBasedOnSystem();
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
        foreach (ComboBoxItem item in LanguageComboBox.Items)
        {
            if (item.Tag as string == tag)
            {
                LanguageComboBox.SelectedItem = item;
                break;
            }
        }
    }
}
