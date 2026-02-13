using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using System.Text;
using Windows.ApplicationModel.DataTransfer;

namespace RyTuneX.Helpers;

internal static partial class LogViewerHelper
{
    private enum LogLevel
    {
        All,
        Info,
        Warn,
        Error,
        Critical
    }

    private sealed class LogEntry
    {
        public string Timestamp { get; init; } = string.Empty;
        public string Level { get; init; } = string.Empty;
        public string Caller { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public string RawLine { get; init; } = string.Empty;
        public bool IsSessionMarker { get; init; }
        public string SessionInfo { get; set; } = string.Empty;
    }

    // Fast IndexOf-based parser for: "yyyy-MM-dd HH:mm:ss.fff: [LEVEL] [Caller.Method] message"
    private static bool TryParseLogLine(string line, out string timestamp, out string level, out string caller, out string message)
    {
        timestamp = level = caller = message = string.Empty;

        // Minimum: "yyyy-MM-dd HH:mm:ss.fff: [X] [X] X" (~35 chars)
        if (line.Length < 35)
            return false;

        // Timestamp is exactly 23 chars, followed by ": ["
        if (line[23] != ':' || line[24] != ' ' || line[25] != '[')
            return false;

        // Validate key timestamp positions: yyyy-MM-dd HH:mm:ss.fff
        if (line[4] != '-' || line[7] != '-' || line[10] != ' ' ||
            line[13] != ':' || line[16] != ':' || line[19] != '.')
            return false;

        timestamp = line[..23];

        // Find end of level: "] ["
        var levelEnd = line.IndexOf(']', 26);
        if (levelEnd < 0 || levelEnd + 2 >= line.Length || line[levelEnd + 1] != ' ' || line[levelEnd + 2] != '[')
            return false;

        level = line[26..levelEnd];

        // Find end of caller: "] "
        var callerStart = levelEnd + 3;
        var callerEnd = line.IndexOf(']', callerStart);
        if (callerEnd < 0 || callerEnd + 1 >= line.Length || line[callerEnd + 1] != ' ')
            return false;

        caller = line[callerStart..callerEnd];
        message = line[(callerEnd + 2)..];

        return true;
    }

    private const string LogEntryTemplateXaml =
        """
        <DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
            <Grid Padding='4,6,8,6' Margin='0,1,0,1' CornerRadius='4'
                  Background='{ThemeResource CardBackgroundFillColorDefaultBrush}'>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width='Auto'/>
                    <ColumnDefinition Width='*'/>
                </Grid.ColumnDefinitions>
                <Border Width='3' CornerRadius='1.5' VerticalAlignment='Stretch' Margin='0,2,8,2'/>
                <StackPanel Grid.Column='1' Spacing='1'>
                    <StackPanel Orientation='Horizontal' Spacing='4'>
                        <FontIcon FontSize='12' VerticalAlignment='Top' Margin='0,2,6,0'/>
                        <TextBlock FontSize='11' FontFamily='Cascadia Mono, Consolas, Courier New'
                                   Foreground='{ThemeResource TextFillColorTertiaryBrush}'
                                   VerticalAlignment='Top' Margin='0,2,8,0'/>
                        <Border Background='{ThemeResource CardBackgroundFillColorSecondaryBrush}'
                                CornerRadius='4' Padding='5,1,5,1' Margin='2,0,0,0'
                                VerticalAlignment='Center'>
                            <TextBlock FontSize='10' FontFamily='Cascadia Mono, Consolas, Courier New'
                                       Foreground='{ThemeResource TextFillColorSecondaryBrush}'/>
                        </Border>
                    </StackPanel>
                    <TextBlock TextWrapping='Wrap' IsTextSelectionEnabled='True' FontSize='12.5'
                               FontFamily='Cascadia Mono, Consolas, Courier New'
                               MaxLines='6' TextTrimming='CharacterEllipsis' Margin='0,2,0,0'/>
                </StackPanel>
            </Grid>
        </DataTemplate>
        """;

    private const string SessionMarkerTemplateXaml =
        """
        <DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
            <Grid Padding='4,10,8,6' Margin='0,4,0,4'>
                <StackPanel Spacing='4'>
                    <Grid Margin='0,2,0,2'>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width='*'/>
                            <ColumnDefinition Width='Auto'/>
                            <ColumnDefinition Width='*'/>
                        </Grid.ColumnDefinitions>
                        <Border Height='1' VerticalAlignment='Center'
                                Background='{ThemeResource DividerStrokeColorDefaultBrush}'/>
                        <StackPanel Grid.Column='1' Orientation='Horizontal' Spacing='6'
                                    Padding='12,0,12,0' HorizontalAlignment='Center'>
                            <FontIcon Glyph='&#xE768;' FontSize='10'
                                      Foreground='{ThemeResource TextFillColorTertiaryBrush}'
                                      VerticalAlignment='Center'/>
                            <TextBlock Text='New Session' FontSize='11' FontWeight='SemiBold'
                                       Foreground='{ThemeResource TextFillColorTertiaryBrush}'
                                       VerticalAlignment='Center'/>
                            <TextBlock FontSize='10'
                                       Foreground='{ThemeResource TextFillColorTertiaryBrush}'
                                       Opacity='0.7' VerticalAlignment='Center'/>
                        </StackPanel>
                        <Border Grid.Column='2' Height='1' VerticalAlignment='Center'
                                Background='{ThemeResource DividerStrokeColorDefaultBrush}'/>
                    </Grid>
                    <TextBlock FontSize='10' FontFamily='Cascadia Mono, Consolas, Courier New'
                               Foreground='{ThemeResource TextFillColorTertiaryBrush}'
                               Opacity='0.7' HorizontalAlignment='Center'
                               TextTrimming='CharacterEllipsis'/>
                </StackPanel>
            </Grid>
        </DataTemplate>
        """;

    private sealed class LogEntryTemplateSelector : DataTemplateSelector
    {
        public DataTemplate LogTemplate { get; init; } = null!;
        public DataTemplate SessionTemplate { get; init; } = null!;

        protected override DataTemplate SelectTemplateCore(object item) =>
            item is LogEntry { IsSessionMarker: true } ? SessionTemplate : LogTemplate;
    }

    public static async Task ShowLogViewerAsync(XamlRoot xamlRoot)
    {
        var allEntries = await LoadLogEntriesAsync();
        var currentFilter = LogLevel.All;

        // Filter bar
        var filterCombo = new ComboBox
        {
            PlaceholderText = "Filter by level",
            MinWidth = 150,
            VerticalAlignment = VerticalAlignment.Center,
            Items =
            {
                new ComboBoxItem { Content = "All", Tag = LogLevel.All },
                new ComboBoxItem { Content = "Info", Tag = LogLevel.Info },
                new ComboBoxItem { Content = "Warning", Tag = LogLevel.Warn },
                new ComboBoxItem { Content = "Error", Tag = LogLevel.Error },
                new ComboBoxItem { Content = "Critical", Tag = LogLevel.Critical }
            },
            SelectedIndex = 0
        };

        var countBadge = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (SolidColorBrush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            FontSize = 12,
            Margin = new Thickness(8, 0, 0, 0)
        };

        var searchBox = new AutoSuggestBox
        {
            PlaceholderText = "Search logs...",
            QueryIcon = new SymbolIcon(Symbol.Find),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        var filterBar = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            },
            ColumnSpacing = 10,
            Margin = new Thickness(0, 0, 0, 12)
        };
        Grid.SetColumn(filterCombo, 0);
        Grid.SetColumn(countBadge, 1);
        Grid.SetColumn(searchBox, 2);
        filterBar.Children.Add(filterCombo);
        filterBar.Children.Add(countBadge);
        filterBar.Children.Add(searchBox);

        // Quick stat pills
        var logEntries = allEntries.Where(e => !e.IsSessionMarker);
        var infoCount = logEntries.Count(e => e.Level == "INFO");
        var warnCount = logEntries.Count(e => e.Level == "WARN");
        var errorCount = logEntries.Count(e => e.Level == "ERROR");
        var criticalCount = logEntries.Count(e => e.Level == "CRITICAL");

        var statBar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(0, 0, 0, 12),
            Children =
            {
                CreateStatPill("\uE946", infoCount.ToString(), GetLevelColor("INFO")),
                CreateStatPill("\uE7BA", warnCount.ToString(), GetLevelColor("WARN")),
                CreateStatPill("\uEA39", errorCount.ToString(), GetLevelColor("ERROR")),
                CreateStatPill("\uE783", criticalCount.ToString(), GetLevelColor("CRITICAL"))
            }
        };

        // Log list
        var templateSelector = new LogEntryTemplateSelector
        {
            LogTemplate = (DataTemplate)XamlReader.Load(LogEntryTemplateXaml),
            SessionTemplate = (DataTemplate)XamlReader.Load(SessionMarkerTemplateXaml)
        };

        var displayedEntries = new ObservableCollection<LogEntry>();

        var logListView = new ListView
        {
            SelectionMode = ListViewSelectionMode.None,
            IsItemClickEnabled = false,
            MaxHeight = 400,
            Padding = new Thickness(0),
            ItemContainerStyle = CreateCompactItemStyle(),
            ItemTemplateSelector = templateSelector,
            ItemsSource = displayedEntries,
            ItemContainerTransitions = []
        };
        ScrollViewer.SetHorizontalScrollMode(logListView, ScrollMode.Disabled);
        ScrollViewer.SetHorizontalScrollBarVisibility(logListView, ScrollBarVisibility.Disabled);
        logListView.ContainerContentChanging += OnContainerContentChanging;

        // Enable ItemsStackPanel recycling
        logListView.Loaded += (_, _) =>
        {
            if (logListView.ItemsPanelRoot is ItemsStackPanel panel)
            {
                panel.CacheLength = 2;
                panel.AreStickyGroupHeadersEnabled = false;
            }
        };

        // Empty state
        var emptyText = new TextBlock
        {
            Text = "No log entries found.",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (SolidColorBrush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            Margin = new Thickness(0, 40, 0, 40),
            Visibility = Visibility.Collapsed
        };

        // Main layout
        var rootPanel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Children = { filterBar, statBar, logListView, emptyText }
        };

        // Populate helper
        var currentSearchText = string.Empty;

        void Repopulate()
        {
            var filtered = allEntries.AsEnumerable();

            if (currentFilter != LogLevel.All)
            {
                var levelStr = currentFilter.ToString().ToUpperInvariant();
                filtered = filtered.Where(e => e.IsSessionMarker || e.Level == levelStr);
            }

            if (!string.IsNullOrWhiteSpace(currentSearchText))
            {
                filtered = filtered.Where(e =>
                    e.IsSessionMarker || e.RawLine.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase));
            }

            var list = filtered.ToList();

            // Reset ItemsSource to avoid per-item change notifications
            logListView.ItemsSource = null;
            displayedEntries = new ObservableCollection<LogEntry>(list);
            logListView.ItemsSource = displayedEntries;

            var logCount = list.Count(e => !e.IsSessionMarker);
            var totalLogCount = allEntries.Count(e => !e.IsSessionMarker);
            countBadge.Text = $"{logCount} of {totalLogCount} entries";
            emptyText.Visibility = logCount == 0 ? Visibility.Visible : Visibility.Collapsed;
            logListView.Visibility = logCount == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        Repopulate();

        // Filter changed
        filterCombo.SelectionChanged += (_, _) =>
        {
            if (filterCombo.SelectedItem is ComboBoxItem item && item.Tag is LogLevel level)
            {
                currentFilter = level;
                Repopulate();
            }
        };

        // Search changed
        searchBox.TextChanged += (sender, args) =>
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                currentSearchText = sender.Text;
                Repopulate();
            }
        };
        searchBox.QuerySubmitted += (sender, _) =>
        {
            currentSearchText = sender.Text;
            Repopulate();
        };

        // --- Dialog ---
        var dialog = new ContentDialog
        {
            XamlRoot = xamlRoot,
            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
            BorderBrush = (SolidColorBrush)Application.Current.Resources["AccentAAFillColorDefaultBrush"],
            Title = CreateDialogTitle(),
            Content = rootPanel,
            PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"],
            CloseButtonText = "Close".GetLocalized(),
            PrimaryButtonText = "Copy",
            SecondaryButtonText = "Open File",
            Resources =
            {
                ["ContentDialogMaxWidth"] = 800d,
                ["ContentDialogMinWidth"] = 800d
            }
        };

        dialog.PrimaryButtonClick += (_, args) =>
        {
            args.Cancel = true; // Keep dialog open
            CopyLogsToClipboard(allEntries, currentFilter, currentSearchText);
        };

        dialog.SecondaryButtonClick += (_, args) =>
        {
            args.Cancel = true; // Keep dialog open
            OpenLogFile();
        };

        await dialog.ShowAsync();
    }

    private static async Task<List<LogEntry>> LoadLogEntriesAsync()
    {
        var entries = new List<LogEntry>();

        try
        {
            var logFilePath = LogHelper.GetLogFilePath();

            if (!File.Exists(logFilePath))
                return entries;

            using var stream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (TryParseLogLine(line, out var timestamp, out var level, out var caller, out var message))
                {
                    // Detect the "New Session" separator line
                    if (message.Contains("New Session", StringComparison.OrdinalIgnoreCase))
                    {
                        entries.Add(new LogEntry
                        {
                            Timestamp = timestamp,
                            Level = "INFO",
                            Caller = caller,
                            Message = message,
                            RawLine = line,
                            IsSessionMarker = true
                        });
                        continue;
                    }

                    // Detect the system info line that follows the session marker
                    if (message.StartsWith("App version:", StringComparison.OrdinalIgnoreCase)
                        && entries.Count > 0 && entries[^1].IsSessionMarker)
                    {
                        // Merge into the previous session marker
                        entries[^1].SessionInfo = message;
                        continue;
                    }

                    entries.Add(new LogEntry
                    {
                        Timestamp = timestamp,
                        Level = level.ToUpperInvariant(),
                        Caller = caller,
                        Message = message,
                        RawLine = line
                    });
                }
                else
                {
                    // Non-matching lines (e.g. multi-line exceptions) — create raw entry
                    entries.Add(new LogEntry
                    {
                        Timestamp = string.Empty,
                        Level = "INFO",
                        Caller = string.Empty,
                        Message = line,
                        RawLine = line
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error loading log entries for viewer: {ex.Message}");
        }

        return entries;
    }

    private static StackPanel CreateDialogTitle()
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Children =
            {
                new FontIcon { Glyph = "\uE9D9", FontSize = 20 },
                new TextBlock
                {
                    Text = "Log Viewer",
                    Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"]
                },
                new TextBlock
                {
                    Text = $"— {DateTime.Now:yyyy-MM-dd}",
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Foreground = (SolidColorBrush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 2)
                }
            }
        };
    }

    private static void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.InRecycleQueue)
            return;

        if (args.Item is not LogEntry entry)
            return;

        var root = (Grid)args.ItemContainer.ContentTemplateRoot;

        if (entry.IsSessionMarker)
            BindSessionMarker(root, entry);
        else
            BindLogEntry(root, entry);
    }

    private static void BindLogEntry(Grid root, LogEntry entry)
    {
        var levelColor = GetLevelColor(entry.Level);

        var levelBar = (Border)root.Children[0];
        levelBar.Background = new SolidColorBrush(levelColor);

        var contentPanel = (StackPanel)root.Children[1];
        var headerRow = (StackPanel)contentPanel.Children[0];
        var messageBlock = (TextBlock)contentPanel.Children[1];

        var levelIcon = (FontIcon)headerRow.Children[0];
        levelIcon.Glyph = GetLevelGlyph(entry.Level);
        levelIcon.Foreground = new SolidColorBrush(levelColor);

        var timestampBlock = (TextBlock)headerRow.Children[1];
        timestampBlock.Text = FormatTimestamp(entry.Timestamp);

        var callerBadge = (Border)headerRow.Children[2];
        if (string.IsNullOrEmpty(entry.Caller))
        {
            callerBadge.Visibility = Visibility.Collapsed;
        }
        else
        {
            callerBadge.Visibility = Visibility.Visible;
            ((TextBlock)callerBadge.Child).Text = entry.Caller;
        }

        messageBlock.Text = entry.Message;
        messageBlock.Foreground = GetMessageBrush(entry.Level);
    }

    private static void BindSessionMarker(Grid root, LogEntry entry)
    {
        var container = (StackPanel)root.Children[0];
        var separatorRow = (Grid)container.Children[0];
        var sessionLabel = (StackPanel)separatorRow.Children[1];

        var timestampText = (TextBlock)sessionLabel.Children[2];
        timestampText.Text = FormatTimestamp(entry.Timestamp);

        var sessionInfoText = (TextBlock)container.Children[1];
        if (string.IsNullOrEmpty(entry.SessionInfo))
        {
            sessionInfoText.Visibility = Visibility.Collapsed;
        }
        else
        {
            sessionInfoText.Visibility = Visibility.Visible;
            sessionInfoText.Text = entry.SessionInfo;
        }
    }

    private static Border CreateStatPill(string glyph, string count, Windows.UI.Color color)
    {
        var foreground = new SolidColorBrush(color);
        return new Border
        {
            Background = new SolidColorBrush(color) { Opacity = 0.12 },
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(10, 4, 10, 4),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Children =
                {
                    new FontIcon
                    {
                        Glyph = glyph,
                        FontSize = 12,
                        Foreground = foreground
                    },
                    new TextBlock
                    {
                        Text = count,
                        FontSize = 12,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = foreground
                    }
                }
            }
        };
    }

    private static Style CreateCompactItemStyle()
    {
        var style = new Style(typeof(ListViewItem));
        style.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, 0d));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0)));
        style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        return style;
    }

    private static string FormatTimestamp(string timestamp)
    {
        if (string.IsNullOrEmpty(timestamp))
            return string.Empty;

        // Show only time portion (HH:mm:ss.fff) from "yyyy-MM-dd HH:mm:ss.fff"
        var spaceIdx = timestamp.IndexOf(' ');
        return spaceIdx >= 0 ? timestamp[(spaceIdx + 1)..] : timestamp;
    }

    private static Windows.UI.Color GetLevelColor(string level)
    {
        return level switch
        {
            "WARN" => Windows.UI.Color.FromArgb(255, 255, 185, 0),
            "ERROR" => Windows.UI.Color.FromArgb(255, 232, 72, 85),
            "CRITICAL" => Windows.UI.Color.FromArgb(255, 175, 60, 235),
            _ => Windows.UI.Color.FromArgb(255, 96, 165, 250)
        };
    }

    private static string GetLevelGlyph(string level)
    {
        return level switch
        {
            "WARN" => "\uE7BA",
            "ERROR" => "\uEA39",
            "CRITICAL" => "\uE783",
            _ => "\uE946"
        };
    }

    private static Brush GetMessageBrush(string level)
    {
        return level switch
        {
            "ERROR" or "CRITICAL" => new SolidColorBrush(GetLevelColor(level)),
            "WARN" => new SolidColorBrush(GetLevelColor(level)),
            _ => (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
        };
    }

    private static void CopyLogsToClipboard(List<LogEntry> allEntries, LogLevel filter, string searchText)
    {
        var filtered = allEntries.AsEnumerable();

        if (filter != LogLevel.All)
        {
            var levelStr = filter.ToString().ToUpperInvariant();
            filtered = filtered.Where(e => e.Level == levelStr);
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            filtered = filtered.Where(e =>
                e.RawLine.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        var sb = new StringBuilder();
        foreach (var entry in filtered)
        {
            sb.AppendLine(entry.RawLine);
        }

        var dataPackage = new DataPackage();
        dataPackage.SetText(sb.ToString());
        Clipboard.SetContent(dataPackage);
    }

    private static void OpenLogFile()
    {
        try
        {
            var logFilePath = LogHelper.GetLogFilePath();
            if (File.Exists(logFilePath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = logFilePath,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error opening log file: {ex.Message}");
        }
    }
}
