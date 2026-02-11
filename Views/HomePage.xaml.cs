using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using RyTuneX.Contracts.Services;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using Windows.Management.Deployment;

namespace RyTuneX.Views;

public sealed partial class HomePage : Page
{
    private readonly string _versionDescription;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _initialized;

    // CPU sampling state
    private ulong _prevIdleTime;
    private ulong _prevKernelTime;
    private ulong _prevUserTime;
    private bool _cpuInitialized;

    // Network sampling state
    private long _prevBytesReceived = 0;
    private long _prevBytesSent = 0;
    private DateTime _lastSampleTime = DateTime.MinValue;

    // Cached Resources
    private readonly List<PerformanceCounter> _gpuCounters = new();
    private DriveInfo? _systemDriveInfo;

    private DateTime _lastGpuRefresh = DateTime.MinValue;
    private readonly TimeSpan _gpuRefreshInterval = TimeSpan.FromSeconds(5);

    public HomePage()
    {
        InitializeComponent();
        NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        LogHelper.Log("Initializing HomePage");
        _versionDescription = "Version " + SettingsPage.GetVersionDescription();

        Loaded += HomePage_Loaded;
        Unloaded += HomePage_Unloaded;
    }

    private void HomePage_Loaded(object sender, RoutedEventArgs e)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _ = UpdateSystemStatsAsync(_cancellationTokenSource.Token);
    }

    private async Task UpdateSystemStatsAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!_initialized)
            {
                // Initialize heavy counters on background thread once
                await Task.Run(() => InitializeGpuCounters(), cancellationToken);
                _systemDriveInfo = new DriveInfo(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) ?? "C:\\");
                _initialized = true;
            }

            // Fetch values that may change between navigations
            var installedAppsCount = await Task.Run(() => GetInstalledAppsCount(), cancellationToken).ConfigureAwait(false);
            var servicesCount = await Task.Run(() => GetServicesCount(), cancellationToken).ConfigureAwait(false);

            // Initial counts
            var processesCount = await Task.Run(() => Process.GetProcesses().Length, cancellationToken).ConfigureAwait(false);

            // Reset network sampling baseline
            _prevBytesReceived = GetTotalBytesReceived();
            _prevBytesSent = GetTotalBytesSent();
            _lastSampleTime = DateTime.UtcNow;

            while (!cancellationToken.IsCancellationRequested)
            {
                // Sample fast values synchronously
                var cpuUsage = GetCpuUsage();
                var ramUsage = GetRamUsage();
                var diskUsage = GetDiskUsage();

                // Network throughput computed based on previous sample and elapsed time
                var (networkUploadUsage, networkDownloadUsage) = GetNetworkThroughputMbps();

                // GPU usage remains somewhat heavier; run it without blocking UI but don't await long inside samplers
                var gpuUsage = GetGpuUsageFromCache();

                // Periodic Process update
                if (DateTime.UtcNow.Second % 2 == 0)
                {
                    processesCount = Process.GetProcesses().Length;
                }

                try
                {
                    // Update the UI on the main thread
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (Visibility == Visibility.Visible && !cancellationToken.IsCancellationRequested)
                        {
                            cpuUsageText.Text = $"{cpuUsage}%";
                            cpuGraph.AddValue(cpuUsage);

                            ramUsageText.Text = $"{ramUsage}%";
                            ramGraph.AddValue(ramUsage);

                            diskUsageText.Text = $"{diskUsage}%";
                            diskGraph.AddValue(diskUsage);

                            networkUploadUsageText.Text = $"{networkUploadUsage:F1} Mb";
                            networkDownloadUsageText.Text = $"{networkDownloadUsage:F1} Mb";
                            networkUploadGraph.AddValue(Math.Min(networkUploadUsage, 100));
                            networkDownloadGraph.AddValue(Math.Min(networkDownloadUsage, 100));

                            installedAppsCountText.Text = installedAppsCount.ToString();
                            processesCountText.Text = processesCount.ToString();
                            servicesCountText.Text = servicesCount.ToString();

                            gpuUsageText.Text = $"{gpuUsage}%";
                            gpuGraph.AddValue(gpuUsage);
                        }
                    });
                }
                catch (Exception ex)
                {
                    _ = LogHelper.LogWarning($"Error updating UI: {ex.Message}");
                }

                // Small delay between samples
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            _ = LogHelper.Log("UpdateSystemStats task was canceled.");
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogException(ex, "UpdateSystemStatsAsync");
        }
    }

    private void HomePage_Unloaded(object sender, RoutedEventArgs e)
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    private int GetCpuUsage()
    {
        if (!GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
        {
            return 0;
        }

        var idle = FileTimeToUInt64(idleTime);
        var kernel = FileTimeToUInt64(kernelTime);
        var user = FileTimeToUInt64(userTime);

        if (!_cpuInitialized)
        {
            _prevIdleTime = idle;
            _prevKernelTime = kernel;
            _prevUserTime = user;
            _cpuInitialized = true;
            return 0; // first sample can't determine usage
        }

        var idleDiff = idle - _prevIdleTime;
        var kernelDiff = kernel - _prevKernelTime;
        var userDiff = user - _prevUserTime;

        // kernel includes idle time on Windows, so subtract idle from kernel
        var system = kernelDiff + userDiff;
        var total = system;

        var usage = 0.0;
        if (total > 0)
        {
            usage = (system - idleDiff) * 100.0 / total;
        }

        _prevIdleTime = idle;
        _prevKernelTime = kernel;
        _prevUserTime = user;

        return (int)Math.Clamp(usage, 0, 100);
    }

    private static ulong FileTimeToUInt64(FILETIME ft)
    {
        return ((ulong)ft.dwHighDateTime << 32) | ft.dwLowDateTime;
    }

    private int GetRamUsage()
    {
        var memStatus = new MEMORYSTATUSEX();
        if (!GlobalMemoryStatusEx(ref memStatus))
        {
            return 0;
        }

        return (int)memStatus.dwMemoryLoad;
    }

    private int GetDiskUsage()
    {
        try
        {
            if (_systemDriveInfo == null || !_systemDriveInfo.IsReady) return 0;

            var total = _systemDriveInfo.TotalSize;
            var free = _systemDriveInfo.TotalFreeSpace;
            var used = total - free;
            var percent = total == 0 ? 0 : (int)((used * 100L) / total);
            return Math.Clamp(percent, 0, 100);
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogWarning($"Error reading disk usage: {ex.Message}");
            return 0;
        }
    }

    // Network throughput (Kbps) computed from previous sample
    private (double uploadKbps, double downloadKbps) GetNetworkThroughputMbps()
    {
        try
        {
            var now = DateTime.UtcNow;
            var currentReceived = GetTotalBytesReceived();
            var currentSent = GetTotalBytesSent();

            if (_lastSampleTime == DateTime.MinValue)
            {
                _prevBytesReceived = currentReceived;
                _prevBytesSent = currentSent;
                _lastSampleTime = now;
                return (0.0, 0.0);
            }

            var elapsed = (now - _lastSampleTime).TotalSeconds;
            if (elapsed < 0.1) return (0.0, 0.0);

            var deltaReceived = currentReceived - _prevBytesReceived;
            var deltaSent = currentSent - _prevBytesSent;

            if (deltaReceived < 0 || deltaSent < 0)
            {
                _prevBytesReceived = currentReceived;
                _prevBytesSent = currentSent;
                _lastSampleTime = now;
                return (0.0, 0.0);
            }

            // Mbps (Megabits per second) = (Bytes * 8) / 1,000,000
            var downloadMbps = (deltaReceived * 8.0) / 1_000_000.0 / elapsed;
            var uploadMbps = (deltaSent * 8.0) / 1_000_000.0 / elapsed;

            _prevBytesReceived = currentReceived;
            _prevBytesSent = currentSent;
            _lastSampleTime = now;

            return (Math.Round(uploadMbps, 1), Math.Round(downloadMbps, 1));
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogWarning($"Error reading network throughput: {ex.Message}");
            return (0.0, 0.0);
        }
    }

    private static long GetTotalBytesReceived()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
            .Sum(ni => ni.GetIPStatistics().BytesReceived);
    }

    // Get total bytes sent
    private static long GetTotalBytesSent()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
            .Sum(ni => ni.GetIPStatistics().BytesSent);
    }

    private int GetInstalledAppsCount()
    {
        try
        {
            var packageManager = new PackageManager();
            return packageManager.FindPackages().Count();
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogWarning($"Error getting installed apps count: {ex.Message}");
            return 0;
        }
    }

    private int GetServicesCount()
    {
        return ServiceController.GetServices().Length;
    }

    // Initialize Counters once, don't recreate them in a loop
    private void InitializeGpuCounters()
    {
        try
        {
            var category = new PerformanceCounterCategory("GPU Engine");
            var instanceNames = category.GetInstanceNames();

            foreach (var instanceName in instanceNames)
            {
                if (instanceName.Contains("engtype_3D"))
                {
                    foreach (var counter in category.GetCounters(instanceName))
                    {
                        if (counter.CounterName == "Utilization Percentage")
                        {
                            _gpuCounters.Add(counter);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogWarning($"Failed to init GPU counters: {ex.Message}");
        }
    }

    private int GetGpuUsageFromCache()
    {
        // Periodically rebuild counters
        if (_gpuCounters.Count == 0 ||
            DateTime.UtcNow - _lastGpuRefresh > _gpuRefreshInterval)
        {
            RefreshGpuCounters();
        }

        if (_gpuCounters.Count == 0)
            return 0;

        float result = 0f;

        // Iterate backwards so we can safely remove dead counters
        for (int i = _gpuCounters.Count - 1; i >= 0; i--)
        {
            var counter = _gpuCounters[i];
            try
            {
                result += counter.NextValue();
            }
            catch (InvalidOperationException)
            {
                // Remove instances that no longer exist
                counter.Dispose();
                _gpuCounters.RemoveAt(i);
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogWarning($"GPU counter error: {ex.Message}");
            }
        }

        return (int)Math.Clamp(result, 0, 100);
    }

    private void RefreshGpuCounters()
    {
        try
        {
            foreach (var counter in _gpuCounters)
                counter.Dispose();

            _gpuCounters.Clear();

            InitializeGpuCounters();
            _lastGpuRefresh = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogWarning($"Failed to refresh GPU counters: {ex.Message}");
        }
    }

    private void GithubButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://rayenghanmi.github.io/rytunex",
            UseShellExecute = true
        });
    }

    private void DiscordButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://discord.gg/gyBzyd364t",
            UseShellExecute = true
        });
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public MEMORYSTATUSEX()
        {
            dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            dwMemoryLoad = 0;
            ullTotalPhys = 0;
            ullAvailPhys = 0;
            ullTotalPageFile = 0;
            ullAvailPageFile = 0;
            ullTotalVirtual = 0;
            ullAvailVirtual = 0;
            ullAvailExtendedVirtual = 0;
        }
    }

    private void InteractiveBlock_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            grid.Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"];
            ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Hand);
        }
    }

    private void InteractiveBlock_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            grid.Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
            ProtectedCursor = null;
        }
    }

    private void InstalledAppsBlock_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // Navigate to the Debloat page
        var navigationService = App.GetService<INavigationService>();
        navigationService.NavigateTo(typeof(DebloatSystemPage).FullName!);
    }

    private void ProcessesBlock_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // Navigate to the Processes page
        var navigationService = App.GetService<INavigationService>();
        navigationService.NavigateTo(typeof(ProcessesPage).FullName!);
    }

    private void ServicesBlock_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // Navigate to the Services page
        var navigationService = App.GetService<INavigationService>();
        navigationService.NavigateTo(typeof(ServicesPage).FullName!);
    }
}