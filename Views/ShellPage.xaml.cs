using DevWinUI;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using RyTuneX.Contracts.Services;
using RyTuneX.Helpers;
using RyTuneX.Models;
using RyTuneX.ViewModels;
using System.Security.Principal;
using Windows.Foundation;
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

    public ShellPage(ShellViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        this.FlowDirection = App.FlowDirectionSetting;

        TitleBarBackButton.Resources["ButtonBackgroundDisabled"] = new SolidColorBrush(Colors.Transparent);
        TitleBarBackButton.Resources["ButtonBorderBrushDisabled"] = new SolidColorBrush(Colors.Transparent);

        // Set corresponding visibility of Admin Icon based on administrator rights
        this.Loaded += (s, e) =>
        {
            _ = Task.Run(() =>
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                var admin = principal.IsInRole(WindowsBuiltInRole.Administrator);

                DispatcherQueue.TryEnqueue(() =>
                {
                    IsAdminIcon.Visibility = admin ? Visibility.Visible : Visibility.Collapsed;
                    NotAdminIcon.Visibility = admin ? Visibility.Collapsed : Visibility.Visible;
                });
            });
        };

        Current = this;
        LogHelper.Log("Initializing ShellPage");
        ViewModel.NavigationService.Frame = NavigationFrame;
        ViewModel.NavigationViewService.Initialize(NavigationViewControl);
        AppTitleBarVersion.Text = SettingsPage.GetVersionDescription();
        App.MainWindow.ExtendsContentIntoTitleBar = true;
        App.MainWindow.SetTitleBar(AppTitleBar);
        App.MainWindow.Activated += MainWindow_Activated;

        var savedStyle = ApplicationData.Current.LocalSettings.Values["NavigationStyle"] as string ?? "Auto";
        ApplyNavigationStyle(savedStyle);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        TitleBarHelper.UpdateTitleBar(RequestedTheme);

        SetTitleBarPassthroughRegions();
        TitleBarBackButton.SizeChanged += (_, _) => SetTitleBarPassthroughRegions();
        TitleBarSearchBox.SizeChanged += (_, _) => SetTitleBarPassthroughRegions();

        // Initialize search cache without blocking the UI thread
        WarmUpSearchCache();

        // Show restore point dialog if it's the first run
        _ = Task.Run(async () =>
        {
            var settings = ApplicationData.Current.LocalSettings.Values;
            if (!settings.ContainsKey("FirstRun") || (bool)settings["FirstRun"])
            {
                DispatcherQueue.TryEnqueue(async () => await ShowRestorePointDialogAsync());
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

    private void TitleBarBackButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NavigationService.GoBack();
    }

    private void TitleBarBackButton_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        AnimatedIcon.SetState(BackAnimatedIcon, "PointerOver");
    }

    private void TitleBarBackButton_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        AnimatedIcon.SetState(BackAnimatedIcon, "Normal");
    }

    private void SetTitleBarPassthroughRegions()
    {
        if (AppTitleBar.XamlRoot is null)
        {
            return;
        }

        var scaleAdjustment = AppTitleBar.XamlRoot.RasterizationScale;
        var nonClientSrc = InputNonClientPointerSource.GetForWindowId(App.MainWindow.AppWindow.Id);

        var rects = new List<Windows.Graphics.RectInt32>();

        // Back button passthrough
        var backTransform = TitleBarBackButton.TransformToVisual(null);
        var backBounds = backTransform.TransformBounds(new Rect(0, 0, TitleBarBackButton.ActualWidth, TitleBarBackButton.ActualHeight));
        rects.Add(ScaleRect(backBounds, scaleAdjustment));

        // Search box passthrough
        if (TitleBarSearchBox.Visibility == Visibility.Visible)
        {
            var searchTransform = TitleBarSearchBox.TransformToVisual(null);
            var searchBounds = searchTransform.TransformBounds(new Rect(0, 0, TitleBarSearchBox.ActualWidth, TitleBarSearchBox.ActualHeight));
            rects.Add(ScaleRect(searchBounds, scaleAdjustment));
        }

        // WS_EX_LAYOUTRTL mirrors Win32 input coordinates but TransformToVisual
        // returns compositor coordinates (unmirrored). Mirror the passthrough rects
        // so they match the coordinate space the system uses for hit-testing.
        if (App.FlowDirectionSetting == FlowDirection.RightToLeft)
        {
            var windowWidthPx = App.MainWindow.AppWindow.Size.Width;
            for (var i = 0; i < rects.Count; i++)
            {
                var r = rects[i];
                rects[i] = new Windows.Graphics.RectInt32(
                    windowWidthPx - r.X - r.Width, r.Y, r.Width, r.Height);
            }
        }

        nonClientSrc.SetRegionRects(NonClientRegionKind.Passthrough, rects.ToArray());
    }

    private static Windows.Graphics.RectInt32 ScaleRect(Rect bounds, double scale)
    {
        return new Windows.Graphics.RectInt32(
            (int)Math.Round(bounds.X * scale),
            (int)Math.Round(bounds.Y * scale),
            (int)Math.Round(bounds.Width * scale),
            (int)Math.Round(bounds.Height * scale));
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        App.AppTitlebar = AppTitleBarText;
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
            Current?.DispatcherQueue.TryEnqueue(Show);
        }
    }
}