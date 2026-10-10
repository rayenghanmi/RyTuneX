using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using RyTuneX.Contracts.Services;
using RyTuneX.Helpers;
using RyTuneX.Models;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace RyTuneX.Services;

public class AccentColorService : IAccentColorService
{
    private const string SettingsKeyMode = "AppAccentColorMode";
    private const string SettingsKeyCustomColor = "AppCustomAccentColor";

    public static readonly Color RyTuneXBrandColor = Color.FromArgb(255, 0xFF, 0x5E, 0x92);

    private readonly ILocalSettingsService _localSettingsService;
    private readonly UISettings _uiSettings;

    public AccentColorMode Mode { get; private set; } = AccentColorMode.RyTuneX;
    public Color CustomColor { get; private set; } = RyTuneXBrandColor;
    public Color CurrentColor { get; private set; } = RyTuneXBrandColor;

    public AccentColorService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
        _uiSettings = new UISettings();

        _uiSettings.ColorValuesChanged += (sender, args) =>
        {
            if (Mode == AccentColorMode.System)
            {
                var dispatcher = App.MainWindow?.DispatcherQueue;
                if (dispatcher != null)
                {
                    dispatcher.TryEnqueue(ApplyAccentColor);
                }
            }
        };
    }

    public async Task InitializeAsync()
    {
        Mode = await LoadAccentColorModeFromSettingsAsync();
        CustomColor = await LoadCustomColorFromSettingsAsync();

        _ = LogHelper.Log($"AccentColor initialized: Mode={Mode}, CustomColor={ColorToHex(CustomColor)}");

        ApplyAccentColor();
    }

    public async Task SetAccentColorModeAsync(AccentColorMode mode)
    {
        Mode = mode;
        _ = LogHelper.Log($"AccentColor mode changed to: {mode}");
        ApplyAccentColor();
        await _localSettingsService.SaveSettingAsync(SettingsKeyMode, mode.ToString());
    }

    private DispatcherTimer? _debounceTimer;

    public async Task SetCustomColorAsync(Color color)
    {
        CustomColor = color;
        if (Mode == AccentColorMode.Custom)
        {
            ApplyAccentColor();
        }

        if (_debounceTimer == null)
        {
            _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _debounceTimer.Tick += async (s, e) =>
            {
                _debounceTimer?.Stop();
                await _localSettingsService.SaveSettingAsync(SettingsKeyCustomColor, ColorToHex(CustomColor));
                _ = LogHelper.Log($"Custom accent color saved: {ColorToHex(CustomColor)}");
            };
        }

        _debounceTimer.Stop();
        _debounceTimer.Start();
        await Task.CompletedTask;
    }

    public void RefreshAccentColor()
    {
        ApplyAccentColor();
    }

    public void ApplyAccentColor()
    {
        void Execute()
        {
            try
            {
                Color baseColor;
                Color light1;
                Color light2;
                Color light3;
                Color dark1;
                Color dark2;
                Color dark3;

                switch (Mode)
                {
                    case AccentColorMode.System:
                        baseColor = _uiSettings.GetColorValue(UIColorType.Accent);
                        light1 = _uiSettings.GetColorValue(UIColorType.AccentLight1);
                        light2 = _uiSettings.GetColorValue(UIColorType.AccentLight2);
                        light3 = _uiSettings.GetColorValue(UIColorType.AccentLight3);
                        dark1 = _uiSettings.GetColorValue(UIColorType.AccentDark1);
                        dark2 = _uiSettings.GetColorValue(UIColorType.AccentDark2);
                        dark3 = _uiSettings.GetColorValue(UIColorType.AccentDark3);
                        break;

                    case AccentColorMode.Custom:
                        baseColor = CustomColor;
                        light1 = Blend(baseColor, Colors.White, 0.15);
                        light2 = Blend(baseColor, Colors.White, 0.30);
                        light3 = Blend(baseColor, Colors.White, 0.45);
                        dark1 = Blend(baseColor, Colors.Black, 0.15);
                        dark2 = Blend(baseColor, Colors.Black, 0.30);
                        dark3 = Blend(baseColor, Colors.Black, 0.45);
                        break;

                    case AccentColorMode.RyTuneX:
                    default:
                        baseColor = RyTuneXBrandColor;
                        light1 = Blend(baseColor, Colors.White, 0.15);
                        light2 = Blend(baseColor, Colors.White, 0.30);
                        light3 = Blend(baseColor, Colors.White, 0.45);
                        dark1 = Blend(baseColor, Colors.Black, 0.15);
                        dark2 = Blend(baseColor, Colors.Black, 0.30);
                        dark3 = Blend(baseColor, Colors.Black, 0.45);
                        break;
                }

                CurrentColor = baseColor;

                // Update color resources
                UpdateColorResource("SystemAccentColor", baseColor);
                UpdateColorResource("SystemAccentColorLight1", light1);
                UpdateColorResource("SystemAccentColorLight2", light2);
                UpdateColorResource("SystemAccentColorLight3", light3);
                UpdateColorResource("SystemAccentColorDark1", dark1);
                UpdateColorResource("SystemAccentColorDark2", dark2);
                UpdateColorResource("SystemAccentColorDark3", dark3);

                // Update derived brush resources
                UpdateBrushResource("AccentFillColorDefaultBrush", baseColor);
                UpdateBrushResource("AccentFillColorSecondaryBrush", light1);
                UpdateBrushResource("AccentFillColorTertiaryBrush", dark1);
                UpdateBrushResource("AccentFillColorDisabledBrush", baseColor, 0.36);
                UpdateBrushResource("AccentTextFillColorDefaultBrush", baseColor);
                UpdateBrushResource("AccentTextFillColorSecondaryBrush", light1);
                UpdateBrushResource("AccentTextFillColorTertiaryBrush", dark1);
                UpdateBrushResource("AccentTextFillColorDisabledBrush", baseColor, 0.36);
                UpdateBrushResource("AccentTextFillColorPrimaryBrush", baseColor);
                UpdateBrushResource("AccentAAFillColorDefaultBrush", baseColor);

                // Refresh theme resource bindings if the visual tree is loaded
                if (App.MainWindow?.Content is FrameworkElement root)
                {
                    var currentTheme = root.RequestedTheme;
                    var temp = currentTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;
                    root.RequestedTheme = temp;
                    root.RequestedTheme = currentTheme;
                    TitleBarHelper.UpdateTitleBar(currentTheme);
                }
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Failed to apply accent color: {ex.Message}");
            }
        }

        var dispatcher = App.MainWindow?.DispatcherQueue;
        if (dispatcher?.HasThreadAccess == true)
        {
            Execute();
            return;
        }

        if (dispatcher is not null)
        {
            dispatcher.TryEnqueue(Execute);
        }
        else
        {
            Execute();
        }
    }

    private static void UpdateColorResource(string key, Color color)
    {
        Application.Current.Resources[key] = color;

        foreach (var dictKey in new[] { "Default", "Light", "Dark" })
        {
            if (Application.Current.Resources.ThemeDictionaries.TryGetValue(dictKey, out var themeObj) && themeObj is ResourceDictionary themeDict)
            {
                themeDict[key] = color;
            }
        }
    }

    private static void UpdateBrushResource(string key, Color color, double opacity = 1.0)
    {
        if (Application.Current.Resources.TryGetValue(key, out var existing) && existing is SolidColorBrush brush)
        {
            brush.Color = color;
            brush.Opacity = opacity;
        }
        else
        {
            Application.Current.Resources[key] = new SolidColorBrush(color) { Opacity = opacity };
        }

        foreach (var dictKey in new[] { "Default", "Light", "Dark" })
        {
            if (Application.Current.Resources.ThemeDictionaries.TryGetValue(dictKey, out var themeObj) && themeObj is ResourceDictionary themeDict)
            {
                if (themeDict.TryGetValue(key, out var tExisting) && tExisting is SolidColorBrush tBrush)
                {
                    tBrush.Color = color;
                    tBrush.Opacity = opacity;
                }
                else
                {
                    themeDict[key] = new SolidColorBrush(color) { Opacity = opacity };
                }
            }
        }
    }

    public static Color Blend(Color baseColor, Color blendColor, double factor)
    {
        var r = (byte)Math.Clamp(baseColor.R + (blendColor.R - baseColor.R) * factor, 0, 255);
        var g = (byte)Math.Clamp(baseColor.G + (blendColor.G - baseColor.G) * factor, 0, 255);
        var b = (byte)Math.Clamp(baseColor.B + (blendColor.B - baseColor.B) * factor, 0, 255);
        return Color.FromArgb(baseColor.A, r, g, b);
    }

    public static Color ColorFromHex(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return RyTuneXBrandColor;
        }

        hex = hex.Trim().TrimStart('#');
        try
        {
            if (hex.Length == 6)
            {
                var r = Convert.ToByte(hex.Substring(0, 2), 16);
                var g = Convert.ToByte(hex.Substring(2, 2), 16);
                var b = Convert.ToByte(hex.Substring(4, 2), 16);
                return Color.FromArgb(255, r, g, b);
            }
            else if (hex.Length == 8)
            {
                var a = Convert.ToByte(hex.Substring(0, 2), 16);
                var r = Convert.ToByte(hex.Substring(2, 2), 16);
                var g = Convert.ToByte(hex.Substring(4, 2), 16);
                var b = Convert.ToByte(hex.Substring(6, 2), 16);
                return Color.FromArgb(a, r, g, b);
            }
        }
        catch
        {
            // Ignore parse errors and fallback
        }

        return RyTuneXBrandColor;
    }

    public static string ColorToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private async Task<AccentColorMode> LoadAccentColorModeFromSettingsAsync()
    {
        var modeName = await _localSettingsService.ReadSettingAsync<string>(SettingsKeyMode);
        if (Enum.TryParse<AccentColorMode>(modeName, out var mode))
        {
            return mode;
        }

        return AccentColorMode.RyTuneX;
    }

    private async Task<Color> LoadCustomColorFromSettingsAsync()
    {
        var hex = await _localSettingsService.ReadSettingAsync<string>(SettingsKeyCustomColor);
        return ColorFromHex(hex);
    }
}
