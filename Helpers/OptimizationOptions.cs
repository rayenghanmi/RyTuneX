using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace RyTuneX.Helpers;

public class AppInfo : Tuple<string, string, bool>
{
    public string Name { get; }
    public string IconPath { get; }
    public bool IsWin32 { get; }
    public string PackageId { get; }

    public AppInfo(string name, string iconPath, bool isWin32, string packageId)
        : base(name, iconPath, isWin32)
    {
        Name = name;
        IconPath = iconPath;
        IsWin32 = isWin32;
        PackageId = packageId;
    }
}

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

    [DllImport("shlwapi.dll", BestFitMapping = false, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = false, ThrowOnUnmappableChar = true)]
    private static extern int SHLoadIndirectString(string pszSource, StringBuilder pszOutBuf, uint cchOutBuf, IntPtr ppvReserved);

    private static string CleanPackageName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return "Unknown App";

        var name = rawName;
        if (name.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase))
        {
            name = name["Microsoft.".Length..];
        }
        if (name.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase))
        {
            name = name["Windows.".Length..];
        }

        var result = Regex.Replace(name, @"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ");
        return string.IsNullOrWhiteSpace(result) ? rawName : result.Trim();
    }

    public static async Task<(List<AppInfo> Apps, HashSet<string> UninstallableNames)> GetInstalledApps()
    {
        Directory.CreateDirectory(IconCacheDirectory);
        EnsureDefaultIcon();

        var uwpAppsTask = Task.Run(GetUwpApps);
        var win32AppsTask = Task.Run(GetWin32Apps);

        await Task.WhenAll(uwpAppsTask, win32AppsTask).ConfigureAwait(false);

        var (uwpApps, uninstallableNames) = uwpAppsTask.Result;

        var installedApps = uwpApps.Concat(win32AppsTask.Result)
            .DistinctBy(app => app.Name, StringComparer.OrdinalIgnoreCase)
            .OrderBy(app => app.Name)
            .ToList();

        _ = LogHelper.Log("Returning Installed Apps [GetInstalledApps]");
        return (installedApps, uninstallableNames);
    }

    private static void EnsureDefaultIcon()
    {
        var defaultPath = Path.Combine(IconCacheDirectory, "defaulticon.png");
        if (File.Exists(defaultPath) && new FileInfo(defaultPath).Length > 0)
            return;
        try
        {
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
        catch (Exception ex)
        {
            _ = LogHelper.LogWarning($"Failed to extract default icon: {ex.Message}");
        }
    }

    private static string GetSafeIconFileName(string identity)
    {
        var safe = new string(identity.Where(c => !InvalidFileNameChars.Contains(c)).ToArray());
        if (safe.Length > 60) safe = safe[..60];
        return $"{safe}_{Math.Abs(identity.GetHashCode())}.png";
    }

    private static string? ResolveIndirectString(string resourceUri)
    {
        try
        {
            var sb = new StringBuilder(1024);
            int hr = SHLoadIndirectString(resourceUri, sb, (uint)sb.Capacity, IntPtr.Zero);
            if (hr == 0 && sb.Length > 0)
            {
                return sb.ToString();
            }
        }
        catch { }
        return null;
    }

    private static async Task<string> GetUwpDisplayNameAsync(Package pkg)
    {
        // 1. Dynamic WinRT AppListEntry DisplayName (returns real-time localized name from Windows app manifest/resource)
        try
        {
            var appListEntries = await pkg.GetAppListEntriesAsync();
            if (appListEntries != null && appListEntries.Count > 0)
            {
                var displayName = appListEntries[0].DisplayInfo?.DisplayName;
                if (!string.IsNullOrWhiteSpace(displayName) && !displayName.StartsWith("ms-resource:", StringComparison.OrdinalIgnoreCase))
                {
                    return displayName.Trim();
                }
            }
        }
        catch { }

        // 2. Package.DisplayName property
        try
        {
            var displayName = pkg.DisplayName;
            if (!string.IsNullOrWhiteSpace(displayName) && !displayName.StartsWith("ms-resource:", StringComparison.OrdinalIgnoreCase))
            {
                return displayName.Trim();
            }
        }
        catch { }

        // 3. Dynamic real-time resolution of ms-resource: indirect strings via SHLoadIndirectString API
        try
        {
            var rawName = pkg.DisplayName;
            if (!string.IsNullOrWhiteSpace(rawName) && rawName.StartsWith("ms-resource:", StringComparison.OrdinalIgnoreCase))
            {
                var indirectUri = $"@{{{pkg.Id.FullName}?{rawName}}}";
                var resolved = ResolveIndirectString(indirectUri);
                if (!string.IsNullOrWhiteSpace(resolved) &&
                    !resolved.StartsWith("ms-resource:", StringComparison.OrdinalIgnoreCase) &&
                    !resolved.StartsWith("@{", StringComparison.OrdinalIgnoreCase))
                {
                    return resolved.Trim();
                }

                var indirectUri2 = $"@{{{pkg.Id.FullName}?ms-resource://{pkg.Id.Name}/resources/AppName}}";
                var resolved2 = ResolveIndirectString(indirectUri2);
                if (!string.IsNullOrWhiteSpace(resolved2) &&
                    !resolved2.StartsWith("ms-resource:", StringComparison.OrdinalIgnoreCase) &&
                    !resolved2.StartsWith("@{", StringComparison.OrdinalIgnoreCase))
                {
                    return resolved2.Trim();
                }
            }
        }
        catch { }

        // 4. Dynamic formatting fallback for internal identity names
        return CleanPackageName(pkg.Id.Name);
    }

    private static async Task<(List<AppInfo> Apps, HashSet<string> UninstallableNames)> GetUwpApps()
    {
        var installedApps = new List<AppInfo>();
        var uninstallableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var packageManager = new PackageManager();
            IEnumerable<Package> packages;
            try
            {
                packages = packageManager.FindPackagesForUser(string.Empty);
            }
            catch
            {
                try { packages = packageManager.FindPackages(); }
                catch { packages = Array.Empty<Package>(); }
            }

            var parsedApps = new List<(string Name, string? Location, bool NonRemovable, string PackageId)>();

            foreach (var pkg in packages)
            {
                try
                {
                    if (pkg.IsFramework || pkg.IsResourcePackage || pkg.IsOptional)
                        continue;

                    var rawName = pkg.Id.Name;
                    if (string.IsNullOrWhiteSpace(rawName))
                        continue;

                    if (rawName.Contains("RyTuneX", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var displayName = await GetUwpDisplayNameAsync(pkg).ConfigureAwait(false);

                    string? location = null;
                    try { location = pkg.InstalledLocation?.Path; } catch { }

                    bool nonRemovable = false;
                    try
                    {
                        nonRemovable = (pkg.SignatureKind == PackageSignatureKind.System);
                    }
                    catch { }

                    var packageId = pkg.Id.FullName ?? rawName;

                    parsedApps.Add((displayName, location, nonRemovable, packageId));

                    if (!nonRemovable)
                    {
                        uninstallableNames.Add(displayName);
                        uninstallableNames.Add(packageId);
                        uninstallableNames.Add(rawName);
                    }
                }
                catch (Exception ex)
                {
                    _ = LogHelper.LogWarning($"Error inspecting package: {ex.Message}");
                }
            }

            var uniqueApps = parsedApps
                .DistinctBy(app => app.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var iconTasks = uniqueApps.Select(async app =>
            {
                var logoPath = await ExtractLogoPath(app.Location, false, app.Name).ConfigureAwait(false);
                return new AppInfo(app.Name, logoPath, false, app.PackageId);
            }).ToList();

            var results = await Task.WhenAll(iconTasks).ConfigureAwait(false);
            installedApps.AddRange(results);
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Failed to load UWP apps via WinRT: {ex.Message}");
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
            catch (Exception ex)
            {
                _ = LogHelper.LogWarning($"Failed to open uninstall root ({hive}/{view}): {ex.Message}");
            }

            if (baseKey != null)
                yield return baseKey;
        }
    }

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

    public static async Task UninstallWin32AppAsync(string appName)
    {
        if (appName.Contains("edge", StringComparison.OrdinalIgnoreCase) ||
            appName.Contains("Microsoft Edge", StringComparison.OrdinalIgnoreCase))
        {
            var scriptFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "RemoveEdge.ps1");
            var cmdCommand = $"powershell.exe -ExecutionPolicy Bypass -File \"{scriptFilePath}\" -UninstallEdge -RemoveEdgeData -NonInteractive";
            await StartInCmd(cmdCommand).ConfigureAwait(false);
            return;
        }

        var uninstallString = GetWin32UninstallString(appName);
        if (string.IsNullOrEmpty(uninstallString))
        {
            throw new Exception($"Uninstall command for '{appName}' not found in registry.");
        }

        _ = LogHelper.Log($"Uninstall string for {appName}: {uninstallString}");

        var exitCode = await StartInCmd(uninstallString).ConfigureAwait(false);
        if (exitCode != 0)
        {
            _ = LogHelper.LogWarning($"Uninstall of Win32 app '{appName}' exited with code {exitCode}.");
        }
    }

    public static async Task UninstallUwpAppAsync(string packageId, string appName)
    {
        _ = LogHelper.Log($"Uninstalling UWP App: '{appName}' (ID: '{packageId}')");

        if (appName.Contains("edge", StringComparison.OrdinalIgnoreCase) ||
            packageId.Contains("Microsoft.MicrosoftEdge", StringComparison.OrdinalIgnoreCase))
        {
            var scriptFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "RemoveEdge.ps1");
            var cmdCommand = $"powershell.exe -ExecutionPolicy Bypass -File \"{scriptFilePath}\" -UninstallEdge -RemoveEdgeData -NonInteractive";
            await StartInCmd(cmdCommand).ConfigureAwait(false);
            return;
        }

        bool removedDirectly = false;

        try
        {
            var packageManager = new PackageManager();
            var packages = packageManager.FindPackages()
                .Where(p => string.Equals(p.Id.FullName, packageId, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(p.Id.FamilyName, packageId, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(p.Id.Name, packageId, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(p.Id.Name, appName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (packages.Count > 0)
            {
                foreach (var pkg in packages)
                {
                    try
                    {
                        var depOp = packageManager.RemovePackageAsync(pkg.Id.FullName, RemovalOptions.RemoveForAllUsers);
                        await depOp;
                        removedDirectly = true;
                        _ = LogHelper.Log($"Successfully removed UWP package {pkg.Id.FullName} via WinRT PackageManager.");
                    }
                    catch (Exception ex)
                    {
                        _ = LogHelper.LogWarning($"RemovePackageAsync (all users) failed for {pkg.Id.FullName}: {ex.Message}. Trying default remove.");
                        try
                        {
                            var depOp = packageManager.RemovePackageAsync(pkg.Id.FullName);
                            await depOp;
                            removedDirectly = true;
                            _ = LogHelper.Log($"Successfully removed UWP package {pkg.Id.FullName} via WinRT PackageManager.");
                        }
                        catch (Exception ex2)
                        {
                            _ = LogHelper.LogWarning($"Default RemovePackageAsync failed: {ex2.Message}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogWarning($"WinRT Package Manager removal error: {ex.Message}");
        }

        try
        {
            var searchPattern = packageId;
            if (searchPattern.Contains('_'))
            {
                searchPattern = searchPattern.Split('_')[0];
            }
            // Sanitize for PowerShell single-quoted strings
            searchPattern = searchPattern.Replace("'", "''");

            var removeProvisioned = $"Get-AppxProvisionedPackage -Online | Where-Object {{ $_.DisplayName -eq '{searchPattern}' -or $_.PackageName -like '*{searchPattern}*' }} | ForEach-Object {{ Remove-AppxProvisionedPackage -Online -PackageName $_.PackageName }}";
            await RunPowerShell(removeProvisioned).ConfigureAwait(false);

            if (!removedDirectly)
            {
                var removeAppx = $"Get-AppxPackage -AllUsers | Where-Object {{ $_.Name -eq '{searchPattern}' -or $_.PackageFullName -eq '{packageId}' }} | Remove-AppxPackage -AllUsers";
                await RunPowerShell(removeAppx).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Fallback PowerShell cleanup error for {packageId}: {ex.Message}");
        }
    }

    public static async Task<List<AppInfo>> GetWin32Apps()
    {
        var appEntries = new List<(string DisplayName, string? InstallLocation, string? DisplayIcon, string UninstallString)>();

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

                var installLocation = subKey.GetValue("InstallLocation") as string;
                if (!string.IsNullOrEmpty(installLocation))
                {
                    installLocation = installLocation.Replace("\"", "");
                    if (installLocation.Contains(".exe"))
                        installLocation = Path.GetDirectoryName(installLocation);
                }

                var uninstallString = subKey.GetValue("QuietUninstallString") as string;
                if (string.IsNullOrEmpty(uninstallString))
                {
                    uninstallString = subKey.GetValue("UninstallString") as string;
                }

                if (string.IsNullOrEmpty(installLocation) && !string.IsNullOrEmpty(uninstallString))
                {
                    var cleanUninstall = uninstallString.Replace("\"", "");
                    installLocation = Path.GetDirectoryName(cleanUninstall);
                    if (!string.IsNullOrEmpty(installLocation) && installLocation.Contains(".exe"))
                        installLocation = Path.GetDirectoryName(installLocation);
                }

                var displayIcon = subKey.GetValue("DisplayIcon") as string;
                appEntries.Add((displayName, installLocation, displayIcon, uninstallString ?? string.Empty));
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Failed to load Win32 apps: {ex.Message}");
        }

        var unique = appEntries
            .DistinctBy(e => e.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var iconTasks = unique.Select(async entry =>
        {
            var logoPath = await ExtractLogoPath(entry.InstallLocation, true, entry.DisplayName, entry.DisplayIcon).ConfigureAwait(false);
            return new AppInfo(entry.DisplayName, logoPath, true, entry.UninstallString);
        }).ToList();

        var results = await Task.WhenAll(iconTasks).ConfigureAwait(false);

        return [.. results.OrderBy(app => app.Name)];
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
        var m = Regex.Match(file, @"Scale-(\d+)");
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
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().ConfigureAwait(false);
        var output = await outputTask.ConfigureAwait(false);
        await errorTask.ConfigureAwait(false); // drain stderr to prevent deadlock
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
                        await ExecuteToggleActionAsync(valueName, false, CancellationToken.None).ConfigureAwait(false);
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

        int executedCount = 0;

        try
        {
            while (_toggleQueue.TryDequeue(out var work))
            {
                Interlocked.Increment(ref _toggleRunning);
                try
                {
                    await work(ct).ConfigureAwait(false);
                    executedCount++;
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

            if (executedCount > 0)
            {
                ReviewPromptHelper.NotifyOptimizationCompleted();
            }
        }
    }

    private static async Task ExecuteToggleActionAsync(string? tag, bool isOn, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(tag)) return;

        _ = LogHelper.Log($"Executing optimization: {tag} = {(isOn ? "ON" : "OFF")}");

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

            case "KeyboardLatency":
                if (isOn) await OptimizeSystemHelper.EnableKeyboardLatencyOptimization().ConfigureAwait(false);
                else await OptimizeSystemHelper.DisableKeyboardLatencyOptimization().ConfigureAwait(false);
                break;

            case "MouseAcceleration":
                if (isOn) await OptimizeSystemHelper.DisableMouseAcceleration().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableMouseAcceleration().ConfigureAwait(false);
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

            case "FullscreenOptimizations":
                if (isOn) await OptimizeSystemHelper.EnableFullscreenOptimizations().ConfigureAwait(false);
                else await OptimizeSystemHelper.DisableFullscreenOptimizations().ConfigureAwait(false);
                break;

            case "UsbPowerSaving":
                if (isOn) await OptimizeSystemHelper.DisableUsbPowerSaving().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableUsbPowerSaving().ConfigureAwait(false);
                break;

            case "PowerThrottling":
                if (isOn) await OptimizeSystemHelper.DisablePowerThrottling().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnablePowerThrottling().ConfigureAwait(false);
                break;

            case "GpuDriverTweaks":
                if (isOn) await OptimizeSystemHelper.ApplyGpuDriverTweaks().ConfigureAwait(false);
                else await OptimizeSystemHelper.RevertGpuDriverTweaks().ConfigureAwait(false);
                break;

            case "StoreUpdates":
                if (isOn) await OptimizeSystemHelper.DisableStoreUpdates().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableStoreUpdates().ConfigureAwait(false);
                break;

            case "OneDrive":
                if (isOn) await OptimizeSystemHelper.DisableOneDrive().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableOneDrive().ConfigureAwait(false);
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

            case "GlobalTimerResolution":
                if (isOn) await OptimizeSystemHelper.EnableGlobalTimerResolution().ConfigureAwait(false);
                else await OptimizeSystemHelper.DisableGlobalTimerResolution().ConfigureAwait(false);
                break;

            case "HPET":
                if (isOn) await OptimizeSystemHelper.DisableHPET().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableHPET().ConfigureAwait(false);
                break;

            case "DynamicTick":
                if (isOn) await OptimizeSystemHelper.DisableDynamicTick().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableDynamicTick().ConfigureAwait(false);
                break;

            case "Prefetch":
                if (isOn) await OptimizeSystemHelper.DisablePrefetch().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnablePrefetch().ConfigureAwait(false);
                break;

            case "LargeSystemCache":
                if (isOn) await OptimizeSystemHelper.EnableLargeSystemCache().ConfigureAwait(false);
                else await OptimizeSystemHelper.DisableLargeSystemCache().ConfigureAwait(false);
                break;

            case "NDU":
                if (isOn) await OptimizeSystemHelper.DisableNDU().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableNDU().ConfigureAwait(false);
                break;

            case "PageFileEncryption":
                if (isOn) await OptimizeSystemHelper.DisablePageFileEncryption().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnablePageFileEncryption().ConfigureAwait(false);
                break;

            case "LockScreenNotifications":
                if (isOn) await OptimizeSystemHelper.DisableLockScreenNotifications().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableLockScreenNotifications().ConfigureAwait(false);
                break;

            case "ToastNotifications":
                if (isOn) await OptimizeSystemHelper.DisableToastNotifications().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableToastNotifications().ConfigureAwait(false);
                break;

            case "NotificationCenter":
                if (isOn) await OptimizeSystemHelper.DisableNotificationCenter().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableNotificationCenter().ConfigureAwait(false);
                break;

            case "SuggestedActions":
                if (isOn) await OptimizeSystemHelper.DisableSuggestedActions().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableSuggestedActions().ConfigureAwait(false);
                break;

            case "WiFiSense":
                if (isOn) await OptimizeSystemHelper.DisableWiFiSense().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableWiFiSense().ConfigureAwait(false);
                break;

            case "WebSearchResults":
                if (isOn) await OptimizeSystemHelper.DisableWebSearchResults().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableWebSearchResults().ConfigureAwait(false);
                break;

            case "AppLaunchTracking":
                if (isOn) await OptimizeSystemHelper.DisableAppLaunchTracking().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableAppLaunchTracking().ConfigureAwait(false);
                break;

            case "TimelineHistory":
                if (isOn) await OptimizeSystemHelper.DisableTimelineHistory().ConfigureAwait(false);
                else await OptimizeSystemHelper.EnableTimelineHistory().ConfigureAwait(false);
                break;

            case "TakeOwnership":
                if (isOn) await OptimizeSystemHelper.AddTakeOwnership().ConfigureAwait(false);
                else await OptimizeSystemHelper.RemoveTakeOwnership().ConfigureAwait(false);
                break;

            case "OpenCmdHere":
                if (isOn) await OptimizeSystemHelper.AddOpenCmdHere().ConfigureAwait(false);
                else await OptimizeSystemHelper.RemoveOpenCmdHere().ConfigureAwait(false);
                break;

            case "CopyFilePath":
                if (isOn) await OptimizeSystemHelper.AddCopyFilePath().ConfigureAwait(false);
                else await OptimizeSystemHelper.RemoveCopyFilePath().ConfigureAwait(false);
                break;

            default:
                _ = LogHelper.Log($"Unhandled toggle tag queued: {tag}");
                break;
        }
    }
}
