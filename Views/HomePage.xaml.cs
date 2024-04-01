using System.Diagnostics;
using System.Security.Policy;
using Microsoft.UI.Xaml.Controls;

namespace RyTuneX.Views;

public sealed partial class HomePage : Page
{
    private readonly string _versionDescription;

    public HomePage()
    {
        InitializeComponent();
        LogHelper.Log("Initializing HomePage");
        _versionDescription = "Version " + SettingsPage.GetVersionDescription();
    }

    private void GithubButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://rayenghanmi.github.io/rytunex",
            UseShellExecute = true
        });
    }

    private void DiscordButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://discord.gg/gyBzyd364t",
            UseShellExecute = true
        });
    }

    private void IssueButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/rayenghanmi/rytunex/issues/new",
            UseShellExecute = true
        });
    }
}
