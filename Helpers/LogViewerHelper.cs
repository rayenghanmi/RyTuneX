using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Text;
using System.Text.RegularExpressions;
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

    // Pattern: 2025-01-01 12:00:00.000: [LEVEL] [Caller.Method] message
    [GeneratedRegex(@"^(\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}\.\d{3}):\s\[(\w+)\]\s\[([^\]]+)\]\s(.*)$", RegexOptions.Singleline)]
    private static partial Regex LogLineRegex();

    public static async Task ShowLogViewerAsync(XamlRoot xamlRoot)
    {
        var allEntries = await LoadLogEntriesAsync();
        var currentFilter = LogLevel.All;

        // --- Filter bar ---
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

        // --- Quick stat pills ---
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

        // --- Log list ---
        var logListView = new ListView
        {
            SelectionMode = ListViewSelectionMode.None,
            IsItemClickEnabled = false,
            MaxHeight = 400,
            Padding = new Thickness(0),
            ItemContainerStyle = CreateCompactItemStyle()
        };

        var scrollViewer = new ScrollViewer
        {
            Content = logListView,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            MaxHeight = 400
        };

        // --- Empty state ---
        var emptyText = new TextBlock
        {
            Text = "No log entries found.",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (SolidColorBrush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            Margin = new Thickness(0, 40, 0, 40),
            Visibility = Visibility.Collapsed
        };

        // --- Main layout ---
        var rootPanel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Children = { filterBar, statBar, scrollViewer, emptyText }
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
            logListView.Items.Clear();

            foreach (var entry in list)
            {
                logListView.Items.Add(entry.IsSessionMarker
                    ? CreateSessionSeparatorUI(entry)
                    : CreateLogItemUI(entry));
            }

            var logCount = list.Count(e => !e.IsSessionMarker);
            var totalLogCount = allEntries.Count(e => !e.IsSessionMarker);
            countBadge.Text = $"{logCount} of {totalLogCount} entries";
            emptyText.Visibility = logCount == 0 ? Visibility.Visible : Visibility.Collapsed;
            scrollViewer.Visibility = logCount == 0 ? Visibility.Collapsed : Visibility.Visible;
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
                ["ContentDialogMinWidth"] = 650d
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

            var lines = await File.ReadAllLinesAsync(logFilePath);
            var regex = LogLineRegex();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var match = regex.Match(line);
                if (match.Success)
                {
                    var message = match.Groups[4].Value;

                    // Detect the "New Session" separator line
                    if (message.Contains("New Session", StringComparison.OrdinalIgnoreCase))
                    {
                        entries.Add(new LogEntry
                        {
                            Timestamp = match.Groups[1].Value,
                            Level = "INFO",
                            Caller = match.Groups[3].Value,
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
                        Timestamp = match.Groups[1].Value,
                        Level = match.Groups[2].Value.ToUpperInvariant(),
                        Caller = match.Groups[3].Value,
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

    private static Grid CreateLogItemUI(LogEntry entry)
    {
        var levelColor = GetLevelColor(entry.Level);
        var levelGlyph = GetLevelGlyph(entry.Level);

        // Level indicator (colored bar on the left side)
        var levelBar = new Border
        {
            Width = 3,
            CornerRadius = new CornerRadius(1.5),
            Background = new SolidColorBrush(levelColor),
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(0, 2, 8, 2)
        };

        // Level icon
        var levelIcon = new FontIcon
        {
            Glyph = levelGlyph,
            FontSize = 12,
            Foreground = new SolidColorBrush(levelColor),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 2, 6, 0)
        };

        // Timestamp
        var timestampBlock = new TextBlock
        {
            Text = FormatTimestamp(entry.Timestamp),
            FontSize = 11,
            FontFamily = new FontFamily("Cascadia Mono, Consolas, Courier New"),
            Foreground = (SolidColorBrush)Application.Current.Resources["TextFillColorTertiaryBrush"],
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 2, 8, 0)
        };

        // Caller badge
        var callerBadge = CreateCallerBadge(entry.Caller);

        // Header row: icon + timestamp + caller
        var headerRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Children = { levelIcon, timestampBlock }
        };
        if (callerBadge != null)
        {
            headerRow.Children.Add(callerBadge);
        }

        // Message text
        var messageBlock = new TextBlock
        {
            Text = entry.Message,
            TextWrapping = TextWrapping.Wrap,
            IsTextSelectionEnabled = true,
            FontSize = 12.5,
            FontFamily = new FontFamily("Cascadia Mono, Consolas, Courier New"),
            Foreground = GetMessageBrush(entry.Level),
            Margin = new Thickness(0, 2, 0, 0),
            MaxLines = 6,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        // Content column
        var contentPanel = new StackPanel
        {
            Spacing = 1,
            Children = { headerRow, messageBlock }
        };

        // Grid: level bar + content
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            },
            Padding = new Thickness(4, 6, 8, 6),
            Margin = new Thickness(0, 1, 0, 1),
            CornerRadius = new CornerRadius(4),
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"]
        };

        Grid.SetColumn(levelBar, 0);
        Grid.SetColumn(contentPanel, 1);
        grid.Children.Add(levelBar);
        grid.Children.Add(contentPanel);

        return grid;
    }

    private static Grid CreateSessionSeparatorUI(LogEntry entry)
    {
        var accentBrush = (SolidColorBrush)Application.Current.Resources["TextFillColorTertiaryBrush"];

        // Left line
        var leftLine = new Border
        {
            Height = 1,
            VerticalAlignment = VerticalAlignment.Center,
            Background = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"]
        };

        // Session label with icon
        var sessionLabel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Padding = new Thickness(12, 0, 12, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            Children =
            {
                new FontIcon
                {
                    Glyph = "\uE768",
                    FontSize = 10,
                    Foreground = accentBrush,
                    VerticalAlignment = VerticalAlignment.Center
                },
                new TextBlock
                {
                    Text = "New Session",
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = accentBrush,
                    VerticalAlignment = VerticalAlignment.Center
                },
                new TextBlock
                {
                    Text = FormatTimestamp(entry.Timestamp),
                    FontSize = 10,
                    Foreground = accentBrush,
                    Opacity = 0.7,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };

        // Right line
        var rightLine = new Border
        {
            Height = 1,
            VerticalAlignment = VerticalAlignment.Center,
            Background = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"]
        };

        // Center row: line — label — line
        var separatorRow = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            },
            Margin = new Thickness(0, 2, 0, 2)
        };
        Grid.SetColumn(leftLine, 0);
        Grid.SetColumn(sessionLabel, 1);
        Grid.SetColumn(rightLine, 2);
        separatorRow.Children.Add(leftLine);
        separatorRow.Children.Add(sessionLabel);
        separatorRow.Children.Add(rightLine);

        // Container
        var container = new StackPanel
        {
            Spacing = 4,
            Children = { separatorRow }
        };

        // Add system info line if available
        if (!string.IsNullOrEmpty(entry.SessionInfo))
        {
            container.Children.Add(new TextBlock
            {
                Text = entry.SessionInfo,
                FontSize = 10,
                FontFamily = new FontFamily("Cascadia Mono, Consolas, Courier New"),
                Foreground = accentBrush,
                Opacity = 0.7,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
        }

        var grid = new Grid
        {
            Padding = new Thickness(4, 10, 8, 6),
            Margin = new Thickness(0, 4, 0, 4),
            Children = { container }
        };

        return grid;
    }

    private static Border? CreateCallerBadge(string caller)
    {
        if (string.IsNullOrEmpty(caller))
            return null;

        return new Border
        {
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(5, 1, 5, 1),
            Margin = new Thickness(2, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = caller,
                FontSize = 10,
                FontFamily = new FontFamily("Cascadia Mono, Consolas, Courier New"),
                Foreground = (SolidColorBrush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            }
        };
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
