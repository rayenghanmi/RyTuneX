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
using Windows.Storage;

namespace RyTuneX;

public partial class App : Application
{
    public IHost Host
    {
        get;
    }

    public static void ShowNotification(string title, string message, Microsoft.UI.Xaml.Controls.InfoBarSeverity severity, int duration) =>
        ShellPage.ShowNotification(title, message, severity, duration);

    public static T GetService<T>()
        where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static WindowEx MainWindow { get; } = new MainWindow();

    public static UIElement? AppTitlebar
    {
        get; set;
    }

    public App()
    {
        InitializeComponent();
        LogHelper.Log("___________ New Session ___________");
        Host = Microsoft.Extensions.Hosting.Host.
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
            services.AddTransient<ShellPage>();
            services.AddTransient<ShellViewModel>();

            // Configuration
            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
        }).
        Build();
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        // setting custom title bar when the app starts to prevent it from briefly show the standard titlebar
        MainWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        MainWindow.AppWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
        await App.GetService<IActivationService>().ActivateAsync(args);
    }

    // Sets flow direction based on the current language.

    public static Microsoft.UI.Xaml.FlowDirection FlowDirectionSetting
    {
        get
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue("SelectedLanguage", out var langObj)
                && langObj is string lang)
            {
                if (lang.StartsWith("ar", StringComparison.OrdinalIgnoreCase) ||
                    lang.StartsWith("he", StringComparison.OrdinalIgnoreCase))
                    return Microsoft.UI.Xaml.FlowDirection.RightToLeft;
            }
            return Microsoft.UI.Xaml.FlowDirection.LeftToRight;
        }
    }
}
