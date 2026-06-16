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
using RyTuneX.Helpers;
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

    // P/Invoke for RTL caption buttons
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_LAYOUTRTL = 0x00400000;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowLongPtr(IntPtr hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static MediaPlayer? _notificationPlayer;

    // Flashes the taskbar icon to alert the user that a background process is complete
    public static void NotifyTaskCompletion()
    {
        void Execute()
        {
            try
            {
                var hwnd = WindowNative.GetWindowHandle(MainWindow);
                if (hwnd == IntPtr.Zero)
                {
                    return;
                }

                // Skip flash and sound when the window is already in the foreground
                if (GetForegroundWindow() == hwnd)
                {
                    return;
                }

                var fInfo = new FLASHWINFO
                {
                    cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
                    hwnd = hwnd,
                    dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG,
                    uCount = uint.MaxValue,
                    dwTimeout = 0
                };

                FlashWindowEx(ref fInfo);

                try
                {
                    _notificationPlayer ??= new MediaPlayer();
                    _notificationPlayer.Source = MediaSource.CreateFromUri(new Uri("ms-winsoundevent:Notification.Default"));
                    _notificationPlayer.Play();
                }
                catch (Exception ex)
                {
                    _ = LogHelper.LogWarning($"Failed to play notification sound: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Failed to flash window: {ex.Message}");
            }
        }

        var dispatcher = MainWindow.DispatcherQueue;
        if (dispatcher?.HasThreadAccess == true)
        {
            Execute();
            return;
        }

        if (dispatcher is null || !dispatcher.TryEnqueue(Execute))
        {
            Execute();
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

    private static readonly object MainWindowLock = new();
    private static WindowEx? _mainWindow;

    public static WindowEx MainWindow
    {
        get
        {
            if (_mainWindow is not null)
            {
                return _mainWindow;
            }

            lock (MainWindowLock)
            {
                return _mainWindow ??= CreateMainWindow();
            }
        }
    }

    public static UIElement? AppTitlebar
    {
        get; set;
    }

    private static WindowEx CreateMainWindow()
    {
        try
        {
            return new MainWindow();
        }
        catch (Exception ex)
        {
            LogHelper.LogCriticalSync($"MainWindow creation failed: {ex}");
            throw;
        }
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
        try
        {
            // Initialize the Host when needed
            _host ??= BuildHost();

            base.OnLaunched(args);

            // Apply RTL window style BEFORE title bar setup so the framework
            // calculates caption button positions in the correct coordinate space
            if (FlowDirectionSetting == Microsoft.UI.Xaml.FlowDirection.RightToLeft)
            {
                try
                {
                    var hwnd = WindowNative.GetWindowHandle(MainWindow);
                    var exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
                    SetWindowLongPtr(hwnd, GWL_EXSTYLE, exStyle | WS_EX_LAYOUTRTL);
                    SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
                }
                catch (Exception ex)
                {
                    _ = LogHelper.LogError($"RTL caption buttons failed: {ex.Message}");
                }
            }

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
            StartSystemStateSync();
        }
        catch (Exception ex)
        {
            LogHelper.LogCriticalSync($"OnLaunched failed: {ex}");
            throw;
        }
    }

    // Fires once at launch to seed the RyTuneX registry with the real system state.
    private static void StartSystemStateSync()
    {
        _ = Task.Run(() =>
        {
            try
            {
                SystemStateDetector.SyncToRegistry();
            }
            catch (Exception ex)
            {
                LogHelper.LogCriticalSync($"System state sync failed: {ex}");
            }
        });
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

            try
            {
                if (ApplicationData.Current.LocalSettings.Values.TryGetValue("SelectedLanguage", out var langObj)
                    && langObj is string lang
                    && (lang.StartsWith("ar", StringComparison.OrdinalIgnoreCase) ||
                        lang.StartsWith("he", StringComparison.OrdinalIgnoreCase)))
                {
                    _flowDirectionCache = Microsoft.UI.Xaml.FlowDirection.RightToLeft;
                }
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogWarning($"Failed to read flow direction setting: {ex.Message}");
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
