using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using RyTuneX.Contracts.Services;
using RyTuneX.Helpers;
using RyTuneX.Models;
using RyTuneX.ViewModels;
using Windows.Storage;
using Windows.System;
using TitleBar = Microsoft.UI.Xaml.Controls.TitleBar;

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

    public ShellPage(ShellViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        this.FlowDirection = App.FlowDirectionSetting;

        Current = this;
        LogHelper.Log("Initializing ShellPage");
        ViewModel.NavigationService.Frame = NavigationFrame;
        ViewModel.NavigationViewService.Initialize(NavigationViewControl);
        App.MainWindow.ExtendsContentIntoTitleBar = true;
        App.MainWindow.SetTitleBar(AppTitleBar);
        App.MainWindow.Activated += MainWindow_Activated;
        TitleBarSearchBox.PlaceholderText = "Search".GetLocalized();

        var savedStyle = ApplicationData.Current.LocalSettings.Values["NavigationStyle"] as string ?? "Auto";
        ApplyNavigationStyle(savedStyle);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        TitleBarHelper.UpdateTitleBar(RequestedTheme);

        // Initialize search cache without blocking the UI thread
        WarmUpSearchCache();

        // Show restore point dialog if it's the first run
        _ = Task.Run(async () =>
        {
            try
            {
                var settings = ApplicationData.Current.LocalSettings.Values;
                if (!settings.ContainsKey("FirstRun") || (bool)settings["FirstRun"])
                {
                    if (!DispatcherQueue.TryEnqueue(async () =>
                    {
                        try
                        {
                            await ShowRestorePointDialogAsync();
                        }
                        catch (Exception ex)
                        {
                            _ = LogHelper.LogException(ex, "Restore point dialog failed");
                        }
                    }))
                    {
                        _ = LogHelper.LogWarning("Restore point dialog dispatcher enqueue failed.");
                    }
                }
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogException(ex, "Restore point first-run check failed");
            }
        });
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
                _ = LogHelper.LogError($"Search cache warmup failed: {ex.Message}");
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

    private async void NavigateToSearchResult(SearchableItem item)
    {
        try
        {
            var navigationService = App.GetService<INavigationService>();

            // Navigate to the page, passing the option tag as parameter if available
            navigationService.NavigateTo(item.PageTypeName, item.OptionTag);

            _ = LogHelper.Log($"Search navigation to: {item.PageTypeName}, Option: {item.OptionTag ?? "none"}");
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error navigating to search result: {ex.Message}");
        }
    }

    private void AppTitleBar_BackRequested(TitleBar sender, object args)
    {
        ViewModel.NavigationService.GoBack();
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        App.AppTitlebar = AppTitleBar;
    }

    private void NavigationViewControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
    {
        if (sender.PaneDisplayMode == NavigationViewPaneDisplayMode.Top)
        {
            TitleBarSearchBox.Visibility = Visibility.Visible;
        }
        else
        {
            TitleBarSearchBox.Visibility = sender.DisplayMode == NavigationViewDisplayMode.Minimal
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }

    // Applies the navigation pane display mode and adjusts the title bar accordingly.
    public void ApplyNavigationStyle(string style)
    {
        if (style == "Top")
        {
            NavigationViewControl.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
        }
        else
        {
            NavigationViewControl.PaneDisplayMode = NavigationViewPaneDisplayMode.Auto;
        }
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
            RequestedTheme = ActualTheme,
            Title = "RestorePointTitle".GetLocalized(),
            Content = new StackPanel
            {
                RequestedTheme = ActualTheme,
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
            PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"],
            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
            BorderBrush = (SolidColorBrush)Application.Current.Resources["AccentAAFillColorDefaultBrush"],
        };

        var xamlRoot = XamlRoot ?? (Content as FrameworkElement)?.XamlRoot;
        if (xamlRoot is null)
        {
            _ = LogHelper.LogWarning("Restore point dialog skipped because XamlRoot is unavailable.");
            return;
        }

        dialog.XamlRoot = xamlRoot;

        _ = LogHelper.Log("Showing Restore Point Dialog");
        var result = await dialog.ShowAsync();
        if (neverShowAgain.IsChecked == true)
        {
            ApplicationData.Current.LocalSettings.Values["FirstRun"] = false;
        }
        if (result == ContentDialogResult.Primary)
        {
            try
            {
                _ = LogHelper.Log("Opening SystemPropertiesProtection");
                await OptimizationOptions.StartInCmd("SystemPropertiesProtection");
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Failed to open System Properties Protection: {ex.Message}");
            }
        }
    }

    public static void ShowNotification(string title, string message, InfoBarSeverity severity, int duration = 3000)
    {
        App.NotifyTaskCompletion();

        void Show()
        {
            var growlInfo = new GrowlInfo
            {
                Title = title,
                Message = message,
                ShowDateTime = true,
                StaysOpen = duration == 0,
                IsClosable = true,
                UseBlueColorForInfo = false,
                Token = "MainToken",
                WaitTime = TimeSpan.FromMilliseconds(duration)
            };
            try
            {
                switch (severity)
                {
                    case InfoBarSeverity.Informational:
                        growlInfo.UseBlueColorForInfo = true;
                        Growl.Info(growlInfo);
                        break;
                    case InfoBarSeverity.Success:
                        Growl.Success(growlInfo);
                        break;
                    case InfoBarSeverity.Warning:
                        Growl.Warning(growlInfo);
                        break;
                    case InfoBarSeverity.Error:
                        Growl.Error(growlInfo);
                        break;
                    default:
                        Growl.Info(growlInfo);
                        break;
                }
                _ = LogHelper.Log($"Showing notification: {title} - {message}");
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Failed to show notification: {ex.Message}");
            }
        }

        if (Current?.DispatcherQueue.HasThreadAccess == true)
        {
            Show();
        }
        else
        {
            if (Current?.DispatcherQueue.TryEnqueue(Show) != true)
            {
                _ = LogHelper.LogWarning("Notification dispatcher enqueue failed.");
            }
        }
    }
}
