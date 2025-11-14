using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace RyTuneX.Views;

public sealed partial class HomePage : Page
{
    private readonly string _versionDescription;
    private readonly CancellationTokenSource _cancellationTokenSource;

    // CPU sampling state
    private ulong _prevIdleTime = 0;
    private ulong _prevKernelTime = 0;
    private ulong _prevUserTime = 0;
    private bool _cpuInitialized = false;

    // Network sampling state
    private long _prevBytesReceived = 0;
    private long _prevBytesSent = 0;
    private DateTime _lastSampleTime = DateTime.UtcNow;

    public HomePage()
    {
        InitializeComponent();
        LogHelper.Log("Initializing HomePage");
        _versionDescription = "Version " + SettingsPage.GetVersionDescription();

        _cancellationTokenSource = new CancellationTokenSource();
        _ = UpdateSystemStatsAsync(_cancellationTokenSource.Token);
        Unloaded += HomePage_Unloaded;
    }

    private async Task UpdateSystemStatsAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Fetch static values once at the beginning
            var installedAppsCount = await Task.Run(() => GetInstalledAppsCount()).ConfigureAwait(false);
            var servicesCount = await Task.Run(() => GetServicesCount()).ConfigureAwait(false);
            var processesCount = await Task.Run(() => GetProcessesCount()).ConfigureAwait(false);

            // Initialize network counters
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
                var (networkUploadUsage, networkDownloadUsage) = GetNetworkThroughputKbps();

                // GPU usage remains somewhat heavier; run it without blocking UI but don't await long inside samplers
                var gpuUsageTask = Task.Run(() => GetGpuUsage());

                var gpuUsage = await gpuUsageTask.ConfigureAwait(false);

                try
                {
                    // Update the UI on the main thread
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (this.Visibility == Visibility.Visible && !cancellationToken.IsCancellationRequested)
                        {
                            cpuUsageText.Text = $"{cpuUsage}%";
                            ramUsageText.Text = $"{ramUsage}%";
                            diskUsageText.Text = $"{diskUsage}%";
                            networkUploadUsageText.Text = $"{networkUploadUsage} Kbps";
                            networkDownloadUsageText.Text = $"{networkDownloadUsage} Kbps";
                            installedAppsCountText.Text = installedAppsCount.ToString();
                            processesCountText.Text = processesCount.ToString();
                            servicesCountText.Text = servicesCount.ToString();
                            gpuUsageText.Text = $"{gpuUsage}%";
                        }
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error updating UI: {ex.Message}");
                }

                // Small delay between samples
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("UpdateSystemStats task was canceled.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unexpected error: {ex.Message}");
        }
    }


    private void HomePage_Unloaded(object sender, RoutedEventArgs e)
    {
        _cancellationTokenSource.Cancel();
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
            var systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) ?? "C:\\";
            var di = new DriveInfo(systemDrive);
            if (!di.IsReady)
                return 0;

            var total = di.TotalSize;
            var free = di.TotalFreeSpace;
            var used = total - free;
            var percent = total == 0 ? 0 : (int)((used * 100L) / total);
            return Math.Clamp(percent, 0, 100);
        }
        catch
        {
            return 0;
        }
    }

    // Network throughput (Kbps) computed from previous sample
    private (int uploadKbps, int downloadKbps) GetNetworkThroughputKbps()
    {
        try
        {
            var now = DateTime.UtcNow;
            var elapsed = (now - _lastSampleTime).TotalSeconds;
            if (elapsed <= 0)
                elapsed = 0.5; // fallback

            var currentReceived = GetTotalBytesReceived();
            var currentSent = GetTotalBytesSent();

            var deltaReceived = currentReceived - _prevBytesReceived;
            var deltaSent = currentSent - _prevBytesSent;

            // KB/s
            var downloadKbps = (int)(deltaReceived / 1024.0 / elapsed);
            var uploadKbps = (int)(deltaSent / 1024.0 / elapsed);

            _prevBytesReceived = currentReceived;
            _prevBytesSent = currentSent;
            _lastSampleTime = now;

            return (uploadKbps, downloadKbps);
        }
        catch
        {
            return (0, 0);
        }
    }

    // Get total bytes received by all network interfaces
    private static long GetTotalBytesReceived()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
            .Sum(ni => ni.GetIPv4Statistics().BytesReceived);
    }

    // Get total bytes sent
    private static long GetTotalBytesSent()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
            .Sum(ni => ni.GetIPv4Statistics().BytesSent);
    }

    private int GetInstalledAppsCount()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "-Command \"(Get-AppxPackage -AllUsers | Select-Object -Unique Name).Count\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            return process != null && int.TryParse(process.StandardOutput.ReadToEnd().Trim(), out var appCount)
                ? appCount
                : 0;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    private async Task<int> GetProcessesCount()
    {
        return await Task.Run(() => Process.GetProcesses().Length);
    }

    private int GetServicesCount()
    {
        var services = ServiceController.GetServices();
        return services.Length;
    }

    public static async Task<int> GetGpuUsage()
    {
        try
        {
            var category = new PerformanceCounterCategory("GPU Engine");
            var counterNames = category.GetInstanceNames();
            var gpuCounters = new List<PerformanceCounter>();
            var result = 0f;

            foreach (var counterName in counterNames)
            {
                if (counterName.EndsWith("engtype_3D"))
                {
                    foreach (var counter in category.GetCounters(counterName))
                    {
                        if (counter.CounterName == "Utilization Percentage")
                        {
                            gpuCounters.Add(counter);
                        }
                    }
                }
            }

            gpuCounters.ForEach(x => _ = x.NextValue());
            await Task.Delay(200);
            gpuCounters.ForEach(x => result += x.NextValue());
            return (int)Math.Clamp(result, 0, 100);
        }
        catch
        {
            return 0;
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
}
