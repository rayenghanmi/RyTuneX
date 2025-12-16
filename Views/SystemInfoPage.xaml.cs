using System.Management;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using RyTuneX.Helpers;

namespace RyTuneX.Views;

public sealed partial class SystemInfoPage : Page
{
    public SystemInfoPage()
    {
        InitializeComponent();
        LogHelper.Log("Initializing SystemInfoPage");
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        _ = UpdateSystemInfoAsync();
    }

    private async Task UpdateSystemInfoAsync()
    {
        try
        {
            await LogHelper.Log("Updating SystemInfo");

            var osTask = Task.Run(GetOsInformation);
            var cpuTask = Task.Run(GetCpuInformation);
            var gpuTask = Task.Run(GetGpuInformation);
            var ramTask = Task.Run(GetRamInformation);
            var diskTask = Task.Run(GetDiskInformation);
            var networkTask = Task.Run(GetNetworkInformation);
            var batteryTask = Task.Run(GetBatteryInformation);

            await Task.WhenAll(osTask, cpuTask, gpuTask, ramTask, diskTask, networkTask, batteryTask).ConfigureAwait(false);

            var osInformation = await osTask.ConfigureAwait(false);
            var cpuInformation = await cpuTask.ConfigureAwait(false);
            var gpuInformation = await gpuTask.ConfigureAwait(false);
            var ramInformation = await ramTask.ConfigureAwait(false);
            var diskInformation = await diskTask.ConfigureAwait(false);
            var networkInformation = await networkTask.ConfigureAwait(false);
            var batteryInformation = await batteryTask.ConfigureAwait(false);

            DispatcherQueue.TryEnqueue(() =>
            {
                os.Text = osInformation;
                cpu.Text = cpuInformation;
                gpu.Text = gpuInformation;
                ram.Text = ramInformation;
                disk.Text = diskInformation;
                network.Text = networkInformation;
                battery.Text = batteryInformation;

                loadingProgressRing.Visibility = Visibility.Collapsed;
                infoPanel.Visibility = Visibility.Visible;
            });
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"Error updating system information: {ex}");
        }
    }

    private static async Task<string> GetCpuInformation()
    {
        try
        {
            await LogHelper.Log("Getting CPU Info").ConfigureAwait(false);
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            using var collection = searcher.Get();

            var sb = new StringBuilder(512);
            foreach (var cpu in collection.Cast<ManagementObject>())
            {
                sb.Append("Name".GetLocalized()).Append(": ").Append(cpu["Name"]).AppendLine()
                  .Append("Manufacturer".GetLocalized()).Append(": ").Append(cpu["Manufacturer"]).AppendLine()
                  .Append("Architecture".GetLocalized()).Append(": ").Append(cpu["Architecture"]).AppendLine()
                  .Append("Cores".GetLocalized()).Append(": ").Append(cpu["NumberOfCores"]).AppendLine()
                  .Append("LogicalProcessors".GetLocalized()).Append(": ").Append(cpu["NumberOfLogicalProcessors"]).AppendLine()
                  .Append("MaxSpeed".GetLocalized()).Append(": ").Append(cpu["MaxClockSpeed"]).Append(" MHz").AppendLine()
                  .Append("SocketDesignation".GetLocalized()).Append(": ").Append(cpu["SocketDesignation"]).AppendLine()
                  .Append("L2Cache".GetLocalized()).Append(": ").Append(cpu["L2CacheSize"]).Append(" KB").AppendLine()
                  .Append("L3Cache".GetLocalized()).Append(": ").Append(cpu["L3CacheSize"]).Append(" KB");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"Error getting CPU info: {ex}").ConfigureAwait(false);
            return string.Empty;
        }
    }

    private static async Task<string> GetGpuInformation()
    {
        try
        {
            await LogHelper.Log("Getting GPU Info").ConfigureAwait(false);
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            using var collection = searcher.Get();

            var sb = new StringBuilder(512);
            var gpuNumber = 0;

            foreach (var gpu in collection.Cast<ManagementObject>())
            {
                if (gpuNumber > 0) sb.AppendLine();

                sb.Append("GPU".GetLocalized()).Append(' ').Append(gpuNumber).AppendLine(":")
                  .Append("   ").Append("Name".GetLocalized()).Append(": ").Append(gpu["Caption"]).AppendLine()
                  .Append("   ").Append("AdapterRAM".GetLocalized()).Append(": ").Append(gpu["AdapterRAM"]).Append(" bytes").AppendLine()
                  .Append("   ").Append("DriverVersion".GetLocalized()).Append(": ").Append(gpu["DriverVersion"]).AppendLine()
                  .Append("   ").Append("VideoArchitecture".GetLocalized()).Append(": ").Append(gpu["VideoArchitecture"]);

                gpuNumber++;
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"Error getting GPU info: {ex}").ConfigureAwait(false);
            return string.Empty;
        }
    }

    private static async Task<string> GetRamInformation()
    {
        try
        {
            await LogHelper.Log("Getting RAM Info").ConfigureAwait(false);
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
            using var collection = searcher.Get();

            var sb = new StringBuilder(512);
            var moduleNumber = 1;

            foreach (var ram in collection.Cast<ManagementObject>())
            {
                if (moduleNumber > 1) sb.AppendLine();

                var capacityMB = ((ulong)ram["Capacity"]) / (1024 * 1024);
                sb.Append("RAMModule".GetLocalized()).Append(' ').Append(moduleNumber).AppendLine(":")
                  .Append("   ").Append("DeviceLocator".GetLocalized()).Append(": ").Append(ram["DeviceLocator"]).AppendLine()
                  .Append("   ").Append("Capacity".GetLocalized()).Append(": ").Append(capacityMB).Append(" MB").AppendLine()
                  .Append("   ").Append("Speed".GetLocalized()).Append(": ").Append(ram["Speed"]).Append(" MHz").AppendLine()
                  .Append("   ").Append("Manufacturer".GetLocalized()).Append(": ").Append(ram["Manufacturer"]);

                moduleNumber++;
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"Error getting RAM info: {ex}").ConfigureAwait(false);
            return string.Empty;
        }
    }

    private static async Task<string> GetDiskInformation()
    {
        try
        {
            await LogHelper.Log("Getting Disks Info").ConfigureAwait(false);
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
            using var collection = searcher.Get();

            var sb = new StringBuilder(512);
            var diskNumber = 0;

            foreach (var disk in collection.Cast<ManagementObject>())
            {
                if (diskNumber > 0) sb.AppendLine();

                sb.Append("Disk".GetLocalized()).Append(' ').Append(diskNumber).AppendLine(":")
                  .Append("   ").Append("Caption".GetLocalized()).Append(": ").Append(disk["Caption"]).AppendLine()
                  .Append("   ").Append("Size".GetLocalized()).Append(": ").Append(disk["Size"]).Append(" bytes").AppendLine()
                  .Append("   ").Append("InterfaceType".GetLocalized()).Append(": ").Append(disk["InterfaceType"]).AppendLine()
                  .Append("   ").Append("Manufacturer".GetLocalized()).Append(": ").Append(disk["Manufacturer"]).AppendLine()
                  .Append("   ").Append("Model".GetLocalized()).Append(": ").Append(disk["Model"]);

                diskNumber++;
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"Error getting Disk info: {ex}").ConfigureAwait(false);
            return string.Empty;
        }
    }

    private static async Task<string> GetOsInformation()
    {
        try
        {
            await LogHelper.Log("Getting OS Info").ConfigureAwait(false);
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            using var collection = searcher.Get();

            var sb = new StringBuilder(512);
            foreach (var os in collection.Cast<ManagementObject>())
            {
                sb.Append("OSName".GetLocalized()).Append(": ").Append(os["Caption"]).AppendLine()
                  .Append("Version".GetLocalized()).Append(": ").Append(os["Version"]).AppendLine()
                  .Append("BuildNumber".GetLocalized()).Append(": ").Append(os["BuildNumber"]).AppendLine()
                  .Append("Architecture".GetLocalized()).Append(": ").Append(os["OSArchitecture"]).AppendLine()
                  .Append("InstallDate".GetLocalized()).Append(": ").Append(os["InstallDate"]).AppendLine()
                  .Append("RegisteredUser".GetLocalized()).Append(": ").Append(os["RegisteredUser"]).AppendLine()
                  .Append("WindowsDirectory".GetLocalized()).Append(": ").Append(os["WindowsDirectory"]).AppendLine()
                  .Append("SystemDirectory".GetLocalized()).Append(": ").Append(os["SystemDirectory"]).AppendLine()
                  .Append("LastBoot".GetLocalized()).Append(": ").Append(os["LastBootUpTime"]);
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"Error getting OS info: {ex}").ConfigureAwait(false);
            return string.Empty;
        }
    }

    private static async Task<string> GetNetworkInformation()
    {
        try
        {
            await LogHelper.Log("Getting Network Info").ConfigureAwait(false);
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter WHERE NetEnabled = TRUE");
            using var collection = searcher.Get();

            var sb = new StringBuilder(512);
            var first = true;

            foreach (var network in collection.Cast<ManagementObject>())
            {
                if (!first) sb.AppendLine().AppendLine();
                first = false;

                sb.Append("Name".GetLocalized()).Append(": ").Append(network["Name"]).AppendLine()
                  .Append("MACAddress".GetLocalized()).Append(": ").Append(network["MACAddress"]).AppendLine()
                  .Append("Speed".GetLocalized()).Append(": ").Append(network["Speed"]).Append(" bps").AppendLine()
                  .Append("AdapterType".GetLocalized()).Append(": ").Append(network["AdapterType"]);
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"Error getting Network info: {ex}").ConfigureAwait(false);
            return "Error retrieving network information.";
        }
    }

    private static async Task<string> GetBatteryInformation()
    {
        try
        {
            await LogHelper.Log("Getting Battery Info").ConfigureAwait(false);
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery");
            using var collection = searcher.Get();

            var sb = new StringBuilder(256);
            var first = true;

            foreach (var battery in collection.Cast<ManagementObject>())
            {
                if (!first) sb.AppendLine().AppendLine();
                first = false;

                sb.Append("Name".GetLocalized()).Append(": ").Append(battery["Name"]).AppendLine()
                  .Append("EstimatedChargeRemaining".GetLocalized()).Append(": ").Append(battery["EstimatedChargeRemaining"]).Append("%").AppendLine()
                  .Append("BatteryStatus".GetLocalized()).Append(": ").Append(battery["BatteryStatus"]).AppendLine()
                  .Append("Chemistry".GetLocalized()).Append(": ").Append(battery["Chemistry"]);
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"Error getting Battery info: {ex}").ConfigureAwait(false);
            return "Error retrieving battery information.";
        }
    }

    private async void ExtractButton_Click(object sender, RoutedEventArgs e)
    {
        var folderPath = FolderPathText.Text + $"\\RyTuneX_Drivers_{DateTime.Now:yyyy-MM-dd}";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        ExtractingStatusText.Text = "ExtractingDrivers".GetLocalized() + "...";
        ExtractingStatusPb.ShowError = false;
        ExtractingStatus.Visibility = Visibility.Visible;
        try
        {
            var exitCode = await OptimizationOptions.StartInCmd($"powershell Export-WindowsDriver -Online -Destination '{folderPath}'");
            if (exitCode == 0)
            {
                ExtractingStatusText.Text = "Done".GetLocalized();
                ExtractingStatusPb.Visibility = Visibility.Collapsed;
            }
            else
            {
                ExtractingStatusText.Text = "ErrDriversExtract".GetLocalized();
                ExtractingStatusPb.ShowError = true;
            }
        }
        catch
        {
            ExtractingStatusText.Text = "ErrDriversExtract".GetLocalized();
            ExtractingStatusPb.ShowError = true;
        }
    }

    private async void SelectPathButton_Click(object sender, RoutedEventArgs e)
    {
        var folderPicker = new FolderPicker(App.MainWindow.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.Desktop
        };

        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            FolderPathText.Text = folder.Path;
            ExtractButton.Visibility = Visibility.Visible;
        }
        else
        {
            ExtractButton.Visibility = Visibility.Collapsed;
            FolderPathText.Text = "SelecFold".GetLocalized();
        }
    }
}

