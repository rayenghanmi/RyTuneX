using System.Diagnostics;
using System.Management;
using Microsoft.UI.Xaml.Controls;
using Windows.Networking.Connectivity;
using Microsoft.UI.Xaml;
using System.ServiceProcess;
using RyTuneX.Helpers;

namespace RyTuneX.Views;

public sealed partial class HomePage : Page
{
    private readonly string _versionDescription;
    private readonly PerformanceCounter cpuCounter;
    private readonly PerformanceCounter diskCounter;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool isFirstUpdate = true;

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
        this.Unloaded += HomePage_Unloaded;
    }

    private async Task UpdateSystemStatsAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();

                // Fetch real-time usage values
                var cpuUsage = await Task.Run(() => GetCpuUsage());
                var ramUsage = await Task.Run(() => GetRamUsage());
                var diskUsage = await Task.Run(() => GetDiskUsage());
                var networkUsage = await Task.Run(() => GetNetworkUsage());
                var installedAppsCount = await Task.Run(() => GetInstalledAppsCount());
                var processesCount = await Task.Run(() => GetProcessesCount());
                var servicesCount = await Task.Run(() => GetServicesCount());
                var gpuUsage = await Task.Run(() => GetGpuUsage());

                // Update the UI with the fetched values
                DispatcherQueue.TryEnqueue(() =>
                {
                    cpuUsageText.Text = $"{cpuUsage}%";
                    ramUsageText.Text = $"{ramUsage}%";
                    diskUsageText.Text = $"{diskUsage}%";
                    networkUsageText.Text = $"{networkUsage}%";
                    installedAppsCountText.Text = installedAppsCount.ToString();
                    processesCountText.Text = processesCount.ToString();
                    servicesCountText.Text = servicesCount.ToString();
                    gpuUsageText.Text = $"{gpuUsage}%";

                    if (isFirstUpdate)
                    {
                        cpuUsageRing.Visibility = Visibility.Collapsed;
                        cpuUsageText.Visibility = Visibility.Visible;

                        ramUsageRing.Visibility = Visibility.Collapsed;
                        ramUsageText.Visibility = Visibility.Visible;

                        diskUsageRing.Visibility = Visibility.Collapsed;
                        diskUsageText.Visibility = Visibility.Visible;

                        networkUsageRing.Visibility = Visibility.Collapsed;
                        networkUsageText.Visibility = Visibility.Visible;

                        installedAppsCountRing.Visibility = Visibility.Collapsed;
                        installedAppsCountText.Visibility = Visibility.Visible;

                        processesCountRing.Visibility = Visibility.Collapsed;
                        processesCountText.Visibility = Visibility.Visible;

                        servicesCountRing.Visibility = Visibility.Collapsed;
                        servicesCountText.Visibility = Visibility.Visible;

                        gpuUsageRing.Visibility = Visibility.Collapsed;
                        gpuUsageText.Visibility = Visibility.Visible;

                        // Only allow to hide the progress ring on the first update
                        isFirstUpdate = false;
                    }
                });

                await Task.Delay(1000, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("UpdateSystemStats task was canceled.");
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
        ObjectQuery wql = new ObjectQuery("SELECT FreePhysicalMemory, TotalVisibleMemorySize FROM Win32_OperatingSystem");
        ManagementObjectSearcher searcher = new ManagementObjectSearcher(wql);
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
        return (int)diskCounter.NextValue();
    }

    private int GetNetworkUsage()
    {
        ConnectionProfile profile = NetworkInformation.GetInternetConnectionProfile();
        if (profile != null)
        {
            var usageStates = new NetworkUsageStates
            {
                Roaming = TriStates.DoNotCare,
                Shared = TriStates.DoNotCare
            };

            var usageDetails = profile.GetNetworkUsageAsync(
                DateTime.Now.AddMinutes(-1),
                DateTime.Now,
                DataUsageGranularity.PerMinute,
                usageStates).GetAwaiter().GetResult();

            foreach (var usage in usageDetails)
            {
                return (int)((usage.BytesReceived / 60) / 1024);
            }
        }
        return 0;
    }

    private int GetInstalledAppsCount()
    {
        var apps = OptimizationOptions.GetUWPApps(false);
        return apps.Count() - 3;
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

    private int GetGpuUsage()
    {
        var gpuUsageQuery = new ObjectQuery("SELECT * FROM Win32_VideoController");
        var searcher = new ManagementObjectSearcher(gpuUsageQuery);
        foreach (ManagementObject queryObj in searcher.Get())
        {
            return Convert.ToInt32(queryObj["CurrentVerticalResolution"]);
        }
        return 0;
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
