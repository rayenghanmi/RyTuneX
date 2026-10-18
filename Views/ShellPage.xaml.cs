using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
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
        NavigationFrame.CacheSize = 20;
        ViewModel.NavigationService.Frame = NavigationFrame;
        ViewModel.NavigationViewService.Initialize(NavigationViewControl);
        App.MainWindow.ExtendsContentIntoTitleBar = true;
        App.MainWindow.SetTitleBar(AppTitleBar);
        App.MainWindow.Activated += MainWindow_Activated;
        AppTitleBar.SizeChanged += AppTitleBar_SizeChanged;
        NavigationFrame.Navigated += NavigationFrame_Navigated;
        TitleBarSearchBox.PlaceholderText = "Search".GetLocalized();

        var savedStyle = ApplicationData.Current.LocalSettings.Values["NavigationStyle"] as string ?? "Auto";
        ApplyNavigationStyle(savedStyle);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        TitleBarHelper.UpdateTitleBar(RequestedTheme);
        UpdateTitleBarCentering();

        // Initialize search cache without blocking the UI thread
        WarmUpSearchCache();

        // Show restore point dialog if it's the first run, or system update suggestion
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(1500);

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
                else if (SystemUpdateHelper.IsUpdatePending && SystemUpdateHelper.PendingUpdateResult != null)
                {
                    if (!DispatcherQueue.TryEnqueue(async () =>
                    {
                        try
                        {
                            await ShowSystemUpdateDialogAsync(SystemUpdateHelper.PendingUpdateResult);
                        }
                        catch (Exception ex)
                        {
                            _ = LogHelper.LogException(ex, "System update dialog failed");
                        }
                    }))
                    {
                        _ = LogHelper.LogWarning("System update dialog dispatcher enqueue failed.");
                    }
                }
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogException(ex, "Startup dialog checks failed");
            }
        });
    }

    private static void WarmUpSearchCache()
    {
        _ = Task.Run(() =>
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
        TitleBarSearchBox.Visibility = Visibility.Visible;
    }

    private void AppTitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateTitleBarCentering();
    }

    private void NavigationFrame_Navigated(object sender, NavigationEventArgs e)
    {
        DispatcherQueue.TryEnqueue(UpdateTitleBarCentering);
    }

    private void UpdateTitleBarCentering()
    {
        try
        {
            if (FlowDirection == FlowDirection.RightToLeft)
            {
                return;
            }

            if (AppTitleBar == null || TitleBarContentGrid == null || TitleBarLeftHeaderContainer == null)
            {
                return;
            }

            if (!AppTitleBar.IsLoaded || !TitleBarContentGrid.IsLoaded || !TitleBarLeftHeaderContainer.IsLoaded)
            {
                return;
            }

            if (AppTitleBar.ActualWidth <= 0 || TitleBarContentGrid.ActualWidth <= 0)
            {
                return;
            }

            // Measure the right inset (caption buttons column width)
            var contentTransform = TitleBarContentGrid.TransformToVisual(AppTitleBar);
            var contentOrigin = contentTransform.TransformPoint(new Windows.Foundation.Point(0, 0));
            var rightInset = AppTitleBar.ActualWidth - (contentOrigin.X + TitleBarContentGrid.ActualWidth);

            if (rightInset <= 0)
            {
                return;
            }

            // Measure offset of LeftHeader from left edge
            var headerTransform = TitleBarLeftHeaderContainer.TransformToVisual(AppTitleBar);
            var headerOrigin = headerTransform.TransformPoint(new Windows.Foundation.Point(0, 0));
            var leftOffset = Math.Max(0.0, headerOrigin.X);

            // Minimum width needed for the icon + app name text
            var baseWidth = TitleBarLeftHeaderContent?.ActualWidth > 0
                ? TitleBarLeftHeaderContent.ActualWidth + 10.0
                : 104.0;

            // When window is wide enough, balance left column with right caption buttons column
            double targetWidth;
            if (AppTitleBar.ActualWidth >= 640.0)
            {
                targetWidth = Math.Max(baseWidth, rightInset - leftOffset);
            }
            else
            {
                targetWidth = baseWidth;
            }

            if (Math.Abs(TitleBarLeftHeaderContainer.Width - targetWidth) > 0.5)
            {
                TitleBarLeftHeaderContainer.Width = targetWidth;
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogWarning($"TitleBar centering update skipped: {ex.Message}");
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

    private async Task ShowSystemUpdateDialogAsync(SystemUpdateCheckResult updateResult)
    {
        var xamlRoot = XamlRoot ?? (Content as FrameworkElement)?.XamlRoot ?? App.MainWindow.Content?.XamlRoot;
        if (xamlRoot is null)
        {
            _ = LogHelper.LogWarning("System update dialog skipped because XamlRoot is unavailable.");
            return;
        }

        int totalSaved = OptimizationOptions.GetSavedOptimizationsCount();

        string contentText = string.Format(
            "SystemUpdate_DialogContent".TryGetLocalized() ?? "Windows was updated from build {0} to {1}.\n\nWindows updates frequently reset system tweaks back to default values.\n\nWould you like RyTuneX to re-apply all your saved optimizations, features, and privacy settings ({2} items)?",
            updateResult.PreviousBuild.FullBuildString,
            updateResult.CurrentBuild.FullBuildString,
            totalSaved);

        var dialog = new ContentDialog
        {
            RequestedTheme = ActualTheme,
            Title = "SystemUpdate_DialogTitle".TryGetLocalized() ?? "Windows Update Detected",
            Content = new StackPanel
            {
                RequestedTheme = ActualTheme,
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = contentText,
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            },
            PrimaryButtonText = "SystemUpdate_ReapplyButton".TryGetLocalized() ?? "Re-apply All Settings",
            CloseButtonText = "Close".GetLocalized(),
            DefaultButton = ContentDialogButton.None,
            PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"],
            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
            BorderBrush = (SolidColorBrush)Application.Current.Resources["AccentAAFillColorDefaultBrush"],
        };

        dialog.XamlRoot = xamlRoot;

        _ = LogHelper.Log($"[SystemUpdateHelper] Showing system update suggestion dialog ({updateResult.PreviousBuild.FullBuildString} -> {updateResult.CurrentBuild.FullBuildString})");
        var result = await dialog.ShowAsync();

        try
        {
            if (result == ContentDialogResult.Primary)
            {
                ShowNotification(
                    "SystemUpdate_DialogTitle".TryGetLocalized() ?? "Windows Update Detected",
                    "SystemUpdate_ReapplyingMessage".TryGetLocalized() ?? "Currently re-applying optimizations, features, and privacy settings...",
                    InfoBarSeverity.Informational,
                    3000);

                var appliedCount = await Task.Run(() => OptimizationOptions.ReapplyAllOptimizationsAsync());

                string successMsg = string.Format(
                    "SystemUpdate_ReappliedSuccess".TryGetLocalized() ?? "Successfully re-applied {0} setting(s).",
                    appliedCount);

                ShowNotification(
                    "RyTuneX",
                    successMsg,
                    InfoBarSeverity.Success,
                    5000);
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Failed to re-apply settings: {ex.Message}");
        }
        finally
        {
            SystemUpdateHelper.SaveWindowsBuild(updateResult.CurrentBuild);
            SystemUpdateHelper.IsUpdatePending = false;
            SystemUpdateHelper.PendingUpdateResult = null;
            _ = Task.Run(() => SystemStateDetector.SyncToRegistry());
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
