using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Storage.Pickers;
using RyTuneX.Helpers;
using RyTuneX.Models;
using System.Collections.ObjectModel;

namespace RyTuneX.Views;

public sealed partial class StartupPage : Page
{
    private List<StartupItem> _allStartupItems = [];
    private readonly ObservableCollection<StartupItem> _filteredStartupItems = [];
    private string _currentSort = "Name";
    private bool _sortAscending = true;
    private bool _isBusy;

    public StartupPage()
    {
        InitializeComponent();
        _ = LogHelper.Log("Initializing StartupPage");
        StartupListView.ItemsSource = _filteredStartupItems;
        Loaded += StartupPage_Loaded;
    }

    private async void StartupPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadStartupItemsAsync();
    }

    private async Task LoadStartupItemsAsync()
    {
        if (_isBusy) return;
        _isBusy = true;

        LoadingRing.IsActive = true;
        LoadingRing.Visibility = Visibility.Visible;
        StartupListView.Visibility = Visibility.Collapsed;

        try
        {
            _allStartupItems = await StartupHelper.GetStartupItemsAsync();
            UpdateSummaryCards();
            ApplyFilterAndSort();
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error loading startup items: {ex.Message}");
            App.ShowNotification("Startup Manager", $"Failed to load startup items: {ex.Message}", InfoBarSeverity.Error, 4000);
        }
        finally
        {
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
            StartupListView.Visibility = Visibility.Visible;
            _isBusy = false;
        }
    }

    private void UpdateSummaryCards()
    {
        TotalAppsText.Text = _allStartupItems.Count.ToString();
        EnabledAppsText.Text = _allStartupItems.Count(x => x.IsEnabled).ToString();
        DisabledAppsText.Text = _allStartupItems.Count(x => !x.IsEnabled).ToString();
        HighImpactAppsText.Text = _allStartupItems.Count(x => x.Impact == StartupImpact.High).ToString();
    }

    private void ApplyFilterAndSort()
    {
        var query = SearchBox.Text?.Trim().ToLowerInvariant() ?? "";
        var filterTag = (FilterComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";

        var filtered = _allStartupItems.Where(item =>
        {
            // Search query filter
            if (!string.IsNullOrEmpty(query))
            {
                var matchesName = item.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
                var matchesCommand = item.Command.Contains(query, StringComparison.OrdinalIgnoreCase);
                var matchesPublisher = item.Publisher.Contains(query, StringComparison.OrdinalIgnoreCase);
                var matchesLocation = item.Location.Contains(query, StringComparison.OrdinalIgnoreCase);

                if (!matchesName && !matchesCommand && !matchesPublisher && !matchesLocation)
                {
                    return false;
                }
            }

            // Category filter
            return filterTag switch
            {
                "Enabled" => item.IsEnabled,
                "Disabled" => !item.IsEnabled,
                "HighImpact" => item.Impact == StartupImpact.High,
                "Broken" => item.Impact == StartupImpact.Broken || !item.IsValid,
                "UserScope" => item.IsUserScope,
                "SystemScope" => !item.IsUserScope,
                _ => true
            };
        }).ToList();

        var sorted = SortItems(filtered);

        _filteredStartupItems.Clear();
        foreach (var item in sorted)
        {
            _filteredStartupItems.Add(item);
        }

        var resultsTemplate = "StartupPage_ResultsText".TryGetLocalized() ?? "Showing {0} of {1} startup items";
        ResultsText.Text = string.Format(resultsTemplate, _filteredStartupItems.Count, _allStartupItems.Count);
    }

    private List<StartupItem> SortItems(List<StartupItem> source)
    {
        return _currentSort switch
        {
            "Name" => _sortAscending ? source.OrderBy(x => x.Name).ToList() : source.OrderByDescending(x => x.Name).ToList(),
            "Publisher" => _sortAscending ? source.OrderBy(x => x.Publisher).ToList() : source.OrderByDescending(x => x.Publisher).ToList(),
            "Location" => _sortAscending ? source.OrderBy(x => x.LocationDisplay).ToList() : source.OrderByDescending(x => x.LocationDisplay).ToList(),
            "Impact" => _sortAscending ? source.OrderBy(x => x.Impact).ToList() : source.OrderByDescending(x => x.Impact).ToList(),
            "Status" => _sortAscending ? source.OrderBy(x => x.IsEnabled).ToList() : source.OrderByDescending(x => x.IsEnabled).ToList(),
            _ => source
        };
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ApplyFilterAndSort();
        }
    }

    private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
        {
            ApplyFilterAndSort();
        }
    }

    private void SortHeader_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string column)
        {
            _sortAscending = _currentSort == column && !_sortAscending;
            _currentSort = column;
            ApplyFilterAndSort();
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadStartupItemsAsync();
    }

    private async void ItemToggle_Toggled(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_isBusy) return;

            if (sender is ToggleSwitch toggle && toggle.Tag is StartupItem item)
            {
                // Ignore toggled events triggered by UI container virtualization during scrolling
                if (toggle.FocusState == FocusState.Unfocused) return;
                if (item.IsEnabled == toggle.IsOn) return;

                var newState = toggle.IsOn;
                var success = await StartupHelper.SetStartupItemEnabledAsync(item, newState);

                if (success)
                {
                    item.IsEnabled = newState;
                    UpdateSummaryCards();
                    var msg = newState
                        ? string.Format("StartupPage_Notification_ToggledMessage".TryGetLocalized() ?? "Enabled {0}", item.Name)
                        : string.Format("StartupPage_Notification_ToggledMessageDisabled".TryGetLocalized() ?? "Disabled {0}", item.Name);
                    App.ShowNotification("Startup Manager", msg, InfoBarSeverity.Success, 2500);
                }
                else
                {
                    // Revert toggle state if operation failed
                    toggle.IsOn = !newState;
                    App.ShowNotification("Startup Manager", "StartupPage_Notification_AdminRequired".TryGetLocalized() ?? "Administrator privileges required to modify system startup items.", InfoBarSeverity.Warning, 4000);
                }
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error in startup item toggle: {ex.Message}");
        }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is StartupItem item)
        {
            StartupHelper.OpenItemLocation(item);
        }
    }

    private async void ItemDetails_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is StartupItem item)
        {
            var stack = new StackPanel { Spacing = 10, Margin = new Thickness(0, 10, 0, 0) };

            void AddDetail(string label, string val)
            {
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var lblText = new TextBlock { Text = label, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Opacity = 0.7, FontSize = 12 };
                var valText = new TextBlock { Text = val, TextWrapping = TextWrapping.Wrap, FontSize = 12, IsTextSelectionEnabled = true };

                Grid.SetColumn(lblText, 0);
                Grid.SetColumn(valText, 1);
                grid.Children.Add(lblText);
                grid.Children.Add(valText);

                stack.Children.Add(grid);
            }

            AddDetail("StartupPage_DetailsDialog_Name".TryGetLocalized() ?? "Name:", item.Name);
            AddDetail("StartupPage_DetailsDialog_Publisher".TryGetLocalized() ?? "Publisher:", item.Publisher);
            AddDetail("StartupPage_DetailsDialog_Description".TryGetLocalized() ?? "Description:", item.Description);
            AddDetail("StartupPage_DetailsDialog_Command".TryGetLocalized() ?? "Command:", item.Command);
            AddDetail("StartupPage_DetailsDialog_TargetPath".TryGetLocalized() ?? "Target Path:", item.ExecutablePath);
            AddDetail("StartupPage_DetailsDialog_Location".TryGetLocalized() ?? "Location:", item.LocationDisplay);
            AddDetail("StartupPage_DetailsDialog_Status".TryGetLocalized() ?? "Status:", item.IsEnabled ? ("StartupPage_EnabledApps".TryGetLocalized() ?? "Enabled") : ("StartupPage_DisabledApps".TryGetLocalized() ?? "Disabled"));
            AddDetail("StartupPage_DetailsDialog_Impact".TryGetLocalized() ?? "Impact:", item.ImpactText);
            AddDetail("StartupPage_DetailsDialog_FileSize".TryGetLocalized() ?? "File Size:", item.FileSizeBytes > 0 ? $"{item.FileSizeBytes / (1024.0 * 1024.0):F2} MB" : "N/A");
            AddDetail("StartupPage_DetailsDialog_FileExists".TryGetLocalized() ?? "File Exists:", item.IsValid ? ("StartupPage_DetailsDialog_Yes".TryGetLocalized() ?? "Yes") : ("StartupPage_DetailsDialog_NoMissing".TryGetLocalized() ?? "No (Missing)"));

            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
                Title = "StartupPage_DetailsDialog_Title".TryGetLocalized() ?? "Startup App Details",
                Content = stack,
                CloseButtonText = "StartupPage_DetailsDialog_CloseButton".TryGetLocalized() ?? "Close"
            };

            await dialog.ShowAsync();
        }
    }

    private async void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is StartupItem item)
        {
            var title = "StartupPage_RemoveDialog_Title".TryGetLocalized() ?? "Remove Startup App";
            var contentFormat = "StartupPage_RemoveDialog_Content".TryGetLocalized() ?? "Are you sure you want to remove '{0}' from startup?";
            var primaryBtn = "StartupPage_RemoveDialog_PrimaryButton".TryGetLocalized() ?? "Remove";
            var closeBtn = "StartupPage_RemoveDialog_CancelButton".TryGetLocalized() ?? "Cancel";

            var confirmDialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
                Title = title,
                Content = string.Format(contentFormat, item.Name),
                PrimaryButtonText = primaryBtn,
                CloseButtonText = closeBtn,
                DefaultButton = ContentDialogButton.Close
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var success = await StartupHelper.RemoveStartupItemAsync(item);
                if (success)
                {
                    _allStartupItems.Remove(item);
                    UpdateSummaryCards();
                    ApplyFilterAndSort();
                    var titleText = "StartupPage_Title".TryGetLocalized() ?? "Startup Manager";
                    var msg = string.Format("StartupPage_Notification_ItemRemoved".TryGetLocalized() ?? "Removed '{0}' from startup.", item.Name);
                    App.ShowNotification(titleText, msg, InfoBarSeverity.Success, 3000);
                }
                else
                {
                    var titleText = "StartupPage_Title".TryGetLocalized() ?? "Startup Manager";
                    var msg = "StartupPage_Notification_RemoveFailed".TryGetLocalized() ?? "Failed to remove item. Administrator privileges may be required.";
                    App.ShowNotification(titleText, msg, InfoBarSeverity.Error, 4000);
                }
            }
        }
    }

    private async void AddAppButton_Click(object sender, RoutedEventArgs e)
    {
        var nameBox = new TextBox { Header = "StartupPage_AddDialog_AppNameHeader".TryGetLocalized() ?? "Application Name", PlaceholderText = "e.g. My Custom Tool" };
        var pathBox = new TextBox { Header = "StartupPage_AddDialog_AppPathHeader".TryGetLocalized() ?? "Executable or Script Path", PlaceholderText = @"C:\Program Files\App\app.exe" };
        var argsBox = new TextBox { Header = "StartupPage_AddDialog_ArgsHeader".TryGetLocalized() ?? "Arguments (Optional)", PlaceholderText = "--autostart --minimized" };

        var browseButton = new Button
        {
            Content = "StartupPage_AddDialog_BrowseButton".TryGetLocalized() ?? "Browse...",
            Margin = new Thickness(0, 24, 0, 0),
            VerticalAlignment = VerticalAlignment.Bottom,
        };

        browseButton.Click += async (_, _) =>
        {
            try
            {
                var picker = new FileOpenPicker(App.MainWindow.AppWindow.Id)
                {
                    SuggestedStartLocation = PickerLocationId.Desktop
                };
                picker.FileTypeFilter.Add(".exe");
                picker.FileTypeFilter.Add(".bat");
                picker.FileTypeFilter.Add(".cmd");
                picker.FileTypeFilter.Add(".vbs");
                picker.FileTypeFilter.Add(".lnk");
                picker.FileTypeFilter.Add("*");

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    pathBox.Text = file.Path;
                    if (string.IsNullOrWhiteSpace(nameBox.Text))
                    {
                        nameBox.Text = System.IO.Path.GetFileNameWithoutExtension(file.Path);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"File picker error: {ex.Message}");
            }
        };

        var pathGrid = new Grid();
        pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(pathBox, 0);
        Grid.SetColumn(browseButton, 1);
        browseButton.Margin = new Thickness(8, 0, 0, 0);
        pathGrid.Children.Add(pathBox);
        pathGrid.Children.Add(browseButton);

        var scopeRadioUser = new RadioButton { Content = "StartupPage_AddDialog_CurrentUser".TryGetLocalized() ?? "Current User (HKCU)", IsChecked = true };
        var scopeRadioSystem = new RadioButton { Content = "StartupPage_AddDialog_AllUsers".TryGetLocalized() ?? "All Users (HKLM - Requires Admin)" };

        var stack = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                nameBox,
                pathGrid,
                argsBox,
                new TextBlock { Text = "StartupPage_AddDialog_ScopeHeader".TryGetLocalized() ?? "Startup Scope:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 4, 0, 0) },
                scopeRadioUser,
                scopeRadioSystem
            }
        };

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
            BorderBrush = (SolidColorBrush)Application.Current.Resources["AccentAAFillColorDefaultBrush"],
            Title = "StartupPage_AddDialog_Title".TryGetLocalized() ?? "Add Startup App",
            Content = stack,
            PrimaryButtonText = "StartupPage_AddDialog_PrimaryButton".TryGetLocalized() ?? "Add to Startup",
            CloseButtonText = "StartupPage_AddDialog_CancelButton".TryGetLocalized() ?? "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"]
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var appName = nameBox.Text.Trim();
            var targetPath = pathBox.Text.Trim();
            var args = argsBox.Text.Trim();
            var isUserScope = scopeRadioUser.IsChecked == true;

            var titleText = "StartupPage_Title".TryGetLocalized() ?? "Startup Manager";

            // Warn if path looks like a file but doesn't exist
            if (!string.IsNullOrEmpty(targetPath) && Path.HasExtension(targetPath) && !File.Exists(targetPath))
            {
                var warnMsg = string.Format("StartupPage_Notification_FileNotFound".TryGetLocalized() ?? "File not found at '{0}'. It will still be added to startup, but may not launch.", targetPath);
                App.ShowNotification(titleText, warnMsg, InfoBarSeverity.Warning, 4000);
            }

            if (string.IsNullOrEmpty(appName) || string.IsNullOrEmpty(targetPath))
            {
                var msg = "StartupPage_Notification_EmptyAppError".TryGetLocalized() ?? "App name and path cannot be empty.";
                App.ShowNotification(titleText, msg, InfoBarSeverity.Warning, 3000);
                return;
            }

            var success = await StartupHelper.AddStartupItemAsync(appName, targetPath, args, isUserScope);
            if (success)
            {
                var msg = string.Format("StartupPage_Notification_ItemAdded".TryGetLocalized() ?? "Added '{0}' to startup.", appName);
                App.ShowNotification(titleText, msg, InfoBarSeverity.Success, 3000);
                await LoadStartupItemsAsync();
            }
            else
            {
                var msg = "StartupPage_Notification_AddFailed".TryGetLocalized() ?? "Failed to add startup item. Administrator privileges may be required.";
                App.ShowNotification(titleText, msg, InfoBarSeverity.Error, 4000);
            }
        }
    }

    private async void EnableAllButton_Click(object sender, RoutedEventArgs e)
    {
        var itemsToEnable = _filteredStartupItems.Where(x => !x.IsEnabled).ToList();
        if (itemsToEnable.Count == 0) return;

        var titleText = "StartupPage_Title".TryGetLocalized() ?? "Startup Manager";
        int count = 0;
        var failedCount = 0;
        foreach (var item in itemsToEnable)
        {
            if (await StartupHelper.SetStartupItemEnabledAsync(item, true))
            {
                item.IsEnabled = true;
                count++;
            }
            else
            {
                failedCount++;
            }
        }

        UpdateSummaryCards();
        ApplyFilterAndSort();
        var msg = failedCount > 0
            ? string.Format("StartupPage_Notification_EnabledCountFailed".TryGetLocalized() ?? "Enabled {0} startup apps. {1} failed (admin required).", count, failedCount)
            : string.Format("StartupPage_Notification_EnabledCount".TryGetLocalized() ?? "Enabled {0} startup apps.", count);
        App.ShowNotification(titleText, msg, failedCount > 0 ? InfoBarSeverity.Warning : InfoBarSeverity.Success, 3000);
    }

    private async void DisableAllButton_Click(object sender, RoutedEventArgs e)
    {
        var itemsToDisable = _filteredStartupItems.Where(x => x.IsEnabled).ToList();
        if (itemsToDisable.Count == 0) return;

        var titleText = "StartupPage_Title".TryGetLocalized() ?? "Startup Manager";
        int count = 0;
        var failedCount = 0;
        foreach (var item in itemsToDisable)
        {
            if (await StartupHelper.SetStartupItemEnabledAsync(item, false))
            {
                item.IsEnabled = false;
                count++;
            }
            else
            {
                failedCount++;
            }
        }

        UpdateSummaryCards();
        ApplyFilterAndSort();
        var msg = failedCount > 0
            ? string.Format("StartupPage_Notification_DisabledCountFailed".TryGetLocalized() ?? "Disabled {0} startup apps. {1} failed (admin required).", count, failedCount)
            : string.Format("StartupPage_Notification_DisabledCount".TryGetLocalized() ?? "Disabled {0} startup apps.", count);
        App.ShowNotification(titleText, msg, failedCount > 0 ? InfoBarSeverity.Warning : InfoBarSeverity.Success, 3000);
    }

    private void StartupListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var count = StartupListView.SelectedItems.Count;
        if (count > 0)
        {
            var template = "StartupPage_SelectionCount".TryGetLocalized() ?? "{0} item(s) selected";
            SelectionCountText.Text = string.Format(template, count);
            SelectionBar.Visibility = Visibility.Visible;
        }
        else
        {
            SelectionBar.Visibility = Visibility.Collapsed;
        }

        if (SelectAllCheckBox != null)
        {
            if (_filteredStartupItems.Count == 0 || count == 0)
            {
                SelectAllCheckBox.IsChecked = false;
            }
            else if (count >= _filteredStartupItems.Count)
            {
                SelectAllCheckBox.IsChecked = true;
            }
            else
            {
                SelectAllCheckBox.IsChecked = null; // Indeterminate
            }
        }
    }

    private void SelectAllCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (SelectAllCheckBox.IsChecked == true)
        {
            StartupListView.SelectAll();
        }
        else
        {
            StartupListView.SelectedItems.Clear();
        }
    }

    private async void RemoveSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedItems = StartupListView.SelectedItems.OfType<StartupItem>().ToList();
        if (selectedItems.Count == 0) return;

        var title = "StartupPage_BatchRemoveDialog_Title".TryGetLocalized() ?? "Remove Startup Apps";
        var contentFormat = "StartupPage_BatchRemoveDialog_Content".TryGetLocalized()
            ?? "Are you sure you want to remove {0} item(s) from startup?";
        var primaryBtn = "StartupPage_RemoveDialog_PrimaryButton".TryGetLocalized() ?? "Remove";
        var closeBtn = "StartupPage_RemoveDialog_CancelButton".TryGetLocalized() ?? "Cancel";

        var confirmDialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
            Title = title,
            Content = string.Format(contentFormat, selectedItems.Count),
            PrimaryButtonText = primaryBtn,
            CloseButtonText = closeBtn,
            DefaultButton = ContentDialogButton.Close
        };

        var result = await confirmDialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return;

        var titleText = "StartupPage_Title".TryGetLocalized() ?? "Startup Manager";
        int removedCount = 0;
        int failedCount = 0;

        RemoveSelectedButton.IsEnabled = false;
        try
        {
            foreach (var item in selectedItems)
            {
                var success = await StartupHelper.RemoveStartupItemAsync(item);
                if (success)
                {
                    _allStartupItems.Remove(item);
                    removedCount++;
                }
                else
                {
                    failedCount++;
                }
            }
        }
        finally
        {
            RemoveSelectedButton.IsEnabled = true;
        }

        UpdateSummaryCards();
        ApplyFilterAndSort();
        StartupListView.SelectedItems.Clear();

        if (failedCount > 0)
        {
            var msg = string.Format(
                "StartupPage_Notification_BatchRemovedFailed".TryGetLocalized()
                    ?? "Removed {0} item(s). {1} failed (admin required).",
                removedCount, failedCount);
            App.ShowNotification(titleText, msg, InfoBarSeverity.Warning, 4000);
        }
        else
        {
            var msg = string.Format(
                "StartupPage_Notification_BatchRemoved".TryGetLocalized()
                    ?? "Removed {0} startup item(s).",
                removedCount);
            App.ShowNotification(titleText, msg, InfoBarSeverity.Success, 3000);
        }
    }

    private void ClearSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        StartupListView.SelectedItems.Clear();
    }
}
