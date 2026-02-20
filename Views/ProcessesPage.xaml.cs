using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace RyTuneX.Views;

public sealed partial class ProcessesPage : Page
{
    private List<ProcessInfoItem> _allProcesses = [];
    private readonly ObservableCollection<ProcessInfoItem> _filteredProcesses = [];
    private string _currentSort = "Memory";
    private bool _sortAscending;
    private DispatcherTimer? _refreshTimer;
    private bool _isUpdating;

    public ProcessesPage()
    {
        InitializeComponent();
        LogHelper.Log("Initializing ProcessesPage");
        ProcessListView.ItemsSource = _filteredProcesses;
        Loaded += ProcessesPage_Loaded;
        Unloaded += ProcessesPage_Unloaded;
    }

    private async void ProcessesPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadProcessesAsync();
        StartAutoRefresh();
    }

    private void ProcessesPage_Unloaded(object sender, RoutedEventArgs e)
    {
        StopAutoRefresh();
    }

    private void StartAutoRefresh()
    {
        if (_refreshTimer != null)
        {
            return;
        }

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _refreshTimer.Tick += async (_, _) => await RefreshProcessesAsync();
        _refreshTimer.Start();
    }

    private void StopAutoRefresh()
    {
        _refreshTimer?.Stop();
        _refreshTimer = null;
    }

    private async Task LoadProcessesAsync()
    {
        LoadingRing.IsActive = true;
        LoadingRing.Visibility = Visibility.Visible;
        ProcessListView.Visibility = Visibility.Collapsed;

        try
        {
            _allProcesses = await GetProcessSnapshotAsync();
            UpdateSummary();
            ApplyFilterAndSort();
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error loading processes: {ex.Message}");
        }
        finally
        {
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
            ProcessListView.Visibility = Visibility.Visible;
        }
    }

    private async Task RefreshProcessesAsync()
    {
        if (_isUpdating)
        {
            return;
        }

        _isUpdating = true;
        try
        {
            _allProcesses = await GetProcessSnapshotAsync();
            UpdateSummary();
            ApplyFilterAndSort();
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error refreshing processes: {ex.Message}");
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private static async Task<List<ProcessInfoItem>> GetProcessSnapshotAsync()
    {
        return await Task.Run(() =>
        {
            return Process.GetProcesses()
                .Select(p =>
                {
                    try
                    {
                        return new ProcessInfoItem
                        {
                            Name = p.ProcessName,
                            Id = p.Id,
                            MemoryMB = p.WorkingSet64 / (1024.0 * 1024.0),
                            ThreadCount = p.Threads.Count
                        };
                    }
                    catch (Exception ex)
                    {
                        _ = LogHelper.LogWarning($"Error reading process info for {p.ProcessName}: {ex.Message}");
                        return new ProcessInfoItem { Name = p.ProcessName, Id = p.Id };
                    }
                })
                .ToList();
        });
    }

    private void UpdateSummary()
    {
        TotalProcessesText.Text = _allProcesses.Count.ToString();
        TotalMemoryText.Text = $"{_allProcesses.Sum(p => p.MemoryMB):F0} MB";
        TotalThreadsText.Text = _allProcesses.Sum(p => p.ThreadCount).ToString();
    }

    private void ApplyFilterAndSort()
    {
        var query = SearchBox.Text?.ToLowerInvariant() ?? "";

        var filtered = string.IsNullOrEmpty(query)
            ? [.. _allProcesses]
            : _allProcesses
                .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           p.Id.ToString().Contains(query))
                .ToList();

        var sorted = SortProcesses(filtered);
        MergeInto(_filteredProcesses, sorted);
    }

    private List<ProcessInfoItem> SortProcesses(List<ProcessInfoItem> source)
    {
        return _currentSort switch
        {
            "Name" => _sortAscending
                ? [.. source.OrderBy(p => p.Name)]
                : [.. source.OrderByDescending(p => p.Name)],
            "PID" => _sortAscending
                ? [.. source.OrderBy(p => p.Id)]
                : [.. source.OrderByDescending(p => p.Id)],
            "Memory" => _sortAscending
                ? [.. source.OrderBy(p => p.MemoryMB)]
                : [.. source.OrderByDescending(p => p.MemoryMB)],
            "Threads" => _sortAscending
                ? [.. source.OrderBy(p => p.ThreadCount)]
                : [.. source.OrderByDescending(p => p.ThreadCount)],
            _ => source
        };
    }

    private static void MergeInto(ObservableCollection<ProcessInfoItem> target, List<ProcessInfoItem> source)
    {
        for (var i = 0; i < source.Count; i++)
        {
            if (i < target.Count)
            {
                if (target[i].Id == source[i].Id)
                {
                    target[i].UpdateFrom(source[i]);
                }
                else
                {
                    target[i] = source[i];
                }
            }
            else
            {
                target.Add(source[i]);
            }
        }

        while (target.Count > source.Count)
        {
            target.RemoveAt(target.Count - 1);
        }
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
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
        if (_refreshTimer?.IsEnabled == true)
        {
            StopAutoRefresh();
            RefreshButtonIcon.Glyph = "\uE768"; // Play
        }
        else
        {
            await RefreshProcessesAsync();
            StartAutoRefresh();
            RefreshButtonIcon.Glyph = "\uE769"; // Pause
        }
    }

    private async void EndTask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int processId)
        {
            await EndProcessAsync(processId);
        }
    }

    private async Task EndProcessAsync(int processId)
    {
        try
        {
            var processItem = _allProcesses.FirstOrDefault(p => p.Id == processId);
            var processName = processItem?.Name ?? "Unknown";

            _ = LogHelper.Log($"Ending process: {processName} (PID: {processId})");

            await Task.Run(() =>
            {
                using var process = Process.GetProcessById(processId);
                process.Kill();
            });

            App.ShowNotification("Process Ended", $"Process '{processName}' (PID: {processId}) was terminated successfully.", InfoBarSeverity.Success, 3000);
            await RefreshProcessesAsync();
        }
        catch (ArgumentException)
        {
            _ = LogHelper.LogWarning($"Process with PID {processId} no longer exists.");
            App.ShowNotification("Process Not Found", $"Process with PID {processId} no longer exists.", InfoBarSeverity.Warning, 3000);
            await RefreshProcessesAsync();
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error ending process {processId}: {ex.Message}");
            App.ShowNotification("Error", $"Failed to end process: {ex.Message}", InfoBarSeverity.Error, 5000);
        }
    }
}

internal class ProcessInfoItem : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private int _id;
    private double _memoryMB;
    private int _threadCount;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name
    {
        get => _name;
        set { if (_name != value) { _name = value; OnPropertyChanged(nameof(Name)); } }
    }

    public int Id
    {
        get => _id;
        set { if (_id != value) { _id = value; OnPropertyChanged(nameof(Id)); } }
    }

    public double MemoryMB
    {
        get => _memoryMB;
        set
        {
            if (Math.Abs(_memoryMB - value) > 0.01)
            {
                _memoryMB = value;
                OnPropertyChanged(nameof(MemoryMB));
                OnPropertyChanged(nameof(MemoryDisplay));
                OnPropertyChanged(nameof(MemoryPercent));
            }
        }
    }

    public int ThreadCount
    {
        get => _threadCount;
        set { if (_threadCount != value) { _threadCount = value; OnPropertyChanged(nameof(ThreadCount)); } }
    }

    public string MemoryDisplay => $"{MemoryMB:F1} MB";
    public double MemoryPercent => Math.Min(MemoryMB / 500.0 * 100, 100);

    public void UpdateFrom(ProcessInfoItem other)
    {
        Name = other.Name;
        MemoryMB = other.MemoryMB;
        ThreadCount = other.ThreadCount;
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
