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

    public string UserName { get; } = Environment.UserName;
    public string AccountType =>
        string.Equals(Environment.UserDomainName, "MicrosoftAccount", StringComparison.OrdinalIgnoreCase)
            ? "MicrosoftAccount".GetLocalized()
            : "LocalAccount".GetLocalized();

    // Track pointer state to defer hide animation while hovered
    private bool _isPointerOver = false;
    private bool _pendingHide = false;

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

        // Attach pointer handlers for pausing/resuming notifications
        Loaded += ShellPage_Loaded;
    }

    private void UserProfileButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:account",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Failed to open account settings: {ex.Message}");
        }
    }

    private void ShellPage_Loaded(object? sender, RoutedEventArgs e)
    {
        // Pointer events to pause/resume animations and the notification queue
        infoBar.PointerEntered -= InfoBar_PointerEntered;
        infoBar.PointerEntered += InfoBar_PointerEntered;
        infoBar.PointerExited -= InfoBar_PointerExited;
        infoBar.PointerExited += InfoBar_PointerExited;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        TitleBarHelper.UpdateTitleBar(RequestedTheme);

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
            var pageType = Type.GetType(item.PageTypeName);
            if (pageType != null)
            {
                var navigationService = App.GetService<INavigationService>();

                // Navigate to the page, passing the option tag as parameter if available
                navigationService.NavigateTo(item.PageTypeName, item.OptionTag);

                _ = LogHelper.Log($"Search navigation to: {item.PageTypeName}, Option: {item.OptionTag ?? "none"}");
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error navigating to search result: {ex.Message}");
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

    public static void ShowNotification(string title, string message, InfoBarSeverity severity, int duration)
    {
        App.NotifyTaskCompletion();
        Current?.ShowNotificationInstance(title, message, severity, duration);
    }

    private Storyboard? GetStoryboard(string key)
    {
        if (infoBar.Resources.TryGetValue(key, out var obj) && obj is Storyboard sb)
        {
            return sb;
        }
        return null;
    }

    private void ShowNotificationInstance(string title, string message, InfoBarSeverity severity, int duration)
    {
        // Make visible and show notification content via behavior
        infoBar.Visibility = Visibility.Visible;

        NotificationQueue.Show(new CommunityToolkit.WinUI.Behaviors.Notification
        {
            Title = title,
            Message = message,
            Severity = severity,
            Duration = TimeSpan.FromMilliseconds(duration)
        });

        // Start entrance animation
        GetStoryboard("ShowNotificationStoryboard")?.Begin();

        // Start progress animation and wire completion
        var progressSb = GetStoryboard("ProgressBarAnimationStoryboard");
        if (progressSb != null)
        {
            // Ensure single subscription
            progressSb.Completed -= ProgressBarAnimationStoryboard_Completed;
            progressSb.Completed += ProgressBarAnimationStoryboard_Completed;
            progressSb.Begin();
        }
    }

    private void ProgressBarAnimationStoryboard_Completed(object? sender, object e)
    {
        // If pointer is over infoBar, defer the hide until pointer exits
        if (_isPointerOver)
        {
            _pendingHide = true;
            return;
        }

        // Start hide animation
        DispatcherQueue.TryEnqueue(() =>
        {
            var hideSb = GetStoryboard("HideNotificationStoryboard");
            if (hideSb != null)
            {
                hideSb.Completed -= HideNotificationStoryboard_Completed;
                hideSb.Completed += HideNotificationStoryboard_Completed;
                hideSb.Begin();
            }
            else
            {
                FinalizeHide();
            }
        });
    }

    private void HideNotificationStoryboard_Completed(object? sender, object e)
    {
        FinalizeHide();
    }

    private void FinalizeHide()
    {
        // Stop storyboards and reset state
        GetStoryboard("ProgressBarAnimationStoryboard")?.Stop();
        GetStoryboard("ShowNotificationStoryboard")?.Stop();
        GetStoryboard("HideNotificationStoryboard")?.Stop();

        progressBar.Value = 0;
        infoBar.Visibility = Visibility.Collapsed;

        _pendingHide = false;
    }

    private void InfoBar_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOver = true;

        GetStoryboard("ProgressBarAnimationStoryboard")?.Pause();
        GetStoryboard("ShowNotificationStoryboard")?.Pause();
        GetStoryboard("HideNotificationStoryboard")?.Pause();
    }

    private void InfoBar_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOver = false;

        GetStoryboard("ProgressBarAnimationStoryboard")?.Resume();
        GetStoryboard("ShowNotificationStoryboard")?.Resume();
        GetStoryboard("HideNotificationStoryboard")?.Resume();

        if (_pendingHide)
        {
            var hideSb = GetStoryboard("HideNotificationStoryboard");
            if (hideSb != null)
            {
                hideSb.Completed -= HideNotificationStoryboard_Completed;
                hideSb.Completed += HideNotificationStoryboard_Completed;
                hideSb.Begin();
            }
            else
            {
                FinalizeHide();
            }
        }
    }
}