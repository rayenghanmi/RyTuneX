using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.Win32;
using Microsoft.Windows.Storage.Pickers;
using RyTuneX.Helpers;
using System.Management;
using System.Net.NetworkInformation;
using System.Text;
using Vortice.DXGI;
using Windows.ApplicationModel.DataTransfer;
using Windows.System.Power;

namespace RyTuneX.Views;

public sealed partial class SystemInfoPage : Page
{
    private const string DisplayClassGuidPath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

    public SystemInfoPage()
    {
        InitializeComponent();
        _ = LogHelper.Log("Initializing SystemInfoPage");
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        _ = UpdateSystemInfoAsync();
    }

    private void CopyExpander_Click(object sender, RoutedEventArgs e)
    {
        LogHelper.Log("CopyExpander_Click invoked");
        try
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                LogHelper.Log($"Button tag is {tag}");
                var element = this.FindName(tag) as TextBlock;
                var text = element?.Text ?? string.Empty;
                if (!string.IsNullOrEmpty(text))
                {
                    var dp = new DataPackage();
                    dp.SetText(text);
                    Clipboard.SetContent(dp);
                }
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error copying expander content: {ex}");
        }
    }

    private static void SetFormattedText(TextBlock textBlock, string info)
    {
        textBlock.Text = string.Empty;
        textBlock.Inlines.Clear();

        if (string.IsNullOrWhiteSpace(info)) return;

        var lines = info.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var colonIndex = line.IndexOf(':');

            if (colonIndex >= 0)
            {
                var title = line[..(colonIndex + 1)];
                var value = line[(colonIndex + 1)..];

                textBlock.Inlines.Add(new Run
                {
                    Text = title,
                    FontWeight = FontWeights.SemiBold
                });

                var valueRun = (Run)Microsoft.UI.Xaml.Markup.XamlReader.Load(
                    @"<Run xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" Foreground=""{ThemeResource TextFillColorSecondaryBrush}"" />"
                );
                valueRun.Text = value + (i < lines.Length - 1 ? "\n" : "");
                textBlock.Inlines.Add(valueRun);
            }
            else
            {
                textBlock.Inlines.Add(new Run
                {
                    Text = line + (i < lines.Length - 1 ? "\n" : ""),
                    FontWeight = FontWeights.SemiBold
                });
            }
        }
    }

    private async Task UpdateSystemInfoAsync()
    {
        try
        {
            _ = LogHelper.Log("Updating SystemInfo");

            var osInformationTask = GetOsInformation();
            var cpuInformationTask = GetCpuInformation();
            var gpuInformationTask = GetGpuInformation();
            var ramInformationTask = GetRamInformation();
            var diskInformationTask = GetDiskInformation();
            var networkInformationTask = GetNetworkInformation();
            var batteryInformationTask = GetBatteryInformation();

            await Task.WhenAll(osInformationTask, cpuInformationTask, gpuInformationTask, ramInformationTask, diskInformationTask, networkInformationTask, batteryInformationTask).ConfigureAwait(false);

            var osInformation = osInformationTask.Result;
            var cpuInformation = cpuInformationTask.Result;
            var gpuInformation = gpuInformationTask.Result;
            var ramInformation = ramInformationTask.Result;
            var diskInformation = diskInformationTask.Result;
            var networkInformation = networkInformationTask.Result;
            var batteryInformation = batteryInformationTask.Result;

            // Extract summary fields from the info strings
            var osCaption = GetFirstLine(osInformation);
            var cpuName = GetFirstLine(cpuInformation);
            var ramSummary = GetRamSummary(ramInformation);

            DispatcherQueue.TryEnqueue(() =>
            {
                SetFormattedText(os, osInformation);
                SetFormattedText(cpu, cpuInformation);
                SetFormattedText(gpu, gpuInformation);
                SetFormattedText(ram, ramInformation);
                SetFormattedText(disk, diskInformation);
                SetFormattedText(network, networkInformation);
                SetFormattedText(battery, batteryInformation);

                try
                {
                    DeviceNameText.Text = Environment.MachineName;
                    var parts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(osCaption)) parts.Add(osCaption.Trim());
                    if (!string.IsNullOrWhiteSpace(cpuName)) parts.Add(cpuName.Trim());
                    if (!string.IsNullOrWhiteSpace(ramSummary)) parts.Add(ramSummary.Trim() + " RAM");
                    DeviceSummaryText.Text = string.Join(" | ", parts);
                }
                catch
                {

                }

                loadingProgressRing.Visibility = Visibility.Collapsed;
                ContentArea.Visibility = Visibility.Visible;
            });
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error updating system information: {ex}");
        }
    }

    // Return the value after the first colon from the first non-empty line in the multi-line info string
    private static string GetFirstLine(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var parts = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts)
        {
            var trimmed = p.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                var colonIndex = trimmed.IndexOf(':');
                if (colonIndex >= 0 && colonIndex < trimmed.Length - 1)
                    return trimmed[(colonIndex + 1)..].Trim();
                return trimmed;
            }
        }
        return string.Empty;
    }

    // Extract total RAM in GB from RAM info string
    private static string GetRamSummary(string ramInfo)
    {
        if (string.IsNullOrWhiteSpace(ramInfo)) return string.Empty;
        var lines = ramInfo.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        long total = 0;
        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith("Capacity".GetLocalized(), StringComparison.OrdinalIgnoreCase))
            {
                var parts = line.Split(':');
                if (parts.Length > 1)
                {
                    var val = parts[1].Trim().Split(' ')[0];
                    if (long.TryParse(val, out var gb)) total += gb;
                }
            }
        }
        return total > 0 ? total + " GB" : string.Empty;
    }

    private static Task<string> GetOsInformation()
    {
        return Task.Run(() =>
        {
            try
            {
                _ = LogHelper.Log("Getting OS Info");

                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");

                var productName = key?.GetValue("ProductName") as string ?? string.Empty;
                var displayVersion = key?.GetValue("DisplayVersion") as string
                                      ?? key?.GetValue("ReleaseId") as string
                                      ?? string.Empty;
                var buildNumber = key?.GetValue("CurrentBuildNumber") as string ?? string.Empty;
                var ubr = key?.GetValue("UBR");
                var fullBuild = ubr != null ? $"{buildNumber}.{ubr}" : buildNumber;
                var registeredOwner = key?.GetValue("RegisteredOwner") as string ?? string.Empty;

                var architecture = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
                var windowsDirectory = Environment.GetEnvironmentVariable("windir") ?? string.Empty;
                var systemDirectory = Environment.SystemDirectory;

                // InstallDate is stored as a Unix timestamp (seconds since epoch)
                var installDateRaw = key?.GetValue("InstallDate");
                var installDate = string.Empty;
                if (installDateRaw is int unixSeconds)
                {
                    installDate = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).LocalDateTime.ToString("g");
                }

                // Uptime via TickCount64 avoids a WMI round-trip entirely
                var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
                var lastBoot = (DateTime.Now - uptime).ToString("g");

                var sb = new StringBuilder(512);
                AppendField(sb, "OSName", productName);
                AppendField(sb, "Version", displayVersion);
                AppendField(sb, "BuildNumber", fullBuild);
                AppendField(sb, "Architecture", architecture);
                AppendField(sb, "InstallDate", installDate);
                AppendField(sb, "RegisteredUser", registeredOwner);
                AppendField(sb, "WindowsDirectory", windowsDirectory);
                AppendField(sb, "SystemDirectory", systemDirectory);
                AppendField(sb, "LastBoot", lastBoot);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Error getting OS info: {ex}");
                return string.Empty;
            }
        });
    }

    private static Task<string> GetCpuInformation()
    {
        return Task.Run(() =>
        {
            try
            {
                _ = LogHelper.Log("Getting CPU Info");

                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name, Manufacturer, Architecture, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed, SocketDesignation, L2CacheSize, L3CacheSize FROM Win32_Processor");

                using var results = searcher.Get();
                var cpu = results.Cast<ManagementObject>().FirstOrDefault();
                if (cpu == null) return string.Empty;

                var archCode = cpu["Architecture"]?.ToString() ?? string.Empty;
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
                AppendField(sb, "Name", cpu["Name"]?.ToString() ?? string.Empty);
                AppendField(sb, "Manufacturer", cpu["Manufacturer"]?.ToString() ?? string.Empty);
                if (!string.IsNullOrEmpty(archCode)) AppendField(sb, "Architecture", arch);
                AppendField(sb, "Cores", cpu["NumberOfCores"]?.ToString() ?? "0");
                AppendField(sb, "LogicalProcessors", cpu["NumberOfLogicalProcessors"]?.ToString() ?? "0");
                AppendField(sb, "MaxSpeed", cpu["MaxClockSpeed"]?.ToString() ?? "0", " MHz");
                AppendField(sb, "SocketDesignation", cpu["SocketDesignation"]?.ToString() ?? string.Empty);
                AppendField(sb, "L2Cache", cpu["L2CacheSize"]?.ToString() ?? "0", " KB");
                AppendField(sb, "L3Cache", cpu["L3CacheSize"]?.ToString() ?? "0", " KB");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Error getting CPU info: {ex}");
                return string.Empty;
            }
        });
    }

    private static Task<string> GetGpuInformation()
    {
        return Task.Run(() =>
        {
            try
            {
                _ = LogHelper.Log("Getting GPU Info");

                var sb = new StringBuilder(512);
                var gpuNumber = 0;
                var dxgiSucceeded = false;

                try
                {
                    var factoryResult = DXGI.CreateDXGIFactory1(out IDXGIFactory1 factory);
                    if (factoryResult.Failure || factory == null)
                    {
                        _ = LogHelper.LogError($"DXGI: CreateDXGIFactory1 failed with HRESULT {factoryResult.Code:X8}");
                    }
                    else
                    {
                        using (factory)
                        {
                            for (uint i = 0; ; i++)
                            {
                                var result = factory.EnumAdapters1(i, out IDXGIAdapter1 adapter);
                                if (result.Failure || adapter == null) break;

                                using (adapter)
                                {
                                    var desc = adapter.Description1;

                                    // Skip the "Microsoft Basic Render Driver" software adapter
                                    if ((desc.Flags & AdapterFlags.Software) != 0)
                                        continue;

                                    if (gpuNumber > 0) sb.AppendLine();

                                    var name = desc.Description;
                                    var vramMb = desc.DedicatedVideoMemory / (1024 * 1024);
                                    var sharedMb = desc.SharedSystemMemory / (1024 * 1024);
                                    var vendorId = desc.VendorId;
                                    var deviceId = desc.DeviceId;

                                    var vendorName = GetVendorName(vendorId);
                                    var rawDriverVersion = GetDriverVersionFromRegistry(vendorId, deviceId);
                                    var driverVersion = string.IsNullOrEmpty(rawDriverVersion)
                                        ? string.Empty
                                        : NormalizeDriverVersion(vendorId, rawDriverVersion);

                                    sb.Append("GPU".GetLocalized()).Append(' ').Append(gpuNumber).AppendLine(":");
                                    AppendField(sb, "Name", name, null, true);
                                    AppendField(sb, "Vendor", vendorName, null, true);
                                    AppendField(sb, "DedicatedVRAM", vramMb.ToString(), " MB", true);
                                    if (sharedMb > 0) AppendField(sb, "SharedMemory", sharedMb.ToString(), " MB", true);
                                    AppendField(sb, "DriverVersion", driverVersion, null, true);

                                    gpuNumber++;
                                    dxgiSucceeded = true;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _ = LogHelper.LogError($"DXGI enumeration threw an exception, falling back to WMI: {ex}");
                }

                // Fallback using WMI
                if (!dxgiSucceeded)
                {
                    sb.Clear();
                    try
                    {
                        using var searcher = new ManagementObjectSearcher(
                            "SELECT Caption, AdapterRAM, DriverVersion FROM Win32_VideoController");
                        using var results = searcher.Get();
                        var fallbackNumber = 0;

                        foreach (ManagementObject gpuObj in results.Cast<ManagementObject>())
                        {
                            if (fallbackNumber > 0) sb.AppendLine();

                            var name = gpuObj["Caption"]?.ToString() ?? string.Empty;
                            var ramRaw = gpuObj["AdapterRAM"];
                            var ramMb = ramRaw != null
                                ? Math.Round(Convert.ToDouble(ramRaw) / (1024 * 1024)).ToString("0")
                                : string.Empty;
                            var driver = gpuObj["DriverVersion"]?.ToString() ?? string.Empty;

                            sb.Append("GPU".GetLocalized()).Append(' ').Append(fallbackNumber).AppendLine(":");
                            AppendField(sb, "Name", name, null, true);
                            AppendField(sb, "AdapterRAM", ramMb, " MB", true);
                            AppendField(sb, "DriverVersion", driver, null, true);

                            fallbackNumber++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _ = LogHelper.LogError($"WMI GPU fallback also failed: {ex}");
                        return string.Empty;
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Error getting GPU info: {ex}");
                return string.Empty;
            }
        });
    }

    private static string GetVendorName(uint vendorId) => vendorId switch
    {
        0x10DE => "NVIDIA",
        0x1002 or 0x1022 => "AMD",
        0x8086 => "Intel",
        _ => $"Unknown (0x{vendorId:X4})",
    };

    private static string? GetDriverVersionFromRegistry(uint vendorId, uint deviceId)
    {
        try
        {
            using var classKey = Registry.LocalMachine.OpenSubKey(DisplayClassGuidPath);
            if (classKey == null) return null;

            foreach (var subKeyName in classKey.GetSubKeyNames())
            {
                using var subKey = classKey.OpenSubKey(subKeyName);
                var matchingId = subKey?.GetValue("MatchingDeviceId") as string;
                if (matchingId != null &&
                    matchingId.Contains($"ven_{vendorId:x4}", StringComparison.OrdinalIgnoreCase) &&
                    matchingId.Contains($"dev_{deviceId:x4}", StringComparison.OrdinalIgnoreCase))
                {
                    return subKey?.GetValue("DriverVersion") as string;
                }
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error reading driver version from registry: {ex}");
        }
        return null;
    }

    private static string NormalizeDriverVersion(uint vendorId, string rawVersion)
    {
        if (vendorId == 0x10DE)
        {
            var parts = rawVersion.Split('.');
            if (parts.Length == 4)
            {
                var combined = parts[2] + parts[3];
                if (combined.Length > 5)
                    combined = combined[^5..];

                if (combined.Length >= 3)
                    return combined.Insert(combined.Length - 2, ".");
            }
            return rawVersion;
        }

        if (vendorId is 0x1002 or 0x1022)
        {
            var adrenalin = GetAmdAdrenalinVersion();
            return string.IsNullOrEmpty(adrenalin) ? rawVersion : adrenalin;
        }

        return rawVersion;
    }

    private static string? GetAmdAdrenalinVersion()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\AMD\CN")
                             ?? Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\AMD\CN");
            return key?.GetValue("ReleaseVersion") as string;
        }
        catch
        {
            return null;
        }
    }

    private static Task<string> GetRamInformation()
    {
        return Task.Run(() =>
        {
            try
            {
                _ = LogHelper.Log("Getting RAM Info");

                using var searcher = new ManagementObjectSearcher(
                    "SELECT DeviceLocator, Capacity, Speed, Manufacturer FROM Win32_PhysicalMemory");

                using var results = searcher.Get();
                var sb = new StringBuilder(512);
                var moduleNumber = 1;

                foreach (ManagementObject module in results.Cast<ManagementObject>())
                {
                    if (moduleNumber > 1) sb.AppendLine();

                    var device = module["DeviceLocator"]?.ToString() ?? string.Empty;
                    var capacityRaw = module["Capacity"];
                    var capacity = capacityRaw != null
                        ? Math.Round(Convert.ToDouble(capacityRaw) / (1024 * 1024 * 1024)).ToString("0")
                        : string.Empty;
                    var speed = module["Speed"]?.ToString() ?? string.Empty;
                    var manufacturer = module["Manufacturer"]?.ToString() ?? string.Empty;

                    sb.Append("RAMModule".GetLocalized()).Append(' ').Append(moduleNumber).AppendLine(":");
                    AppendField(sb, "DeviceLocator", device, null, true);
                    AppendField(sb, "Capacity", capacity, " GB", true);
                    AppendField(sb, "Speed", speed, " MHz", true);
                    AppendField(sb, "Manufacturer", manufacturer, null, true);

                    moduleNumber++;
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Error getting RAM info: {ex}");
                return string.Empty;
            }
        });
    }

    private static Task<string> GetDiskInformation()
    {
        return Task.Run(() =>
        {
            try
            {
                _ = LogHelper.Log("Getting Disks Info");

                var mediaTypeByIndex = new Dictionary<uint, string>();
                try
                {
                    var storageScope = new ManagementScope(@"\\.\root\Microsoft\Windows\Storage");
                    storageScope.Connect();
                    using var storageSearcher = new ManagementObjectSearcher(storageScope,
                        new ObjectQuery("SELECT DeviceId, MediaType FROM MSFT_PhysicalDisk"));
                    using var storageResults = storageSearcher.Get();
                    foreach (ManagementObject disk in storageResults.Cast<ManagementObject>())
                    {
                        if (uint.TryParse(disk["DeviceId"]?.ToString(), out var id))
                        {
                            var mediaTypeCode = disk["MediaType"]?.ToString() ?? string.Empty;
                            mediaTypeByIndex[id] = mediaTypeCode switch
                            {
                                "3" => "HDD",
                                "4" => "SSD",
                                "5" => "SCM",
                                _ => "Unknown",
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Falls back to no media type if this namespace is unavailable
                    _ = LogHelper.LogError($"Error querying MSFT_PhysicalDisk: {ex}");
                }

                List<ManagementObject> disks;
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT Index, Caption, Size, InterfaceType, Manufacturer, Model FROM Win32_DiskDrive"))
                using (var results = searcher.Get())
                {
                    disks = results.Cast<ManagementObject>().ToList();
                }

                if (disks.Count == 0)
                {
                    _ = LogHelper.LogError("Win32_DiskDrive query returned zero drives");
                }

                var sb = new StringBuilder(512);
                var diskNumber = 0;

                foreach (var disk in disks)
                {
                    try
                    {
                        if (diskNumber > 0) sb.AppendLine();

                        var name = disk["Caption"]?.ToString() ?? string.Empty;
                        var sizeRaw = disk["Size"];
                        var size = sizeRaw != null
                            ? Math.Round(Convert.ToDouble(sizeRaw) / 1e9).ToString("0")
                            : string.Empty;
                        var iface = disk["InterfaceType"]?.ToString() ?? string.Empty;
                        var manuf = disk["Manufacturer"]?.ToString() ?? string.Empty;
                        var model = disk["Model"]?.ToString() ?? string.Empty;

                        uint? index = null;
                        if (disk["Index"] != null && uint.TryParse(disk["Index"].ToString(), out var parsedIndex))
                        {
                            index = parsedIndex;
                        }
                        var mediaType = index.HasValue && mediaTypeByIndex.TryGetValue(index.Value, out var mt) ? mt : string.Empty;

                        sb.Append("Disk".GetLocalized()).Append(' ').Append(diskNumber).AppendLine(":");
                        AppendField(sb, "Name", name, null, true);
                        AppendField(sb, "Size", size, " GB", true);
                        AppendField(sb, "Type", mediaType, null, true);
                        AppendField(sb, "InterfaceType", iface, null, true);
                        AppendField(sb, "Manufacturer", manuf, null, true);
                        AppendField(sb, "Model", model, null, true);

                        diskNumber++;
                    }
                    catch (Exception diskEx)
                    {
                        _ = LogHelper.LogError($"Error reading a disk entry, skipping it: {diskEx}");
                    }
                    finally
                    {
                        disk.Dispose();
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Error getting Disk info: {ex}");
                return string.Empty;
            }
        });
    }

    private static Task<string> GetNetworkInformation()
    {
        return Task.Run(() =>
        {
            try
            {
                _ = LogHelper.Log("Getting Network Info");

                var sb = new StringBuilder(512);
                var first = true;

                var adapters = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(nic => nic.OperationalStatus == OperationalStatus.Up &&
                                  nic.NetworkInterfaceType != NetworkInterfaceType.Loopback);

                foreach (var nic in adapters)
                {
                    if (!first) sb.AppendLine().AppendLine();
                    first = false;

                    var name = nic.Description;
                    var mac = string.Join(":", nic.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2")));
                    var speedGbps = nic.Speed > 0 ? Math.Round(nic.Speed / 1e9, 2).ToString("0.##") : string.Empty;
                    var adapterType = nic.NetworkInterfaceType.ToString();

                    AppendField(sb, "Name", name);
                    AppendField(sb, "MACAddress", mac);
                    AppendField(sb, "Speed", speedGbps, " Gbps");
                    AppendField(sb, "AdapterType", adapterType);
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Error getting Network info: {ex}");
                return string.Empty;
            }
        });
    }

    private static Task<string> GetBatteryInformation()
    {
        return Task.Run(() =>
        {
            try
            {
                _ = LogHelper.Log("Getting Battery Info");

                var status = PowerManager.BatteryStatus;
                if (status == BatteryStatus.NotPresent)
                {
                    return string.Empty;
                }

                var chargePercent = PowerManager.RemainingChargePercent;
                var powerSupplyStatus = PowerManager.PowerSupplyStatus;

                var statusText = status switch
                {
                    BatteryStatus.Charging => "Charging",
                    BatteryStatus.Discharging => "Discharging",
                    BatteryStatus.Idle => powerSupplyStatus == PowerSupplyStatus.Adequate
                        ? "Fully Charged"
                        : "Idle",
                    _ => "Unknown",
                };

                var sb = new StringBuilder(256);
                AppendField(sb, "EstimatedChargeRemaining", chargePercent.ToString(), "%");
                AppendField(sb, "BatteryStatus", statusText);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Error getting Battery info: {ex}");
                return string.Empty;
            }
        });
    }

    private static void AppendField(StringBuilder sb, string labelKey, string value, string? suffix = null, bool indent = false)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (indent) sb.Append("   ");
        sb.Append(SafeGetLocalized(labelKey)).Append(": ");
        if (suffix == null)
            sb.AppendLine(value);
        else
            sb.Append(value).AppendLine(suffix);
    }

    private static string SafeGetLocalized(string resourceKey)
    {
        try
        {
            var localized = resourceKey.GetLocalized();
            return string.IsNullOrEmpty(localized) ? resourceKey : localized;
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Missing localization resource for key '{resourceKey}': {ex.Message}");
            return resourceKey;
        }
    }

    private async void ExtractButton_Click(object sender, RoutedEventArgs e)
    {
        var folderPath = FolderPathText.Text + $"\\RyTuneX_Drivers_{DateTime.Now:yyyy-MM-dd}";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        // Get the Long Path prefix to prevent errors with long paths
        var longPathSafe = $@"\\?\{Path.GetFullPath(folderPath)}";
        ExtractingStatusText.Text = "ExtractingDrivers".GetLocalized() + "...";
        ExtractingStatusPb.ShowError = false;
        ExtractingStatus.Visibility = Visibility.Visible;
        try
        {
            await LogHelper.Log("Starting driver extraction via DISM");
            var exitCode = await OptimizationOptions.StartInCmd($"dism.exe /online /export-driver /destination:\"{longPathSafe}\"");
            if (exitCode == 0)
            {
                ExtractingStatusText.Text = "Done".GetLocalized();
                ExtractingStatusPb.Visibility = Visibility.Collapsed;
                await LogHelper.Log("Driver extraction completed successfully");
            }
            else
            {
                ExtractingStatusText.Text = "ErrDriversExtract".GetLocalized();
                ExtractingStatusPb.ShowError = true;
                await LogHelper.LogError($"Driver extraction failed with exit code {exitCode}");
            }
        }
        catch (Exception ex)
        {
            ExtractingStatusText.Text = "ErrDriversExtract".GetLocalized();
            ExtractingStatusPb.ShowError = true;
            await LogHelper.LogError($"Exception during driver extraction: {ex}");
        }
    }

    private async void SelectPathButton_Click(object sender, RoutedEventArgs e)
    {
        await LogHelper.Log("SelectPathButton_Click invoked");
        var folderPicker = new FolderPicker(App.MainWindow.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.Desktop
        };

        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            await LogHelper.Log($"Folder selected: {folder.Path}");
            FolderPathText.Text = folder.Path;
            ExtractButton.Visibility = Visibility.Visible;
        }
        else
        {
            await LogHelper.Log("No folder selected");
            ExtractButton.Visibility = Visibility.Collapsed;
            FolderPathText.Text = "SelecFold".GetLocalized();
        }
    }
}