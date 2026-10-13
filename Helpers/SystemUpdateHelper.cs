using Microsoft.Win32;
using Windows.Storage;

namespace RyTuneX.Helpers;

public record WindowsBuildInfo(int BuildNumber, int UBR, string FullBuildString)
{
    public static WindowsBuildInfo Empty => new(0, 0, "0.0");

    public bool IsNewerThan(WindowsBuildInfo other)
    {
        if (BuildNumber > other.BuildNumber) return true;
        if (BuildNumber == other.BuildNumber && UBR > other.UBR) return true;
        return false;
    }

    public bool IsOlderThan(WindowsBuildInfo other)
    {
        if (BuildNumber < other.BuildNumber) return true;
        if (BuildNumber == other.BuildNumber && UBR < other.UBR) return true;
        return false;
    }
}

public record SystemUpdateCheckResult(bool HasUpdated, WindowsBuildInfo PreviousBuild, WindowsBuildInfo CurrentBuild);

public static class SystemUpdateHelper
{
    private const string CurrentVersionRegKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
    private const string RyTuneXBaseRegKey = @"SOFTWARE\RyTuneX";
    private const string SettingKeyLastBuild = "LastWindowsBuild";
    private const string SettingKeyLastUBR = "LastWindowsUBR";
    private const string SettingKeyDetectUpdates = "DetectSystemUpdates";

    public static bool IsUpdatePending { get; set; }
    public static SystemUpdateCheckResult? PendingUpdateResult { get; set; }

    public static bool IsDetectionEnabled
    {
        get
        {
            try
            {
                var settings = ApplicationData.Current.LocalSettings.Values;
                if (settings.TryGetValue(SettingKeyDetectUpdates, out var val) && val is bool b)
                {
                    return b;
                }
            }
            catch { }
            return true;
        }
        set
        {
            try
            {
                ApplicationData.Current.LocalSettings.Values[SettingKeyDetectUpdates] = value;
            }
            catch { }
        }
    }

    public static WindowsBuildInfo GetCurrentWindowsBuild()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(CurrentVersionRegKey);
            if (key != null)
            {
                var buildStr = key.GetValue("CurrentBuildNumber") as string ?? key.GetValue("CurrentBuild") as string ?? "0";
                int.TryParse(buildStr, out int build);

                int ubr = 0;
                var ubrVal = key.GetValue("UBR");
                if (ubrVal != null)
                {
                    _ = int.TryParse(ubrVal.ToString(), out ubr);
                }

                return new WindowsBuildInfo(build, ubr, ubr > 0 ? $"{build}.{ubr}" : $"{build}");
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"GetCurrentWindowsBuild failed: {ex.Message}");
        }

        return WindowsBuildInfo.Empty;
    }

    public static WindowsBuildInfo? GetSavedWindowsBuild()
    {
        // Try LocalSettings first
        try
        {
            var settings = ApplicationData.Current.LocalSettings.Values;
            if (settings.TryGetValue(SettingKeyLastBuild, out var buildObj) && buildObj is int build && build > 0)
            {
                int ubr = 0;
                if (settings.TryGetValue(SettingKeyLastUBR, out var ubrObj) && ubrObj is int u)
                {
                    ubr = u;
                }
                return new WindowsBuildInfo(build, ubr, ubr > 0 ? $"{build}.{ubr}" : $"{build}");
            }
        }
        catch { }

        // Fallback to Registry HKLM\SOFTWARE\RyTuneX
        try
        {
            var regView = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                ? RegistryView.Registry64
                : RegistryView.Default;
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, regView);
            using var rytunexKey = baseKey.OpenSubKey(RyTuneXBaseRegKey);
            if (rytunexKey != null)
            {
                var buildVal = rytunexKey.GetValue(SettingKeyLastBuild);
                if (buildVal is int build && build > 0)
                {
                    int ubr = rytunexKey.GetValue(SettingKeyLastUBR) is int u ? u : 0;
                    return new WindowsBuildInfo(build, ubr, ubr > 0 ? $"{build}.{ubr}" : $"{build}");
                }
            }
        }
        catch { }

        return null;
    }

    public static void SaveWindowsBuild(WindowsBuildInfo buildInfo)
    {
        if (buildInfo == null || buildInfo.BuildNumber <= 0) return;

        try
        {
            var settings = ApplicationData.Current.LocalSettings.Values;
            settings[SettingKeyLastBuild] = buildInfo.BuildNumber;
            settings[SettingKeyLastUBR] = buildInfo.UBR;
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogWarning($"Failed to save Windows build to LocalSettings: {ex.Message}");
        }

        try
        {
            var regView = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                ? RegistryView.Registry64
                : RegistryView.Default;
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, regView);
            using var rytunexKey = baseKey.CreateSubKey(RyTuneXBaseRegKey);
            rytunexKey?.SetValue(SettingKeyLastBuild, buildInfo.BuildNumber, RegistryValueKind.DWord);
            rytunexKey?.SetValue(SettingKeyLastUBR, buildInfo.UBR, RegistryValueKind.DWord);
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogWarning($"Failed to save Windows build to Registry: {ex.Message}");
        }
    }

    public static SystemUpdateCheckResult CheckForSystemUpdate()
    {
        var current = GetCurrentWindowsBuild();
        if (current.BuildNumber <= 0)
        {
            return new SystemUpdateCheckResult(false, WindowsBuildInfo.Empty, WindowsBuildInfo.Empty);
        }

        var saved = GetSavedWindowsBuild();

        if (saved == null || saved.BuildNumber <= 0)
        {
            // First run on this installation: record current build as baseline
            _ = LogHelper.Log($"[SystemUpdateHelper] First launch baseline recorded: Build {current.FullBuildString}");
            SaveWindowsBuild(current);
            return new SystemUpdateCheckResult(false, current, current);
        }

        if (!IsDetectionEnabled)
        {
            // Detection disabled by user in settings: keep build updated silently
            SaveWindowsBuild(current);
            return new SystemUpdateCheckResult(false, saved, current);
        }

        if (current.IsNewerThan(saved))
        {
            _ = LogHelper.Log($"[SystemUpdateHelper] Windows build increment detected: {saved.FullBuildString} -> {current.FullBuildString}");

            var savedOptCount = OptimizationOptions.GetSavedOptimizationsCount();
            if (savedOptCount > 0)
            {
                IsUpdatePending = true;
                var result = new SystemUpdateCheckResult(true, saved, current);
                PendingUpdateResult = result;
                return result;
            }
            else
            {
                _ = LogHelper.Log("[SystemUpdateHelper] System updated, but no saved optimizations found in registry. Updating baseline.");
                SaveWindowsBuild(current);
                return new SystemUpdateCheckResult(false, saved, current);
            }
        }
        else if (current.IsOlderThan(saved))
        {
            _ = LogHelper.Log($"[SystemUpdateHelper] OS rollback detected ({saved.FullBuildString} -> {current.FullBuildString}). Updating baseline.");
            SaveWindowsBuild(current);
            return new SystemUpdateCheckResult(false, saved, current);
        }

        return new SystemUpdateCheckResult(false, saved, current);
    }
}
