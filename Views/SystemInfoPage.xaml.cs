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
            _ = LogHelper.Log("Updating SystemInfo");

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
            _ = LogHelper.LogError($"Error updating system information: {ex}");
        }
    }

    private static async Task<string> GetCpuInformation()
    {
        try
        {
            _ = LogHelper.Log("Getting CPU Info").ConfigureAwait(false);

            var command = @"
                $cpu = Get-CimInstance Win32_Processor | Select-Object -First 1
                $cpu.Name
                $cpu.Manufacturer
                $cpu.Architecture
                $cpu.NumberOfCores
                $cpu.NumberOfLogicalProcessors
                $cpu.MaxClockSpeed
                $cpu.SocketDesignation
                $cpu.L2CacheSize
                $cpu.L3CacheSize
            ";

            var output = await OptimizationOptions.RunPowerShell(command).ConfigureAwait(false);
            var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

            switch (lines[2])
            {
                case "0":
                    lines[2] = "x86";
                    break;
                case "1":
                    lines[2] = "MIPS";
                    break;
                case "2":
                    lines[2] = "Alpha";
                    break;
                case "3":
                    lines[2] = "PowerPC";
                    break;
                case "5":
                    lines[2] = "ARM";
                    break;
                case "6":
                    lines[2] = "Itanium-based systems";
                    break;
                case "9":
                    lines[2] = "x64";
                    break;
                default:
                    lines[2] = "Unknown";
                    break;
            }

            var sb = new StringBuilder(512);
            sb.Append("Name".GetLocalized()).Append(": ").AppendLine(lines[0])
              .Append("Manufacturer".GetLocalized()).Append(": ").AppendLine(lines[1])
              .Append("Architecture".GetLocalized()).Append(": ").AppendLine(lines[2])
              .Append("Cores".GetLocalized()).Append(": ").AppendLine(lines[3])
              .Append("LogicalProcessors".GetLocalized()).Append(": ").AppendLine(lines[4])
              .Append("MaxSpeed".GetLocalized()).Append(": ").Append(lines[5]).AppendLine(" MHz")
              .Append("SocketDesignation".GetLocalized()).Append(": ").AppendLine(lines[6])
              .Append("L2Cache".GetLocalized()).Append(": ").Append(lines[7]).AppendLine(" KB")
              .Append("L3Cache".GetLocalized()).Append(": ").Append(lines[8]).Append(" KB");

            return sb.ToString();
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error getting CPU info: {ex}").ConfigureAwait(false);
            return string.Empty;
        }
    }

    private static async Task<string> GetGpuInformation()
    {
        try
        {
            _ = LogHelper.Log("Getting GPU Info").ConfigureAwait(false);

            var command = @"
                Get-CimInstance Win32_VideoController | ForEach-Object {
                    'GPU_START'
                    $_.Caption
                    [math]::Round($_.AdapterRAM / 1MB)
                    $_.DriverVersion
                    $_.VideoArchitecture
                }
            ";

            var output = await OptimizationOptions.RunPowerShell(command).ConfigureAwait(false);
            var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

            var sb = new StringBuilder(512);
            var gpuNumber = 0;

            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i] == "GPU_START")
                {
                    if (gpuNumber > 0)
                        sb.AppendLine();

                    if (i + 4 < lines.Length)
                    {
                        var name = lines[i + 1];
                        var ram = lines[i + 2];
                        var driver = lines[i + 3];
                        var architecture = MapVideoArchitecture(lines[i + 4]);

                        sb.Append("GPU".GetLocalized()).Append(' ').Append(gpuNumber).AppendLine(":")
                          .Append("   ").Append("Name".GetLocalized()).Append(": ").AppendLine(name)
                          .Append("   ").Append("AdapterRAM".GetLocalized()).Append(": ").Append(ram).AppendLine(" MB")
                          .Append("   ").Append("DriverVersion".GetLocalized()).Append(": ").AppendLine(driver)
                          .Append("   ").Append("VideoArchitecture".GetLocalized()).Append(": ").Append(architecture);
                    }

                    gpuNumber++;
                }
            }


            return sb.ToString();
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error getting GPU info: {ex}").ConfigureAwait(false);
            return string.Empty;
        }
    }

    private static async Task<string> GetRamInformation()
    {
        try
        {
            _ = LogHelper.Log("Getting RAM Info").ConfigureAwait(false);

            var command = @"
                Get-CimInstance Win32_PhysicalMemory | ForEach-Object {
                    'RAM_START'
                    $_.DeviceLocator
                    [math]::Round($_.Capacity / 1GB)
                    $_.Speed
                    $_.Manufacturer
                }
            ";

            var output = await OptimizationOptions.RunPowerShell(command).ConfigureAwait(false);
            var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

            var sb = new StringBuilder(512);
            var moduleNumber = 1;

            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i] == "RAM_START")
                {
                    if (moduleNumber > 1) sb.AppendLine();

                    if (i + 4 < lines.Length)
                    {
                        sb.Append("RAMModule".GetLocalized()).Append(' ').Append(moduleNumber).AppendLine(":")
                          .Append("   ").Append("DeviceLocator".GetLocalized()).Append(": ").AppendLine(lines[i + 1])
                          .Append("   ").Append("Capacity".GetLocalized()).Append(": ").Append(lines[i + 2]).AppendLine(" GB")
                          .Append("   ").Append("Speed".GetLocalized()).Append(": ").Append(lines[i + 3]).AppendLine(" MHz")
                          .Append("   ").Append("Manufacturer".GetLocalized()).Append(": ").Append(lines[i + 4]);
                    }
                    moduleNumber++;
                }
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error getting RAM info: {ex}").ConfigureAwait(false);
            return string.Empty;
        }
    }

    private static async Task<string> GetDiskInformation()
    {
        try
        {
            _ = LogHelper.Log("Getting Disks Info").ConfigureAwait(false);

            var command = @"
                Get-CimInstance Win32_DiskDrive | ForEach-Object {
                    'DISK_START'
                    $_.Caption
                    [math]::Round($_.Size / 1e9)
                    $_.InterfaceType
                    $_.Manufacturer
                    $_.Model
                }
            ";

            var output = await OptimizationOptions.RunPowerShell(command).ConfigureAwait(false);
            var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

            var sb = new StringBuilder(512);
            var diskNumber = 0;

            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i] == "DISK_START")
                {
                    if (diskNumber > 0) sb.AppendLine();

                    if (i + 5 < lines.Length)
                    {
                        sb.Append("Disk".GetLocalized()).Append(' ').Append(diskNumber).AppendLine(":")
                          .Append("   ").Append("Name".GetLocalized()).Append(": ").AppendLine(lines[i + 1])
                          .Append("   ").Append("Size".GetLocalized()).Append(": ").Append(lines[i + 2]).AppendLine(" GB")
                          .Append("   ").Append("InterfaceType".GetLocalized()).Append(": ").AppendLine(lines[i + 3])
                          .Append("   ").Append("Manufacturer".GetLocalized()).Append(": ").AppendLine(lines[i + 4])
                          .Append("   ").Append("Model".GetLocalized()).Append(": ").Append(lines[i + 5]);
                    }
                    diskNumber++;
                }
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error getting Disk info: {ex}").ConfigureAwait(false);
            return string.Empty;
        }
    }

    private static async Task<string> GetOsInformation()
    {
        try
        {
            _ = LogHelper.Log("Getting OS Info").ConfigureAwait(false);

            var command = @"
                $os = Get-CimInstance Win32_OperatingSystem
                $os.Caption
                $os.Version
                $os.BuildNumber
                $os.OSArchitecture
                $os.InstallDate.ToString('g')
                $os.RegisteredUser
                $os.WindowsDirectory
                $os.SystemDirectory
                $os.LastBootUpTime.ToString('g')
            ";

            var output = await OptimizationOptions.RunPowerShell(command).ConfigureAwait(false);
            var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

            var sb = new StringBuilder(512);
            sb.Append("OSName".GetLocalized()).Append(": ").AppendLine(lines[0])
              .Append("Version".GetLocalized()).Append(": ").AppendLine(lines[1])
              .Append("BuildNumber".GetLocalized()).Append(": ").AppendLine(lines[2])
              .Append("Architecture".GetLocalized()).Append(": ").AppendLine(lines[3])
              .Append("InstallDate".GetLocalized()).Append(": ").AppendLine(lines[4])
              .Append("RegisteredUser".GetLocalized()).Append(": ").AppendLine(lines[5])
              .Append("WindowsDirectory".GetLocalized()).Append(": ").AppendLine(lines[6])
              .Append("SystemDirectory".GetLocalized()).Append(": ").AppendLine(lines[7])
              .Append("LastBoot".GetLocalized()).Append(": ").Append(lines[8]);

            return sb.ToString();
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error getting OS info: {ex}").ConfigureAwait(false);
            return string.Empty;
        }
    }

    private static async Task<string> GetNetworkInformation()
    {
        try
        {
            _ = LogHelper.Log("Getting Network Info").ConfigureAwait(false);

            var command = @"
                Get-CimInstance Win32_NetworkAdapter -Filter 'NetEnabled=TRUE' | ForEach-Object {
                    'NET_START'
                    $_.Name
                    $_.Manufacturer
                    $_.MACAddress
                    [math]::Round($_.Speed / 1e9, 2)
                    $_.AdapterType
                }
            ";

            var output = await OptimizationOptions.RunPowerShell(command).ConfigureAwait(false);
            var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

            var sb = new StringBuilder(512);
            var first = true;

            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i] == "NET_START")
                {
                    if (!first) sb.AppendLine().AppendLine();
                    first = false;

                    if (i + 4 < lines.Length)
                    {
                        sb.Append("Name".GetLocalized()).Append(": ").AppendLine(lines[i + 1])
                          .Append("Manufacturer".GetLocalized()).Append(": ").AppendLine(lines[i + 2])
                          .Append("MACAddress".GetLocalized()).Append(": ").AppendLine(lines[i + 3])
                          .Append("Speed".GetLocalized()).Append(": ").Append(lines[i + 4]).AppendLine(" Gbps")
                          .Append("AdapterType".GetLocalized()).Append(": ").Append(lines[i + 5]);
                    }
                }
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error getting Network info: {ex}").ConfigureAwait(false);
            return string.Empty;
        }
    }

    private static async Task<string> GetBatteryInformation()
    {
        try
        {
            _ = LogHelper.Log("Getting Battery Info").ConfigureAwait(false);

            var command = @"
                Get-CimInstance Win32_Battery | ForEach-Object {
                    'BAT_START'
                    $_.Name
                    $_.EstimatedChargeRemaining
                    $_.BatteryStatus
                    $_.Chemistry
                }
            ";

            var output = await OptimizationOptions.RunPowerShell(command).ConfigureAwait(false);
            var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

            switch (lines[3])
            {
                case "1":
                    lines[3] = "Discharging";
                    break;
                case "2":
                    lines[3] = "AC Power";
                    break;
                case "3":
                    lines[3] = "Fully Charged";
                    break;
                case "4":
                    lines[3] = "Low";
                    break;
                case "5":
                    lines[3] = "Critical";
                    break;
                case "6":
                    lines[3] = "Charging";
                    break;
                case "7":
                    lines[3] = "Charging and High";
                    break;
                case "8":
                    lines[3] = "Charging and Low";
                    break;
                case "9":
                    lines[3] = "Charging and Critical";
                    break;
                case "10":
                    lines[3] = "Undefined";
                    break;
                case "11":
                    lines[3] = "Partially Charged";
                    break;
                default:
                    lines[3] = "Unknown";
                    break;
            }

            switch (lines[4])
            {
                case "1":
                    lines[4] = "Other";
                    break;
                case "3":
                    lines[4] = "Lead Acid";
                    break;
                case "4":
                    lines[4] = "Nickel Cadmium";
                    break;
                case "5":
                    lines[4] = "Nickel Metal Hydride";
                    break;
                case "6":
                    lines[4] = "Lithium-ion";
                    break;
                case "7":
                    lines[4] = "Zinc air";
                    break;
                case "8":
                    lines[4] = "Lithium Polymer";
                    break;
                default:
                    lines[4] = "Unknown";
                    break;
            }

            var sb = new StringBuilder(256);
            var first = true;

            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i] == "BAT_START")
                {
                    if (!first) sb.AppendLine().AppendLine();
                    first = false;

                    if (i + 4 < lines.Length)
                    {
                        sb.Append("Name".GetLocalized()).Append(": ").AppendLine(lines[i + 1])
                          .Append("EstimatedChargeRemaining".GetLocalized()).Append(": ").Append(lines[i + 2]).AppendLine("%")
                          .Append("BatteryStatus".GetLocalized()).Append(": ").AppendLine(lines[i + 3])
                          .Append("Chemistry".GetLocalized()).Append(": ").Append(lines[i + 4]);
                    }
                }
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error getting Battery info: {ex}").ConfigureAwait(false);
            return string.Empty;
        }
    }

    private static string MapVideoArchitecture(string value)
    {
        return value switch
        {
            "0" => "Other",
            "2" => "CGA",
            "3" => "EGA",
            "4" => "VGA",
            "5" => "SVGA",
            "6" => "MDA",
            "7" => "HGC",
            "8" => "MCGA",
            "9" => "8514A",
            "10" => "XGA",
            "11" => "Linear Frame Buffer",
            "12" => "PC-98",
            _ => "Unknown"
        };
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