using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using Windows.Storage;

namespace RyTuneX.Helpers;
internal partial class OptimizationOptions
{
    private static readonly string CacheFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "iconCache.json");
    private static Dictionary<string, string> iconCache = [];

    static OptimizationOptions()
    {
        LoadIconCache();
    }

    [DllImport("psapi.dll")]
    public static extern bool EmptyWorkingSet(IntPtr hProcess);

    public static async Task<List<Tuple<string, string, bool>>> GetInstalledApps(bool uninstallableOnly)
    {
        var uwpAppsTask = Task.Run(() => GetUwpApps(uninstallableOnly));
        var win32AppsTask = Task.Run(GetWin32Apps);

        await Task.WhenAll(uwpAppsTask, win32AppsTask);

        var installedApps = uwpAppsTask.Result.Concat(win32AppsTask.Result).ToList();

        installedApps = [.. installedApps
            .DistinctBy(app => app.Item1)  // Remove duplicates based on app name
            .OrderBy(app => app.Item1)];   // Sort the apps alphabetically by name

        await LogHelper.Log("Returning Installed Apps [GetInstalledApps]");
        return installedApps;
    }

    private static List<Tuple<string, string, bool>> GetUwpApps(bool uninstallableOnly)
    {
        var installedApps = new List<Tuple<string, string, bool>>();
        var command = uninstallableOnly
            ? @"Get-AppxPackage | Where-Object { $_.NonRemovable -eq $false } | Select-Object Name,InstallLocation | Format-List"
            : @"Get-AppxPackage | Select-Object Name,InstallLocation | Format-List";

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -Command \"{command}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();

            var output = process.StandardOutput.ReadToEnd();
            string? currentName = null;
            string? currentLocation = null;

            foreach (var line in output.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.StartsWith("Name"))
                {
                    if (!string.IsNullOrEmpty(currentName) && !string.IsNullOrEmpty(currentLocation))
                    {
                        var logoPath = ExtractLogoPath(currentLocation);
                        installedApps.Add(new Tuple<string, string, bool>(currentName, logoPath, false)); // false for UWP
                    }

                    currentName = line.Split([':'], 2)[1].Trim();
                    currentLocation = null;
                }
                else if (line.StartsWith("InstallLocation"))
                {
                    currentLocation = line.Split([':'], 2)[1].Trim();
                }
                else if (!string.IsNullOrWhiteSpace(currentLocation) && line.StartsWith(" "))
                {
                    currentLocation += " " + line.Trim();
                }
            }

            if (!string.IsNullOrEmpty(currentName) && !string.IsNullOrEmpty(currentLocation))
            {
                var logoPath = ExtractLogoPath(currentLocation);
                installedApps.Add(new Tuple<string, string, bool>(currentName, logoPath, false)); // false for UWP
            }

            process.WaitForExit();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }

        return installedApps;
    }

    public static List<Tuple<string, string, bool>> GetWin32Apps()
    {
        var win32Apps = new List<Tuple<string, string, bool>>();

        var registryPaths = new string[]
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",            // 64-bit on 64-bit systems, 32-bit on 32-bit systems
            @"SOFTWARE\Wow6432node\Microsoft\Windows\CurrentVersion\Uninstall" // 32-bit on 64-bit systems
        };

        foreach (var registryPath in registryPaths)
        {
            try
            {
                using var machineKey = Registry.LocalMachine.OpenSubKey(registryPath);
                using var userKey = Registry.CurrentUser.OpenSubKey(registryPath);

                if (machineKey != null || userKey != null)
                {
                    foreach (var subKeyName in machineKey?.GetSubKeyNames().Concat(userKey?.GetSubKeyNames() ?? Enumerable.Empty<string>()) ?? Enumerable.Empty<string>())
                    {
                        using var subKey = machineKey?.OpenSubKey(subKeyName) ?? userKey?.OpenSubKey(subKeyName);
                        var displayName = subKey?.GetValue("DisplayName")?.ToString();
                        var installLocation = subKey?.GetValue("InstallLocation")?.ToString();

                        if (!string.IsNullOrEmpty(displayName) && !string.IsNullOrEmpty(installLocation) && !displayName.Contains("edge", StringComparison.CurrentCultureIgnoreCase))
                        {
                            var logoPath = ExtractLogoPath(installLocation, true); // true for Win32
                            win32Apps.Add(new Tuple<string, string, bool>(displayName, logoPath, true));
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load Win32 apps: {ex.Message}");
            }
        }

        return [.. win32Apps
            .DistinctBy(app => app.Item1)  // Remove duplicates based on app name
            .OrderBy(app => app.Item1)];   // Sort the apps alphabetically by name
    }

    private static string ExtractLogoPath(string installLocation, bool isWin32 = false)
    {
        if (iconCache.TryGetValue(installLocation, out var cachedIconPath))
        {
            return cachedIconPath;
        }

        var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "StoreLogo.backup.png");

        if (isWin32)
        {
            try
            {
                if (Directory.Exists(installLocation))
                {
                    var exeFile = Directory.GetFiles(installLocation, "*.exe").FirstOrDefault();
                    if (!string.IsNullOrEmpty(exeFile))
                    {
                        using var icon = System.Drawing.Icon.ExtractAssociatedIcon(exeFile);
                        if (icon != null)
                        {
                            var iconPath = Path.Combine(installLocation, "icon.png");
                            SaveIconAsPng(icon, iconPath);
                            logoPath = iconPath;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to extract logo for Win32 app: {ex.Message}");
            }
        }
        else
        {
            try
            {
                var packageName = Path.GetFileName(installLocation).ToLower();
                if (packageName.Contains("sechealth"))
                {
                    logoPath = Path.Combine(installLocation, "Assets", "WindowsSecurityAppList.targetsize-48.png");
                }
                else if (packageName.Contains("edge"))
                {
                    logoPath = Path.Combine(installLocation, "SmallLogo.png");
                }
                else
                {
                    string[] possibleManifestPaths = {
                        Path.Combine(installLocation, "AppxManifest.xml"),
                        Path.Combine(installLocation, "appxmanifest.xml")
                    };

                    var manifestPath = possibleManifestPaths.FirstOrDefault(File.Exists);

                    if (manifestPath != null)
                    {
                        var doc = XDocument.Load(manifestPath);
                        XNamespace ns = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";

                        var logoElement = doc.Descendants(ns + "Logo").FirstOrDefault();
                        if (logoElement != null)
                        {
                            var relativeLogoPath = logoElement.Value.Replace('/', '\\');
                            var baseLogoName = Path.GetFileNameWithoutExtension(relativeLogoPath);
                            var logoDirectory = Path.Combine(installLocation, Path.GetDirectoryName(relativeLogoPath) ?? "");

                            if (Directory.Exists(logoDirectory))
                            {
                                var exactLogoPath = Path.Combine(logoDirectory, relativeLogoPath);
                                if (File.Exists(exactLogoPath))
                                {
                                    logoPath = exactLogoPath;
                                }
                                else
                                {
                                    var logoFiles = Directory.GetFiles(logoDirectory, $"{baseLogoName}.Scale-*.png");
                                    var selectedLogoFile = logoFiles
                                        .OrderBy(f => Math.Abs(GetScaleFromFileName(f) - 200))
                                        .FirstOrDefault();

                                    if (!string.IsNullOrEmpty(selectedLogoFile) && File.Exists(selectedLogoFile))
                                    {
                                        logoPath = selectedLogoFile;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to extract logo path: {ex.Message}");
            }
        }
        iconCache[installLocation] = logoPath;
        SaveIconCache();
        return logoPath;
    }

    // Save the extracted icon as a PNG file
    private static void SaveIconAsPng(System.Drawing.Icon icon, string filePath)
    {
        using var stream = new MemoryStream();
        // Convert the icon to a bitmap and then save it as PNG
        using (var bitmap = new Bitmap(icon.ToBitmap()))
        {
            bitmap.Save(stream, ImageFormat.Png);
        }

        // Write the stream to the file
        File.WriteAllBytes(filePath, stream.ToArray());
    }

    private static int GetScaleFromFileName(string fileName)
    {
        var match = Regex.Match(fileName, @"Scale-(\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : 100;
    }

    internal static bool ServiceExists(string serviceName)
    {
        return Array.Exists(ServiceController.GetServices(), serviceController => serviceController.ServiceName.Equals(serviceName));
    }

    internal static void StopService(string serviceName)
    {
        if (ServiceExists(serviceName))
        {
            LogHelper.Log($"Stopping svc: {serviceName}");
            var sc = new ServiceController(serviceName);
            if (sc.CanStop)
            {
                sc.Stop();
            }
        }
    }

    internal static async Task ExecuteBatchFileAsync()
    {
        try
        {
            // Get the path to the PowerShell script file
            var scriptFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "RemoveEdge.ps1");

            if (!File.Exists(scriptFilePath))
            {
                await LogHelper.LogError($"Script file not found: {scriptFilePath}");
                return;
            }

            // Read the content of the script file
            var scriptContent = await File.ReadAllTextAsync(scriptFilePath);

            // Create a PowerShell instance
            using var PowerShellInstance = PowerShell.Create();
            await LogHelper.Log("Getting Installed Apps [OptimizationOptions.cs]");

            // Add the script content
            PowerShellInstance.AddScript(scriptContent)
                .AddArgument("-Set-ExecutionPolicy Unrestricted");

            // Invoke the script asynchronously
            await Task.Run(() => PowerShellInstance.Invoke());

            // Check for errors
            if (PowerShellInstance.HadErrors)
            {
                foreach (var error in PowerShellInstance.Streams.Error)
                {
                    await LogHelper.LogError($"PowerShell Error: {error}");
                }
            }
            else
            {
                await LogHelper.Log("PowerShell script executed successfully.");
            }
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"Error executing batch file: {ex.Message}");
        }
    }
    internal static async Task<int> StartInCmd(string command)
    {
        try
        {
            using var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Environment.Is64BitOperatingSystem
                        ? Path.Combine(Environment.GetEnvironmentVariable("windir"), @"SysNative\cmd.exe")
                        : Path.Combine(Environment.GetEnvironmentVariable("windir"), @"System32\cmd.exe"),
                    Arguments = $"/C {command}",
                    CreateNoWindow = true,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            // Start the process in a separate Task
            await Task.Run(() => p.Start());

            // Await process completion and capture exit code
            await p.WaitForExitAsync();
            return p.ExitCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error running command: {ex.Message}");
            throw;
        }
    }

    internal static void StartService(string serviceName)
    {
        if (ServiceExists(serviceName))
        {
            LogHelper.Log($"Starting svc: {serviceName}");
            var sc = new ServiceController(serviceName);
            sc.Start();
        }
    }

    internal static void ClearWorkingSet()
    {
        // Clear the working set for the current process
        EmptyWorkingSet(Process.GetCurrentProcess().Handle);

        // Get all running processes
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                // Clear the working set for each process
                EmptyWorkingSet(process.Handle);
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex.Message);
            }
        }
    }

    private static void SaveRevertAction(string action)
    {
        // Get the current revert list
        var revertListJson = ApplicationData.Current.LocalSettings.Values["RevertList"] as string;
        var revertList = string.IsNullOrEmpty(revertListJson) ? [] : JsonSerializer.Deserialize<List<string>>(revertListJson);

        // Add the action if it's not already present
        if (!revertList.Contains(action))
        {
            revertList.Add(action);
            ApplicationData.Current.LocalSettings.Values["RevertList"] = JsonSerializer.Serialize(revertList);
        }
    }

    private static void RemoveRevertAction(string action)
    {
        // Get the current revert list
        var revertListJson = ApplicationData.Current.LocalSettings.Values["RevertList"] as string;
        var revertList = string.IsNullOrEmpty(revertListJson) ? [] : JsonSerializer.Deserialize<List<string>>(revertListJson);

        // Remove the action if it's present
        if (revertList.Contains(action))
        {
            revertList.Remove(action);
            ApplicationData.Current.LocalSettings.Values["RevertList"] = JsonSerializer.Serialize(revertList);
        }
    }

    private static void LoadIconCache()
    {
        if (File.Exists(CacheFilePath))
        {
            var json = File.ReadAllText(CacheFilePath);
            iconCache = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
        }
    }

    private static void SaveIconCache()
    {
        var json = JsonSerializer.Serialize(iconCache);
        File.WriteAllText(CacheFilePath, json);
    }

    public static async Task RevertAllChanges()
    {
        var revertListJson = ApplicationData.Current.LocalSettings.Values["RevertList"] as string;
        var revertList = string.IsNullOrEmpty(revertListJson) ? [] : JsonSerializer.Deserialize<List<string>>(revertListJson);

        // Execute each action asynchronously
        foreach (var action in revertList)
        {
            await Task.Run(() =>
            {
                switch (action)
                {
                    case "EnableMenuShowDelay":
                        OptimizeSystemHelper.EnableMenuShowDelay();
                        break;
                    case "EnableMouseHoverTime":
                        OptimizeSystemHelper.EnableMouseHoverTime();
                        break;
                    case "EnableBackgroundApps":
                        OptimizeSystemHelper.EnableBackgroundApps();
                        break;
                    case "EnableAutoComplete":
                        OptimizeSystemHelper.EnableAutoComplete();
                        break;
                    case "DisableCrashDump":
                        OptimizeSystemHelper.DisableCrashDump();
                        break;
                    case "EnableRemoteAssistance":
                        OptimizeSystemHelper.EnableRemoteAssistance();
                        break;
                    case "EnableWindowShake":
                        OptimizeSystemHelper.EnableWindowShake();
                        break;
                    case "RemoveCopyMoveContextMenu":
                        OptimizeSystemHelper.RemoveCopyMoveContextMenu();
                        break;
                    case "IncreaseTaskTimeouts":
                        OptimizeSystemHelper.IncreaseTaskTimeouts();
                        break;
                    case "DisableLowDiskSpaceChecks":
                        OptimizeSystemHelper.DisableLowDiskSpaceChecks();
                        break;
                    case "EnableLinkResolve":
                        OptimizeSystemHelper.EnableLinkResolve();
                        break;
                    case "RevertServiceTimeouts":
                        OptimizeSystemHelper.RevertServiceTimeouts();
                        break;
                    case "EnableRemoteRegistry":
                        OptimizeSystemHelper.EnableRemoteRegistry();
                        break;
                    case "ShowFileExtensionsAndHiddenFiles":
                        OptimizeSystemHelper.ShowFileExtensionsAndHiddenFiles();
                        break;
                    case "RevertSystemProfile":
                        OptimizeSystemHelper.RevertSystemProfile();
                        break;
                    case "RevertGPUAndPrioritySettings":
                        OptimizeSystemHelper.RevertGPUAndPrioritySettings();
                        break;
                    case "EnableFrameServerMode":
                        OptimizeSystemHelper.EnableFrameServerMode();
                        break;
                    case "RevertLowLatencyGPUSettings":
                        OptimizeSystemHelper.RevertLowLatencyGPUSettings();
                        break;
                    case "RevertNonBestEffortLimit":
                        OptimizeSystemHelper.RevertNonBestEffortLimit();
                        break;
                    case "EnableTelemetryServices":
                        OptimizeSystemHelper.EnableTelemetryServices();
                        break;
                    case "EnableHomeGroup":
                        OptimizeSystemHelper.EnableHomeGroup();
                        break;
                    case "EnablePrintService":
                        OptimizeSystemHelper.EnablePrintService();
                        break;
                    case "EnableSysMain":
                        OptimizeSystemHelper.EnableSysMain();
                        break;
                    case "EnableCompatibilityAssistant":
                        OptimizeSystemHelper.EnableCompatibilityAssistant();
                        break;
                    case "EnableSystemRestore":
                        OptimizeSystemHelper.EnableSystemRestore();
                        break;
                    case "DisableVerboseLogon":
                        OptimizeSystemHelper.DisableVerboseLogon();
                        break;
                    case "DisableClassicContextMenu":
                        OptimizeSystemHelper.DisableClassicContextMenu();
                        break;
                    case "EnableSearch":
                        OptimizeSystemHelper.EnableSearch();
                        break;
                    case "EnableBiometrics":
                        OptimizeSystemHelper.EnableBiometrics();
                        break;
                    case "EnableSMB(\"1\")":
                        OptimizeSystemHelper.EnableSMB("1");
                        break;
                    case "EnableSMB(\"2\")":
                        OptimizeSystemHelper.EnableSMB("2");
                        break;
                    case "EnableNTFSTimeStamp":
                        OptimizeSystemHelper.EnableNTFSTimeStamp();
                        break;
                    case "EnableErrorReporting":
                        OptimizeSystemHelper.EnableErrorReporting();
                        break;
                    case "EnableCortana":
                        OptimizeSystemHelper.EnableCortana();
                        break;
                    case "DisableGamingMode":
                        OptimizeSystemHelper.DisableGamingMode();
                        break;
                    case "EnableAutomaticUpdates":
                        OptimizeSystemHelper.EnableAutomaticUpdates();
                        break;
                    case "EnableStoreUpdates":
                        OptimizeSystemHelper.EnableStoreUpdates();
                        break;
                    case "EnableOneDrive":
                        OptimizeSystemHelper.EnableOneDrive();
                        break;
                    case "EnableSensorServices":
                        OptimizeSystemHelper.EnableSensorServices();
                        break;
                    case "EnableNewsAndInterests":
                        OptimizeSystemHelper.EnableNewsAndInterests();
                        break;
                    case "EnableSpotlightFeatures":
                        OptimizeSystemHelper.EnableSpotlightFeatures();
                        break;
                    case "EnableTailoredExperiences":
                        OptimizeSystemHelper.EnableTailoredExperiences();
                        break;
                    case "EnableCloudOptimizedContent":
                        OptimizeSystemHelper.EnableCloudOptimizedContent();
                        break;
                    case "EnableFeedbackNotifications":
                        OptimizeSystemHelper.EnableFeedbackNotifications();
                        break;
                    case "EnableAdvertisingID":
                        OptimizeSystemHelper.EnableAdvertisingID();
                        break;
                    case "EnableBluetoothAdvertising":
                        OptimizeSystemHelper.EnableBluetoothAdvertising();
                        break;
                    case "EnableAutomaticRestartSignOn":
                        OptimizeSystemHelper.EnableAutomaticRestartSignOn();
                        break;
                    case "EnableHandwritingDataSharing":
                        OptimizeSystemHelper.EnableHandwritingDataSharing();
                        break;
                    case "EnableTextInputDataCollection":
                        OptimizeSystemHelper.EnableTextInputDataCollection();
                        break;
                    case "EnableInputPersonalization":
                        OptimizeSystemHelper.EnableInputPersonalization();
                        break;
                    case "EnableSafeSearchMode":
                        OptimizeSystemHelper.EnableSafeSearchMode();
                        break;
                    case "EnableActivityUploads":
                        OptimizeSystemHelper.EnableActivityUploads();
                        break;
                    case "EnableClipboardSync":
                        OptimizeSystemHelper.EnableClipboardSync();
                        break;
                    case "EnableMessageSync":
                        OptimizeSystemHelper.EnableMessageSync();
                        break;
                    case "EnableSettingSync":
                        OptimizeSystemHelper.EnableSettingSync();
                        break;
                    case "EnableVoiceActivation":
                        OptimizeSystemHelper.EnableVoiceActivation();
                        break;
                    case "EnableFindMyDevice":
                        OptimizeSystemHelper.EnableFindMyDevice();
                        break;
                    case "EnableActivityFeed":
                        OptimizeSystemHelper.EnableActivityFeed();
                        break;
                    case "EnableCdp":
                        OptimizeSystemHelper.EnableCdp();
                        break;
                    case "EnableDiagnosticsToast":
                        OptimizeSystemHelper.EnableDiagnosticsToast();
                        break;
                    case "EnableOnlineSpeechPrivacy":
                        OptimizeSystemHelper.EnableOnlineSpeechPrivacy();
                        break;
                    case "EnableLocationFeatures":
                        OptimizeSystemHelper.EnableLocationFeatures();
                        break;
                    case "EnableGameBar":
                        OptimizeSystemHelper.EnableGameBar();
                        break;
                    case "EnableQuickAccessHistory":
                        OptimizeSystemHelper.EnableQuickAccessHistory();
                        break;
                    case "EnableMyPeople":
                        OptimizeSystemHelper.EnableMyPeople();
                        break;
                    case "IncludeDrivers":
                        OptimizeSystemHelper.IncludeDrivers();
                        break;
                    case "EnableWindowsInk":
                        OptimizeSystemHelper.EnableWindowsInk();
                        break;
                    case "EnableSpellingAndTypingFeatures":
                        OptimizeSystemHelper.EnableSpellingAndTypingFeatures();
                        break;
                    case "EnableFaxService":
                        OptimizeSystemHelper.EnableFaxService();
                        break;
                    case "EnableInsiderService":
                        OptimizeSystemHelper.EnableInsiderService();
                        break;
                    case "EnableSmartScreen":
                        OptimizeSystemHelper.EnableSmartScreen();
                        break;
                    case "EnableCloudClipboard":
                        OptimizeSystemHelper.EnableCloudClipboard();
                        break;
                    case "EnableStickyKeys":
                        OptimizeSystemHelper.EnableStickyKeys();
                        break;
                    case "AddCastToDevice":
                        OptimizeSystemHelper.AddCastToDevice();
                        break;
                    case "EnableVBS":
                        OptimizeSystemHelper.EnableVBS();
                        break;
                    case "AlignTaskbarToCenter":
                        OptimizeSystemHelper.AlignTaskbarToCenter();
                        break;
                    case "EnableSnapAssist":
                        OptimizeSystemHelper.EnableSnapAssist();
                        break;
                    case "EnableWidgets":
                        OptimizeSystemHelper.EnableWidgets();
                        break;
                    case "EnableChat":
                        OptimizeSystemHelper.EnableChat();
                        break;
                    case "DisableFilesCompactMode":
                        OptimizeSystemHelper.DisableFilesCompactMode();
                        break;
                    case "EnableStickers":
                        OptimizeSystemHelper.EnableStickers();
                        break;
                    case "EnableEdgeDiscoverBar":
                        OptimizeSystemHelper.EnableEdgeDiscoverBar();
                        break;
                    case "EnableEdgeTelemetry":
                        OptimizeSystemHelper.EnableEdgeTelemetry();
                        break;
                    case "EnableCoPilotAI":
                        OptimizeSystemHelper.EnableCoPilotAI();
                        break;
                    case "EnableVisualStudioTelemetry":
                        OptimizeSystemHelper.EnableVisualStudioTelemetry();
                        break;
                    case "EnableNvidiaTelemetry":
                        OptimizeSystemHelper.EnableNvidiaTelemetry();
                        break;
                    case "EnableChromeTelemetry":
                        OptimizeSystemHelper.EnableChromeTelemetry();
                        break;
                    case "EnableFirefoxTelemetry":
                        OptimizeSystemHelper.EnableFirefoxTelemetry();
                        break;
                    case "EnableHibernation":
                        OptimizeSystemHelper.EnableHibernation();
                        break;
                    case "DisableEndTask":
                        OptimizeSystemHelper.DisableEndTask();
                        break;
                }
            });
        }
    }
    public static async Task XamlSwitchesAsync(ToggleSwitch toggleSwitch)
    {
        if (toggleSwitch != null && toggleSwitch.Tag != null)
        {
            switch (toggleSwitch.Tag)
            {
                case "MenuShowDelay":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableMenuShowDelay();
                        SaveRevertAction("EnableMenuShowDelay");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableMenuShowDelay();
                        RemoveRevertAction("EnableMenuShowDelay");
                    }
                    break;

                case "MouseHoverTime":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableMouseHoverTime();
                        SaveRevertAction("EnableMouseHoverTime");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableMouseHoverTime();
                        RemoveRevertAction("EnableMouseHoverTime");
                    }
                    break;

                case "BackgroundApps":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableBackgroundApps();
                        SaveRevertAction("EnableBackgroundApps");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableBackgroundApps();
                        RemoveRevertAction("EnableBackgroundApps");
                    }
                    break;

                case "AutoComplete":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableAutoComplete();
                        SaveRevertAction("EnableAutoComplete");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableAutoComplete();
                        RemoveRevertAction("EnableAutoComplete");
                    }
                    break;

                case "CrashDump":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.EnableCrashDump();
                        SaveRevertAction("DisableCrashDump");
                    }
                    else
                    {
                        OptimizeSystemHelper.DisableCrashDump();
                        RemoveRevertAction("DisableCrashDump");
                    }
                    break;

                case "RemoteAssistance":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableRemoteAssistance();
                        SaveRevertAction("EnableRemoteAssistance");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableRemoteAssistance();
                        RemoveRevertAction("EnableRemoteAssistance");
                    }
                    break;

                case "WindowShake":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableWindowShake();
                        SaveRevertAction("EnableWindowShake");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableWindowShake();
                        RemoveRevertAction("EnableWindowShake");
                    }
                    break;

                case "CopyMoveContextMenu":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.AddCopyMoveContextMenu();
                        SaveRevertAction("RemoveCopyMoveContextMenu");
                    }
                    else
                    {
                        OptimizeSystemHelper.RemoveCopyMoveContextMenu();
                        RemoveRevertAction("RemoveCopyMoveContextMenu");
                    }
                    break;

                case "TaskTimeouts":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.AdjustTaskTimeouts();
                        SaveRevertAction("IncreaseTaskTimeouts");
                    }
                    else
                    {
                        OptimizeSystemHelper.IncreaseTaskTimeouts();
                        RemoveRevertAction("IncreaseTaskTimeouts");
                    }
                    break;

                case "LowDiskSpaceChecks":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.EnableLowDiskSpaceChecks();
                        SaveRevertAction("DisableLowDiskSpaceChecks");
                    }
                    else
                    {
                        OptimizeSystemHelper.DisableLowDiskSpaceChecks();
                        RemoveRevertAction("DisableLowDiskSpaceChecks");
                    }
                    break;

                case "LinkResolve":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableLinkResolve();
                        SaveRevertAction("EnableLinkResolve");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableLinkResolve();
                        RemoveRevertAction("EnableLinkResolve");
                    }
                    break;

                case "ServiceTimeouts":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DecreaseServiceTimeouts();
                        SaveRevertAction("RevertServiceTimeouts");
                    }
                    else
                    {
                        OptimizeSystemHelper.RevertServiceTimeouts();
                        RemoveRevertAction("RevertServiceTimeouts");
                    }
                    break;

                case "RemoteRegistry":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableRemoteRegistry();
                        SaveRevertAction("EnableRemoteRegistry");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableRemoteRegistry();
                        RemoveRevertAction("EnableRemoteRegistry");
                    }
                    break;

                case "FileExtensionsAndHiddenFiles":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.HideFileExtensionsAndHiddenFiles();
                        SaveRevertAction("ShowFileExtensionsAndHiddenFiles");
                    }
                    else
                    {
                        OptimizeSystemHelper.ShowFileExtensionsAndHiddenFiles();
                        RemoveRevertAction("ShowFileExtensionsAndHiddenFiles");
                    }
                    break;

                case "SystemProfile":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.OptimizeSystemProfile();
                        SaveRevertAction("RevertSystemProfile");
                    }
                    else
                    {
                        OptimizeSystemHelper.RevertSystemProfile();
                        RemoveRevertAction("RevertSystemProfile");
                    }
                    break;

                case "GPUAndPrioritySettings":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.SetGPUAndPrioritySettings();
                        SaveRevertAction("RevertGPUAndPrioritySettings");
                    }
                    else
                    {
                        OptimizeSystemHelper.RevertGPUAndPrioritySettings();
                        RemoveRevertAction("RevertGPUAndPrioritySettings");
                    }
                    break;

                case "FrameServerMode":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableFrameServerMode();
                        SaveRevertAction("EnableFrameServerMode");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableFrameServerMode();
                        RemoveRevertAction("EnableFrameServerMode");
                    }
                    break;

                case "LowLatencyGPUSettings":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.SetLowLatencyGPUSettings();
                        SaveRevertAction("RevertLowLatencyGPUSettings");
                    }
                    else
                    {
                        OptimizeSystemHelper.RevertLowLatencyGPUSettings();
                        RemoveRevertAction("RevertLowLatencyGPUSettings");
                    }
                    break;

                case "NonBestEffortLimit":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.SetNonBestEffortLimit();
                        SaveRevertAction("RevertNonBestEffortLimit");
                    }
                    else
                    {
                        OptimizeSystemHelper.RevertNonBestEffortLimit();
                        RemoveRevertAction("RevertNonBestEffortLimit");
                    }
                    break;

                case "TelemetryServices":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableTelemetryServices();
                        SaveRevertAction("EnableTelemetryServices");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableTelemetryServices();
                        RemoveRevertAction("EnableTelemetryServices");
                    }
                    break;

                case "HomeGroup":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableHomeGroup();
                        SaveRevertAction("EnableHomeGroup");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableHomeGroup();
                        RemoveRevertAction("EnableHomeGroup");
                    }
                    break;

                case "PrintService":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisablePrintService();
                        SaveRevertAction("EnablePrintService");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnablePrintService();
                        RemoveRevertAction("EnablePrintService");
                    }
                    break;

                case "SysMain":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSysMain();
                        SaveRevertAction("EnableSysMain");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSysMain();
                        RemoveRevertAction("EnableSysMain");
                    }
                    break;

                case "CompatibilityAssistant":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableCompatibilityAssistant();
                        SaveRevertAction("EnableCompatibilityAssistant");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableCompatibilityAssistant();
                        RemoveRevertAction("EnableCompatibilityAssistant");
                    }
                    break;

                case "SystemRestore":
                    if (toggleSwitch.IsOn)
                    {
                        var restoreWarning = new ContentDialog
                        {
                            XamlRoot = App.MainWindow.Content.XamlRoot,
                            Title = "Warning".GetLocalized(),
                            Content = "RestoreWarningDialogText".GetLocalized(),
                            PrimaryButtonText = "Continue".GetLocalized(),
                            CloseButtonText = "Cancel".GetLocalized(),
                            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
                            BorderBrush = (SolidColorBrush)Application.Current.Resources["AccentAAFillColorDefaultBrush"],
                            PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"]
                        };
                        var dialogResult = await restoreWarning.ShowAsync();
                        if (dialogResult == ContentDialogResult.Primary)
                        {
                            OptimizeSystemHelper.DisableSystemRestore();
                            SaveRevertAction("EnableSystemRestore");
                        }
                        else
                        {
                            toggleSwitch.IsOn = false;
                        }
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSystemRestore();
                        RemoveRevertAction("EnableSystemRestore");
                    }
                    break;

                case "VerboseLogon":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.EnableVerboseLogon();
                        SaveRevertAction("DisableVerboseLogon");
                    }
                    else
                    {
                        OptimizeSystemHelper.DisableVerboseLogon();
                        RemoveRevertAction("DisableVerboseLogon");
                    }
                    break;

                case "ClassicContextMenu":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.EnableClassicContextMenu();
                        SaveRevertAction("DisableClassicContextMenu");
                    }
                    else
                    {
                        OptimizeSystemHelper.DisableClassicContextMenu();
                        RemoveRevertAction("DisableClassicContextMenu");
                    }
                    break;

                case "Search":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSearch();
                        SaveRevertAction("EnableSearch");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSearch();
                        RemoveRevertAction("EnableSearch");
                    }
                    break;

                case "Biometrics":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableBiometrics();
                        SaveRevertAction("EnableBiometrics");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableBiometrics();
                        RemoveRevertAction("EnableBiometrics");
                    }
                    break;

                case "SMBv1":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSMB("1");
                        SaveRevertAction("EnableSMB(\"1\")");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSMB("1");
                        RemoveRevertAction("EnableSMB(\"1\")");
                    }
                    break;

                case "SMBv2":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSMB("2");
                        SaveRevertAction("EnableSMB(\"2\")");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSMB("2");
                        RemoveRevertAction("EnableSMB(\"2\")");
                    }
                    break;
                case "NTFSTimeStamp":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableNTFSTimeStamp();
                        SaveRevertAction("EnableNTFSTimeStamp");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableNTFSTimeStamp();
                        RemoveRevertAction("EnableNTFSTimeStamp");
                    }
                    break;

                case "ErrorReporting":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableErrorReporting();
                        SaveRevertAction("EnableErrorReporting");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableErrorReporting();
                        RemoveRevertAction("EnableErrorReporting");
                    }
                    break;

                case "Cortana":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableCortana();
                        SaveRevertAction("EnableCortana");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableCortana();
                        RemoveRevertAction("EnableCortana");
                    }
                    break;

                case "GamingMode":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.EnableGamingMode();
                        SaveRevertAction("DisableGamingMode");
                    }
                    else
                    {
                        OptimizeSystemHelper.DisableGamingMode();
                        RemoveRevertAction("DisableGamingMode");
                    }
                    break;

                case "AutomaticUpdates":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableAutomaticUpdates();
                        SaveRevertAction("EnableAutomaticUpdates");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableAutomaticUpdates();
                        RemoveRevertAction("EnableAutomaticUpdates");
                    }
                    break;

                case "StoreUpdates":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableStoreUpdates();
                        SaveRevertAction("EnableStoreUpdates");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableStoreUpdates();
                        RemoveRevertAction("EnableStoreUpdates");
                    }
                    break;

                case "OneDrive":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableOneDrive();
                        SaveRevertAction("EnableOneDrive");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableOneDrive();
                        RemoveRevertAction("EnableOneDrive");
                    }
                    break;

                case "SensorServices":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSensorServices();
                        SaveRevertAction("EnableSensorServices");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSensorServices();
                        RemoveRevertAction("EnableSensorServices");
                    }
                    break;

                case "NewsAndInterests":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableNewsAndInterests();
                        SaveRevertAction("EnableNewsAndInterests");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableNewsAndInterests();
                        RemoveRevertAction("EnableNewsAndInterests");
                    }
                    break;

                case "SpotlightFeatures":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSpotlightFeatures();
                        SaveRevertAction("EnableSpotlightFeatures");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSpotlightFeatures();
                        RemoveRevertAction("EnableSpotlightFeatures");
                    }
                    break;

                case "TailoredExperiences":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableTailoredExperiences();
                        SaveRevertAction("EnableTailoredExperiences");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableTailoredExperiences();
                        RemoveRevertAction("EnableTailoredExperiences");
                    }
                    break;

                case "CloudOptimizedContent":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableCloudOptimizedContent();
                        SaveRevertAction("EnableCloudOptimizedContent");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableCloudOptimizedContent();
                        RemoveRevertAction("EnableCloudOptimizedContent");
                    }
                    break;

                case "FeedbackNotifications":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableFeedbackNotifications();
                        SaveRevertAction("EnableFeedbackNotifications");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableFeedbackNotifications();
                        RemoveRevertAction("EnableFeedbackNotifications");
                    }
                    break;

                case "AdvertisingID":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableAdvertisingID();
                        SaveRevertAction("EnableAdvertisingID");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableAdvertisingID();
                        RemoveRevertAction("EnableAdvertisingID");
                    }
                    break;

                case "BluetoothAdvertising":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableBluetoothAdvertising();
                        SaveRevertAction("EnableBluetoothAdvertising");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableBluetoothAdvertising();
                        RemoveRevertAction("EnableBluetoothAdvertising");
                    }
                    break;

                case "AutomaticRestartSignOn":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableAutomaticRestartSignOn();
                        SaveRevertAction("EnableAutomaticRestartSignOn");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableAutomaticRestartSignOn();
                        RemoveRevertAction("EnableAutomaticRestartSignOn");
                    }
                    break;

                case "HandwritingDataSharing":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableHandwritingDataSharing();
                        SaveRevertAction("EnableHandwritingDataSharing");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableHandwritingDataSharing();
                        RemoveRevertAction("EnableHandwritingDataSharing");
                    }
                    break;

                case "TextInputDataCollection":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableTextInputDataCollection();
                        SaveRevertAction("EnableTextInputDataCollection");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableTextInputDataCollection();
                        RemoveRevertAction("EnableTextInputDataCollection");
                    }
                    break;

                case "InputPersonalization":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableInputPersonalization();
                        SaveRevertAction("EnableInputPersonalization");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableInputPersonalization();
                        RemoveRevertAction("EnableInputPersonalization");
                    }
                    break;

                case "SafeSearchMode":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSafeSearchMode();
                        SaveRevertAction("EnableSafeSearchMode");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSafeSearchMode();
                        RemoveRevertAction("EnableSafeSearchMode");
                    }
                    break;

                case "ActivityUploads":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableActivityUploads();
                        SaveRevertAction("EnableActivityUploads");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableActivityUploads();
                        RemoveRevertAction("EnableActivityUploads");
                    }
                    break;

                case "ClipboardSync":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableClipboardSync();
                        SaveRevertAction("EnableClipboardSync");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableClipboardSync();
                        RemoveRevertAction("EnableClipboardSync");
                    }
                    break;

                case "MessageSync":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableMessageSync();
                        SaveRevertAction("EnableMessageSync");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableMessageSync();
                        RemoveRevertAction("EnableMessageSync");
                    }
                    break;

                case "SettingSync":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSettingSync();
                        SaveRevertAction("EnableSettingSync");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSettingSync();
                        RemoveRevertAction("EnableSettingSync");
                    }
                    break;

                case "VoiceActivation":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableVoiceActivation();
                        SaveRevertAction("EnableVoiceActivation");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableVoiceActivation();
                        RemoveRevertAction("EnableVoiceActivation");
                    }
                    break;

                case "FindMyDevice":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableFindMyDevice();
                        SaveRevertAction("EnableFindMyDevice");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableFindMyDevice();
                        RemoveRevertAction("EnableFindMyDevice");
                    }
                    break;

                case "ActivityFeed":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableActivityFeed();
                        SaveRevertAction("EnableActivityFeed");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableActivityFeed();
                        RemoveRevertAction("EnableActivityFeed");
                    }
                    break;

                case "Cdp":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableCdp();
                        SaveRevertAction("EnableCdp");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableCdp();
                        RemoveRevertAction("EnableCdp");
                    }
                    break;

                case "DiagnosticsToast":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableDiagnosticsToast();
                        SaveRevertAction("EnableDiagnosticsToast");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableDiagnosticsToast();
                        RemoveRevertAction("EnableDiagnosticsToast");
                    }
                    break;

                case "OnlineSpeechPrivacy":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableOnlineSpeechPrivacy();
                        SaveRevertAction("EnableOnlineSpeechPrivacy");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableOnlineSpeechPrivacy();
                        RemoveRevertAction("EnableOnlineSpeechPrivacy");
                    }
                    break;

                case "LocationAccess":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableLocationFeatures();
                        SaveRevertAction("EnableLocationFeatures");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableLocationFeatures();
                        RemoveRevertAction("EnableLocationFeatures");
                    }
                    break;

                case "LocationFeatures":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableLocationFeatures();
                        SaveRevertAction("EnableLocationFeatures");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableLocationFeatures();
                        RemoveRevertAction("EnableLocationFeatures");
                    }
                    break;

                case "GameBar":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableGameBar();
                        SaveRevertAction("EnableGameBar");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableGameBar();
                        RemoveRevertAction("EnableGameBar");
                    }
                    break;

                case "QuickAccessHistory":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableQuickAccessHistory();
                        SaveRevertAction("EnableQuickAccessHistory");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableQuickAccessHistory();
                        RemoveRevertAction("EnableQuickAccessHistory");
                    }
                    break;

                case "MyPeople":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableMyPeople();
                        SaveRevertAction("EnableMyPeople");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableMyPeople();
                        RemoveRevertAction("EnableMyPeople");
                    }
                    break;

                case "Drivers":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.ExcludeDrivers();
                        SaveRevertAction("IncludeDrivers");
                    }
                    else
                    {
                        OptimizeSystemHelper.IncludeDrivers();
                        RemoveRevertAction("IncludeDrivers");
                    }
                    break;

                case "WindowsInk":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableWindowsInk();
                        SaveRevertAction("EnableWindowsInk");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableWindowsInk();
                        RemoveRevertAction("EnableWindowsInk");
                    }
                    break;

                case "SpellingAndTypingFeatures":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSpellingAndTypingFeatures();
                        SaveRevertAction("EnableSpellingAndTypingFeatures");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSpellingAndTypingFeatures();
                        RemoveRevertAction("EnableSpellingAndTypingFeatures");
                    }
                    break;

                case "FaxService":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableFaxService();
                        SaveRevertAction("EnableFaxService");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableFaxService();
                        RemoveRevertAction("EnableFaxService");
                    }
                    break;

                case "InsiderService":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableInsiderService();
                        SaveRevertAction("EnableInsiderService");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableInsiderService();
                        RemoveRevertAction("EnableInsiderService");
                    }
                    break;

                case "SmartScreen":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSmartScreen();
                        SaveRevertAction("EnableSmartScreen");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSmartScreen();
                        RemoveRevertAction("EnableSmartScreen");
                    }
                    break;

                case "CloudClipboard":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableCloudClipboard();
                        SaveRevertAction("EnableCloudClipboard");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableCloudClipboard();
                        RemoveRevertAction("EnableCloudClipboard");
                    }
                    break;

                case "StickyKeys":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableStickyKeys();
                        SaveRevertAction("EnableStickyKeys");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableStickyKeys();
                        RemoveRevertAction("EnableStickyKeys");
                    }
                    break;

                case "CastToDevice":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.RemoveCastToDevice();
                        SaveRevertAction("AddCastToDevice");
                    }
                    else
                    {
                        OptimizeSystemHelper.AddCastToDevice();
                        RemoveRevertAction("AddCastToDevice");
                    }
                    break;

                case "VBS":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableVBS();
                        SaveRevertAction("EnableVBS");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableVBS();
                        RemoveRevertAction("EnableVBS");
                    }
                    break;

                case "TaskbarToLeft":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.AlignTaskbarToLeft();
                        SaveRevertAction("AlignTaskbarToCenter");
                    }
                    else
                    {
                        OptimizeSystemHelper.AlignTaskbarToCenter();
                        RemoveRevertAction("AlignTaskbarToCenter");
                    }
                    break;

                case "SnapAssist":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSnapAssist();
                        SaveRevertAction("EnableSnapAssist");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSnapAssist();
                        RemoveRevertAction("EnableSnapAssist");
                    }
                    break;

                case "Widgets":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableWidgets();
                        SaveRevertAction("EnableWidgets");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableWidgets();
                        RemoveRevertAction("EnableWidgets");
                    }
                    break;

                case "Chat":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableChat();
                        SaveRevertAction("EnableChat");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableChat();
                        RemoveRevertAction("EnableChat");
                    }
                    break;

                case "FilesCompactMode":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.EnableFilesCompactMode();
                        SaveRevertAction("DisableFilesCompactMode");
                    }
                    else
                    {
                        OptimizeSystemHelper.DisableFilesCompactMode();
                        RemoveRevertAction("DisableFilesCompactMode");
                    }
                    break;

                case "Stickers":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableStickers();
                        SaveRevertAction("EnableStickers");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableStickers();
                        RemoveRevertAction("EnableStickers");
                    }
                    break;

                case "EdgeDiscoverBar":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableEdgeDiscoverBar();
                        SaveRevertAction("EnableEdgeDiscoverBar");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableEdgeDiscoverBar();
                        RemoveRevertAction("EnableEdgeDiscoverBar");
                    }
                    break;

                case "EdgeTelemetry":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableEdgeTelemetry();
                        SaveRevertAction("EnableEdgeTelemetry");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableEdgeTelemetry();
                        RemoveRevertAction("EnableEdgeTelemetry");
                    }
                    break;

                case "CoPilotAI":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableCoPilotAI();
                        SaveRevertAction("EnableCoPilotAI");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableCoPilotAI();
                        RemoveRevertAction("EnableCoPilotAI");
                    }
                    break;

                case "VisualStudioTelemetry":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableVisualStudioTelemetry();
                        SaveRevertAction("EnableVisualStudioTelemetry");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableVisualStudioTelemetry();
                        RemoveRevertAction("EnableVisualStudioTelemetry");
                    }
                    break;

                case "NvidiaTelemetry":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableNvidiaTelemetry();
                        SaveRevertAction("EnableNvidiaTelemetry");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableNvidiaTelemetry();
                        RemoveRevertAction("EnableNvidiaTelemetry");
                    }
                    break;

                case "ChromeTelemetry":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableChromeTelemetry();
                        SaveRevertAction("EnableChromeTelemetry");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableChromeTelemetry();
                        RemoveRevertAction("EnableChromeTelemetry");
                    }
                    break;

                case "FirefoxTelemetry":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableFirefoxTelemetry();
                        SaveRevertAction("EnableFirefoxTelemetry");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableFirefoxTelemetry();
                        RemoveRevertAction("EnableFirefoxTelemetry");
                    }
                    break;

                case "Hibernation":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableHibernation();
                        SaveRevertAction("EnableHibernation");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableHibernation();
                        RemoveRevertAction("EnableHibernation");
                    }
                    break;

                case "EndTask":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.EnableEndTask();
                        SaveRevertAction("DisableEndTask");
                    }
                    else
                    {
                        OptimizeSystemHelper.DisableEndTask();
                        RemoveRevertAction("DisableEndTask");
                    }
                    break;
            }
        }
    }
}