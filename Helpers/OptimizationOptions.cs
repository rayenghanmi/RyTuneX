using System.Diagnostics;
using System.Drawing.Imaging;
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
    private static extern int ExtractIconEx(string file, int index, IntPtr[] large, IntPtr[]? small, int icons);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static async Task<List<Tuple<string, string, bool>>> GetInstalledApps(bool uninstallableOnly)
    {
        // Ensure the icon cache directory exists
        if (!Directory.Exists(IconCacheDirectory))
        {
            Directory.CreateDirectory(IconCacheDirectory);
        }

        var largeIcons = new IntPtr[1];
        ExtractIconEx(@"C:\Windows\System32\imageres.dll", 152, largeIcons, null, 1);
        var hIcon = largeIcons[0];
        if (hIcon != IntPtr.Zero)
        {
            // Clone the icon from handle so the original HICON can be safely destroyed
            using var clonedIcon = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(hIcon).Clone();
            DestroyIcon(hIcon);

            using var bmp = clonedIcon.ToBitmap();
            bmp.Save(Path.Combine(IconCacheDirectory, "defaulticon.png"), ImageFormat.Png);
        }

        var uwpAppsTask = Task.Run(() => GetUwpApps(uninstallableOnly));
        var win32AppsTask = Task.Run(GetWin32Apps);

        await Task.WhenAll(uwpAppsTask, win32AppsTask);

        var installedApps = uwpAppsTask.Result.Concat(win32AppsTask.Result).ToList();

        installedApps = [.. installedApps
            .DistinctBy(app => app.Item1)  // Remove duplicates based on app name
            .OrderBy(app => app.Item1)];   // Sort the apps alphabetically by name

        _ = LogHelper.Log("Returning Installed Apps [GetInstalledApps]");
        return installedApps;
    }

    private static string GetSafeIconFileName(string identity)
    {
        var safe = new string(identity.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray());
        if (safe.Length > 60) safe = safe[..60];
        return $"{safe}_{Math.Abs(identity.GetHashCode())}.png";
    }

    private static async Task<List<Tuple<string, string, bool>>> GetUwpApps(bool uninstallableOnly)
    {
        var installedApps = new List<Tuple<string, string, bool>>();

        // Use string interpolation with raw string literals for commands
        var command = uninstallableOnly
            ? """Get-AppxPackage -AllUsers | Where-Object { $_.NonRemovable -eq $false } | Select-Object Name,InstallLocation,PackageFullName | Format-List"""
            : """Get-AppxPackage -AllUsers | Select-Object Name,InstallLocation,PackageFullName | Format-List""";

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

            var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            string? currentName = null;
            string? currentLocation = null;

            // Use collection expression for split separators
            ReadOnlySpan<char> newLine = Environment.NewLine;
            foreach (var line in output.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.StartsWith("Name", StringComparison.Ordinal))
                {
                    if (!string.IsNullOrEmpty(currentName) && !string.IsNullOrEmpty(currentLocation))
                    {
                        var logoPath = await ExtractLogoPath(currentLocation, false, currentName).ConfigureAwait(false);
                        installedApps.Add(new Tuple<string, string, bool>(currentName, logoPath, false));
                    }

                    currentName = line.Split([':'], 2)[1].Trim();
                    currentLocation = null;
                }
                else if (line.StartsWith("InstallLocation", StringComparison.Ordinal))
                {
                    currentLocation = line.Split([':'], 2)[1].Trim();
                }
                else if (!string.IsNullOrWhiteSpace(currentLocation) && line.StartsWith(" ", StringComparison.Ordinal))
                {
                    currentLocation += " " + line.Trim();
                }
            }

            if (!string.IsNullOrEmpty(currentName) && !string.IsNullOrEmpty(currentLocation))
            {
                var logoPath = await ExtractLogoPath(currentLocation, false, currentName).ConfigureAwait(false);
                installedApps.Add(new Tuple<string, string, bool>(currentName, logoPath, false));
            }

            await process.WaitForExitAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError(ex.Message).ConfigureAwait(false);
        }

        return installedApps;
    }

    private static IEnumerable<RegistryKey> OpenUninstallRoots()
    {
        const string path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

        var roots = new[]
        {
        (RegistryHive.LocalMachine, RegistryView.Registry64),
        (RegistryHive.LocalMachine, RegistryView.Registry32),
        (RegistryHive.CurrentUser,  RegistryView.Registry64),
        (RegistryHive.CurrentUser,  RegistryView.Registry32),
    };

        foreach (var (hive, view) in roots)
        {
            RegistryKey? baseKey = null;
            try
            {
                baseKey = RegistryKey.OpenBaseKey(hive, view).OpenSubKey(path);
            }
            catch { }

            if (baseKey != null)
                yield return baseKey;
        }
    }

    public static async Task<List<Tuple<string, string, bool>>> GetWin32Apps()
    {
        var win32Apps = new List<Tuple<string, string, bool>>();

        try
        {
            var uninstallRoots = OpenUninstallRoots().ToList();

            var allSubKeys = uninstallRoots
                .SelectMany(k => k.GetSubKeyNames()
                    .Select(name => (Root: k, Name: name)))
                .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First());

            foreach (var uninstallRoot in allSubKeys)
            {
                using var subKey = uninstallRoot.Root.OpenSubKey(uninstallRoot.Name);

                if (subKey == null)
                {
                    _ = LogHelper.LogError($"Failed to open subkey {uninstallRoot.Name}");
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
            _ = LogHelper.LogError($"Failed to load Win32 apps: {ex.Message}");
        }

        return [.. win32Apps
            .DistinctBy(app => app.Item1)  // Remove duplicates based on app name
            .OrderBy(app => app.Item1)];   // Sort the apps alphabetically by name
    }

    private static async Task<string> ExtractLogoPath(string? installLocation, bool isWin32, string appName)
    {
        Directory.CreateDirectory(IconCacheDirectory);
        var defaultIcon = isWin32
            ? Path.Combine(IconCacheDirectory, "defaulticon.png")
            : string.Empty;

        if (string.IsNullOrEmpty(installLocation)) return defaultIcon;

        var identity = $"{appName}|{installLocation}";
        var cached = Path.Combine(IconCacheDirectory, GetSafeIconFileName(identity));

        // Prevent poisoned cache
        if (File.Exists(cached))
        {
            if (new FileInfo(cached).Length > 2048)
                return cached;
            File.Delete(cached);
        }

        // -------- WIN32 --------
        if (isWin32 && Directory.Exists(installLocation))
        {
            // Try DisplayIcon from registry
            try
            {
                using var key = OpenUninstallRoots()
                    .SelectMany(r => r.GetSubKeyNames().Select(n => r.OpenSubKey(n)))
                    .FirstOrDefault(k => (k?.GetValue("DisplayName") as string)?.Equals(appName, StringComparison.OrdinalIgnoreCase) == true);

                var displayIcon = key?.GetValue("DisplayIcon") as string;
                if (!string.IsNullOrEmpty(displayIcon))
                {
                    displayIcon = displayIcon.Split(',')[0].Replace("\"", "");
                    if (File.Exists(displayIcon))
                    {
                        var large = new IntPtr[1];
                        ExtractIconEx(displayIcon, 0, large, null, 1);
                        if (large[0] != IntPtr.Zero)
                        {
                            using var icon = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(large[0]).Clone();
                            DestroyIcon(large[0]);
                            await SaveIcon(icon, cached);
                            return cached;
                        }
                    }
                }
            }
            catch { }

            // Fallback: scan executables (largest EXE wins)
            var exe = Directory
                .EnumerateFiles(installLocation, "*.exe", SearchOption.AllDirectories)
                .OrderByDescending(f => new FileInfo(f).Length)
                .FirstOrDefault();

            if (exe != null)
            {
                var large = new IntPtr[1];
                ExtractIconEx(exe, 0, large, null, 1);

                if (large[0] != IntPtr.Zero)
                {
                    using var icon = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(large[0]).Clone();
                    DestroyIcon(large[0]);
                    await SaveIcon(icon, cached);
                    return cached;
                }
            }
        }

        // -------- UWP --------
        else if (!isWin32 && Directory.Exists(installLocation))
        {
            try
            {
                var manifest = Directory.GetFiles(installLocation, "AppxManifest.xml", SearchOption.TopDirectoryOnly)
                    .Concat(Directory.GetFiles(installLocation, "appxmanifest.xml", SearchOption.TopDirectoryOnly))
                    .FirstOrDefault();

                if (manifest == null)
                    return defaultIcon;

                var doc = XDocument.Load(manifest);

                // Namespaces used by UWP manifests
                XNamespace foundation = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
                XNamespace uap = "http://schemas.microsoft.com/appx/manifest/uap/windows10";

                // Prefer VisualElements icons
                var visual = doc.Descendants(uap + "VisualElements").FirstOrDefault();

                var logoPath =
                    visual?.Attribute("Square44x44Logo")?.Value ??
                    visual?.Attribute("Square150x150Logo")?.Value;

                // Fallback to old <Logo> element
                if (string.IsNullOrEmpty(logoPath))
                {
                    logoPath = doc.Descendants(foundation + "Logo").FirstOrDefault()?.Value;
                }

                if (string.IsNullOrEmpty(logoPath))
                    return defaultIcon;

                logoPath = logoPath.Replace('/', '\\');
                var logoDir = Path.Combine(installLocation, Path.GetDirectoryName(logoPath) ?? "");
                var baseName = Path.GetFileNameWithoutExtension(logoPath);

                if (!Directory.Exists(logoDir))
                    return defaultIcon;

                // Collect all possible logo candidates
                var candidates = Directory.GetFiles(logoDir, baseName + "*.png");

                if (candidates.Length == 0)
                    return defaultIcon;

                // Prefer targetsize-48/64 then Scale-200
                var selected = candidates
                .OrderByDescending(f => f.Contains("targetsize-48"))
                .ThenByDescending(f => f.Contains("targetsize-64"))
                .ThenBy(f => Math.Abs(GetScale(f) - 200))
                .FirstOrDefault();

                if (selected != null && File.Exists(selected))
                    return selected;
            }
            catch
            {
                return defaultIcon;
            }
        }
        return defaultIcon;
    }

    // Save the extracted icon as a PNG file
    private static async Task SaveIcon(System.Drawing.Icon icon, string path)
    {
        using var bmp = icon.ToBitmap();
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        await File.WriteAllBytesAsync(path, ms.ToArray());
    }

    private static int GetScale(string file)
    {
        var m = Regex.Match(file, @"Scale-(\\d+)");
        return m.Success ? int.Parse(m.Groups[1].Value) : 100;
    }

    internal static async Task<int> StartInCmd(string command)
    {
        try
        {
            var cmdPath = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SysNative", "cmd.exe")
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "cmd.exe");

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = cmdPath,
                    Arguments = $"/C {command}",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardError = true
                }
            };

            process.Start();

            var stdErrTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync().ConfigureAwait(false);

            var errorOutput = await stdErrTask.ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                _ = LogHelper.LogError($"Command failed (exit {process.ExitCode})\n{errorOutput}");
            }

            return process.ExitCode;
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error running command: {ex}");
            throw;
        }
    }

    internal static async Task<string> RunPowerShell(string command)
    {
        var psPath = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SysNative", "WindowsPowerShell", "v1.0", "powershell.exe")
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "WindowsPowerShell", "v1.0", "powershell.exe");

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = psPath,
                Arguments = $"-NoProfile -NonInteractive -Command \"{command}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        await process.WaitForExitAsync().ConfigureAwait(false);
        return output.Trim();
    }

    public static async Task RevertAllChanges()
    {
        try
        {
            // Get all toggle switches that have been applied (saved state == 1)
            using var rytunexKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                    ? RegistryView.Registry64
                    : RegistryView.Default).OpenSubKey(RegistryBaseKey);

            if (rytunexKey != null)
            {
                var valueNames = rytunexKey.GetValueNames();

                foreach (var valueName in valueNames)
                {
                    // Handle Windows Updates mode separately
                    if (valueName == "WindowsUpdatesMode")
                    {
                        var savedMode = rytunexKey.GetValue(valueName) as string;
                        if (!string.IsNullOrEmpty(savedMode) && !savedMode.Equals("Default", StringComparison.OrdinalIgnoreCase))
                        {
                            // Revert Windows Updates to default
                            await OptimizeSystemHelper.SetWindowsUpdatesDefault().ConfigureAwait(false);
                        }
                        continue;
                    }

                    var savedState = rytunexKey.GetValue(valueName);
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
            _ = LogHelper.LogError($"RevertAllChanges: {ex.Message}\n Stack Trace: {ex.StackTrace}");
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
                _ = LogHelper.LogError($"Error saving registry state: {ex.Message}");
            }
            switch (toggleSwitch.Tag)
            {
                case "RecommendedSectionStartMenu":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableRecommendedSectionStartMenu();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableRecommendedSectionStartMenu();
                    }
                    break;

                case "LegacyBootMenu":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.EnableLegacyBootMenu();
                    }
                    else
                    {
                        await OptimizeSystemHelper.DisableLegacyBootMenu();
                    }
                    break;

                case "OptimizeNTFS":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.EnableOptimizeNTFS();
                    }
                    else
                    {
                        await OptimizeSystemHelper.DisableOptimizeNTFS();
                    }
                    break;

                case "PrioritizeForegroundApplications":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.EnablePrioritizeForegroundApplications();
                    }
                    else
                    {
                        await OptimizeSystemHelper.DisablePrioritizeForegroundApplications();
                    }
                    break;

                case "WPBT":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableWPBT();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableWPBT();
                    }
                    break;

                case "ServiceHostSplitting":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableServiceHostSplitting();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableServiceHostSplitting();
                    }
                    break;

                case "MenuShowDelay":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableMenuShowDelay();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableMenuShowDelay();
                    }
                    break;

                case "MouseHoverTime":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableMouseHoverTime();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableMouseHoverTime();
                    }
                    break;

                case "BackgroundApps":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableBackgroundApps();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableBackgroundApps();
                    }
                    break;

                case "AutoComplete":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableAutoComplete();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableAutoComplete();
                    }
                    break;

                case "CrashDump":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.EnableCrashDump();
                    }
                    else
                    {
                        await OptimizeSystemHelper.DisableCrashDump();
                    }
                    break;

                case "RemoteAssistance":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableRemoteAssistance();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableRemoteAssistance();
                    }
                    break;

                case "WindowShake":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableWindowShake();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableWindowShake();
                    }
                    break;

                case "CopyMoveContextMenu":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.AddCopyMoveContextMenu();
                    }
                    else
                    {
                        await OptimizeSystemHelper.RemoveCopyMoveContextMenu();
                    }
                    break;

                case "TaskTimeouts":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.AdjustTaskTimeouts();
                    }
                    else
                    {
                        await OptimizeSystemHelper.IncreaseTaskTimeouts();
                    }
                    break;

                case "LowDiskSpaceChecks":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.EnableLowDiskSpaceChecks();
                    }
                    else
                    {
                        await OptimizeSystemHelper.DisableLowDiskSpaceChecks();
                    }
                    break;

                case "LinkResolve":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableLinkResolve();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableLinkResolve();
                    }
                    break;

                case "ServiceTimeouts":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DecreaseServiceTimeouts();
                    }
                    else
                    {
                        await OptimizeSystemHelper.RevertServiceTimeouts();
                    }
                    break;

                case "RemoteRegistry":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableRemoteRegistry();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableRemoteRegistry();
                    }
                    break;

                case "FileExtensionsAndHiddenFiles":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.HideFileExtensionsAndHiddenFiles();
                    }
                    else
                    {
                        await OptimizeSystemHelper.ShowFileExtensionsAndHiddenFiles();
                    }
                    break;

                case "SystemProfile":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.OptimizeSystemProfile();
                    }
                    else
                    {
                        await OptimizeSystemHelper.RevertSystemProfile();
                    }
                    break;

                case "TelemetryServices":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableTelemetryServices();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableTelemetryServices();
                    }
                    break;

                case "HomeGroup":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableHomeGroup();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableHomeGroup();
                    }
                    break;

                case "PrintService":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisablePrintService();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnablePrintService();
                    }
                    break;

                case "SysMain":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableSysMain();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableSysMain();
                    }
                    break;

                case "CompatibilityAssistant":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableCompatibilityAssistant();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableCompatibilityAssistant();
                    }
                    break;

                case "SystemRestore":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableSystemRestore();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableSystemRestore();
                    }
                    break;

                case "WindowsTransparency":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableWindowsTransparency();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableWindowsTransparency();
                    }
                    break;

                case "WindowsDarkMode":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.EnableWindowsDarkMode();
                    }
                    else
                    {
                        await OptimizeSystemHelper.DisableWindowsDarkMode();
                    }
                    break;

                case "VerboseLogon":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.EnableVerboseLogon();
                    }
                    else
                    {
                        await OptimizeSystemHelper.DisableVerboseLogon();
                    }
                    break;

                case "ClassicContextMenu":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.EnableClassicContextMenu();
                    }
                    else
                    {
                        await OptimizeSystemHelper.DisableClassicContextMenu();
                    }
                    break;

                case "Search":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableSearch();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableSearch();
                    }
                    break;

                case "Biometrics":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableBiometrics();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableBiometrics();
                    }
                    break;

                case "SMBv1":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableSMBAsync("1");
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableSMBAsync("1");
                    }
                    break;

                case "SMBv2":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableSMBAsync("2");
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableSMBAsync("2");
                    }
                    break;

                case "ErrorReporting":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableErrorReporting();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableErrorReporting();
                    }
                    break;

                case "Cortana":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableCortana();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableCortana();
                    }
                    break;

                case "GamingMode":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.EnableGamingMode();
                    }
                    else
                    {
                        await OptimizeSystemHelper.DisableGamingMode();
                    }
                    break;

                case "StoreUpdates":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableStoreUpdates();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableStoreUpdates();
                    }
                    break;

                case "OneDrive":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableOneDrive();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableOneDrive();
                    }
                    break;

                case "SensorServices":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableSensorServices();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableSensorServices();
                    }
                    break;

                case "NewsAndInterests":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableNewsAndInterests();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableNewsAndInterests();
                    }
                    break;

                case "SpotlightFeatures":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableSpotlightFeatures();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableSpotlightFeatures();
                    }
                    break;

                case "TailoredExperiences":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableTailoredExperiences();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableTailoredExperiences();
                    }
                    break;

                case "CloudOptimizedContent":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableCloudOptimizedContent();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableCloudOptimizedContent();
                    }
                    break;

                case "FeedbackNotifications":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableFeedbackNotifications();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableFeedbackNotifications();
                    }
                    break;

                case "AdvertisingID":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableAdvertisingID();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableAdvertisingID();
                    }
                    break;

                case "BluetoothAdvertising":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableBluetoothAdvertising();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableBluetoothAdvertising();
                    }
                    break;

                case "AutomaticRestartSignOn":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableAutomaticRestartSignOn();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableAutomaticRestartSignOn();
                    }
                    break;

                case "HandwritingDataSharing":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableHandwritingDataSharing();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableHandwritingDataSharing();
                    }
                    break;

                case "TextInputDataCollection":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableTextInputDataCollection();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableTextInputDataCollection();
                    }
                    break;

                case "InputPersonalization":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableInputPersonalization();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableInputPersonalization();
                    }
                    break;

                case "SafeSearchMode":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableSafeSearchMode();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableSafeSearchMode();
                    }
                    break;

                case "ActivityUploads":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableActivityUploads();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableActivityUploads();
                    }
                    break;

                case "ClipboardSync":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableClipboardSync();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableClipboardSync();
                    }
                    break;

                case "MessageSync":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableMessageSync();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableMessageSync();
                    }
                    break;

                case "SettingSync":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableSettingSync();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableSettingSync();
                    }
                    break;

                case "VoiceActivation":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableVoiceActivation();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableVoiceActivation();
                    }
                    break;

                case "FindMyDevice":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableFindMyDevice();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableFindMyDevice();
                    }
                    break;

                case "ActivityFeed":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableActivityFeed();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableActivityFeed();
                    }
                    break;

                case "Cdp":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableCdp();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableCdp();
                    }
                    break;

                case "DiagnosticsToast":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableDiagnosticsToast();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableDiagnosticsToast();
                    }
                    break;

                case "OnlineSpeechPrivacy":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableOnlineSpeechPrivacy();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableOnlineSpeechPrivacy();
                    }
                    break;

                case "LocationAccess":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableLocationFeatures();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableLocationFeatures();
                    }
                    break;

                case "LocationFeatures":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableLocationFeatures();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableLocationFeatures();
                    }
                    break;

                case "GameBar":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableGameBar();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableGameBar();
                    }
                    break;

                case "QuickAccessHistory":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableQuickAccessHistory();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableQuickAccessHistory();
                    }
                    break;

                case "MyPeople":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableMyPeople();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableMyPeople();
                    }
                    break;

                case "Drivers":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.ExcludeDrivers();
                    }
                    else
                    {
                        await OptimizeSystemHelper.IncludeDrivers();
                    }
                    break;

                case "WindowsInk":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableWindowsInk();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableWindowsInk();
                    }
                    break;

                case "SpellingAndTypingFeatures":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableSpellingAndTypingFeatures();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableSpellingAndTypingFeatures();
                    }
                    break;

                case "FaxService":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableFaxService();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableFaxService();
                    }
                    break;

                case "InsiderService":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableInsiderService();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableInsiderService();
                    }
                    break;

                case "SmartScreen":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableSmartScreen();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableSmartScreen();
                    }
                    break;

                case "CloudClipboard":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableCloudClipboard();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableCloudClipboard();
                    }
                    break;

                case "StickyKeys":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableStickyKeys();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableStickyKeys();
                    }
                    break;

                case "CastToDevice":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.RemoveCastToDevice();
                    }
                    else
                    {
                        await OptimizeSystemHelper.AddCastToDevice();
                    }
                    break;

                case "VBS":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableVBS();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableVBS();
                    }
                    break;

                case "TaskbarToLeft":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.AlignTaskbarToLeft();
                    }
                    else
                    {
                        await OptimizeSystemHelper.AlignTaskbarToCenter();
                    }
                    break;

                case "SnapAssist":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableSnapAssist();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableSnapAssist();
                    }
                    break;

                case "Widgets":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableWidgets();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableWidgets();
                    }
                    break;

                case "Chat":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableChat();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableChat();
                    }
                    break;

                case "FilesCompactMode":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.EnableFilesCompactMode();
                    }
                    else
                    {
                        await OptimizeSystemHelper.DisableFilesCompactMode();
                    }
                    break;

                case "Stickers":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableStickers();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableStickers();
                    }
                    break;

                case "EdgeDiscoverBar":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableEdgeDiscoverBar();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableEdgeDiscoverBar();
                    }
                    break;

                case "EdgeTelemetry":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableEdgeTelemetry();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableEdgeTelemetry();
                    }
                    break;

                case "CoPilotAI":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableCoPilotAI();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableCoPilotAI();
                    }
                    break;

                case "WindowsRecall":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableWindowsRecall();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableWindowsRecall();
                    }
                    break;

                case "VisualStudioTelemetry":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableVisualStudioTelemetry();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableVisualStudioTelemetry();
                    }
                    break;

                case "NvidiaTelemetry":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableNvidiaTelemetry();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableNvidiaTelemetry();
                    }
                    break;

                case "ChromeTelemetry":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableChromeTelemetry();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableChromeTelemetry();
                    }
                    break;

                case "FirefoxTelemetry":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableFirefoxTelemetry();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableFirefoxTelemetry();
                    }
                    break;

                case "Hibernation":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.DisableHibernation();
                    }
                    else
                    {
                        await OptimizeSystemHelper.EnableHibernation();
                    }
                    break;

                case "EndTask":
                    if (toggleSwitch.IsOn)
                    {
                        await OptimizeSystemHelper.EnableEndTask();
                    }
                    else
                    {
                        await OptimizeSystemHelper.DisableEndTask();
                    }
                    break;
            }
        }
    }
}