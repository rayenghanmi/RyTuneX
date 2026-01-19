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
                $query = 'SELECT Name, Manufacturer, Architecture, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed, SocketDesignation, L2CacheSize, L3CacheSize FROM Win32_Processor'
                $cpu = Get-CimInstance -Query $query | Select-Object -First 1
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
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var archCode = GetValue(lines, 2, string.Empty);
            var arch = archCode switch
            {
                "0" => "x86",
                "1" => "MIPS",
                "2" => "Alpha",
                "3" => "PowerPC",
                "5" => "ARM",
                "6" => "Itanium-based systems",
                "9" => "x64",
                _ => "Unknown",
            };

            var sb = new StringBuilder(512);
            AppendField(sb, "Name", GetValue(lines, 0, string.Empty));
            AppendField(sb, "Manufacturer", GetValue(lines, 1, string.Empty));
            if (!string.IsNullOrEmpty(archCode)) AppendField(sb, "Architecture", arch);
            AppendField(sb, "Cores", GetValue(lines, 3, "0"));
            AppendField(sb, "LogicalProcessors", GetValue(lines, 4, "0"));
            AppendField(sb, "MaxSpeed", GetValue(lines, 5, "0"), " MHz");
            AppendField(sb, "SocketDesignation", GetValue(lines, 6, string.Empty));
            AppendField(sb, "L2Cache", GetValue(lines, 7, "0"), " KB");
            AppendField(sb, "L3Cache", GetValue(lines, 8, "0"), " KB");

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
                $query = 'SELECT Caption, AdapterRAM, DriverVersion, VideoArchitecture FROM Win32_VideoController'
                Get-CimInstance -Query $query | ForEach-Object {
                    'GPU_START'
                    $_.Caption
                    [math]::Round($_.AdapterRAM / 1MB)
                    $_.DriverVersion
                    $_.VideoArchitecture
                }
            ";

            var output = await OptimizationOptions.RunPowerShell(command).ConfigureAwait(false);
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var sb = new StringBuilder(512);
            var gpuNumber = 0;

            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i] == "GPU_START")
                {
                    if (gpuNumber > 0)
                        sb.AppendLine();

                    var name = GetValue(lines, i + 1, string.Empty);
                    var ram = GetValue(lines, i + 2, string.Empty);
                    var driver = GetValue(lines, i + 3, string.Empty);
                    var archVal = GetValue(lines, i + 4, string.Empty);
                    var architecture = string.IsNullOrEmpty(archVal) ? string.Empty : MapVideoArchitecture(archVal);

                    if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(ram) || !string.IsNullOrEmpty(driver) || !string.IsNullOrEmpty(architecture))
                    {
                        sb.Append("GPU".GetLocalized()).Append(' ').Append(gpuNumber).AppendLine(":");
                        AppendField(sb, "Name", name, null, true);
                        AppendField(sb, "AdapterRAM", ram, " MB", true);
                        AppendField(sb, "DriverVersion", driver, null, true);
                        AppendField(sb, "VideoArchitecture", architecture, null, true);
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
                $query = 'SELECT DeviceLocator, Capacity, Speed, Manufacturer FROM Win32_PhysicalMemory'
                Get-CimInstance -Query $query | ForEach-Object {
                    'RAM_START'
                    $_.DeviceLocator
                    [math]::Round($_.Capacity / 1GB)
                    $_.Speed
                    $_.Manufacturer
                }
            ";

            var output = await OptimizationOptions.RunPowerShell(command).ConfigureAwait(false);
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var sb = new StringBuilder(512);
            var moduleNumber = 1;

            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i] == "RAM_START")
                {
                    if (moduleNumber > 1) sb.AppendLine();

                    if (i + 4 < lines.Length)
                    {
                        var device = GetValue(lines, i + 1, string.Empty);
                        var capacity = GetValue(lines, i + 2, string.Empty);
                        var speed = GetValue(lines, i + 3, string.Empty);
                        var manufacturer = GetValue(lines, i + 4, string.Empty);

                        if (!string.IsNullOrEmpty(device) || !string.IsNullOrEmpty(capacity) || !string.IsNullOrEmpty(speed) || !string.IsNullOrEmpty(manufacturer))
                        {
                            sb.Append("RAMModule".GetLocalized()).Append(' ').Append(moduleNumber).AppendLine(":");
                            AppendField(sb, "DeviceLocator", device, null, true);
                            AppendField(sb, "Capacity", capacity, " GB", true);
                            AppendField(sb, "Speed", speed, " MHz", true);
                            AppendField(sb, "Manufacturer", manufacturer, null, true);
                        }
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
                $query = 'SELECT Caption, Size, InterfaceType, Manufacturer, Model FROM Win32_DiskDrive'
                Get-CimInstance -Query $query | ForEach-Object {
                    'DISK_START'
                    $_.Caption
                    [math]::Round($_.Size / 1e9)
                    $_.InterfaceType
                    $_.Manufacturer
                    $_.Model
                }
            ";

            var output = await OptimizationOptions.RunPowerShell(command).ConfigureAwait(false);
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var sb = new StringBuilder(512);
            var diskNumber = 0;

            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i] == "DISK_START")
                {
                    if (diskNumber > 0) sb.AppendLine();

                    if (i + 5 < lines.Length)
                    {
                        var name = GetValue(lines, i + 1, string.Empty);
                        var size = GetValue(lines, i + 2, string.Empty);
                        var iface = GetValue(lines, i + 3, string.Empty);
                        var manuf = GetValue(lines, i + 4, string.Empty);
                        var model = GetValue(lines, i + 5, string.Empty);

                        if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(size) || !string.IsNullOrEmpty(iface) || !string.IsNullOrEmpty(manuf) || !string.IsNullOrEmpty(model))
                        {
                            sb.Append("Disk".GetLocalized()).Append(' ').Append(diskNumber).AppendLine(":");
                            AppendField(sb, "Name", name, null, true);
                            AppendField(sb, "Size", size, " GB", true);
                            AppendField(sb, "InterfaceType", iface, null, true);
                            AppendField(sb, "Manufacturer", manuf, null, true);
                            AppendField(sb, "Model", model, null, true);
                        }
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
                $query = 'SELECT Caption, Version, BuildNumber, OSArchitecture, InstallDate, RegisteredUser, WindowsDirectory, SystemDirectory, LastBootUpTime FROM Win32_OperatingSystem'
                $os = Get-CimInstance -Query $query
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
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var sb = new StringBuilder(512);
            AppendField(sb, "OSName", GetValue(lines, 0, string.Empty));
            AppendField(sb, "Version", GetValue(lines, 1, string.Empty));
            AppendField(sb, "BuildNumber", GetValue(lines, 2, string.Empty));
            AppendField(sb, "Architecture", GetValue(lines, 3, string.Empty));
            AppendField(sb, "InstallDate", GetValue(lines, 4, string.Empty));
            AppendField(sb, "RegisteredUser", GetValue(lines, 5, string.Empty));
            AppendField(sb, "WindowsDirectory", GetValue(lines, 6, string.Empty));
            AppendField(sb, "SystemDirectory", GetValue(lines, 7, string.Empty));
            AppendField(sb, "LastBoot", GetValue(lines, 8, string.Empty));

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
                $query = 'SELECT Name, Manufacturer, MACAddress, Speed, AdapterType FROM Win32_NetworkAdapter WHERE NetEnabled=TRUE'
                Get-CimInstance -Query $query | ForEach-Object {
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

                    if (i + 5 < lines.Length)
                    {
                        var name = GetValue(lines, i + 1, string.Empty);
                        var manuf = GetValue(lines, i + 2, string.Empty);
                        var mac = GetValue(lines, i + 3, string.Empty);
                        var speed = GetValue(lines, i + 4, string.Empty);
                        var adapter = GetValue(lines, i + 5, string.Empty);

                        if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(manuf) || !string.IsNullOrEmpty(mac) || !string.IsNullOrEmpty(speed) || !string.IsNullOrEmpty(adapter))
                        {
                            AppendField(sb, "Name", name);
                            AppendField(sb, "Manufacturer", manuf);
                            AppendField(sb, "MACAddress", mac);
                            AppendField(sb, "Speed", speed, " Gbps");
                            AppendField(sb, "AdapterType", adapter);
                        }
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
                $query = 'SELECT Name, EstimatedChargeRemaining, BatteryStatus, Chemistry FROM Win32_Battery'
                Get-CimInstance -Query $query | ForEach-Object {
                    'BAT_START'
                    $_.Name
                    $_.EstimatedChargeRemaining
                    $_.BatteryStatus
                    $_.Chemistry
                }
            ";

            var output = await OptimizationOptions.RunPowerShell(command).ConfigureAwait(false);
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var sb = new StringBuilder(256);
            var first = true;

            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i] == "BAT_START")
                {
                    if (!first) sb.AppendLine().AppendLine();
                    first = false;

                    var name = GetValue(lines, i + 1, string.Empty);
                    var charge = GetValue(lines, i + 2, string.Empty);
                    var statusCode = GetValue(lines, i + 3, string.Empty);
                    var chemistryCode = GetValue(lines, i + 4, string.Empty);

                    var status = statusCode switch
                    {
                        "1" => "Discharging",
                        "2" => "AC Power",
                        "3" => "Fully Charged",
                        "4" => "Low",
                        "5" => "Critical",
                        "6" => "Charging",
                        "7" => "Charging and High",
                        "8" => "Charging and Low",
                        "9" => "Charging and Critical",
                        "10" => "Undefined",
                        "11" => "Partially Charged",
                        _ => string.Empty,
                    };

                    var chemistry = chemistryCode switch
                    {
                        "1" => "Other",
                        "3" => "Lead Acid",
                        "4" => "Nickel Cadmium",
                        "5" => "Nickel Metal Hydride",
                        "6" => "Lithium-ion",
                        "7" => "Zinc air",
                        "8" => "Lithium Polymer",
                        _ => string.Empty,
                    };

                    if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(charge) || !string.IsNullOrEmpty(status) || !string.IsNullOrEmpty(chemistry))
                    {
                        AppendField(sb, "Name", name);
                        AppendField(sb, "EstimatedChargeRemaining", charge, "%");
                        AppendField(sb, "BatteryStatus", status);
                        AppendField(sb, "Chemistry", chemistry);
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

    // Safe accessor for parsed PowerShell output lines to avoid IndexOutOfRange
    private static string GetValue(string[] lines, int index, string defaultValue = "")
    {
        if (lines == null) return defaultValue;
        if (index < 0 || index >= lines.Length) return defaultValue;
        return string.IsNullOrWhiteSpace(lines[index]) ? defaultValue : lines[index];
    }

    // Append a localized label and value only when value is present
    private static void AppendField(StringBuilder sb, string labelKey, string value, string suffix = null, bool indent = false)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (indent) sb.Append("   ");
        sb.Append(labelKey.GetLocalized()).Append(": ");
        if (suffix == null)
            sb.AppendLine(value);
        else
            sb.Append(value).AppendLine(suffix);
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