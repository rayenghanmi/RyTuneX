using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RyTuneX.Contracts.Services;
using RyTuneX.Helpers;
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

    private DriveInfo? _systemDriveInfo;

    public HomePage()
    {
        InitializeComponent();
        NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        LogHelper.Log("Initializing HomePage");
        _versionDescription = "HomePage_Version".GetLocalized() + " " + SettingsPage.GetVersionDescription();

        Loaded += HomePage_Loaded;
        Unloaded += HomePage_Unloaded;

        // Apply lighting to Usage Cards
        cpuUsage.Lights.Add(new AmbLight()); cpuUsage.Lights.Add(new HoverLight());
        ramUsage.Lights.Add(new AmbLight()); ramUsage.Lights.Add(new HoverLight());
        diskUsage.Lights.Add(new AmbLight()); diskUsage.Lights.Add(new HoverLight());
        networkUsage.Lights.Add(new AmbLight()); networkUsage.Lights.Add(new HoverLight());
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
                _systemDriveInfo = new DriveInfo(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) ?? "C:\\");
                _initialized = true;
            }

            var installedAppsCount = await Task.Run(() => GetInstalledAppsCount(), cancellationToken).ConfigureAwait(false);
            var servicesCount = await Task.Run(() => GetServicesCount(), cancellationToken).ConfigureAwait(false);
            var processesCount = await Task.Run(() =>
            {
                var procs = Process.GetProcesses();
                var count = procs.Length;
                foreach (var p in procs) p.Dispose();
                return count;
            }, cancellationToken).ConfigureAwait(false);

            var (initRx, initTx) = GetTotalNetworkBytes();
            _prevBytesReceived = initRx;
            _prevBytesSent = initTx;
            _lastSampleTime = DateTime.UtcNow;

            var processCountTick = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                var cpuUsageVal = GetCpuUsage();
                var ramUsageVal = GetRamUsage();
                var diskUsageVal = GetDiskUsage();
                var (networkUploadUsage, networkDownloadUsage) = GetNetworkThroughputMbps();

                if (++processCountTick >= 15)
                {
                    processCountTick = 0;
                    try
                    {
                        var procs = Process.GetProcesses();
                        processesCount = procs.Length;
                        foreach (var p in procs) p.Dispose();
                    }
                    catch { }
                }

                try
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (Visibility == Visibility.Visible && !cancellationToken.IsCancellationRequested)
                        {
                            cpuUsageText.Text = $"{cpuUsageVal}%";
                            cpuGraph.AddValue(cpuUsageVal);

                            ramUsageText.Text = $"{ramUsageVal}%";
                            ramGraph.AddValue(ramUsageVal);

                            diskUsageText.Text = $"{diskUsageVal}%";
                            diskGraph.AddValue(diskUsageVal);

                            networkUploadUsageText.Text = $"{networkUploadUsage:F1} Mb";
                            networkDownloadUsageText.Text = $"{networkDownloadUsage:F1} Mb";
                            networkUploadGraph.AddValue(Math.Min(networkUploadUsage, 100));
                            networkDownloadGraph.AddValue(Math.Min(networkDownloadUsage, 100));

                            installedAppsCountText.Text = installedAppsCount.ToString();
                            processesCountText.Text = processesCount.ToString();
                            servicesCountText.Text = servicesCount.ToString();
                        }
                    });
                }
                catch (Exception ex)
                {
                    _ = LogHelper.LogWarning($"Error updating UI: {ex.Message}");
                }

                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
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
        if (!GetSystemTimes(out var idleTime, out var kernelTime, out var userTime)) return 0;

        var idle = FileTimeToUInt64(idleTime);
        var kernel = FileTimeToUInt64(kernelTime);
        var user = FileTimeToUInt64(userTime);

        if (!_cpuInitialized)
        {
            _prevIdleTime = idle; _prevKernelTime = kernel; _prevUserTime = user;
            _cpuInitialized = true;
            return 0;
        }

        var idleDiff = idle - _prevIdleTime;
        var kernelDiff = kernel - _prevKernelTime;
        var userDiff = user - _prevUserTime;

        var total = kernelDiff + userDiff;
        var usage = total > 0 ? (total - idleDiff) * 100.0 / total : 0.0;

        _prevIdleTime = idle; _prevKernelTime = kernel; _prevUserTime = user;

        return (int)Math.Clamp(usage, 0, 100);
    }

    private static ulong FileTimeToUInt64(FILETIME ft) => ((ulong)ft.dwHighDateTime << 32) | ft.dwLowDateTime;

    private int GetRamUsage()
    {
        var memStatus = new MEMORYSTATUSEX();
        return GlobalMemoryStatusEx(ref memStatus) ? (int)memStatus.dwMemoryLoad : 0;
    }

    private int GetDiskUsage()
    {
        try
        {
            if (_systemDriveInfo == null || !_systemDriveInfo.IsReady) return 0;
            var total = _systemDriveInfo.TotalSize;
            var used = total - _systemDriveInfo.TotalFreeSpace;
            return total == 0 ? 0 : Math.Clamp((int)((used * 100L) / total), 0, 100);
        }
        catch { return 0; }
    }

    private (double uploadKbps, double downloadKbps) GetNetworkThroughputMbps()
    {
        try
        {
            var now = DateTime.UtcNow;
            var (currentReceived, currentSent) = GetTotalNetworkBytes();

            if (_lastSampleTime == DateTime.MinValue)
            {
                _prevBytesReceived = currentReceived; _prevBytesSent = currentSent; _lastSampleTime = now;
                return (0.0, 0.0);
            }

            var elapsed = (now - _lastSampleTime).TotalSeconds;
            if (elapsed < 0.1) return (0.0, 0.0);

            var deltaReceived = currentReceived - _prevBytesReceived;
            var deltaSent = currentSent - _prevBytesSent;

            if (deltaReceived < 0 || deltaSent < 0)
            {
                _prevBytesReceived = currentReceived; _prevBytesSent = currentSent; _lastSampleTime = now;
                return (0.0, 0.0);
            }

            var downloadMbps = (deltaReceived * 8.0) / 1_000_000.0 / elapsed;
            var uploadMbps = (deltaSent * 8.0) / 1_000_000.0 / elapsed;

            _prevBytesReceived = currentReceived; _prevBytesSent = currentSent; _lastSampleTime = now;
            return (Math.Round(uploadMbps, 1), Math.Round(downloadMbps, 1));
        }
        catch { return (0.0, 0.0); }
    }

    private static (long Rx, long Tx) GetTotalNetworkBytes()
    {
        try
        {
            long rx = 0;
            long tx = 0;
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up)
                {
                    var stats = ni.GetIPStatistics();
                    rx += stats.BytesReceived;
                    tx += stats.BytesSent;
                }
            }
            return (rx, tx);
        }
        catch
        {
            return (0, 0);
        }
    }

    private int GetInstalledAppsCount()
    {
        try { return new PackageManager().FindPackages().Count(); } catch { return 0; }
    }

    private int GetServicesCount() => ServiceController.GetServices().Length;

    private void GithubButton_Click(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo { FileName = "https://rayenghanmi.github.io/rytunex", UseShellExecute = true });
    private void DiscordButton_Click(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo { FileName = "https://discord.gg/gyBzyd364t", UseShellExecute = true });

    // Navigation handlers — System section
    private void InstalledApps_Click(object sender, RoutedEventArgs e) => App.GetService<INavigationService>().NavigateTo(typeof(DebloatSystemPage).FullName!);
    private void Processes_Click(object sender, RoutedEventArgs e) => App.GetService<INavigationService>().NavigateTo(typeof(ProcessesPage).FullName!);
    private void Services_Click(object sender, RoutedEventArgs e) => App.GetService<INavigationService>().NavigateTo(typeof(ServicesPage).FullName!);

    // Navigation handlers — Quick Actions section
    private void LaunchIntelligentOptimizer_Click(object sender, RoutedEventArgs e) => App.GetService<INavigationService>().NavigateTo(typeof(OptimizeSystemPage).FullName!);
    private void OptimizeSystem_Click(object sender, RoutedEventArgs e) => App.GetService<INavigationService>().NavigateTo(typeof(OptimizeSystemPage).FullName!);
    private void RepairIssues_Click(object sender, RoutedEventArgs e) => App.GetService<INavigationService>().NavigateTo(typeof(RepairPage).FullName!);
    private void CleanTemporaryFiles_Click(object sender, RoutedEventArgs e) => App.GetService<INavigationService>().NavigateTo(typeof(DebloatSystemPage).FullName!);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME { public uint dwLowDateTime; public uint dwHighDateTime; }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength; public uint dwMemoryLoad; public ulong ullTotalPhys; public ulong ullAvailPhys;
        public ulong ullTotalPageFile; public ulong ullAvailPageFile; public ulong ullTotalVirtual;
        public ulong ullAvailVirtual; public ulong ullAvailExtendedVirtual;
        public MEMORYSTATUSEX() { dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)); dwMemoryLoad = 0; ullTotalPhys = 0; ullAvailPhys = 0; ullTotalPageFile = 0; ullAvailPageFile = 0; ullTotalVirtual = 0; ullAvailVirtual = 0; ullAvailExtendedVirtual = 0; }
    }
}