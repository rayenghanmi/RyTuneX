using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using RyTuneX.Helpers;
using Windows.UI;

namespace RyTuneX.Views;

public sealed partial class StressTestPage : Page
{
    private readonly StressEngine _stressEngine = new();

    private DateTime _testStartTime;

    // Selected CPU indices
    private List<int> _selectedCpuIndices =
        Enumerable.Range(0, Environment.ProcessorCount).ToList();

    // Monitoring
    private CancellationTokenSource? _monitorCts;

    public StressTestPage()
    {
        InitializeComponent();
        NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        LogHelper.Log("Initializing StressTestPage");

        // Wire up engine events
        _stressEngine.CpuStopped += OnCpuStopped;
        _stressEngine.RamStopped += OnRamStopped;
        _stressEngine.RamAllocated += OnRamAllocated;
        _stressEngine.RamBandwidth += OnRamBandwidth;
        _stressEngine.DiskStopped += OnDiskStopped;
        _stressEngine.DiskMetrics += OnDiskMetrics;
        _stressEngine.GpuStopped += OnGpuStopped;

        Loaded += StressTestPage_Loaded;
        Unloaded += StressTestPage_Unloaded;
    }

    private void StressTestPage_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateCoreSelectionButton();

        _stressEngine.DetectGpuInfo((_, vramBytes) =>
        {
            DispatcherQueue?.TryEnqueue(() =>
            {
                if (vramBytes > 0)
                {
                    GpuVramText.Text = $"{vramBytes / (1024 * 1024):N0} MB";
                }
                GpuDetailText.Text = _stressEngine.VramDisplay;
            });
        });

        StartMonitoring();
    }

    private void StressTestPage_Unloaded(object sender, RoutedEventArgs e)
    {
        StopAllTests();
        StopMonitoring();
        _stressEngine.Dispose();
    }

    //  CPU Core Selector
    private void UpdateCoreSelectionButton()
    {
        var total = Environment.ProcessorCount;
        SelectCoresText.Text = _selectedCpuIndices.Count == total
            ? string.Format("StressTestPage_AllCoresWithCount".TryGetLocalized() ?? "All Cores ({0})", total)
            : string.Format("StressTestPage_SelectedCoresCount".TryGetLocalized() ?? "{0} of {1} Cores", _selectedCpuIndices.Count, total);
    }

    private async void SelectCoresButton_Click(object sender, RoutedEventArgs e)
    {
        await ShowCpuCoreSelectorAsync();
    }

    private async Task ShowCpuCoreSelectorAsync()
    {
        var total = Environment.ProcessorCount;
        var checkboxes = new CheckBox[total];
        var suppressEvents = false;

        var allCheck = new CheckBox
        {
            Content = string.Format("StressTestPage_AllCoresWithCount".TryGetLocalized() ?? "All Cores ({0})", total),
            IsChecked = _selectedCpuIndices.Count == total,
            Margin = new Thickness(0, 0, 0, 10),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };

        var coresGrid = new Grid { ColumnSpacing = 4, RowSpacing = 6 };
        const int columns = 4;
        var rows = (int)Math.Ceiling(total / (double)columns);
        for (var c = 0; c < columns; c++)
            coresGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (var r = 0; r < rows; r++)
            coresGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var cpuPrefix = "StressTestPage_CPU.Text".TryGetLocalized() ?? "CPU";
        for (var i = 0; i < total; i++)
        {
            var cb = new CheckBox
            {
                Content = $"{cpuPrefix} {i}",
                IsChecked = _selectedCpuIndices.Contains(i),
                Tag = i,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            checkboxes[i] = cb;
            Grid.SetRow(cb, i / columns);
            Grid.SetColumn(cb, i % columns);
            coresGrid.Children.Add(cb);
        }

        allCheck.Click += (s, _) =>
        {
            if (suppressEvents) return;
            var target = allCheck.IsChecked == true;
            suppressEvents = true;
            foreach (var cb in checkboxes) cb.IsChecked = target;
            suppressEvents = false;
        };

        foreach (var cb in checkboxes)
        {
            cb.Checked += (s, _) =>
            {
                if (suppressEvents) return;
                if (checkboxes.All(c => c.IsChecked == true))
                {
                    suppressEvents = true;
                    allCheck.IsChecked = true;
                    suppressEvents = false;
                }
            };
            cb.Unchecked += (s, _) =>
            {
                if (suppressEvents) return;
                suppressEvents = true;
                allCheck.IsChecked = false;
                suppressEvents = false;
            };
        }

        var warningText = new TextBlock
        {
            Text = "StressTestPage_SelectCpuCoresDialog_Warning".TryGetLocalized() ?? "Select at least one core.",
            Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x88, 0x00)),
            FontSize = 12,
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(0, 8, 0, 0)
        };

        var separator = new Border
        {
            Height = 1,
            Background = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"],
            Margin = new Thickness(0, 0, 0, 10)
        };

        var content = new StackPanel { Spacing = 0, MinWidth = 360 };
        content.Children.Add(allCheck);
        content.Children.Add(separator);
        content.Children.Add(new ScrollViewer
        {
            Content = coresGrid,
            MaxHeight = 320,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        });
        content.Children.Add(warningText);

        var dialog = new ContentDialog
        {
            Title = "StressTestPage_SelectCpuCoresDialog_Title".TryGetLocalized() ?? "Select CPU Cores",
            Content = content,
            PrimaryButtonText = "StressTestPage_SelectCpuCoresDialog_Apply".TryGetLocalized() ?? "Apply",
            CloseButtonText = "StressTestPage_SelectCpuCoresDialog_Cancel".TryGetLocalized() ?? "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        // Validate before applying
        dialog.PrimaryButtonClick += (d, args) =>
        {
            var anyChecked = checkboxes.Any(cb => cb.IsChecked == true);
            if (!anyChecked)
            {
                args.Cancel = true; // keep dialog open
                warningText.Visibility = Visibility.Visible;
            }
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            _selectedCpuIndices = checkboxes
                .Where(cb => cb.IsChecked == true)
                .Select(cb => (int)cb.Tag)
                .ToList();

            UpdateCoreSelectionButton();

            // Update the running threads count display
            CpuThreadsText.Text = _selectedCpuIndices.Count.ToString();
        }
    }

    //  Monitoring Loop
    private void StartMonitoring()
    {
        _monitorCts = new CancellationTokenSource();
        _ = MonitorLoopAsync(_monitorCts.Token);
    }

    private void StopMonitoring()
    {
        _monitorCts?.Cancel();
        _monitorCts?.Dispose();
        _monitorCts = null;
    }

    private async Task MonitorLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var cpuUsage = _stressEngine.GetCpuUsage();
                var ramUsage = StressEngine.GetRamUsage(out var usedMb, out var totalMb);

                float gpuUsage = 0;
                uint gpuTemp = 0;
                try
                {
                    (gpuUsage, gpuTemp) = await Task.Run(() =>
                    {
                        var u = _stressEngine.GetGpuUsage(out var t);
                        return (u, t);
                    }, ct).ConfigureAwait(false);
                }
                catch { }

                var isGpuRunning = _stressEngine.IsGpuRunning;
                var anyRunning = _stressEngine.IsCpuRunning || _stressEngine.IsRamRunning || _stressEngine.IsDiskRunning || isGpuRunning;
                var elapsed = anyRunning ? DateTime.UtcNow - _testStartTime : TimeSpan.Zero;
                var gpuGFlops = isGpuRunning ? _stressEngine.CurrentGFlops : 0;

                DispatcherQueue.TryEnqueue(() =>
                {
                    if (ct.IsCancellationRequested) return;

                    string tempStr = gpuTemp > 0 ? $"{gpuTemp}°C | " : "";

                    // Summary cards
                    CpuSummaryText.Text = $"{cpuUsage}%";
                    RamSummaryText.Text = $"{ramUsage}%";
                    RamDetailText.Text = $"{usedMb:F0} / {totalMb:F0} MB";
                    GpuSummaryText.Text = $"{gpuUsage:F0}%";
                    GpuDetailText.Text = isGpuRunning ? $"{tempStr}{gpuGFlops:F0} GFLOPS | {_stressEngine.VramDisplay}" : $"{tempStr}{_stressEngine.VramDisplay}";

                    // Elapsed time
                    if (anyRunning)
                    {
                        ElapsedTimeText.Text = elapsed.ToString(@"hh\:mm\:ss");
                    }

                    // CPU test metrics
                    if (_stressEngine.IsCpuRunning)
                    {
                        CpuUsageText.Text = $"{cpuUsage}%";
                        CpuStressGraph.AddValue(cpuUsage);
                        CpuScoreText.Text = $"{_stressEngine.CpuIterations:N0}";
                    }

                    // RAM test metrics
                    if (_stressEngine.IsRamRunning)
                    {
                        RamUsageText.Text = $"{ramUsage}%";
                        RamStressGraph.AddValue(ramUsage);
                    }

                    // GPU test metrics
                    GpuUsageText.Text = $"{gpuUsage:F0}%";
                    GpuStressGraph.AddValue(gpuUsage);
                    GpuFpsText.Text = isGpuRunning ? $"{gpuGFlops:F1} GFLOPS" : "--";
                });

                await Task.Delay(500, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _ = LogHelper.LogWarning($"Monitor loop error: {ex.Message}");
        }
    }

    private void StartAll_Click(object sender, RoutedEventArgs e)
    {
        _testStartTime = DateTime.UtcNow;
        if (!_stressEngine.IsCpuRunning) StartCpuTest();
        if (!_stressEngine.IsRamRunning) StartRamTest();
        if (!_stressEngine.IsDiskRunning) StartDiskTest();
        if (!_stressEngine.IsGpuRunning) StartGpuTest();

        StartAllButton.IsEnabled = false;
        StopAllButton.IsEnabled = true;
    }

    private void StopAll_Click(object sender, RoutedEventArgs e) => StopAllTests();

    private void StopAllTests()
    {
        StopCpuTest();
        StopRamTest();
        StopDiskTest();
        StopGpuTest();

        DispatcherQueue?.TryEnqueue(() =>
        {
            StartAllButton.IsEnabled = true;
            StopAllButton.IsEnabled = false;
        });
    }

    private void UpdateGlobalButtons()
    {
        var anyRunning = _stressEngine.IsCpuRunning || _stressEngine.IsRamRunning
                      || _stressEngine.IsDiskRunning || _stressEngine.IsGpuRunning;
        StartAllButton.IsEnabled = !anyRunning;
        StopAllButton.IsEnabled = anyRunning;
    }

    //  CPU Stress Test
    private void CpuStart_Click(object sender, RoutedEventArgs e)
    {
        if (_stressEngine.IsCpuRunning) { StopCpuTest(); return; }
        _testStartTime = DateTime.UtcNow;
        StartCpuTest();
    }

    private void StartCpuTest()
    {
        CpuStartIcon.Glyph = "\uE71A";
        CpuStartText.Text = "StressTestPage_Stop.Text".TryGetLocalized() ?? "Stop";
        CpuStatusText.Text = "StressTestPage_Running.Text".TryGetLocalized() ?? "Running";
        CpuThreadsText.Text = _selectedCpuIndices.Count.ToString();

        // Disable the selector while test is running
        SelectCoresButton.IsEnabled = false;
        UpdateGlobalButtons();

        _stressEngine.StartCpu(_selectedCpuIndices);
    }

    private void StopCpuTest() => _stressEngine.StopCpu();

    private void OnCpuStopped()
    {
        DispatcherQueue?.TryEnqueue(() =>
        {
            CpuStartIcon.Glyph = "\uE768";
            CpuStartText.Text = "StressTestPage_Start.Text".TryGetLocalized() ?? "Start";
            CpuStatusText.Text = "StressTestPage_Idle.Text".TryGetLocalized() ?? "Idle";
            SelectCoresButton.IsEnabled = true;
            UpdateGlobalButtons();
        });
    }

    //  RAM Stress Test
    private void RamStart_Click(object sender, RoutedEventArgs e)
    {
        if (_stressEngine.IsRamRunning) { StopRamTest(); return; }
        _testStartTime = DateTime.UtcNow;
        StartRamTest();
    }

    private void StartRamTest()
    {
        var percentIndex = RamPercentCombo.SelectedIndex;
        var targetPercent = percentIndex switch { 0 => 0.25, 1 => 0.50, 2 => 0.75, 3 => 0.90, _ => 0.25 };

        RamPercentCombo.IsEnabled = false;
        RamStartIcon.Glyph = "\uE71A";
        RamStartText.Text = "StressTestPage_Stop.Text".TryGetLocalized() ?? "Stop";
        RamStatusText.Text = "StressTestPage_Allocating.Text".TryGetLocalized() ?? "Allocating";
        UpdateGlobalButtons();

        _stressEngine.StartRam(targetPercent);
    }

    private void StopRamTest() => _stressEngine.StopRam();

    private void OnRamStopped()
    {
        DispatcherQueue?.TryEnqueue(() =>
        {
            RamPercentCombo.IsEnabled = true;
            RamStartIcon.Glyph = "\uE768";
            RamStartText.Text = "StressTestPage_Start.Text".TryGetLocalized() ?? "Start";
            RamStatusText.Text = "StressTestPage_Idle.Text".TryGetLocalized() ?? "Idle";
            RamAllocatedText.Text = "0 MB";
            RamBandwidthText.Text = "--";
            UpdateGlobalButtons();
        });
    }

    private void OnRamAllocated(long allocatedMb)
    {
        DispatcherQueue?.TryEnqueue(() =>
        {
            RamAllocatedText.Text = $"{allocatedMb} MB";
            RamStatusText.Text = "StressTestPage_Running.Text".TryGetLocalized() ?? "Running";
        });
    }

    private void OnRamBandwidth(double bandwidthMBps)
    {
        DispatcherQueue?.TryEnqueue(() => RamBandwidthText.Text = $"{bandwidthMBps:F0} MB/s");
    }

    //  Disk Stress Test
    private void DiskStart_Click(object sender, RoutedEventArgs e)
    {
        if (_stressEngine.IsDiskRunning) { StopDiskTest(); return; }
        _testStartTime = DateTime.UtcNow;
        StartDiskTest();
    }

    private void StartDiskTest()
    {
        DiskStartIcon.Glyph = "\uE71A";
        DiskStartText.Text = "StressTestPage_Stop.Text".TryGetLocalized() ?? "Stop";
        DiskStatusText.Text = "StressTestPage_Running.Text".TryGetLocalized() ?? "Running";
        UpdateGlobalButtons();
        _stressEngine.StartDisk();
    }

    private void StopDiskTest() => _stressEngine.StopDisk();

    private void OnDiskStopped()
    {
        DispatcherQueue?.TryEnqueue(() =>
        {
            DiskStartIcon.Glyph = "\uE768";
            DiskStartText.Text = "StressTestPage_Start.Text".TryGetLocalized() ?? "Start";
            DiskStatusText.Text = "StressTestPage_Idle.Text".TryGetLocalized() ?? "Idle";
            UpdateGlobalButtons();
        });
    }

    private void OnDiskMetrics(double writeMBps, double readMBps, long totalIops)
    {
        DispatcherQueue?.TryEnqueue(() =>
        {
            DiskWriteText.Text = $"{writeMBps:F0} MB/s";
            DiskReadText.Text = $"{readMBps:F0} MB/s";
            DiskSummaryText.Text = $"{writeMBps:F0} / {readMBps:F0} MB/s";
            DiskIopsText.Text = $"{totalIops:N0}";
            DiskStressGraph.AddValue(Math.Min(writeMBps / 50.0 * 100.0, 100));
        });
    }

    //  GPU Stress Test
    private void GpuStart_Click(object sender, RoutedEventArgs e)
    {
        if (_stressEngine.IsGpuRunning) { StopGpuTest(); return; }
        _testStartTime = DateTime.UtcNow;
        StartGpuTest();
    }

    private void StartGpuTest()
    {
        if (_stressEngine.IsGpuRunning) return;
        _stressEngine.StartGpu();
        GpuStartIcon.Glyph = "\uE71A";
        GpuStartText.Text = "StressTestPage_Stop.Text".TryGetLocalized() ?? "Stop";
        GpuStatusText.Text = "StressTestPage_RunningHlsl.Text".TryGetLocalized() ?? "Running (HLSL Compute)";
        UpdateGlobalButtons();
        LogHelper.Log("[StressTestPage] Hardware HLSL GPU stress test started");
    }

    private void StopGpuTest() => _stressEngine.StopGpu();

    private void OnGpuStopped()
    {
        DispatcherQueue?.TryEnqueue(() =>
        {
            GpuStartIcon.Glyph = "\uE768";
            GpuStartText.Text = "StressTestPage_Start.Text".TryGetLocalized() ?? "Start";
            GpuStatusText.Text = "StressTestPage_Idle.Text".TryGetLocalized() ?? "Idle";
            GpuFpsText.Text = "--";
            UpdateGlobalButtons();
        });
        LogHelper.Log("[StressTestPage] Hardware GPU stress test stopped");
    }
}
