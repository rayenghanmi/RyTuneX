using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;

namespace RyTuneX.Helpers;
internal partial class OptimizationOptions
{
    private const string RegistryBaseKey = @"SOFTWARE\RyTuneX\Optimizations";
    private static readonly string IconCacheDirectory = Path.Combine(Path.GetTempPath(), "RyTuneX_AppIcons");

    [DllImport("Shell32.dll", CharSet = CharSet.Auto)]
    public static extern int ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[] phiconLarge, IntPtr[]? phiconSmall, int nIcons);

    public static async Task<List<Tuple<string, string, bool>>> GetInstalledApps(bool uninstallableOnly)
    {
        // Ensure the icon cache directory exists
        if (!Directory.Exists(IconCacheDirectory))
        {
            Directory.CreateDirectory(IconCacheDirectory);
        }

        var largeIcons = new IntPtr[1];
        ExtractIconEx(@"C:\Windows\System32\imageres.dll", 152, largeIcons, null, 1);
        var extractedIcon = System.Drawing.Icon.FromHandle(largeIcons[0]);
        var bmp = extractedIcon.ToBitmap();
        bmp.Save(Path.Combine(IconCacheDirectory, "defaulticon.png"), ImageFormat.Png);

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

    private static string GetSafeIconFileName(string appName, string extension = ".png")
    {
        // Remove invalid filename characters and limit length
        var invalidChars = Path.GetInvalidFileNameChars();
        var safeName = string.Concat(appName.Where(c => !invalidChars.Contains(c)));

        // Limit length to prevent long filenames
        if (safeName.Length > 50)
        {
            safeName = safeName.Substring(0, 50);
        }

        // Add timestamp hash to ensure uniqueness if needed
        var hash = Math.Abs(appName.GetHashCode()).ToString();
        return $"{safeName}_{hash}{extension}";
    }

    private static async Task<List<Tuple<string, string, bool>>> GetUwpApps(bool uninstallableOnly)
    {
        var installedApps = new List<Tuple<string, string, bool>>();
        var command = uninstallableOnly
            ? @"Get-AppxPackage -AllUsers | Where-Object { $_.NonRemovable -eq $false } | Select-Object Name,InstallLocation,PackageFullName | Format-List"
            : @"Get-AppxPackage -AllUsers | Select-Object Name,InstallLocation,PackageFullName | Format-List";

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
                        var logoPath = await ExtractLogoPath(currentLocation, false, currentName);
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
                var logoPath = await ExtractLogoPath(currentLocation, false, currentName);
                installedApps.Add(new Tuple<string, string, bool>(currentName, logoPath, false)); // false for UWP
            }

            process.WaitForExit();
        }
        catch (Exception ex)
        {
            await LogHelper.LogError(ex.Message);
        }

        return installedApps;
    }

    public static async Task<List<Tuple<string, string, bool>>> GetWin32Apps()
    {
        var win32Apps = new List<Tuple<string, string, bool>>();

        var registryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        try
        {
            using var machineKey = Registry.LocalMachine.OpenSubKey(registryPath);
            using var userKey = Registry.CurrentUser.OpenSubKey(registryPath);

            var allSubKeys = (machineKey?.GetSubKeyNames() ?? Enumerable.Empty<string>())
                .Concat(userKey?.GetSubKeyNames() ?? Enumerable.Empty<string>())
                .Distinct();

            foreach (var subKeyName in allSubKeys)
            {
                using var subKey = machineKey?.OpenSubKey(subKeyName) ?? userKey?.OpenSubKey(subKeyName);

                if (subKey == null)
                {
                    await LogHelper.LogError($"Failed to open subkey {subKeyName}");
                    continue;
                }

                var displayName = subKey.GetValue("DisplayName") as string;
                var installLocation = subKey.GetValue("InstallLocation") as string;
                if (!string.IsNullOrEmpty(installLocation))
                {
                    installLocation = installLocation.Replace("\"", ""); // Remove all double quotes
                    if (installLocation.Contains(".exe")) // If it contains a file extension
                        installLocation = Path.GetDirectoryName(installLocation); // Extract directory path
                }

                var uninstallString = subKey.GetValue("UninstallString") as string;
                if (!string.IsNullOrEmpty(uninstallString))
                {
                    uninstallString = uninstallString.Replace("\"", ""); // Remove all double quotes
                }

                var systemComponent = subKey.GetValue("SystemComponent") as int?; // Returns 1 if the app is marked as system components

                // Skip entries without names or marked as system components
                if (string.IsNullOrEmpty(displayName) || systemComponent == 1)
                {
                    continue;
                }

                // Some apps don't have InstallLocation but have an UninstallString
                if (string.IsNullOrEmpty(installLocation) && !string.IsNullOrEmpty(uninstallString))
                {
                    installLocation = Path.GetDirectoryName(uninstallString);
                    if (!string.IsNullOrEmpty(installLocation))
                    {
                        if (installLocation.Contains(".exe")) // If it contains a file extension
                        {
                            installLocation = Path.GetDirectoryName(installLocation); // Extract directory path
                        }
                    }
                }

                // Exclude Win32 Microsoft Edge
                if (displayName.Contains("edge", StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }

                var logoPath = await ExtractLogoPath(installLocation, true, displayName); // true for Win32, pass displayName
                win32Apps.Add(new Tuple<string, string, bool>(displayName, logoPath, true));
            }
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"Failed to load Win32 apps: {ex.Message}");
        }

        return [.. win32Apps
            .DistinctBy(app => app.Item1)  // Remove duplicates based on app name
            .OrderBy(app => app.Item1)];   // Sort the apps alphabetically by name
    }

    private static async Task<string> ExtractLogoPath(string installLocation, bool isWin32 = false, string? appName = null)
    {
        var logoPath = Path.Combine(IconCacheDirectory, "defaulticon.png");

        if (isWin32)
        {
            try
            {
                if (!string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
                {
                    // Use the provided app name or fallback to directory name
                    var nameForIcon = appName ?? Path.GetFileName(installLocation) ?? "Unknown";
                    var cachedIconPath = Path.Combine(IconCacheDirectory, GetSafeIconFileName(nameForIcon));

                    // Check if icon already exists in cache and is valid
                    if (File.Exists(cachedIconPath) && new FileInfo(cachedIconPath).Length > 0)
                    {
                        return cachedIconPath;
                    }

                    // Ensure cache directory exists before trying to save
                    if (!Directory.Exists(IconCacheDirectory))
                    {
                        Directory.CreateDirectory(IconCacheDirectory);
                    }

                    // Look for existing icons in the app directory
                    var iconIcoPath = Path.Combine(installLocation, "app.ico");
                    var iconPngPath = Path.Combine(installLocation, "icon.png");

                    // If existing icons are found, copy them to cache
                    if (File.Exists(iconIcoPath))
                    {
                        try
                        {
                            // Copy the existing .ico file to cache as .png
                            using var icon = new System.Drawing.Icon(iconIcoPath);
                            await SaveIconAsPng(icon, cachedIconPath);
                            logoPath = cachedIconPath;
                        }
                        catch (Exception ex)
                        {
                            await LogHelper.LogError($"Failed to copy existing .ico file: {ex.Message}");
                            // Fall back to original path if copying fails
                            logoPath = iconIcoPath;
                        }
                    }
                    else if (File.Exists(iconPngPath))
                    {
                        try
                        {
                            // Copy the existing icon.png file to cache
                            File.Copy(iconPngPath, cachedIconPath, true);
                            logoPath = cachedIconPath;
                        }
                        catch (Exception ex)
                        {
                            await LogHelper.LogError($"Failed to copy existing .png file: {ex.Message}");
                            // Fall back to original path if copying fails
                            logoPath = iconPngPath;
                        }
                    }
                    else
                    {
                        // Extract icon from executable and save to temp cache directory
                        var exeFile = Directory.GetFiles(installLocation, "*.exe").FirstOrDefault();
                        if (!string.IsNullOrEmpty(exeFile))
                        {
                            try
                            {
                                using var icon = System.Drawing.Icon.ExtractAssociatedIcon(exeFile);
                                if (icon != null)
                                {
                                    await SaveIconAsPng(icon, cachedIconPath);
                                    logoPath = cachedIconPath;
                                }
                            }
                            catch (Exception ex)
                            {
                                await LogHelper.LogError($"Failed to extract icon from executable: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await LogHelper.LogError($"Failed to extract logo for Win32 app: {ex.Message}");
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
                await LogHelper.LogError($"Failed to extract logo path: {ex.Message}");
            }
        }
        return logoPath;
    }

    // Save the extracted icon as a PNG file
    private static async Task SaveIconAsPng(System.Drawing.Icon icon, string filePath)
    {
        try
        {
            // Ensure the directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var stream = new MemoryStream();
            // Convert the icon to a bitmap and then save it as PNG
            using (var bitmap = new Bitmap(icon.ToBitmap()))
            {
                bitmap.Save(stream, ImageFormat.Png);
            }

            // Write the stream to the file
            File.WriteAllBytes(filePath, stream.ToArray());
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"Failed to save icon as PNG to {filePath}: {ex.Message}");
        }
    }

    private static int GetScaleFromFileName(string fileName)
    {
        var match = Regex.Match(fileName, @"Scale-(\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : 100;
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
                    FileName = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
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
            await LogHelper.LogError($"Error running command: {ex.Message}");
            throw;
        }
    }
    public static async Task RevertAllChanges()
    {
        try
        {
            // Get all toggle switches that have been applied (saved state == 1)
            using var ryTuneXKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                    ? RegistryView.Registry64
                    : RegistryView.Default).OpenSubKey(RegistryBaseKey);

            if (ryTuneXKey != null)
            {
                var valueNames = ryTuneXKey.GetValueNames();

                foreach (var valueName in valueNames)
                {
                    var savedState = ryTuneXKey.GetValue(valueName);
                    if (savedState is int state && state == 1)
                    {
                        // Create a fake toggle switch to revert the optimization
                        var fakeToggleSwitch = new ToggleSwitch
                        {
                            Tag = valueName,
                            IsOn = false // Set to false to trigger the reverse action
                        };

                        // Call the method to revert
                        await XamlSwitchesAsync(fakeToggleSwitch);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"RevertAllChanges: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
    }
    public static async Task XamlSwitchesAsync(ToggleSwitch toggleSwitch)
    {
        if (toggleSwitch != null && toggleSwitch.Tag != null)
        {
            try
            {
                // Save the state to RyTuneX registry first (64-bit registry with 32-bit app)
                using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                    Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                        ? RegistryView.Registry64
                        : RegistryView.Default).CreateSubKey(RegistryBaseKey);

                key?.SetValue((string)toggleSwitch.Tag, toggleSwitch.IsOn ? 1 : 0, RegistryValueKind.DWord);

                Debug.WriteLine($"ToggleSwitch Tag: {toggleSwitch.Tag}, IsOn: {toggleSwitch.IsOn}");
            }
            catch (Exception ex)
            {
                await LogHelper.LogError($"Error saving registry state: {ex.Message}");
            }
            switch (toggleSwitch.Tag)
            {
                case "RecommendedSectionStartMenu":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableRecommendedSectionStartMenu();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableRecommendedSectionStartMenu();
                    }
                    break;

                case "LegacyBootMenu":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.EnableLegacyBootMenu();
                    }
                    else
                    {
                        OptimizeSystemHelper.DisableLegacyBootMenu();
                    }
                    break;

                case "OptimizeNTFS":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.EnableOptimizeNTFS();
                    }
                    else
                    {
                        OptimizeSystemHelper.DisableOptimizeNTFS();
                    }
                    break;

                case "PrioritizeForegroundApplications":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.EnablePrioritizeForegroundApplications();
                    }
                    else
                    {
                        OptimizeSystemHelper.DisablePrioritizeForegroundApplications();
                    }
                    break;

                case "WPBT":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableWPBT();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableWPBT();
                    }
                    break;

                case "ServiceHostSplitting":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableServiceHostSplitting();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableServiceHostSplitting();
                    }
                    break;

                case "MenuShowDelay":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableMenuShowDelay();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableMenuShowDelay();
                    }
                    break;

                case "MouseHoverTime":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableMouseHoverTime();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableMouseHoverTime();
                    }
                    break;

                case "BackgroundApps":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableBackgroundApps();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableBackgroundApps();
                    }
                    break;

                case "AutoComplete":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableAutoComplete();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableAutoComplete();
                    }
                    break;

                case "CrashDump":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.EnableCrashDump();
                    }
                    else
                    {
                        OptimizeSystemHelper.DisableCrashDump();
                    }
                    break;

                case "RemoteAssistance":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableRemoteAssistance();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableRemoteAssistance();
                    }
                    break;

                case "WindowShake":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableWindowShake();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableWindowShake();
                    }
                    break;

                case "CopyMoveContextMenu":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.AddCopyMoveContextMenu();
                    }
                    else
                    {
                        OptimizeSystemHelper.RemoveCopyMoveContextMenu();
                    }
                    break;

                case "TaskTimeouts":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.AdjustTaskTimeouts();
                    }
                    else
                    {
                        OptimizeSystemHelper.IncreaseTaskTimeouts();
                    }
                    break;

                case "LowDiskSpaceChecks":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.EnableLowDiskSpaceChecks();
                    }
                    else
                    {
                        OptimizeSystemHelper.DisableLowDiskSpaceChecks();
                    }
                    break;

                case "LinkResolve":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableLinkResolve();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableLinkResolve();
                    }
                    break;

                case "ServiceTimeouts":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DecreaseServiceTimeouts();
                    }
                    else
                    {
                        OptimizeSystemHelper.RevertServiceTimeouts();
                    }
                    break;

                case "RemoteRegistry":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableRemoteRegistry();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableRemoteRegistry();
                    }
                    break;

                case "FileExtensionsAndHiddenFiles":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.HideFileExtensionsAndHiddenFiles();
                    }
                    else
                    {
                        OptimizeSystemHelper.ShowFileExtensionsAndHiddenFiles();
                    }
                    break;

                case "SystemProfile":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.OptimizeSystemProfile();
                    }
                    else
                    {
                        OptimizeSystemHelper.RevertSystemProfile();
                    }
                    break;

                case "TelemetryServices":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableTelemetryServices();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableTelemetryServices();
                    }
                    break;

                case "HomeGroup":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableHomeGroup();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableHomeGroup();
                    }
                    break;

                case "PrintService":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisablePrintService();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnablePrintService();
                    }
                    break;

                case "SysMain":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSysMain();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSysMain();
                    }
                    break;

                case "CompatibilityAssistant":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableCompatibilityAssistant();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableCompatibilityAssistant();
                    }
                    break;

                case "SystemRestore":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSystemRestore();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSystemRestore();
                    }
                    break;

                case "WindowsTransparency":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableWindowsTransparency();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableWindowsTransparency();
                    }
                    break;

                case "WindowsDarkMode":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.EnableWindowsDarkMode();
                    }
                    else
                    {
                        OptimizeSystemHelper.DisableWindowsDarkMode();
                    }
                    break;

                case "VerboseLogon":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.EnableVerboseLogon();
                    }
                    else
                    {
                        OptimizeSystemHelper.DisableVerboseLogon();
                    }
                    break;

                case "ClassicContextMenu":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.EnableClassicContextMenu();
                    }
                    else
                    {
                        OptimizeSystemHelper.DisableClassicContextMenu();
                    }
                    break;

                case "Search":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSearch();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSearch();
                    }
                    break;

                case "Biometrics":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableBiometrics();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableBiometrics();
                    }
                    break;

                case "SMBv1":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSMB("1");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSMB("1");
                    }
                    break;

                case "SMBv2":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSMB("2");
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSMB("2");
                    }
                    break;

                case "ErrorReporting":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableErrorReporting();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableErrorReporting();
                    }
                    break;

                case "Cortana":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableCortana();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableCortana();
                    }
                    break;

                case "GamingMode":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.EnableGamingMode();
                    }
                    else
                    {
                        OptimizeSystemHelper.DisableGamingMode();
                    }
                    break;

                case "AutomaticUpdates":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableAutomaticUpdates();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableAutomaticUpdates();
                    }
                    break;

                case "StoreUpdates":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableStoreUpdates();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableStoreUpdates();
                    }
                    break;

                case "OneDrive":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableOneDrive();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableOneDrive();
                    }
                    break;

                case "SensorServices":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSensorServices();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSensorServices();
                    }
                    break;

                case "NewsAndInterests":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableNewsAndInterests();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableNewsAndInterests();
                    }
                    break;

                case "SpotlightFeatures":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSpotlightFeatures();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSpotlightFeatures();
                    }
                    break;

                case "TailoredExperiences":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableTailoredExperiences();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableTailoredExperiences();
                    }
                    break;

                case "CloudOptimizedContent":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableCloudOptimizedContent();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableCloudOptimizedContent();
                    }
                    break;

                case "FeedbackNotifications":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableFeedbackNotifications();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableFeedbackNotifications();
                    }
                    break;

                case "AdvertisingID":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableAdvertisingID();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableAdvertisingID();
                    }
                    break;

                case "BluetoothAdvertising":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableBluetoothAdvertising();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableBluetoothAdvertising();
                    }
                    break;

                case "AutomaticRestartSignOn":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableAutomaticRestartSignOn();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableAutomaticRestartSignOn();
                    }
                    break;

                case "HandwritingDataSharing":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableHandwritingDataSharing();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableHandwritingDataSharing();
                    }
                    break;

                case "TextInputDataCollection":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableTextInputDataCollection();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableTextInputDataCollection();
                    }
                    break;

                case "InputPersonalization":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableInputPersonalization();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableInputPersonalization();
                    }
                    break;

                case "SafeSearchMode":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSafeSearchMode();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSafeSearchMode();
                    }
                    break;

                case "ActivityUploads":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableActivityUploads();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableActivityUploads();
                    }
                    break;

                case "ClipboardSync":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableClipboardSync();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableClipboardSync();
                    }
                    break;

                case "MessageSync":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableMessageSync();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableMessageSync();
                    }
                    break;

                case "SettingSync":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSettingSync();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSettingSync();
                    }
                    break;

                case "VoiceActivation":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableVoiceActivation();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableVoiceActivation();
                    }
                    break;

                case "FindMyDevice":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableFindMyDevice();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableFindMyDevice();
                    }
                    break;

                case "ActivityFeed":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableActivityFeed();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableActivityFeed();
                    }
                    break;

                case "Cdp":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableCdp();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableCdp();
                    }
                    break;

                case "DiagnosticsToast":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableDiagnosticsToast();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableDiagnosticsToast();
                    }
                    break;

                case "OnlineSpeechPrivacy":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableOnlineSpeechPrivacy();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableOnlineSpeechPrivacy();
                    }
                    break;

                case "LocationAccess":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableLocationFeatures();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableLocationFeatures();
                    }
                    break;

                case "LocationFeatures":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableLocationFeatures();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableLocationFeatures();
                    }
                    break;

                case "GameBar":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableGameBar();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableGameBar();
                    }
                    break;

                case "QuickAccessHistory":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableQuickAccessHistory();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableQuickAccessHistory();
                    }
                    break;

                case "MyPeople":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableMyPeople();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableMyPeople();
                    }
                    break;

                case "Drivers":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.ExcludeDrivers();
                    }
                    else
                    {
                        OptimizeSystemHelper.IncludeDrivers();
                    }
                    break;

                case "WindowsInk":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableWindowsInk();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableWindowsInk();
                    }
                    break;

                case "SpellingAndTypingFeatures":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSpellingAndTypingFeatures();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSpellingAndTypingFeatures();
                    }
                    break;

                case "FaxService":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableFaxService();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableFaxService();
                    }
                    break;

                case "InsiderService":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableInsiderService();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableInsiderService();
                    }
                    break;

                case "SmartScreen":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSmartScreen();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSmartScreen();
                    }
                    break;

                case "CloudClipboard":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableCloudClipboard();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableCloudClipboard();
                    }
                    break;

                case "StickyKeys":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableStickyKeys();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableStickyKeys();
                    }
                    break;

                case "CastToDevice":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.RemoveCastToDevice();
                    }
                    else
                    {
                        OptimizeSystemHelper.AddCastToDevice();
                    }
                    break;

                case "VBS":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableVBS();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableVBS();
                    }
                    break;

                case "TaskbarToLeft":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.AlignTaskbarToLeft();
                    }
                    else
                    {
                        OptimizeSystemHelper.AlignTaskbarToCenter();
                    }
                    break;

                case "SnapAssist":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSnapAssist();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSnapAssist();
                    }
                    break;

                case "Widgets":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableWidgets();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableWidgets();
                    }
                    break;

                case "Chat":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableChat();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableChat();
                    }
                    break;

                case "FilesCompactMode":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.EnableFilesCompactMode();
                    }
                    else
                    {
                        OptimizeSystemHelper.DisableFilesCompactMode();
                    }
                    break;

                case "Stickers":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableStickers();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableStickers();
                    }
                    break;

                case "EdgeDiscoverBar":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableEdgeDiscoverBar();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableEdgeDiscoverBar();
                    }
                    break;

                case "EdgeTelemetry":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableEdgeTelemetry();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableEdgeTelemetry();
                    }
                    break;

                case "CoPilotAI":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableCoPilotAI();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableCoPilotAI();
                    }
                    break;

                case "WindowsRecall":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableWindowsRecall();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableWindowsRecall();
                    }
                    break;

                case "VisualStudioTelemetry":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableVisualStudioTelemetry();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableVisualStudioTelemetry();
                    }
                    break;

                case "NvidiaTelemetry":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableNvidiaTelemetry();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableNvidiaTelemetry();
                    }
                    break;

                case "ChromeTelemetry":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableChromeTelemetry();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableChromeTelemetry();
                    }
                    break;

                case "FirefoxTelemetry":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableFirefoxTelemetry();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableFirefoxTelemetry();
                    }
                    break;

                case "Hibernation":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableHibernation();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableHibernation();
                    }
                    break;

                case "EndTask":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.EnableEndTask();
                    }
                    else
                    {
                        OptimizeSystemHelper.DisableEndTask();
                    }
                    break;
            }
        }
    }
}