using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RyTuneX.Helpers;

// Represents a single cleanup category with its paths, commands, and runtime state
public class CleanupCategory
{
    public required string Id { get; init; }
    public required string NameKey { get; init; }
    public required string DescriptionKey { get; init; }
    public required string Glyph { get; init; }

    // Directories to delete during cleaning (also scanned for size estimation)
    public string[] DirectoryPaths { get; init; } = [];

    // Shell commands to run during cleaning
    public string[] CleanCommands { get; init; } = [];

    // Windows services to stop before cleaning this category
    public string[] ServicesToStop { get; init; } = [];

    // Whether Explorer.exe must be killed for locked file access
    public bool RequiresExplorerRestart { get; init; }

    // Whether to query Recycle Bin via Shell API for size
    public bool IsRecycleBin { get; init; }

    // Runtime state — set during scanning
    public long SizeBytes { get; set; }
    public bool IsSelected { get; set; } = true;
    public bool SizeScanned { get; set; }
}

// Engine for scanning and cleaning system junk across categorized targets
internal static class DeepCleanEngine
{
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHQueryRecycleBin(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct SHQUERYRBINFO
    {
        public uint cbSize;
        public long i64Size;
        public long i64NumItems;
    }

    // Returns all available cleanup categories with their associated paths and commands
    public static List<CleanupCategory> GetCategories() =>
    [
        new()
        {
            Id = "temp",
            NameKey = "DeepClean_Cat_TempFiles",
            DescriptionKey = "DeepClean_Cat_TempFiles_Desc",
            Glyph = "\uEA99",
            DirectoryPaths =
            [
                "%TEMP%",
                "%windir%\\Temp",
                "%localappdata%\\Packages\\Microsoft.WindowsStore_8wekyb3d8bbwe\\LocalCache",
                "%localappdata%\\Packages\\Microsoft.WindowsStore_8wekyb3d8bbwe\\TempState",
                "%localappdata%\\Packages\\Microsoft.XboxGamingOverlay_8wekyb3d8bbwe\\TempState",
                "%localappdata%\\Packages\\Microsoft.GamingApp_8wekyb3d8bbwe\\TempState"
            ]
        },
        new()
        {
            Id = "update",
            NameKey = "DeepClean_Cat_UpdateCache",
            DescriptionKey = "DeepClean_Cat_UpdateCache_Desc",
            Glyph = "\uE777",
            DirectoryPaths =
            [
                "%windir%\\SoftwareDistribution\\Download",
                "%windir%\\SoftwareDistribution\\DataStore",
                "%windir%\\SoftwareDistribution\\DeliveryOptimization",
                "%programdata%\\USOPrivate\\UpdateStore",
                "%programdata%\\USOShared\\Logs"
            ],
            ServicesToStop = ["wuauserv", "bits", "dosvc"]
        },
        new()
        {
            Id = "explorer",
            NameKey = "DeepClean_Cat_ExplorerCache",
            DescriptionKey = "DeepClean_Cat_ExplorerCache_Desc",
            Glyph = "\uE8B9",
            DirectoryPaths =
            [
                "%localappdata%\\Microsoft\\Windows\\INetCache",
                "%localappdata%\\Microsoft\\Windows\\WebCache",
                "%localappdata%\\Microsoft\\Windows\\History",
                "%localappdata%\\ConnectedDevicesPlatform"
            ],
            CleanCommands =
            [
                "del /F /Q %localappdata%\\IconCache.db",
                "del /F /S /Q %localappdata%\\Microsoft\\Windows\\Explorer\\iconcache*",
                "del /F /S /Q %localappdata%\\Microsoft\\Windows\\Explorer\\thumbcache*",
                "del /F /Q %appdata%\\Microsoft\\Windows\\Recent\\AutomaticDestinations\\*",
                "del /F /Q %appdata%\\Microsoft\\Windows\\Recent\\CustomDestinations\\*"
            ],
            RequiresExplorerRestart = true
        },
        new()
        {
            Id = "crashes",
            NameKey = "DeepClean_Cat_CrashDumps",
            DescriptionKey = "DeepClean_Cat_CrashDumps_Desc",
            Glyph = "\uEBE8",
            DirectoryPaths =
            [
                "%programdata%\\Microsoft\\Windows\\WER\\ReportQueue",
                "%programdata%\\Microsoft\\Windows\\WER\\ReportArchive",
                "%programdata%\\Microsoft\\Windows\\WER\\Temp",
                "%programdata%\\Microsoft\\Diagnosis",
                "%localappdata%\\CrashDumps",
                "%windir%\\LiveKernelReports"
            ]
        },
        new()
        {
            Id = "browser",
            NameKey = "DeepClean_Cat_BrowserCache",
            DescriptionKey = "DeepClean_Cat_BrowserCache_Desc",
            Glyph = "\uE774",
            DirectoryPaths =
            [
                "%localappdata%\\Microsoft\\EdgeWebView"
            ],
            CleanCommands =
            [
                "PowerShell.exe -NoProfile -Command \"& { Get-ChildItem -Path \\\"$env:LOCALAPPDATA\\Google\\Chrome\\User Data\\\", \\\"$env:LOCALAPPDATA\\Microsoft\\Edge\\User Data\\\", \\\"$env:LOCALAPPDATA\\BraveSoftware\\Brave-Browser\\User Data\\\" -Recurse -Include 'Cache','Code Cache','GPUCache','ShaderCache','Service Worker' -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue }\"",
                "PowerShell.exe -NoProfile -Command \"& { Get-ChildItem -Path \\\"$env:APPDATA\\Mozilla\\Firefox\\Profiles\\\", \\\"$env:LOCALAPPDATA\\Mozilla\\Firefox\\Profiles\\\" -Recurse -Include 'cache2' -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue }\""
            ]
        },
        new()
        {
            Id = "shader",
            NameKey = "DeepClean_Cat_ShaderCache",
            DescriptionKey = "DeepClean_Cat_ShaderCache_Desc",
            Glyph = "\uF211",
            DirectoryPaths =
            [
                "%localappdata%\\NVIDIA\\GLCache",
                "%localappdata%\\NVIDIA\\DXCache",
                "%localappdata%\\AMD\\DxCache",
                "%localappdata%\\D3DSCache",
                "%localappdata%\\Local\\DirectXShaderCache",
                "%localappdata%\\Steam\\htmlcache"
            ]
        },
        new()
        {
            Id = "syslogs",
            NameKey = "DeepClean_Cat_SystemLogs",
            DescriptionKey = "DeepClean_Cat_SystemLogs_Desc",
            Glyph = "\uE9D9",
            DirectoryPaths =
            [
                "%windir%\\WinSxS\\Temp\\PendingDeletes",
                "%windir%\\WinSxS\\Temp\\PendingRenames",
                "%programdata%\\Microsoft\\Search\\Data\\Applications\\Windows"
            ],
            CleanCommands =
            [
                "del /F /S /Q %windir%\\Logs\\CBS\\*",
                "del /F /S /Q %windir%\\Logs\\DISM\\*",
                "del /F /S /Q %windir%\\Panther\\*",
                "del /F /S /Q %appdata%\\TeamViewer\\*.log",
                "del /F /S /Q %appdata%\\AnyDesk\\*.trace"
            ],
            ServicesToStop = ["WSearch"]
        },
        new()
        {
            Id = "oemlogs",
            NameKey = "DeepClean_Cat_OEMLogs",
            DescriptionKey = "DeepClean_Cat_OEMLogs_Desc",
            Glyph = "\uE770",
            DirectoryPaths =
            [
                // Dell
                "%ProgramData%\\Dell\\SARemediation\\log",
                "%ProgramData%\\Dell\\SupportAssist\\logs",
                "%ProgramData%\\Dell\\SupportAssistAgent\\logs",
                "%ProgramData%\\Dell\\UpdateService\\Log",
                "%LocalAppData%\\Dell\\Logs",
                // HP
                "%ProgramData%\\HP\\SupportFramework\\Logs",
                "%ProgramData%\\HP\\Logs",
                "%LocalAppData%\\HP\\Logs",
                "%ProgramData%\\HP\\HP Support Assistant\\Logs",
                // Lenovo
                "%ProgramData%\\Lenovo\\ImController\\Logs",
                "%ProgramData%\\Lenovo\\SystemUpdate\\Logs",
                "%ProgramData%\\Lenovo\\Vantage\\Logs",
                "%ProgramData%\\Lenovo\\Modern\\Logs",
                "%LocalAppData%\\Lenovo\\Logs",
                // ASUS
                "%ProgramData%\\ASUS\\ASUS System Control Interface\\Log",
                "%ProgramData%\\ASUS\\Logs",
                "%LocalAppData%\\ASUS\\Logs",
                "%ProgramData%\\ASUS\\ArmouryCrate\\Logs",
                // Acer
                "%ProgramData%\\Acer\\Care Center\\Log",
                "%ProgramData%\\Acer\\Logs",
                "%LocalAppData%\\Acer\\Logs",
                // Enterprise
                "%windir%\\CCM\\Logs",
                "%windir%\\ccmsetup\\Logs"
            ]
        },
        new()
        {
            Id = "prefetch",
            NameKey = "DeepClean_Cat_Prefetch",
            DescriptionKey = "DeepClean_Cat_Prefetch_Desc",
            Glyph = "\uE916",
            DirectoryPaths =
            [
                "%windir%\\Prefetch"
            ]
        },
        new()
        {
            Id = "network",
            NameKey = "DeepClean_Cat_NetworkCache",
            DescriptionKey = "DeepClean_Cat_NetworkCache_Desc",
            Glyph = "\uE968",
            CleanCommands =
            [
                "ipconfig /flushdns",
                "netsh winsock reset"
            ]
        },
        new()
        {
            Id = "recyclebin",
            NameKey = "DeepClean_Cat_RecycleBin",
            DescriptionKey = "DeepClean_Cat_RecycleBin_Desc",
            Glyph = "\uE74D",
            IsRecycleBin = true,
            CleanCommands =
            [
                "PowerShell.exe -NoProfile -Command \"Clear-RecycleBin -Force -ErrorAction SilentlyContinue\""
            ]
        },
        new()
        {
            Id = "fontcache",
            NameKey = "DeepClean_Cat_FontCache",
            DescriptionKey = "DeepClean_Cat_FontCache_Desc",
            Glyph = "\uE8D2",
            DirectoryPaths =
            [
                "%windir%\\ServiceProfiles\\LocalService\\AppData\\Local\\FontCache"
            ],
            ServicesToStop = ["FontCache"]
        }
    ];

    // Scans a single category to estimate its reclaimable size
    public static async Task ScanCategoryAsync(CleanupCategory category)
    {
        long total = 0;

        await Task.Run(() =>
        {
            // Standard directory scanning
            foreach (var rawPath in category.DirectoryPaths)
            {
                var path = Environment.ExpandEnvironmentVariables(rawPath);
                total += GetDirectorySize(path);
            }

            // Special scanning per category type
            switch (category.Id)
            {
                case "browser":
                    total += ScanBrowserCaches();
                    break;
                case "explorer":
                    total += ScanExplorerCacheFiles();
                    break;
                case "recyclebin":
                    total += GetRecycleBinSize();
                    break;
            }
        }).ConfigureAwait(false);

        category.SizeBytes = total;
        category.SizeScanned = true;
    }

    // Cleans all provided categories, handling service and Explorer lifecycle
    public static async Task<(bool Success, long BytesCleaned)> CleanCategoriesAsync(List<CleanupCategory> categories)
    {
        _ = LogHelper.Log($"DeepClean: starting with {categories.Count} categories");
        long totalCleaned = 0;

        try
        {
            // Collect all unique services to stop across selected categories
            var allServices = categories
                .SelectMany(c => c.ServicesToStop)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Stop all required services upfront
            foreach (var service in allServices)
            {
                _ = LogHelper.Log($"DeepClean: stopping service {service}");
                await OptimizationOptions.StartInCmd($"net stop {service} /y").ConfigureAwait(false);
            }

            // Separate explorer-dependent and normal categories
            var explorerCategories = categories.Where(c => c.RequiresExplorerRestart).ToList();
            var normalCategories = categories.Where(c => !c.RequiresExplorerRestart).ToList();

            // Process explorer-dependent categories with Explorer killed
            if (explorerCategories.Count > 0)
            {
                _ = LogHelper.Log("DeepClean: killing Explorer for locked file access");
                await OptimizationOptions.StartInCmd("taskkill /F /IM explorer.exe").ConfigureAwait(false);

                foreach (var cat in explorerCategories)
                {
                    totalCleaned += await CleanSingleCategoryAsync(cat).ConfigureAwait(false);
                }

                _ = LogHelper.Log("DeepClean: restarting Explorer");
                await OptimizationOptions.StartInCmd("start %SystemRoot%\\explorer.exe").ConfigureAwait(false);
            }

            // Process remaining categories
            foreach (var cat in normalCategories)
            {
                totalCleaned += await CleanSingleCategoryAsync(cat).ConfigureAwait(false);
            }

            // Restart all services
            foreach (var service in allServices)
            {
                _ = LogHelper.Log($"DeepClean: restarting service {service}");
                await OptimizationOptions.StartInCmd($"net start {service}").ConfigureAwait(false);
            }

            _ = LogHelper.Log($"DeepClean: completed successfully, cleaned {FormatBytes(totalCleaned)}");
            return (true, totalCleaned);
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"DeepClean error: {ex.Message}");

            // Safety: ensure Explorer is running
            if (Process.GetProcessesByName("explorer").Length == 0)
            {
                await OptimizationOptions.StartInCmd("start %SystemRoot%\\explorer.exe").ConfigureAwait(false);
            }

            // Attempt to restart all services from selected categories
            var allServices = categories.SelectMany(c => c.ServicesToStop).Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var service in allServices)
            {
                try { await OptimizationOptions.StartInCmd($"net start {service}").ConfigureAwait(false); } catch { }
            }

            return (false, totalCleaned);
        }
    }

    // Cleans a single category: deletes directories then runs commands
    private static async Task<long> CleanSingleCategoryAsync(CleanupCategory category)
    {
        _ = LogHelper.Log($"DeepClean: cleaning category '{category.Id}'");
        long cleaned = 0;

        // Delete directories
        foreach (var rawPath in category.DirectoryPaths)
        {
            var path = Environment.ExpandEnvironmentVariables(rawPath);
            try
            {
                if (Directory.Exists(path))
                {
                    var size = GetDirectorySize(path);
                    Directory.Delete(path, true);
                    cleaned += size;
                }
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogWarning($"DeepClean: could not delete '{path}': {ex.Message}");
            }
        }

        // Run shell commands
        foreach (var cmd in category.CleanCommands)
        {
            try
            {
                await OptimizationOptions.StartInCmd(cmd).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogWarning($"DeepClean: command failed: {ex.Message}");
            }
        }

        return cleaned;
    }

    // Formats a byte count into a human-readable string
    public static string FormatBytes(long bytes)
    {
        if (bytes < 0) return "0 B";
        if (bytes < 1024) return $"{bytes} B";
        var kb = bytes / 1024.0;
        if (kb < 1024) return $"{kb:F1} KB";
        var mb = kb / 1024.0;
        if (mb < 1024) return $"{mb:F1} MB";
        var gb = mb / 1024.0;
        return $"{gb:F2} GB";
    }

    // Recursively calculates the total size of a directory
    private static long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path)) return 0;
        long size = 0;
        try
        {
            foreach (var file in new DirectoryInfo(path).EnumerateFiles("*", new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint
            }))
            {
                try { size += file.Length; } catch { }
            }
        }
        catch { }
        return size;
    }

    // Queries the Recycle Bin size via the Shell API
    private static long GetRecycleBinSize()
    {
        try
        {
            var info = new SHQUERYRBINFO { cbSize = (uint)Marshal.SizeOf<SHQUERYRBINFO>() };
            return SHQueryRecycleBin(null, ref info) == 0 ? info.i64Size : 0;
        }
        catch { return 0; }
    }

    // Scans browser cache directories across Chrome, Edge, Brave, and Firefox
    private static long ScanBrowserCaches()
    {
        long total = 0;
        var cacheNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Cache", "Code Cache", "GPUCache", "ShaderCache", "Service Worker"
        };

        // Chromium-based browsers
        var chromiumUserDataPaths = new[]
        {
            Environment.ExpandEnvironmentVariables("%localappdata%\\Google\\Chrome\\User Data"),
            Environment.ExpandEnvironmentVariables("%localappdata%\\Microsoft\\Edge\\User Data"),
            Environment.ExpandEnvironmentVariables("%localappdata%\\BraveSoftware\\Brave-Browser\\User Data"),
        };

        foreach (var userDataPath in chromiumUserDataPaths)
        {
            if (!Directory.Exists(userDataPath)) continue;
            try
            {
                foreach (var profileDir in Directory.GetDirectories(userDataPath))
                {
                    foreach (var cacheName in cacheNames)
                    {
                        var cachePath = Path.Combine(profileDir, cacheName);
                        if (Directory.Exists(cachePath))
                        {
                            total += GetDirectorySize(cachePath);
                        }
                    }
                }
            }
            catch { }
        }

        // Firefox
        var firefoxPaths = new[]
        {
            Environment.ExpandEnvironmentVariables("%appdata%\\Mozilla\\Firefox\\Profiles"),
            Environment.ExpandEnvironmentVariables("%localappdata%\\Mozilla\\Firefox\\Profiles"),
        };

        foreach (var profilesPath in firefoxPaths)
        {
            if (!Directory.Exists(profilesPath)) continue;
            try
            {
                foreach (var profileDir in Directory.GetDirectories(profilesPath))
                {
                    var cache2Path = Path.Combine(profileDir, "cache2");
                    if (Directory.Exists(cache2Path))
                    {
                        total += GetDirectorySize(cache2Path);
                    }
                }
            }
            catch { }
        }

        return total;
    }

    // Scans Explorer-specific cache files (icon cache, thumbnail cache, recent items)
    private static long ScanExplorerCacheFiles()
    {
        long total = 0;

        // Icon and thumbnail cache files in Explorer directory
        var explorerDir = Environment.ExpandEnvironmentVariables("%localappdata%\\Microsoft\\Windows\\Explorer");
        if (Directory.Exists(explorerDir))
        {
            try
            {
                foreach (var file in Directory.GetFiles(explorerDir))
                {
                    var name = Path.GetFileName(file).ToLowerInvariant();
                    if (name.StartsWith("iconcache") || name.StartsWith("thumbcache"))
                    {
                        try { total += new FileInfo(file).Length; } catch { }
                    }
                }
            }
            catch { }
        }

        // IconCache.db
        var iconCacheDb = Environment.ExpandEnvironmentVariables("%localappdata%\\IconCache.db");
        try { if (File.Exists(iconCacheDb)) total += new FileInfo(iconCacheDb).Length; } catch { }

        // Recent items
        var recentPaths = new[]
        {
            Environment.ExpandEnvironmentVariables("%appdata%\\Microsoft\\Windows\\Recent\\AutomaticDestinations"),
            Environment.ExpandEnvironmentVariables("%appdata%\\Microsoft\\Windows\\Recent\\CustomDestinations"),
        };
        foreach (var path in recentPaths)
        {
            total += GetDirectorySize(path);
        }

        return total;
    }
}
