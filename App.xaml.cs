/*
 * RyTuneX - A Windows 10 and 11 optimizer designed to enhance system performance.
 * Copyright (C) 2023 Rayen Ghanmi
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 *
 * Contact: ghanmirayen12@gmail.com
 */

using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using RyTuneX.Activation;
using RyTuneX.Contracts.Services;
using RyTuneX.Core.Contracts.Services;
using RyTuneX.Core.Services;
using RyTuneX.Models;
using RyTuneX.Services;
using RyTuneX.ViewModels;
using RyTuneX.Views;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using WinRT.Interop;

namespace RyTuneX;

public partial class App : Application
{
    // P/Invoke for Taskbar Flashing
    [StructLayout(LayoutKind.Sequential)]
    private struct FLASHWINFO
    {
        public uint cbSize;
        public IntPtr hwnd;
        public uint dwFlags;
        public uint uCount;
        public uint dwTimeout;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

    private const uint FLASHW_ALL = 3;           // Flash both window caption and taskbar button
    private const uint FLASHW_TIMERNOFG = 12;    // Flash continuously until the window comes to the foreground

    // Flashes the taskbar icon to alert the user that a background process is complete
    public static void NotifyTaskCompletion()
    {
        try
        {
            var hwnd = WindowNative.GetWindowHandle(MainWindow);
            if (hwnd == IntPtr.Zero) return;

            FLASHWINFO fInfo = new FLASHWINFO();
            fInfo.cbSize = (uint)Marshal.SizeOf(fInfo);
            fInfo.hwnd = hwnd;
            fInfo.dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG;
            fInfo.uCount = uint.MaxValue; // Flash until focused
            fInfo.dwTimeout = 0;          // Use default cursor blink rate

            FlashWindowEx(ref fInfo);

            // Play a system notification sound
            var mediaPlayer = new MediaPlayer();
            mediaPlayer.Source = MediaSource.CreateFromUri(new Uri("ms-winsoundevent:Notification.Default"));
            mediaPlayer.Play();

        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Failed to flash window: {ex.Message}");
        }
    }

    private IHost? _host;
    public IHost Host => _host ?? throw new InvalidOperationException("Host accessed before initialization.");

    public static T GetService<T>() where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    private static Microsoft.UI.Xaml.FlowDirection? _flowDirectionCache;

    public static void ShowNotification(string title, string message, Microsoft.UI.Xaml.Controls.InfoBarSeverity severity, int duration) =>
        ShellPage.ShowNotification(title, message, severity, duration);

    public static WindowEx MainWindow { get; } = new MainWindow();

    public static UIElement? AppTitlebar
    {
        get; set;
    }

    public App()
    {
        InitializeComponent();
        LogHelper.Log("___________ New Session ___________");

        // Catch unhandled exceptions early to avoid silent activation failures
        UnhandledException += App_UnhandledException;
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Initialize the Host when needed
        _host ??= BuildHost();

        base.OnLaunched(args);

        // setting custom title bar when the app starts to prevent it from briefly show the standard titlebar
        try
        {
            MainWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            MainWindow.AppWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"TitleBar init failed: {ex}");
        }

        await App.GetService<IActivationService>().ActivateAsync(args);
    }

    private IHost BuildHost() => Microsoft.Extensions.Hosting.Host.
    CreateDefaultBuilder().
       UseContentRoot(AppContext.BaseDirectory).
       ConfigureServices((context, services) =>
       {
           // Default Activation Handler
           services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

           // Services
           services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
           services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
           services.AddTransient<INavigationViewService, NavigationViewService>();

           services.AddSingleton<IActivationService, ActivationService>();
           services.AddSingleton<IPageService, PageService>();
           services.AddSingleton<INavigationService, NavigationService>();

           // Core Services
           services.AddSingleton<IFileService, FileService>();

           // Views and ViewModels
           services.AddTransient<SettingsPage>();
           services.AddTransient<OptimizeSystemPage>();
           services.AddTransient<SystemInfoPage>();
           services.AddTransient<DebloatSystemPage>();
           services.AddTransient<HomePage>();
           services.AddTransient<NetworkPage>();
           services.AddTransient<SecurityPage>();
           services.AddTransient<RepairPage>();
           services.AddTransient<ProcessesPage>();
           services.AddTransient<ServicesPage>();
           services.AddTransient<ShellPage>();
           services.AddTransient<ShellViewModel>();

           // Configuration
           services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
       }).
       Build();

    // Sets flow direction based on the current language.
    public static Microsoft.UI.Xaml.FlowDirection FlowDirectionSetting
    {
        get
        {
            // Return cached value if we already have it
            if (_flowDirectionCache.HasValue) return _flowDirectionCache.Value;

            // Fallback: Read from disk only once
            _flowDirectionCache = Microsoft.UI.Xaml.FlowDirection.LeftToRight;

            if (ApplicationData.Current.LocalSettings.Values.TryGetValue("SelectedLanguage", out var langObj)
                && langObj is string lang)
            {
                if (lang.StartsWith("ar", StringComparison.OrdinalIgnoreCase) ||
                    lang.StartsWith("he", StringComparison.OrdinalIgnoreCase))
                {
                    _flowDirectionCache = Microsoft.UI.Xaml.FlowDirection.RightToLeft;
                }
            }
            return _flowDirectionCache.Value;
        }
    }

    private async void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        await LogHelper.LogError($"App_UnhandledException: {e.Exception}");
    }
}
