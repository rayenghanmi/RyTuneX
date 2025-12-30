using System.Diagnostics;
using System.Security.Principal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using RyTuneX.Contracts.Services;
using RyTuneX.Helpers;
using RyTuneX.Models;
using RyTuneX.ViewModels;
using Windows.Storage;
using Windows.System;

namespace RyTuneX.Views;

public sealed partial class ShellPage : Page
{
    public static ShellPage? Current
    {
        get; private set;
    }
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
        IsAdminIcon.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        NotAdminIcon.Visibility = isAdmin ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        TitleBarHelper.UpdateTitleBar(RequestedTheme);

        KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu));
        KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.GoBack));

        // Add Ctrl+K keyboard accelerator for search focus
        var searchAccelerator = new KeyboardAccelerator
        {
            Key = VirtualKey.K,
            Modifiers = VirtualKeyModifiers.Control
        };
        searchAccelerator.Invoked += (s, args) =>
        {
            TitleBarSearchBox.Focus(FocusState.Programmatic);
            args.Handled = true;
        };
        KeyboardAccelerators.Add(searchAccelerator);

        // Initialize search cache without blocking the UI thread
        WarmUpSearchCache();

        // Show restore point dialog if it's the first run
        if (!ApplicationData.Current.LocalSettings.Values.ContainsKey("FirstRun"))
        {
            DispatcherQueue.TryEnqueue(async () => await ShowRestorePointDialogAsync());
        }
        else
        {
            // Show restore point dialog if the setting is set to true
            if ((bool)ApplicationData.Current.LocalSettings.Values["FirstRun"] != false)
            {
                DispatcherQueue.TryEnqueue(async () => await ShowRestorePointDialogAsync());
            }
        }
    }

    private static void WarmUpSearchCache()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                AppSearchService.InitializeCache();
            }
            catch (Exception ex)
            {
                await LogHelper.LogError($"Search cache warmup failed: {ex.Message}");
            }
        });
    }

    private void TitleBarSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var query = sender.Text;
            if (string.IsNullOrWhiteSpace(query))
            {
                sender.ItemsSource = null;
                return;
            }

            var results = AppSearchService.Search(query).ToList();
            sender.ItemsSource = results;
        }
    }

    private void TitleBarSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        // TextMemberPath handles text update automatically
        // This handler can be used for any additional actions when a suggestion is highlighted
    }

    private void TitleBarSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        SearchableItem? selectedItem = null;

        if (args.ChosenSuggestion is SearchableItem item)
        {
            selectedItem = item;
        }
        else if (!string.IsNullOrWhiteSpace(args.QueryText))
        {
            // If user pressed Enter without selecting, pick the first result
            var results = AppSearchService.Search(args.QueryText).ToList();
            selectedItem = results.FirstOrDefault();
        }

        if (selectedItem != null)
        {
            NavigateToSearchResult(selectedItem);
        }

        // Clear the search box after navigation
        sender.Text = string.Empty;
        sender.ItemsSource = null;
    }

    private void NavigateToSearchResult(SearchableItem item)
    {
        try
        {
            var pageType = Type.GetType(item.PageTypeName);
            if (pageType != null)
            {
                var navigationService = App.GetService<INavigationService>();

                // Navigate to the page, passing the option tag as parameter if available
                navigationService.NavigateTo(item.PageTypeName, item.OptionTag);

                LogHelper.Log($"Search navigation to: {item.PageTypeName}, Option: {item.OptionTag ?? "none"}");
            }
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"Error navigating to search result: {ex.Message}");
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

        // Adjust search box visibility based on display mode
        TitleBarSearchBox.Visibility = sender.DisplayMode == NavigationViewDisplayMode.Minimal
            ? Visibility.Collapsed
            : Visibility.Visible;
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