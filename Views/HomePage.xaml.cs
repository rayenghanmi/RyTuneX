using System.Diagnostics;
using System.Management;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Networking.Connectivity;

namespace RyTuneX.Views;

public sealed partial class HomePage : Page
{
    private readonly string _versionDescription;
    private readonly PerformanceCounter cpuCounter;
    private readonly PerformanceCounter diskCounter;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public HomePage()
    {
        InitializeComponent();
        LogHelper.Log("Initializing HomePage");
        _versionDescription = "Version " + SettingsPage.GetVersionDescription();
        // Initialize performance counters
        cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");

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

            while (!cancellationToken.IsCancellationRequested)
            {
                // Fetch real-time usage values concurrently
                var cpuUsageTask = Task.Run(() => GetCpuUsage());
                var ramUsageTask = Task.Run(() => GetRamUsage());
                var diskUsageTask = Task.Run(() => GetDiskUsage());
                var networkUploadUsageTask = Task.Run(() => GetNetworkUploadUsage());
                var networkDownloadUsageTask = Task.Run(() => GetNetworkDownloadUsage());
                var gpuUsageTask = Task.Run(() => GetGpuUsage());

                await Task.WhenAll(cpuUsageTask, ramUsageTask, diskUsageTask, networkUploadUsageTask, networkDownloadUsageTask, gpuUsageTask).ConfigureAwait(false);

                var cpuUsage = cpuUsageTask.Result;
                var ramUsage = ramUsageTask.Result;
                var diskUsage = diskUsageTask.Result;
                var networkUploadUsage = networkUploadUsageTask.Result;
                var networkDownloadUsage = networkDownloadUsageTask.Result;
                var gpuUsage = gpuUsageTask.Result;

                try
                {
                    // Update the UI on the main thread
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (this.Visibility == Visibility.Visible)
                        {
                            cpuUsageText.Text = $"{cpuUsage}%";
                            ramUsageText.Text = $"{ramUsage}%";
                            diskUsageText.Text = $"{diskUsage}%";
                            networkUploadUsageText.Text = $"{networkUploadUsage} KB";
                            networkDownloadUsageText.Text = $"{networkDownloadUsage} KB";
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
        return (int)cpuCounter.NextValue();
    }

    private int GetRamUsage()
    {
        var wql = new ObjectQuery("SELECT FreePhysicalMemory, TotalVisibleMemorySize FROM Win32_OperatingSystem");
        var searcher = new ManagementObjectSearcher(wql);
        foreach (ManagementObject queryObj in searcher.Get())
        {
            var freeMemory = Convert.ToUInt64(queryObj["FreePhysicalMemory"]);
            var totalMemory = Convert.ToUInt64(queryObj["TotalVisibleMemorySize"]);
            var usedMemory = totalMemory - freeMemory;
            return (int)((usedMemory * 100) / totalMemory);
        }
        return 0;
    }

    private int GetDiskUsage()
    {
        return (int)Math.Min(diskCounter.NextValue(), 100);
    }

    private static int GetNetworkDownloadUsage()
    {
        var firstBytes = GetTotalBytesReceived(); // Get total bytes received at a point in time
        Thread.Sleep(500); // Sleep for 500ms
        var secondBytes = GetTotalBytesReceived(); // Get total bytes received after the 500ms
        Debug.WriteLine($"Download: {secondBytes - firstBytes}");
        return (int)((secondBytes - firstBytes) / 1024); // Convert Bytes to KB
    }

    // Similar to GetNetworkDownloadUsage but for upload
    private static int GetNetworkUploadUsage()
    {
        var firstBytes = GetTotalBytesSent();
        Thread.Sleep(500);
        var secondBytes = GetTotalBytesSent();
        Debug.WriteLine($"Upload: {secondBytes - firstBytes}");
        return (int)((secondBytes - firstBytes) / 1024);
    }

    // Get total bytes received by all network interfaces
    private static long GetTotalBytesReceived()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
            .Sum(ni => ni.GetIPv4Statistics().BytesReceived);
    }

    // Same as GetTotalBytesReceived but for sent bytes
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
        catch (Exception ex)
        {
            LogHelper.LogError($"Failed to count Installed apps: {ex.Message}");
            return 0;
        }
    }

    private int GetProcessesCount()
    {
        return Process.GetProcesses().Length;
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

            gpuCounters.ForEach(x =>
            {
                _ = x.NextValue();
            });
            await Task.Delay(1000);
            gpuCounters.ForEach(x =>
            {
                result += x.NextValue();
            });
            return (int)result;
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
}
