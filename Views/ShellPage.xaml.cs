using System.Diagnostics;
using System.Security.Principal;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using RyTuneX.Contracts.Services;
using RyTuneX.Helpers;
using RyTuneX.ViewModels;
using Windows.Storage;
using Windows.System;
using Windows.UI.ViewManagement;

namespace RyTuneX.Views;

public sealed partial class ShellPage : Page
{
    public static ShellPage? Current
    {
        get; private set;
    } // Static reference to the current ShellPage
    public ShellViewModel ViewModel
    {
        get;
    }

    readonly bool isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent())
                  .IsInRole(WindowsBuiltInRole.Administrator);
    public ShellPage(ShellViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        this.FlowDirection = App.FlowDirectionSetting;

        // Temporary fix for RTL layout issue with the overlap of NavigationViewControl and caption buttons
        if (FlowDirection == FlowDirection.RightToLeft)
        {
            NavigationViewControl.Margin = new Thickness(0, 40, 0, 0);
            AppTitleBar.Padding = new Thickness(120, 0, 0, 0);
        }

        Current = this;
        LogHelper.Log("Initializing ShellPage");
        ViewModel.NavigationService.Frame = NavigationFrame;
        ViewModel.NavigationViewService.Initialize(NavigationViewControl);
        AppTitleBarVersion.Text = SettingsPage.GetVersionDescription();
        App.MainWindow.ExtendsContentIntoTitleBar = true;
        App.MainWindow.SetTitleBar(AppTitleBar);
        App.MainWindow.Activated += MainWindow_Activated;

        // Set corresponding visibility of Admin Icon based on administrator rights

        IsAdminIcon.Visibility = isAdmin ? Microsoft.UI.Xaml.Visibility.Visible
                                     : Microsoft.UI.Xaml.Visibility.Collapsed;

        NotAdminIcon.Visibility = isAdmin ? Microsoft.UI.Xaml.Visibility.Collapsed
                                          : Microsoft.UI.Xaml.Visibility.Visible;

        // Subscribe to the ActualThemeChanged event
        this.ActualThemeChanged += ShellPage_ActualThemeChanged;

        // Update the window icon based on the current theme
        UpdateWindowIcon();
    }

    private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        TitleBarHelper.UpdateTitleBar(RequestedTheme);

        KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu));
        KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.GoBack));

        // Show restore point dialog if it's the first run
        if (!ApplicationData.Current.LocalSettings.Values.ContainsKey("FirstRun"))
        {
            DispatcherQueue.TryEnqueue(async () => await ShowRestorePointDialogAsync());
        }
        else
        {
            // Show restore point dialog if he setting is set to true
            if ((bool)ApplicationData.Current.LocalSettings.Values["FirstRun"] != false)
            {
                DispatcherQueue.TryEnqueue(async () => await ShowRestorePointDialogAsync());
            }

        }
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        App.AppTitlebar = AppTitleBarText;
    }

    private void NavigationViewControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
    {
        AppTitleBar.Margin = new Thickness()
        {
            Left = sender.CompactPaneLength * (sender.DisplayMode == NavigationViewDisplayMode.Minimal ? 2 : 1),
            Top = AppTitleBar.Margin.Top,
            Right = AppTitleBar.Margin.Right,
            Bottom = AppTitleBar.Margin.Bottom
        };
    }

    private static KeyboardAccelerator BuildKeyboardAccelerator(VirtualKey key, VirtualKeyModifiers? modifiers = null)
    {
        var keyboardAccelerator = new KeyboardAccelerator() { Key = key };

        if (modifiers.HasValue)
        {
            keyboardAccelerator.Modifiers = modifiers.Value;
        }

        keyboardAccelerator.Invoked += OnKeyboardAcceleratorInvoked;

        return keyboardAccelerator;
    }

    private static void OnKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        var navigationService = App.GetService<INavigationService>();

        var result = navigationService.GoBack();

        args.Handled = result;
    }
    private void IssueButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/rayenghanmi/rytunex/issues/new",
            UseShellExecute = true
        });
    }

    private void SupportButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://buymeacoffee.com/rayen.ghanmi.22",
            UseShellExecute = true
        });
    }

    private async Task ShowRestorePointDialogAsync()
    {
        var neverShowAgain = new CheckBox
        {
            Content = "DoNotShowCheckBox".GetLocalized(),
            Margin = new Thickness(0, 10, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };

        var dialog = new ContentDialog
        {
            Title = "RestorePointTitle".GetLocalized(),
            Content = new StackPanel
            {
                Children =
            {
                new TextBlock
                {
                    Text = "RestorePointDialogText".GetLocalized(),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                },
                neverShowAgain
            }
            },
            PrimaryButtonText = "Continue".GetLocalized(),
            CloseButtonText = "Close".GetLocalized(),
            XamlRoot = this.Content.XamlRoot,
            PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"],
            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
            BorderBrush = (SolidColorBrush)Application.Current.Resources["AccentAAFillColorDefaultBrush"],
        };
        await LogHelper.Log("Showing Restore Point Dialog");
        var result = await dialog.ShowAsync();
        if (neverShowAgain.IsChecked == true)
        {
            ApplicationData.Current.LocalSettings.Values["FirstRun"] = false;
        }
        if (result == ContentDialogResult.Primary)
        {
            try
            {
                await LogHelper.Log("Opening SystemPropertiesProtection");
                await OptimizationOptions.StartInCmd("SystemPropertiesProtection");
            }
            catch (Exception ex)
            {
                await LogHelper.LogError($"Failed to open System Properties Protection: {ex.Message}");
            }
        }
    }

    private void ShellPage_ActualThemeChanged(FrameworkElement sender, object args)
    {
        UpdateWindowIcon();
    }

    private void UpdateWindowIcon()
    {
        var themeSelectorService = App.GetService<IThemeSelectorService>();
        var theme = themeSelectorService.Theme;

        if (theme == ElementTheme.Default)
        {
            var uiSettings = new UISettings();
            var background = uiSettings.GetColorValue(UIColorType.Background);
            theme = background == Colors.White ? ElementTheme.Light : ElementTheme.Dark;
        }

        var iconPath = theme switch
        {
            ElementTheme.Light => "Assets/LightWindowIcon.ico",
            _ => "Assets/WindowIcon.ico"
        };

        ShellTitleBarImage.Source = new BitmapImage(new Uri(Path.Combine(AppContext.BaseDirectory, iconPath)));
    }

    public static void ShowNotification(string title, string message, InfoBarSeverity severity, int duration)
    {
        Current?.ShowNotificationInstance(title, message, severity, duration);
    }

    private void ShowNotificationInstance(string title, string message, InfoBarSeverity severity, int duration)
    {
        // Show notification in the NotificationQueue
        NotificationQueue.Show(new CommunityToolkit.WinUI.Behaviors.Notification
        {
            Title = title,
            Message = message,
            Severity = severity,
            Duration = TimeSpan.FromMilliseconds(duration)
        });

        // Trigger animation for showing the notification
        var showStoryboard = (Storyboard)infoBar.Resources["ShowNotificationStoryboard"];
        showStoryboard.Begin();

        // Start ProgressBar animation
        var progressBarAnimationStoryboard = (Storyboard)infoBar.Resources["ProgressBarAnimationStoryboard"];
        progressBarAnimationStoryboard.Begin();
    }
}