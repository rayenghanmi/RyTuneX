using System.Management;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RyTuneX.Helpers;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace RyTuneX.Views;

public sealed partial class SystemInfoPage : Page
{
    public SystemInfoPage()
    {
        InitializeComponent();
        LogHelper.Log("Initializing SystemInfoPage");
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        UpdateSystemInfoAsync();
    }

    private async void UpdateSystemInfoAsync()
    {
        await Task.Run(async () =>
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
        });
    }

    private static async Task<string> GetCpuInformation()
    {
        try
        {
            await LogHelper.Log("Getting CPU Info");
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            var collection = searcher.Get();
            var cpuInfoLines = collection.Cast<ManagementObject>().Select(cpu =>
            "Name".GetLocalized() + $": {cpu["Name"]}\n" +
            "Manufacturer".GetLocalized() + $": {cpu["Manufacturer"]}\n" +
            "Architecture".GetLocalized() + $": {cpu["Architecture"]}\n" +
            "Cores".GetLocalized() + $": {cpu["NumberOfCores"]}\n" +
            "LogicalProcessors".GetLocalized() + $": {cpu["NumberOfLogicalProcessors"]}\n" +
            "MaxSpeed".GetLocalized() + $": {cpu["MaxClockSpeed"]} MHz\n" +
            "SocketDesignation".GetLocalized() + $": {cpu["SocketDesignation"]}\n" +
            "L2Cache".GetLocalized() + $": {cpu["L2CacheSize"]} KB\n" +
            "L3Cache".GetLocalized() + $": {cpu["L3CacheSize"]} KB");

            return string.Join(Environment.NewLine, cpuInfoLines);
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"Error getting CPU info: {ex}");
            return string.Empty;
        }
    }

    private static async Task<string> GetGpuInformation()
    {
        try
        {
            await LogHelper.Log("Getting GPU Info");
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            var collection = searcher.Get();
            var gpuNumber = 0;
            var gpuInfoLines = collection.Cast<ManagementObject>().Select(gpu =>
            {
                var gpuInfo = "GPU".GetLocalized() + $" {gpuNumber}:\n" +
                              "   " + "Name".GetLocalized() + $": {gpu["Caption"]}\n" +
                              "   " + "AdapterRAM".GetLocalized() + $": {gpu["AdapterRAM"]} bytes\n" +
                              "   " + "DriverVersion".GetLocalized() + $": {gpu["DriverVersion"]}\n" +
                              "   " + "VideoArchitecture".GetLocalized() + $": {gpu["VideoArchitecture"]}";

                gpuNumber++;
                return gpuInfo;
            });

            return string.Join(Environment.NewLine, gpuInfoLines);
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"Error getting GPU info: {ex}");
            return string.Empty;
        }
    }

    private static async Task<string> GetRamInformation()
    {
        try
        {
            await LogHelper.Log("Getting RAM Info");
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
            var collection = searcher.Get();

            var ramInfoLines = collection.Cast<ManagementObject>().Select((ram, i) =>
                "RAMModule".GetLocalized() + $" {i + 1}:\n" +
                "   " + "DeviceLocator".GetLocalized() + $": {ram["DeviceLocator"]}\n" +
                "   " + "Capacity".GetLocalized() + $": {((ulong)ram["Capacity"]) / (1024 * 1024)} MB\n" +
                "   " + "Speed".GetLocalized() + $": {ram["Speed"]} MHz\n" +
                "   " + "Manufacturer".GetLocalized() + $": {ram["Manufacturer"]}");

            return string.Join(Environment.NewLine, ramInfoLines);
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"Error getting RAM info: {ex}");
            return string.Empty;
        }
    }

    private static async Task<string> GetDiskInformation()
    {
        try
        {
            await LogHelper.Log("Getting Disks Info");
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
            var collection = searcher.Get();
            var diskNumber = 0;
            var diskInfoLines = collection.Cast<ManagementObject>().Select(disk =>
            {
                var diskInfo = "Disk".GetLocalized() + $" {diskNumber}:\n" +
                               "   " + "Caption".GetLocalized() + $": {disk["Caption"]}\n" +
                               "   " + "Size".GetLocalized() + $": {disk["Size"]} bytes\n" +
                               "   " + "InterfaceType".GetLocalized() + $": {disk["InterfaceType"]}\n" +
                               "   " + "Manufacturer".GetLocalized() + $": {disk["Manufacturer"]}\n" +
                               "   " + "Model".GetLocalized() + $": {disk["Model"]}";

                diskNumber++;
                return diskInfo;
            });
            return string.Join(Environment.NewLine, diskInfoLines);
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"Error getting Disk info: {ex}");
            return string.Empty;
        }
    }

    private static async Task<string> GetOsInformation()
    {
        try
        {
            await LogHelper.Log("Getting OS Info");
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            var collection = searcher.Get();

            var osInfoLines = collection.Cast<ManagementObject>().Select(os =>
                "OSName".GetLocalized() + $": {os["Caption"]}\n" +
                "Version".GetLocalized() + $": {os["Version"]}\n" +
                "BuildNumber".GetLocalized() + $": {os["BuildNumber"]}\n" +
                "Architecture".GetLocalized() + $": {os["OSArchitecture"]}\n" +
                "InstallDate".GetLocalized() + $": {os["InstallDate"]}\n" +
                "RegisteredUser".GetLocalized() + $": {os["RegisteredUser"]}\n" +
                "WindowsDirectory".GetLocalized() + $": {os["WindowsDirectory"]}\n" +
                "SystemDirectory".GetLocalized() + $": {os["SystemDirectory"]}\n" +
                "LastBoot".GetLocalized() + $": {os["LastBootUpTime"]}");

            return string.Join(Environment.NewLine, osInfoLines);
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"Error getting OS info: {ex}");
            return string.Empty;
        }
    }

    private static async Task<string> GetNetworkInformation()
    {
        try
        {
            await LogHelper.Log("Getting Network Info");
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter WHERE NetEnabled = TRUE");
            var collection = searcher.Get();

            var networkInfoLines = collection.Cast<ManagementObject>().Select(network =>
                "Name".GetLocalized() + $": {network["Name"]}\n" +
                "MACAddress".GetLocalized() + $": {network["MACAddress"]}\n" +
                "Speed".GetLocalized() + $": {network["Speed"]} bps\n" +
                "AdapterType".GetLocalized() + $": {network["AdapterType"]}");

            return string.Join(Environment.NewLine, networkInfoLines);
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"Error getting Network info: {ex}");
            return "Error retrieving network information.";
        }
    }

    private static async Task<string> GetBatteryInformation()
    {
        try
        {
            await LogHelper.Log("Getting Battery Info");
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery");
            var collection = searcher.Get();

            var batteryInfoLines = collection.Cast<ManagementObject>().Select(battery =>
                "Name".GetLocalized() + $": {battery["Name"]}\n" +
                "EstimatedChargeRemaining".GetLocalized() + $": {battery["EstimatedChargeRemaining"]}%\n" +
                "BatteryStatus".GetLocalized() + $": {battery["BatteryStatus"]}\n" +
                "Chemistry".GetLocalized() + $": {battery["Chemistry"]}");

            return string.Join(Environment.NewLine, batteryInfoLines);
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"Error getting Battery info: {ex}");
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
        var folderPicker = new DevWinUI.FolderPicker(WindowNative.GetWindowHandle(App.MainWindow))
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

