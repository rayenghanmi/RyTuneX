using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using RyTuneX.Contracts.Services;
using RyTuneX.Helpers;
using RyTuneX.Views;
using System.IO;
using Windows.UI.ViewManagement;

namespace RyTuneX.Services;

public class ThemeSelectorService : IThemeSelectorService
{
    private const string SettingsKey = "AppBackgroundRequestedTheme";

    public ElementTheme Theme { get; set; } = ElementTheme.Default;

    private readonly ILocalSettingsService _localSettingsService;
    private readonly UISettings _uiSettings;

    public ThemeSelectorService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
        _uiSettings = new UISettings();
        _uiSettings.ColorValuesChanged += UiSettings_ColorValuesChanged;
    }

    public async Task InitializeAsync()
    {
        Theme = await LoadThemeFromSettingsAsync();
        await SetRequestedThemeAsync(); // Ensure the theme is set on startup
        await Task.CompletedTask;
    }

    public async Task SetThemeAsync(ElementTheme theme)
    {
        Theme = theme;

        await SetRequestedThemeAsync();
        await SaveThemeInSettingsAsync(Theme);
    }

    public async Task SetRequestedThemeAsync()
    {
        if (App.MainWindow.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = Theme;

            TitleBarHelper.UpdateTitleBar(Theme);
            UpdateWindowIcon(Theme); // Update the window icon based on the theme
        }

        await Task.CompletedTask;
    }

    private async Task<ElementTheme> LoadThemeFromSettingsAsync()
    {
        var themeName = await _localSettingsService.ReadSettingAsync<string>(SettingsKey);

        if (Enum.TryParse(themeName, out ElementTheme cacheTheme))
        {
            return cacheTheme;
        }

        return ElementTheme.Default;
    }

    private async Task SaveThemeInSettingsAsync(ElementTheme theme)
    {
        await _localSettingsService.SaveSettingAsync(SettingsKey, theme.ToString());
    }

    private void UpdateWindowIcon(ElementTheme theme)
    {
        if (theme == ElementTheme.Default)
        {
            var background = _uiSettings.GetColorValue(UIColorType.Background);
            theme = background == Colors.White ? ElementTheme.Light : ElementTheme.Dark;
        }

        var iconPath = theme switch
        {
            ElementTheme.Light => "Assets/LightWindowIcon.ico",
            _ => "Assets/WindowIcon.ico"
        };

        App.MainWindow.AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, iconPath));
    }

    private void UiSettings_ColorValuesChanged(UISettings sender, object args)
    {
        App.MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            if (Theme == ElementTheme.Default)
            {
                UpdateWindowIcon(Theme);
            }
        });
    }
}
