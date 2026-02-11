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
using System.Runtime.InteropServices;
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

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    private const uint FLASHW_ALL = 3;           // Flash both window caption and taskbar button
    private const uint FLASHW_TIMERNOFG = 12;    // Flash continuously until the window comes to the foreground

    // Flashes the taskbar icon to alert the user that a background process is complete
    public static void NotifyTaskCompletion()
    {
        try
        {
            var hwnd = WindowNative.GetWindowHandle(MainWindow);
            if (hwnd == IntPtr.Zero) return;

            // Skip flash and sound when the window is already in the foreground
            if (GetForegroundWindow() == hwnd) return;

            var fInfo = new FLASHWINFO();
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

        // Catch unhandled exceptions early to avoid silent activation failures
        UnhandledException += App_UnhandledException;

        // Catch unobserved Task exceptions (fire-and-forget tasks that throw)
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        // Catch truly fatal AppDomain exceptions (native crashes, thread aborts, etc.)
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        LogHelper.Log("___________ New Session ___________");
        LogHelper.Log($"App version: {Views.SettingsPage.GetVersionDescription()}, OS: {Environment.OSVersion}, Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
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
        // Mark as handled to prevent immediate crash and allow the log to be written
        e.Handled = true;
        try
        {
            await LogHelper.LogException(e.Exception, "UnhandledException (XAML)");
        }
        catch
        {
            LogHelper.LogCriticalSync($"UnhandledException (XAML): {e.Exception}");
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // Mark observed so the process is not terminated
        e.SetObserved();
        try
        {
            _ = LogHelper.LogException(e.Exception, "UnobservedTaskException");
        }
        catch
        {
            LogHelper.LogCriticalSync($"UnobservedTaskException: {e.Exception}");
        }
    }

    private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        // This fires for fatal exceptions; use sync logging because the process may terminate immediately
        var ex = e.ExceptionObject as Exception;
        LogHelper.LogCriticalSync($"AppDomain.UnhandledException (IsTerminating={e.IsTerminating}): {ex}");
    }
}
