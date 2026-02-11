using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using RyTuneX.Contracts.Services;
using RyTuneX.Helpers;
using RyTuneX.Models;
using RyTuneX.ViewModels;
using System.Diagnostics;
using System.Security.Principal;
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
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/rayenghanmi/rytunex/issues/new",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Failed to open issue page: {ex.Message}");
        }
    }

    private void SupportButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://buymeacoffee.com/rayen.ghanmi.22",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Failed to open support page: {ex.Message}");
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
                StaysOpen = false,
                IsClosable = true,
                Token = "MainToken",
                WaitTime = TimeSpan.FromMilliseconds(duration)
            };
            try
            {
                switch (severity)
                {
                    case InfoBarSeverity.Informational:
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