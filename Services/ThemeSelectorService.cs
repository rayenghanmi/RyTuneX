using Microsoft.UI.Xaml;
using RyTuneX.Contracts.Services;
using RyTuneX.Helpers;
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
    }

    public async Task InitializeAsync()
    {
        Theme = await LoadThemeFromSettingsAsync();
        _ = LogHelper.Log($"Theme initialized: {Theme}");
        await Task.CompletedTask;
    }

    public async Task SetThemeAsync(ElementTheme theme)
    {
        Theme = theme;
        _ = LogHelper.Log($"Theme changed to: {theme}");
        await SetRequestedThemeAsync();
        await SaveThemeInSettingsAsync(Theme);
    }

    public async Task SetRequestedThemeAsync()
    {
        if (App.MainWindow.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = Theme;

            TitleBarHelper.UpdateTitleBar(Theme);
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
}