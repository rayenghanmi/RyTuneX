using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using Microsoft.Windows.Storage.Pickers;
using RyTuneX.Helpers;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using Vortice.DXGI;
using Windows.ApplicationModel.DataTransfer;
using Windows.System.Power;

namespace RyTuneX.Views;

public sealed partial class SystemInfoPage : Page
{
    private const string DisplayClassGuidPath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint OPEN_EXISTING = 3;
    private const uint IOCTL_STORAGE_QUERY_PROPERTY = 0x002D1400;
    private const uint IOCTL_DISK_GET_DRIVE_GEOMETRY_EX = 0x000700A0;

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer,
        uint nInBufferSize,
        IntPtr lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetLogicalProcessorInformationEx(
        int relationshipType,
        IntPtr buffer,
        ref uint returnedLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint GetSystemFirmwareTable(
        uint firmwareTableProviderSignature,
        uint firmwareTableID,
        IntPtr pFirmwareTableBuffer,
        uint bufferSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetPhysicallyInstalledSystemMemory(out ulong totalMemoryInKilobytes);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
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
        public MEMORYSTATUSEX() { dwLength = (uint)Marshal.SizeOf(this); }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    private static (string Os, string Cpu, string Gpu, string Ram, string Disk, string Network, string Battery)? _cachedSystemInfo;

    public SystemInfoPage()
    {
        InitializeComponent();
        _ = LogHelper.Log("Initializing SystemInfoPage");
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Disabled;
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
        var secondaryBrush = Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush;

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

                var valueRun = new Run
                {
                    Text = value + (i < lines.Length - 1 ? "\n" : ""),
                    Foreground = secondaryBrush
                };
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

            string osInformation;
            string cpuInformation;
            string gpuInformation;
            string ramInformation;
            string diskInformation;
            string networkInformation;
            string batteryInformation;

            if (_cachedSystemInfo.HasValue)
            {
                (osInformation, cpuInformation, gpuInformation, ramInformation, diskInformation, networkInformation, batteryInformation) = _cachedSystemInfo.Value;
            }
            else
            {
                var osInformationTask = GetOsInformation();
                var cpuInformationTask = GetCpuInformation();
                var gpuInformationTask = GetGpuInformation();
                var ramInformationTask = GetRamInformation();
                var diskInformationTask = GetDiskInformation();
                var networkInformationTask = GetNetworkInformation();
                var batteryInformationTask = GetBatteryInformation();

                await Task.WhenAll(osInformationTask, cpuInformationTask, gpuInformationTask, ramInformationTask, diskInformationTask, networkInformationTask, batteryInformationTask).ConfigureAwait(false);

                osInformation = osInformationTask.Result;
                cpuInformation = cpuInformationTask.Result;
                gpuInformation = gpuInformationTask.Result;
                ramInformation = ramInformationTask.Result;
                diskInformation = diskInformationTask.Result;
                networkInformation = networkInformationTask.Result;
                batteryInformation = batteryInformationTask.Result;

                _cachedSystemInfo = (osInformation, cpuInformation, gpuInformation, ramInformation, diskInformation, networkInformation, batteryInformation);
            }

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
                var buildNumber = key?.GetValue("CurrentBuildNumber") as string ?? string.Empty;

                if (int.TryParse(buildNumber, out int buildNum) && buildNum >= 22000)
                {
                    productName = productName.Replace("10", "11");
                }

                var displayVersion = key?.GetValue("DisplayVersion") as string
                                      ?? key?.GetValue("ReleaseId") as string
                                      ?? string.Empty;
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

    private static byte[]? GetRawSmbiosTable()
    {
        try
        {
            uint smbiosSig = 0x52534D42; // 'RSMB'
            uint size = GetSystemFirmwareTable(smbiosSig, 0, IntPtr.Zero, 0);
            if (size == 0) return null;

            IntPtr ptr = Marshal.AllocHGlobal((int)size);
            try
            {
                if (GetSystemFirmwareTable(smbiosSig, 0, ptr, size) > 0)
                {
                    byte[] data = new byte[size];
                    Marshal.Copy(ptr, data, 0, (int)size);
                    return data;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
        catch { }
        return null;
    }

    private static string GetSmbiosString(byte[] raw, int strTableStart, byte strIndex)
    {
        if (strIndex == 0) return string.Empty;
        int cur = 1;
        int s = strTableStart;
        while (s < raw.Length && cur < strIndex)
        {
            while (s < raw.Length && raw[s] != 0) s++;
            s++;
            cur++;
        }
        int end = s;
        while (end < raw.Length && raw[end] != 0) end++;
        return s < raw.Length ? Encoding.ASCII.GetString(raw, s, Math.Max(0, end - s)).Trim() : string.Empty;
    }

    private static int GetPhysicalCoreCount()
    {
        try
        {
            uint length = 0;
            GetLogicalProcessorInformationEx(0, IntPtr.Zero, ref length); // RelationProcessorCore = 0
            if (length == 0) return Environment.ProcessorCount;

            IntPtr ptr = Marshal.AllocHGlobal((int)length);
            try
            {
                if (GetLogicalProcessorInformationEx(0, ptr, ref length))
                {
                    int cores = 0;
                    int offset = 0;
                    while (offset < length)
                    {
                        IntPtr current = IntPtr.Add(ptr, offset);
                        int relationship = Marshal.ReadInt32(current, 0);
                        int size = Marshal.ReadInt32(current, 4);
                        if (size <= 0) break;
                        if (relationship == 0) cores++;
                        offset += size;
                    }
                    return cores > 0 ? cores : Environment.ProcessorCount;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
        catch { }
        return Environment.ProcessorCount;
    }

    private static (long l2Kb, long l3Kb) GetCacheSizes()
    {
        long l2Total = 0;
        long l3Total = 0;
        try
        {
            uint length = 0;
            GetLogicalProcessorInformationEx(2, IntPtr.Zero, ref length); // RelationCache = 2
            if (length == 0) return (0, 0);

            IntPtr ptr = Marshal.AllocHGlobal((int)length);
            try
            {
                if (GetLogicalProcessorInformationEx(2, ptr, ref length))
                {
                    int offset = 0;
                    while (offset < length)
                    {
                        IntPtr current = IntPtr.Add(ptr, offset);
                        int relationship = Marshal.ReadInt32(current, 0);
                        int size = Marshal.ReadInt32(current, 4);
                        if (size <= 0) break;
                        if (relationship == 2)
                        {
                            byte level = Marshal.ReadByte(current, 8);
                            int cacheSize = Marshal.ReadInt32(current, 12);
                            int type = Marshal.ReadInt32(current, 16);
                            if (level == 2 && (type == 0 || type == 2)) l2Total += cacheSize;
                            if (level == 3 && (type == 0 || type == 2)) l3Total += cacheSize;
                        }
                        offset += size;
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
        catch { }
        return (l2Total / 1024, l3Total / 1024);
    }

    private static string GetCpuSocketDesignation()
    {
        try
        {
            var raw = GetRawSmbiosTable();
            if (raw == null) return string.Empty;

            int offset = 8;
            while (offset < raw.Length - 4)
            {
                byte type = raw[offset];
                byte len = raw[offset + 1];
                if (len == 0) break;

                int strTableStart = offset + len;
                int nextStruct = strTableStart;
                while (nextStruct < raw.Length - 1 && !(raw[nextStruct] == 0 && raw[nextStruct + 1] == 0))
                {
                    nextStruct++;
                }

                if (type == 4) // Processor Information
                {
                    if (len > 4)
                    {
                        byte socketIdx = raw[offset + 4];
                        var socket = GetSmbiosString(raw, strTableStart, socketIdx);
                        if (!string.IsNullOrWhiteSpace(socket)) return socket;
                    }
                }

                offset = nextStruct + 2;
            }
        }
        catch { }
        return string.Empty;
    }

    private static Task<string> GetCpuInformation()
    {
        return Task.Run(() =>
        {
            try
            {
                _ = LogHelper.Log("Getting CPU Info");

                var sb = new StringBuilder(512);

                string name = string.Empty;
                string manufacturer = string.Empty;
                string maxSpeed = string.Empty;
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                    if (key != null)
                    {
                        name = (key.GetValue("ProcessorNameString") as string)?.Trim() ?? string.Empty;
                        manufacturer = (key.GetValue("VendorIdentifier") as string)?.Trim() ?? string.Empty;
                        var mhz = key.GetValue("~MHz");
                        if (mhz != null)
                        {
                            maxSpeed = mhz.ToString() ?? string.Empty;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _ = LogHelper.LogWarning($"Failed to read CPU registry info: {ex.Message}");
                }

                var arch = RuntimeInformation.OSArchitecture switch
                {
                    Architecture.X64 => "x64",
                    Architecture.Arm64 => "ARM64",
                    Architecture.X86 => "x86",
                    Architecture.Arm => "ARM",
                    _ => RuntimeInformation.OSArchitecture.ToString()
                };

                int logicalCores = Environment.ProcessorCount;
                int physicalCores = GetPhysicalCoreCount();
                if (physicalCores <= 0) physicalCores = logicalCores;

                var (l2Kb, l3Kb) = GetCacheSizes();
                var socket = GetCpuSocketDesignation();

                AppendField(sb, "Name", name);
                AppendField(sb, "Manufacturer", manufacturer);
                AppendField(sb, "Architecture", arch);
                AppendField(sb, "Cores", physicalCores.ToString());
                AppendField(sb, "LogicalProcessors", logicalCores.ToString());
                if (!string.IsNullOrEmpty(maxSpeed)) AppendField(sb, "MaxSpeed", maxSpeed, " MHz");
                if (!string.IsNullOrEmpty(socket)) AppendField(sb, "SocketDesignation", socket);
                if (l2Kb > 0) AppendField(sb, "L2Cache", l2Kb.ToString(), " KB");
                if (l3Kb > 0) AppendField(sb, "L3Cache", l3Kb.ToString(), " KB");

                try
                {
                    var isCopilotPlus = Microsoft.Windows.AI.AICapabilities.HasAICapability(Microsoft.Windows.AI.AICapabilityCategory.CopilotPlusPC);
                    AppendField(sb, "CopilotPlusPC", SafeGetLocalized(isCopilotPlus ? "Supported" : "NotSupported"));
                }
                catch (Exception ex)
                {
                    _ = LogHelper.LogWarning($"AICapabilities check skipped or not supported: {ex.Message}");
                }

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
                    var factoryResult = DXGI.CreateDXGIFactory1(out IDXGIFactory1? factory);
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
                                var result = factory.EnumAdapters1(i, out IDXGIAdapter1? adapter);
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
                    _ = LogHelper.LogError($"DXGI enumeration threw an exception, falling back to registry: {ex}");
                }

                // Fallback using Display Class Registry
                if (!dxgiSucceeded)
                {
                    sb.Clear();
                    try
                    {
                        var fallbackNumber = 0;
                        using var classKey = Registry.LocalMachine.OpenSubKey(DisplayClassGuidPath);
                        if (classKey != null)
                        {
                            foreach (var subKeyName in classKey.GetSubKeyNames())
                            {
                                if (subKeyName.Length != 4 || !int.TryParse(subKeyName, out _)) continue;
                                using var subKey = classKey.OpenSubKey(subKeyName);
                                if (subKey == null) continue;

                                var name = subKey.GetValue("DriverDesc") as string;
                                if (string.IsNullOrWhiteSpace(name)) continue;

                                if (fallbackNumber > 0) sb.AppendLine();

                                var driver = subKey.GetValue("DriverVersion") as string ?? string.Empty;
                                var provider = subKey.GetValue("ProviderName") as string ?? string.Empty;
                                var memRaw = subKey.GetValue("HardwareInformation.qwMemorySize")
                                             ?? subKey.GetValue("HardwareInformation.MemorySize");

                                string ramMb = string.Empty;
                                if (memRaw != null && long.TryParse(memRaw.ToString(), out var memBytes) && memBytes > 0)
                                {
                                    ramMb = Math.Round(memBytes / (1024.0 * 1024.0)).ToString("0");
                                }

                                sb.Append("GPU".GetLocalized()).Append(' ').Append(fallbackNumber).AppendLine(":");
                                AppendField(sb, "Name", name, null, true);
                                if (!string.IsNullOrEmpty(provider)) AppendField(sb, "Vendor", provider, null, true);
                                if (!string.IsNullOrEmpty(ramMb)) AppendField(sb, "DedicatedVRAM", ramMb, " MB", true);
                                AppendField(sb, "DriverVersion", driver, null, true);

                                fallbackNumber++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _ = LogHelper.LogError($"Registry GPU fallback failed: {ex}");
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

                var sb = new StringBuilder(512);
                int moduleNumber = 1;

                var raw = GetRawSmbiosTable();
                if (raw != null)
                {
                    int offset = 8;
                    while (offset < raw.Length - 4)
                    {
                        byte type = raw[offset];
                        byte len = raw[offset + 1];
                        if (len == 0) break;

                        int strTableStart = offset + len;
                        int nextStruct = strTableStart;
                        while (nextStruct < raw.Length - 1 && !(raw[nextStruct] == 0 && raw[nextStruct + 1] == 0))
                        {
                            nextStruct++;
                        }

                        if (type == 17) // Memory Device
                        {
                            ushort sizeVal = BitConverter.ToUInt16(raw, offset + 12);
                            long capMb = 0;
                            if (sizeVal != 0 && sizeVal != 0xFFFF)
                            {
                                if (sizeVal == 0x7FFF && len >= 0x20)
                                {
                                    capMb = BitConverter.ToUInt32(raw, offset + 0x1C);
                                }
                                else if ((sizeVal & 0x8000) != 0)
                                {
                                    capMb = (sizeVal & 0x7FFF) / 1024;
                                }
                                else
                                {
                                    capMb = sizeVal;
                                }
                            }

                            if (capMb > 0)
                            {
                                byte locIdx = len > 16 ? raw[offset + 16] : (byte)0;
                                byte mfgIdx = len > 23 ? raw[offset + 23] : (byte)0;
                                ushort speed = len >= 23 ? BitConverter.ToUInt16(raw, offset + 21) : (ushort)0;
                                if (len >= 34)
                                {
                                    ushort confSpeed = BitConverter.ToUInt16(raw, offset + 32);
                                    if (confSpeed > 0) speed = confSpeed;
                                }

                                var device = GetSmbiosString(raw, strTableStart, locIdx);
                                if (string.IsNullOrWhiteSpace(device)) device = $"DIMM {moduleNumber - 1}";
                                var manufacturer = GetSmbiosString(raw, strTableStart, mfgIdx);
                                var capGb = (long)Math.Round(capMb / 1024.0);

                                if (moduleNumber > 1) sb.AppendLine();
                                sb.Append("RAMModule".GetLocalized()).Append(' ').Append(moduleNumber).AppendLine(":");
                                AppendField(sb, "DeviceLocator", device, null, true);
                                AppendField(sb, "Capacity", capGb.ToString(), " GB", true);
                                if (speed > 0) AppendField(sb, "Speed", speed.ToString(), " MHz", true);
                                if (!string.IsNullOrWhiteSpace(manufacturer)) AppendField(sb, "Manufacturer", manufacturer, null, true);

                                moduleNumber++;
                            }
                        }

                        offset = nextStruct + 2;
                    }
                }

                // Fallback to GlobalMemoryStatusEx or GetPhysicallyInstalledSystemMemory if no modules found
                if (moduleNumber == 1)
                {
                    ulong totalMb = 0;
                    if (GetPhysicallyInstalledSystemMemory(out ulong kb) && kb > 0)
                    {
                        totalMb = kb / 1024;
                    }
                    else
                    {
                        var memStatus = new MEMORYSTATUSEX();
                        if (GlobalMemoryStatusEx(memStatus) && memStatus.ullTotalPhys > 0)
                        {
                            totalMb = memStatus.ullTotalPhys / (1024 * 1024);
                        }
                    }

                    if (totalMb > 0)
                    {
                        var capGb = (long)Math.Round(totalMb / 1024.0);
                        sb.Append("RAMModule".GetLocalized()).Append(" 1:\n");
                        AppendField(sb, "DeviceLocator", "System Memory", null, true);
                        AppendField(sb, "Capacity", capGb.ToString(), " GB", true);
                    }
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

                var sb = new StringBuilder(512);
                int diskNumber = 0;

                for (int i = 0; i < 32; i++)
                {
                    string drivePath = $@"\\.\PhysicalDrive{i}";
                    using var handle = CreateFile(
                        drivePath,
                        0,
                        FILE_SHARE_READ | FILE_SHARE_WRITE,
                        IntPtr.Zero,
                        OPEN_EXISTING,
                        0,
                        IntPtr.Zero);

                    if (handle.IsInvalid) continue;

                    int querySize = 12;
                    IntPtr queryPtr = Marshal.AllocHGlobal(querySize);
                    int outBufferSize = 2048;
                    IntPtr outBuffer = Marshal.AllocHGlobal(outBufferSize);
                    try
                    {
                        Marshal.WriteInt32(queryPtr, 0, 0); // StorageDeviceProperty
                        Marshal.WriteInt32(queryPtr, 4, 0); // PropertyStandardQuery

                        if (DeviceIoControl(handle, IOCTL_STORAGE_QUERY_PROPERTY, queryPtr, (uint)querySize, outBuffer, (uint)outBufferSize, out _, IntPtr.Zero))
                        {
                            int vendorOffset = Marshal.ReadInt32(outBuffer, 12);
                            int productOffset = Marshal.ReadInt32(outBuffer, 16);
                            int busType = Marshal.ReadInt32(outBuffer, 28);

                            string vendor = vendorOffset > 0 ? Marshal.PtrToStringAnsi(IntPtr.Add(outBuffer, vendorOffset))?.Trim() ?? string.Empty : string.Empty;
                            string product = productOffset > 0 ? Marshal.PtrToStringAnsi(IntPtr.Add(outBuffer, productOffset))?.Trim() ?? string.Empty : string.Empty;

                            string iface = busType switch
                            {
                                11 => "SATA",
                                17 => "NVMe",
                                7 => "USB",
                                3 => "ATA",
                                1 => "SCSI",
                                8 => "RAID",
                                14 => "Virtual",
                                _ => "Unknown"
                            };

                            string mediaType = busType == 17 ? "SSD" : "Unknown";
                            Marshal.WriteInt32(queryPtr, 0, 7); // StorageDeviceSeekPenaltyProperty
                            Marshal.WriteInt32(queryPtr, 4, 0);
                            if (DeviceIoControl(handle, IOCTL_STORAGE_QUERY_PROPERTY, queryPtr, (uint)querySize, outBuffer, (uint)outBufferSize, out _, IntPtr.Zero))
                            {
                                byte incursPenalty = Marshal.ReadByte(outBuffer, 8);
                                mediaType = incursPenalty == 0 ? "SSD" : "HDD";
                            }

                            long diskSizeBytes = 0;
                            IntPtr geomBuffer = Marshal.AllocHGlobal(256);
                            try
                            {
                                if (DeviceIoControl(handle, IOCTL_DISK_GET_DRIVE_GEOMETRY_EX, IntPtr.Zero, 0, geomBuffer, 256, out _, IntPtr.Zero))
                                {
                                    diskSizeBytes = Marshal.ReadInt64(geomBuffer, 24);
                                }
                            }
                            finally
                            {
                                Marshal.FreeHGlobal(geomBuffer);
                            }

                            long sizeGb = diskSizeBytes > 0 ? (long)Math.Round(diskSizeBytes / 1e9) : 0;
                            string name = !string.IsNullOrWhiteSpace(vendor) && !product.StartsWith(vendor, StringComparison.OrdinalIgnoreCase)
                                ? $"{vendor} {product}".Trim()
                                : product;
                            if (string.IsNullOrWhiteSpace(name)) name = $"PhysicalDrive {i}";

                            if (diskNumber > 0) sb.AppendLine();
                            sb.Append("Disk".GetLocalized()).Append(' ').Append(diskNumber).AppendLine(":");
                            AppendField(sb, "Name", name, null, true);
                            if (sizeGb > 0) AppendField(sb, "Size", sizeGb.ToString(), " GB", true);
                            AppendField(sb, "Type", mediaType, null, true);
                            AppendField(sb, "InterfaceType", iface, null, true);
                            if (!string.IsNullOrWhiteSpace(vendor)) AppendField(sb, "Manufacturer", vendor, null, true);
                            if (!string.IsNullOrWhiteSpace(product)) AppendField(sb, "Model", product, null, true);

                            diskNumber++;
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(queryPtr);
                        Marshal.FreeHGlobal(outBuffer);
                    }
                }

                if (diskNumber == 0)
                {
                    foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
                    {
                        if (diskNumber > 0) sb.AppendLine();
                        var name = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? drive.Name : $"{drive.Name} ({drive.VolumeLabel})";
                        var sizeGb = (long)Math.Round(drive.TotalSize / 1e9);

                        sb.Append("Disk".GetLocalized()).Append(' ').Append(diskNumber).AppendLine(":");
                        AppendField(sb, "Name", name, null, true);
                        AppendField(sb, "Size", sizeGb.ToString(), " GB", true);
                        AppendField(sb, "Type", "Fixed Disk", null, true);
                        AppendField(sb, "InterfaceType", drive.DriveFormat, null, true);
                        diskNumber++;
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
            SuggestedStartLocation = PickerLocationId.Desktop,
            Title = "Select Driver Export Folder",
            SettingsIdentifier = "RyTuneX_DriverExportPicker"
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