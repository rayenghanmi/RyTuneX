using System.Collections.Concurrent;
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
    // Queueing to serialize toggle operations
    private static readonly SemaphoreSlim _toggleQueueLock = new(1, 1);
    private static readonly ConcurrentQueue<Func<CancellationToken, Task>> _toggleQueue = new();
    private static int _toggleRunning = 0; // 0 = none, > 0 running
    private static readonly CancellationTokenSource _toggleCts = new();

    public static bool HasPendingToggleOperations => _toggleQueue.Count > 0 || _toggleRunning > 0;

    private const string RegistryBaseKey = @"SOFTWARE\RyTuneX\Optimizations";
    private static readonly string IconCacheDirectory = Path.Combine(Path.GetTempPath(), "RyTuneX_AppIcons");
    private static readonly HashSet<char> InvalidFileNameChars = new(Path.GetInvalidFileNameChars());

    [DllImport("Shell32.dll", CharSet = CharSet.Auto)]
    private static extern int ExtractIconEx(string file, int index, IntPtr[] large, IntPtr[]? small, int icons);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static async Task<(List<Tuple<string, string, bool>> Apps, HashSet<string> UninstallableNames)> GetInstalledApps()
    {
        Directory.CreateDirectory(IconCacheDirectory);
        EnsureDefaultIcon();

        var uwpAppsTask = Task.Run(GetUwpApps);
        var win32AppsTask = Task.Run(GetWin32Apps);

        await Task.WhenAll(uwpAppsTask, win32AppsTask).ConfigureAwait(false);

        var (uwpApps, uninstallableNames) = uwpAppsTask.Result;

        var installedApps = uwpApps.Concat(win32AppsTask.Result)
            .DistinctBy(app => app.Item1)
            .OrderBy(app => app.Item1)
            .ToList();

        _ = LogHelper.Log("Returning Installed Apps [GetInstalledApps]");
        return (installedApps, uninstallableNames);
    }

    private static void EnsureDefaultIcon()
    {
        var defaultPath = Path.Combine(IconCacheDirectory, "defaulticon.png");
        if (File.Exists(defaultPath) && new FileInfo(defaultPath).Length > 0)
            return;

        var largeIcons = new IntPtr[1];
        ExtractIconEx(@"C:\Windows\System32\imageres.dll", 152, largeIcons, null, 1);
        var hIcon = largeIcons[0];
        if (hIcon != IntPtr.Zero)
        {
            using var clonedIcon = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(hIcon).Clone();
            DestroyIcon(hIcon);

            using var bmp = clonedIcon.ToBitmap();
            bmp.Save(defaultPath, ImageFormat.Png);
        }
    }

    private static string GetSafeIconFileName(string identity)
    {
        var safe = new string(identity.Where(c => !InvalidFileNameChars.Contains(c)).ToArray());
        if (safe.Length > 60) safe = safe[..60];
        return $"{safe}_{Math.Abs(identity.GetHashCode())}.png";
    }

    private static async Task<(List<Tuple<string, string, bool>> Apps, HashSet<string> UninstallableNames)> GetUwpApps()
    {
        var installedApps = new List<Tuple<string, string, bool>>();
        var uninstallableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Single PowerShell call that returns Name, InstallLocation, and NonRemovable
        var command = """Get-AppxPackage -AllUsers | Select-Object Name,InstallLocation,NonRemovable | Format-List""";

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

            // Parse all apps and track which are removable
            string? currentName = null;
            string? currentLocation = null;
            bool currentNonRemovable = false;
            var parsedApps = new List<(string Name, string? Location)>();

            foreach (var line in output.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.StartsWith("Name", StringComparison.Ordinal))
                {
                    // Flush previous app
                    if (!string.IsNullOrEmpty(currentName))
                    {
                        parsedApps.Add((currentName, currentLocation));
                        if (!currentNonRemovable)
                            uninstallableNames.Add(currentName);
                    }

                    currentName = line.Split([':'], 2)[1].Trim();
                    currentLocation = null;
                    currentNonRemovable = false;
                }
                else if (line.StartsWith("InstallLocation", StringComparison.Ordinal))
                {
                    currentLocation = line.Split([':'], 2)[1].Trim();
                }
                else if (line.StartsWith("NonRemovable", StringComparison.Ordinal))
                {
                    var val = line.Split([':'], 2)[1].Trim();
                    currentNonRemovable = val.Equals("True", StringComparison.OrdinalIgnoreCase);
                }
                else if (!string.IsNullOrWhiteSpace(currentLocation) && line.StartsWith(" ", StringComparison.Ordinal))
                {
                    currentLocation += " " + line.Trim();
                }
            }

            // Flush last app
            if (!string.IsNullOrEmpty(currentName))
            {
                parsedApps.Add((currentName, currentLocation));
                if (!currentNonRemovable)
                    uninstallableNames.Add(currentName);
            }

            await process.WaitForExitAsync().ConfigureAwait(false);

            // Extract icons in parallel (I/O-bound, safe to parallelize)
            var iconTasks = parsedApps
                .Where(app => !string.IsNullOrEmpty(app.Location))
                .Select(async app =>
                {
                    var logoPath = await ExtractLogoPath(app.Location, false, app.Name).ConfigureAwait(false);
                    return new Tuple<string, string, bool>(app.Name, logoPath, false);
                })
                .ToList();

            // Apps without install location still get added with empty icon
            foreach (var app in parsedApps.Where(app => string.IsNullOrEmpty(app.Location)))
            {
                installedApps.Add(new Tuple<string, string, bool>(app.Name, string.Empty, false));
            }

            installedApps.AddRange(await Task.WhenAll(iconTasks).ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError(ex.Message).ConfigureAwait(false);
        }

        return (installedApps, uninstallableNames);
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

    // Find the uninstall (or quiet uninstall) string for a Win32 app by its display name using the same uninstall roots as GetWin32Apps.
    internal static string? GetWin32UninstallString(string appName)
    {
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
                    continue;

                var displayName = subKey.GetValue("DisplayName") as string;
                if (string.IsNullOrEmpty(displayName))
                    continue;

                if (!displayName.Equals(appName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var uninstallString = subKey.GetValue("QuietUninstallString") as string;
                if (string.IsNullOrEmpty(uninstallString))
                {
                    uninstallString = subKey.GetValue("UninstallString") as string;
                }

                return uninstallString;
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"GetWin32UninstallString failed: {ex.Message}");
        }

        return null;
    }

    public static async Task<List<Tuple<string, string, bool>>> GetWin32Apps()
    {
        var appEntries = new List<(string DisplayName, string? InstallLocation, string? DisplayIcon)>();

        try
        {
            var uninstallRoots = OpenUninstallRoots().ToList();

            var allSubKeys = uninstallRoots
                .SelectMany(k => k.GetSubKeyNames()
                    .Select(name => (Root: k, Name: name)))
                .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            foreach (var uninstallRoot in allSubKeys)
            {
                using var subKey = uninstallRoot.Root.OpenSubKey(uninstallRoot.Name);

                if (subKey == null)
                    continue;

                var displayName = subKey.GetValue("DisplayName") as string;
                var systemComponent = subKey.GetValue("SystemComponent") as int?;

                if (string.IsNullOrEmpty(displayName) || systemComponent == 1)
                    continue;

                if (displayName.Contains("edge", StringComparison.CurrentCultureIgnoreCase))
                    continue;

                var installLocation = subKey.GetValue("InstallLocation") as string;
                if (!string.IsNullOrEmpty(installLocation))
                {
                    installLocation = installLocation.Replace("\"", "");
                    if (installLocation.Contains(".exe"))
                        installLocation = Path.GetDirectoryName(installLocation);
                }

                if (string.IsNullOrEmpty(installLocation))
                {
                    var uninstallString = (subKey.GetValue("UninstallString") as string)?.Replace("\"", "");
                    if (!string.IsNullOrEmpty(uninstallString))
                    {
                        installLocation = Path.GetDirectoryName(uninstallString);
                        if (!string.IsNullOrEmpty(installLocation) && installLocation.Contains(".exe"))
                            installLocation = Path.GetDirectoryName(installLocation);
                    }
                }

                var displayIcon = subKey.GetValue("DisplayIcon") as string;
                appEntries.Add((displayName, installLocation, displayIcon));
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Failed to load Win32 apps: {ex.Message}");
        }

        // Deduplicate before icon extraction to avoid wasted work
        var unique = appEntries
            .DistinctBy(e => e.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Extract icons in parallel
        var iconTasks = unique.Select(async entry =>
        {
            var logoPath = await ExtractLogoPath(entry.InstallLocation, true, entry.DisplayName, entry.DisplayIcon).ConfigureAwait(false);
            return new Tuple<string, string, bool>(entry.DisplayName, logoPath, true);
        }).ToList();

        var results = await Task.WhenAll(iconTasks).ConfigureAwait(false);

        return [.. results.OrderBy(app => app.Item1)];
    }

    private static async Task<string> ExtractLogoPath(string? installLocation, bool isWin32, string appName, string? displayIconPath = null)
    {
        var defaultIcon = isWin32
            ? Path.Combine(IconCacheDirectory, "defaulticon.png")
            : string.Empty;

        if (string.IsNullOrEmpty(installLocation)) return defaultIcon;

        var identity = $"{appName}|{installLocation}";
        var cached = Path.Combine(IconCacheDirectory, GetSafeIconFileName(identity));

        // Return cached icon if valid
        if (File.Exists(cached))
        {
            if (new FileInfo(cached).Length > 2048)
                return cached;
            try { File.Delete(cached); } catch { }
        }

        // -------- WIN32 --------
        if (isWin32 && Directory.Exists(installLocation))
        {
            // Try DisplayIcon passed from registry (no re-scan needed)
            if (!string.IsNullOrEmpty(displayIconPath))
            {
                try
                {
                    var iconFile = displayIconPath.Split(',')[0].Replace("\"", "").Trim();
                    if (File.Exists(iconFile))
                    {
                        var large = new IntPtr[1];
                        ExtractIconEx(iconFile, 0, large, null, 1);
                        if (large[0] != IntPtr.Zero)
                        {
                            using var icon = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(large[0]).Clone();
                            DestroyIcon(large[0]);
                            await SaveIcon(icon, cached).ConfigureAwait(false);
                            return cached;
                        }
                    }
                }
                catch { }
            }

            // Fallback: scan executables in top directory only (avoid deep recursion)
            try
            {
                var exe = Directory
                    .EnumerateFiles(installLocation, "*.exe", SearchOption.TopDirectoryOnly)
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
                        await SaveIcon(icon, cached).ConfigureAwait(false);
                        return cached;
                    }
                }
            }
            catch { }
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
        if (toggleSwitch == null || toggleSwitch.Tag == null) return;

        // Save the state to RyTuneX registry first (64-bit registry with 32-bit app)
        try
        {
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

        // Enqueue the work so operations are serialized
        var tag = toggleSwitch.Tag.ToString();
        var isOn = toggleSwitch.IsOn;

        _toggleQueue.Enqueue(ct => ExecuteToggleActionAsync(tag, isOn, ct));

        // Try to process the queue (fire-and-forget safe runner)
        _ = Task.Run(() => ProcessToggleQueueAsync(_toggleCts.Token));
    }

    private static async Task ProcessToggleQueueAsync(CancellationToken ct)
    {
        if (!await _toggleQueueLock.WaitAsync(0, ct).ConfigureAwait(false))
            return; // another process is running

        try
        {
            while (_toggleQueue.TryDequeue(out var work))
            {
                Interlocked.Increment(ref _toggleRunning);
                try
                {
                    await work(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _ = LogHelper.LogError($"Toggle operation failed: {ex.Message}");
                }
                finally
                {
                    Interlocked.Decrement(ref _toggleRunning);
                }
            }
        }
        finally
        {
            _toggleQueueLock.Release();
        }
    }

    private static async Task ExecuteToggleActionAsync(string? tag, bool isOn, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(tag)) return;

        // Provide a cancellable wrapper around each action and centralize switch
        switch (tag)
        {
            case "RecommendedSectionStartMenu":
                if (isOn) await OptimizeSystemHelper.DisableRecommendedSectionStartMenu().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableRecommendedSectionStartMenu().ConfigureAwait(false);
                break;

            case "LegacyBootMenu":
                if (isOn) await OptimizeSystemHelper.EnableLegacyBootMenu().ConfigureAwait(false);
                else await OptimizeSystemHelper.DisableLegacyBootMenu().ConfigureAwait(false);
                break;

            case "OptimizeNTFS":
                if (isOn) await OptimizeSystemHelper.EnableOptimizeNTFS().ConfigureAwait(false);
                else await OptimizeSystemHelper.DisableOptimizeNTFS().ConfigureAwait(false);
                break;

            case "PrioritizeForegroundApplications":
                if (isOn) await OptimizeSystemHelper.EnablePrioritizeForegroundApplications().ConfigureAwait(false);
                else await OptimizeSystemHelper.DisablePrioritizeForegroundApplications().ConfigureAwait(false);
                break;

            case "WPBT":
                if (isOn) await OptimizeSystemHelper.DisableWPBT().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableWPBT().ConfigureAwait(false);
                break;

            case "ServiceHostSplitting":
                if (isOn) await OptimizeSystemHelper.DisableServiceHostSplitting().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableServiceHostSplitting().ConfigureAwait(false);
                break;

            case "MenuShowDelay":
                if (isOn) await OptimizeSystemHelper.DisableMenuShowDelay().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableMenuShowDelay().ConfigureAwait(false);
                break;

            case "MouseHoverTime":
                if (isOn) await OptimizeSystemHelper.DisableMouseHoverTime().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableMouseHoverTime().ConfigureAwait(false);
                break;

            case "BackgroundApps":
                if (isOn) await OptimizeSystemHelper.DisableBackgroundApps().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableBackgroundApps().ConfigureAwait(false);
                break;

            case "AutoComplete":
                if (isOn) await OptimizeSystemHelper.DisableAutoComplete().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableAutoComplete().ConfigureAwait(false);
                break;

            case "CrashDump":
                if (isOn) await OptimizeSystemHelper.EnableCrashDump().ConfigureAwait(false);
                else await OptimizeSystemHelper.DisableCrashDump().ConfigureAwait(false);
                break;

            case "RemoteAssistance":
                if (isOn) await OptimizeSystemHelper.DisableRemoteAssistance().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableRemoteAssistance().ConfigureAwait(false);
                break;

            case "WindowShake":
                if (isOn) await OptimizeSystemHelper.DisableWindowShake().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableWindowShake().ConfigureAwait(false);
                break;

            case "CopyMoveContextMenu":
                if (isOn) await OptimizeSystemHelper.AddCopyMoveContextMenu().ConfigureAwait(false);
                else await OptimizeSystemHelper.RemoveCopyMoveContextMenu().ConfigureAwait(false);
                break;

            case "TaskTimeouts":
                if (isOn) await OptimizeSystemHelper.AdjustTaskTimeouts().ConfigureAwait(false);
                else await OptimizeSystemHelper.IncreaseTaskTimeouts().ConfigureAwait(false);
                break;

            case "LowDiskSpaceChecks":
                if (isOn) await OptimizeSystemHelper.EnableLowDiskSpaceChecks().ConfigureAwait(false);
                else await OptimizeSystemHelper.DisableLowDiskSpaceChecks().ConfigureAwait(false);
                break;

            case "LinkResolve":
                if (isOn) await OptimizeSystemHelper.DisableLinkResolve().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableLinkResolve().ConfigureAwait(false);
                break;

            case "ServiceTimeouts":
                if (isOn) await OptimizeSystemHelper.DecreaseServiceTimeouts().ConfigureAwait(false);
                else await OptimizeSystemHelper.RevertServiceTimeouts().ConfigureAwait(false);
                break;

            case "RemoteRegistry":
                if (isOn) await OptimizeSystemHelper.DisableRemoteRegistry().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableRemoteRegistry().ConfigureAwait(false);
                break;

            case "FileExtensionsAndHiddenFiles":
                if (isOn) await OptimizeSystemHelper.HideFileExtensionsAndHiddenFiles().ConfigureAwait(false);
                else await OptimizeSystemHelper.ShowFileExtensionsAndHiddenFiles().ConfigureAwait(false);
                break;

            case "SystemProfile":
                if (isOn) await OptimizeSystemHelper.OptimizeSystemProfile().ConfigureAwait(false);
                else await OptimizeSystemHelper.RevertSystemProfile().ConfigureAwait(false);
                break;

            case "TelemetryServices":
                if (isOn) await OptimizeSystemHelper.DisableTelemetryServices().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableTelemetryServices().ConfigureAwait(false);
                break;

            case "HomeGroup":
                if (isOn) await OptimizeSystemHelper.DisableHomeGroup().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableHomeGroup().ConfigureAwait(false);
                break;

            case "PrintService":
                if (isOn) await OptimizeSystemHelper.DisablePrintService().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnablePrintService().ConfigureAwait(false);
                break;

            case "SysMain":
                if (isOn) await OptimizeSystemHelper.DisableSysMain().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableSysMain().ConfigureAwait(false);
                break;

            case "CompatibilityAssistant":
                if (isOn) await OptimizeSystemHelper.DisableCompatibilityAssistant().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableCompatibilityAssistant().ConfigureAwait(false);
                break;

            case "SystemRestore":
                if (isOn) await OptimizeSystemHelper.DisableSystemRestore().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableSystemRestore().ConfigureAwait(false);
                break;

            case "WindowsTransparency":
                if (isOn) await OptimizeSystemHelper.DisableWindowsTransparency().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableWindowsTransparency().ConfigureAwait(false);
                break;

            case "WindowsDarkMode":
                if (isOn) await OptimizeSystemHelper.EnableWindowsDarkMode().ConfigureAwait(false);
                else await OptimizeSystemHelper.DisableWindowsDarkMode().ConfigureAwait(false);
                break;

            case "VerboseLogon":
                if (isOn) await OptimizeSystemHelper.EnableVerboseLogon().ConfigureAwait(false);
                else await OptimizeSystemHelper.DisableVerboseLogon().ConfigureAwait(false);
                break;

            case "ClassicContextMenu":
                if (isOn) await OptimizeSystemHelper.EnableClassicContextMenu().ConfigureAwait(false);
                else await OptimizeSystemHelper.DisableClassicContextMenu().ConfigureAwait(false);
                break;

            case "Search":
                if (isOn) await OptimizeSystemHelper.DisableSearch().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableSearch().ConfigureAwait(false);
                break;

            case "Biometrics":
                if (isOn) await OptimizeSystemHelper.DisableBiometrics().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableBiometrics().ConfigureAwait(false);
                break;

            case "SMBv1":
                if (isOn) await OptimizeSystemHelper.DisableSMBAsync("1").ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableSMBAsync("1").ConfigureAwait(false);
                break;

            case "SMBv2":
                if (isOn) await OptimizeSystemHelper.DisableSMBAsync("2").ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableSMBAsync("2").ConfigureAwait(false);
                break;

            case "ErrorReporting":
                if (isOn) await OptimizeSystemHelper.DisableErrorReporting().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableErrorReporting().ConfigureAwait(false);
                break;

            case "Cortana":
                if (isOn) await OptimizeSystemHelper.DisableCortana().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableCortana().ConfigureAwait(false);
                break;

            case "GamingMode":
                if (isOn) await OptimizeSystemHelper.EnableGamingMode().ConfigureAwait(false);
                else await OptimizeSystemHelper.DisableGamingMode().ConfigureAwait(false);
                break;

            case "StoreUpdates":
                if (isOn) await OptimizeSystemHelper.DisableStoreUpdates().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableStoreUpdates().ConfigureAwait(false);
                break;

            case "OneDrive":
                if (isOn) await OptimizeSystemHelper.DisableOneDrive().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableOneDrive().ConfigureAwait(false);
                break;

            case "SensorServices":
                if (isOn) await OptimizeSystemHelper.DisableSensorServices().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableSensorServices().ConfigureAwait(false);
                break;

            case "NewsAndInterests":
                if (isOn) await OptimizeSystemHelper.DisableNewsAndInterests().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableNewsAndInterests().ConfigureAwait(false);
                break;

            case "SpotlightFeatures":
                if (isOn) await OptimizeSystemHelper.DisableSpotlightFeatures().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableSpotlightFeatures().ConfigureAwait(false);
                break;

            case "TailoredExperiences":
                if (isOn) await OptimizeSystemHelper.DisableTailoredExperiences().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableTailoredExperiences().ConfigureAwait(false);
                break;

            case "CloudOptimizedContent":
                if (isOn) await OptimizeSystemHelper.DisableCloudOptimizedContent().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableCloudOptimizedContent().ConfigureAwait(false);
                break;

            case "FeedbackNotifications":
                if (isOn) await OptimizeSystemHelper.DisableFeedbackNotifications().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableFeedbackNotifications().ConfigureAwait(false);
                break;

            case "AdvertisingID":
                if (isOn) await OptimizeSystemHelper.DisableAdvertisingID().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableAdvertisingID().ConfigureAwait(false);
                break;

            case "BluetoothAdvertising":
                if (isOn) await OptimizeSystemHelper.DisableBluetoothAdvertising().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableBluetoothAdvertising().ConfigureAwait(false);
                break;

            case "AutomaticRestartSignOn":
                if (isOn) await OptimizeSystemHelper.DisableAutomaticRestartSignOn().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableAutomaticRestartSignOn().ConfigureAwait(false);
                break;

            case "HandwritingDataSharing":
                if (isOn) await OptimizeSystemHelper.DisableHandwritingDataSharing().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableHandwritingDataSharing().ConfigureAwait(false);
                break;

            case "TextInputDataCollection":
                if (isOn) await OptimizeSystemHelper.DisableTextInputDataCollection().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableTextInputDataCollection().ConfigureAwait(false);
                break;

            case "InputPersonalization":
                if (isOn) await OptimizeSystemHelper.DisableInputPersonalization().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableInputPersonalization().ConfigureAwait(false);
                break;

            case "SafeSearchMode":
                if (isOn) await OptimizeSystemHelper.DisableSafeSearchMode().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableSafeSearchMode().ConfigureAwait(false);
                break;

            case "ActivityUploads":
                if (isOn) await OptimizeSystemHelper.DisableActivityUploads().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableActivityUploads().ConfigureAwait(false);
                break;

            case "ClipboardSync":
                if (isOn) await OptimizeSystemHelper.DisableClipboardSync().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableClipboardSync().ConfigureAwait(false);
                break;

            case "MessageSync":
                if (isOn) await OptimizeSystemHelper.DisableMessageSync().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableMessageSync().ConfigureAwait(false);
                break;

            case "SettingSync":
                if (isOn) await OptimizeSystemHelper.DisableSettingSync().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableSettingSync().ConfigureAwait(false);
                break;

            case "VoiceActivation":
                if (isOn) await OptimizeSystemHelper.DisableVoiceActivation().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableVoiceActivation().ConfigureAwait(false);
                break;

            case "FindMyDevice":
                if (isOn) await OptimizeSystemHelper.DisableFindMyDevice().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableFindMyDevice().ConfigureAwait(false);
                break;

            case "ActivityFeed":
                if (isOn) await OptimizeSystemHelper.DisableActivityFeed().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableActivityFeed().ConfigureAwait(false);
                break;

            case "Cdp":
                if (isOn) await OptimizeSystemHelper.DisableCdp().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableCdp().ConfigureAwait(false);
                break;

            case "DiagnosticsToast":
                if (isOn) await OptimizeSystemHelper.DisableDiagnosticsToast().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableDiagnosticsToast().ConfigureAwait(false);
                break;

            case "OnlineSpeechPrivacy":
                if (isOn) await OptimizeSystemHelper.DisableOnlineSpeechPrivacy().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableOnlineSpeechPrivacy().ConfigureAwait(false);
                break;

            case "LocationAccess":
                if (isOn) await OptimizeSystemHelper.DisableLocationFeatures().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableLocationFeatures().ConfigureAwait(false);
                break;

            case "LocationFeatures":
                if (isOn) await OptimizeSystemHelper.DisableLocationFeatures().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableLocationFeatures().ConfigureAwait(false);
                break;

            case "GameBar":
                if (isOn) await OptimizeSystemHelper.DisableGameBar().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableGameBar().ConfigureAwait(false);
                break;

            case "QuickAccessHistory":
                if (isOn) await OptimizeSystemHelper.DisableQuickAccessHistory().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableQuickAccessHistory().ConfigureAwait(false);
                break;

            case "MyPeople":
                if (isOn) await OptimizeSystemHelper.DisableMyPeople().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableMyPeople().ConfigureAwait(false);
                break;

            case "Drivers":
                if (isOn) await OptimizeSystemHelper.ExcludeDrivers().ConfigureAwait(false);
                else await OptimizeSystemHelper.IncludeDrivers().ConfigureAwait(false);
                break;

            case "WindowsInk":
                if (isOn) await OptimizeSystemHelper.DisableWindowsInk().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableWindowsInk().ConfigureAwait(false);
                break;

            case "SpellingAndTypingFeatures":
                if (isOn) await OptimizeSystemHelper.DisableSpellingAndTypingFeatures().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableSpellingAndTypingFeatures().ConfigureAwait(false);
                break;

            case "FaxService":
                if (isOn) await OptimizeSystemHelper.DisableFaxService().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableFaxService().ConfigureAwait(false);
                break;

            case "InsiderService":
                if (isOn) await OptimizeSystemHelper.DisableInsiderService().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableInsiderService().ConfigureAwait(false);
                break;

            case "SmartScreen":
                if (isOn) await OptimizeSystemHelper.DisableSmartScreen().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableSmartScreen().ConfigureAwait(false);
                break;

            case "CloudClipboard":
                if (isOn) await OptimizeSystemHelper.DisableCloudClipboard().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableCloudClipboard().ConfigureAwait(false);
                break;

            case "StickyKeys":
                if (isOn) await OptimizeSystemHelper.DisableStickyKeys().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableStickyKeys().ConfigureAwait(false);
                break;

            case "CastToDevice":
                if (isOn) await OptimizeSystemHelper.RemoveCastToDevice().ConfigureAwait(false);
                else await OptimizeSystemHelper.AddCastToDevice().ConfigureAwait(false);
                break;

            case "VBS":
                if (isOn) await OptimizeSystemHelper.DisableVBS().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableVBS().ConfigureAwait(false);
                break;

            case "TaskbarToLeft":
                if (isOn) await OptimizeSystemHelper.AlignTaskbarToLeft().ConfigureAwait(false);
                else await OptimizeSystemHelper.AlignTaskbarToCenter().ConfigureAwait(false);
                break;

            case "SnapAssist":
                if (isOn) await OptimizeSystemHelper.DisableSnapAssist().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableSnapAssist().ConfigureAwait(false);
                break;

            case "Widgets":
                if (isOn) await OptimizeSystemHelper.DisableWidgets().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableWidgets().ConfigureAwait(false);
                break;

            case "Chat":
                if (isOn) await OptimizeSystemHelper.DisableChat().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableChat().ConfigureAwait(false);
                break;

            case "FilesCompactMode":
                if (isOn) await OptimizeSystemHelper.EnableFilesCompactMode().ConfigureAwait(false);
                else await OptimizeSystemHelper.DisableFilesCompactMode().ConfigureAwait(false);
                break;

            case "Stickers":
                if (isOn) await OptimizeSystemHelper.DisableStickers().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableStickers().ConfigureAwait(false);
                break;

            case "EdgeDiscoverBar":
                if (isOn) await OptimizeSystemHelper.DisableEdgeDiscoverBar().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableEdgeDiscoverBar().ConfigureAwait(false);
                break;

            case "EdgeTelemetry":
                if (isOn) await OptimizeSystemHelper.DisableEdgeTelemetry().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableEdgeTelemetry().ConfigureAwait(false);
                break;

            case "CoPilotAI":
                if (isOn) await OptimizeSystemHelper.DisableCoPilotAI().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableCoPilotAI().ConfigureAwait(false);
                break;

            case "WindowsRecall":
                if (isOn) await OptimizeSystemHelper.DisableWindowsRecall().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableWindowsRecall().ConfigureAwait(false);
                break;

            case "VisualStudioTelemetry":
                if (isOn) await OptimizeSystemHelper.DisableVisualStudioTelemetry().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableVisualStudioTelemetry().ConfigureAwait(false);
                break;

            case "NvidiaTelemetry":
                if (isOn) await OptimizeSystemHelper.DisableNvidiaTelemetry().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableNvidiaTelemetry().ConfigureAwait(false);
                break;

            case "ChromeTelemetry":
                if (isOn) await OptimizeSystemHelper.DisableChromeTelemetry().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableChromeTelemetry().ConfigureAwait(false);
                break;

            case "FirefoxTelemetry":
                if (isOn) await OptimizeSystemHelper.DisableFirefoxTelemetry().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableFirefoxTelemetry().ConfigureAwait(false);
                break;

            case "Hibernation":
                if (isOn) await OptimizeSystemHelper.DisableHibernation().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableHibernation().ConfigureAwait(false);
                break;

            case "EndTask":
                if (isOn) await OptimizeSystemHelper.EnableEndTask().ConfigureAwait(false);
                else await OptimizeSystemHelper.DisableEndTask().ConfigureAwait(false);
                break;

            case "WindowsAI":
                if (isOn) await OptimizeSystemHelper.DisableWindowsAI().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableWindowsAI().ConfigureAwait(false);
                break;

            default:
                _ = LogHelper.Log($"Unhandled toggle tag queued: {tag}");
                break;
        }
    }
}