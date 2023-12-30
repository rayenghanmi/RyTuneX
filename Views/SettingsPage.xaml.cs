using System.Diagnostics;
using CommunityToolkit.WinUI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using RyTuneX.ViewModels;
using Windows.ApplicationModel.Core;
using Windows.Globalization;
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
            }
            else
            {
                throw new Exception($"Invalid language tag");
            }
        }
    }
}
