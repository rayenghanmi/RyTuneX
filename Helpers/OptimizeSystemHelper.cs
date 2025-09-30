namespace RyTuneX.Helpers;
public static partial class OptimizeSystemHelper
{
    public static async Task DisableWindowsRecall()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI\" /v DisableAIDataAnalysis /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI\" /v DisableAIDataAnalysis /t REG_DWORD /d 1 /f");
    }

    public static async Task EnableWindowsRecall()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI\" /v DisableAIDataAnalysis /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI\" /v DisableAIDataAnalysis /f");
    }

    public static async Task DisableRecommendedSectionStartMenu()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Start\" /v HideRecommendedSection /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Education\" /v IsEducationEnvironment /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer\" /v HideRecommendedSection /t REG_DWORD /d 1 /f");
    }

    public static async Task EnableRecommendedSectionStartMenu()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Start\" /v HideRecommendedSection /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Education\" /v IsEducationEnvironment /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer\" /v HideRecommendedSection /f");
    }

    public static async Task DisableWPBT()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\" /v DisableWpbtExecution /t REG_DWORD /d 1 /f");
    }

    public static async Task EnableWPBT()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\" /v DisableWpbtExecution /t REG_DWORD /d 0 /f");
    }

    public static async Task EnablePrioritizeForegroundApplications()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl\" /v Win32PrioritySeparation /t REG_DWORD /d 38 /f");
    }

    public static async Task DisablePrioritizeForegroundApplications()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl\" /v Win32PrioritySeparation /t REG_DWORD /d 2 /f");
    }

    public static async Task DisablePagingSettings()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\" /v DisablePagingExecutive /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\" /v DisablePageCombining /t REG_DWORD /d 1 /f");
    }

    public static async Task EnablePagingSettings()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\" /v DisablePagingExecutive /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\" /v DisablePageCombining /t REG_DWORD /d 0 /f");
    }

    public static async Task EnableOptimizeNTFS()
    {
        await OptimizationOptions.StartInCmd("fsutil behavior set disablelastaccess 1");
        await OptimizationOptions.StartInCmd("fsutil behavior set disable8dot3 1");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem\" /v NtfsMftZoneReservation /t REG_DWORD /d 2 /f");
    }

    public static async Task DisableOptimizeNTFS()
    {
        await OptimizationOptions.StartInCmd("fsutil behavior set disablelastaccess 0");
        await OptimizationOptions.StartInCmd("fsutil behavior set disable8dot3 0");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem\" /v NtfsMftZoneReservation /t REG_DWORD /d 1 /f");
    }

    public static async Task EnableLegacyBootMenu()
    {
        await OptimizationOptions.StartInCmd("bcdedit /set bootmenupolicy legacy");
    }

    public static async Task DisableLegacyBootMenu()
    {
        await OptimizationOptions.StartInCmd("bcdedit /set bootmenupolicy standard");
    }

    public static async Task DisableServiceHostSplitting()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\" /v SvcHostSplitThresholdInKB /t REG_DWORD /d 4294967295 /f");
    }

    public static async Task EnableServiceHostSplitting()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SYSTEM\\CurrentControlSet\\Control\" /v SvcHostSplitThresholdInKB /f");
    }

    public static async Task DisableMenuShowDelay()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Control Panel\\Desktop\" /v MenuShowDelay /t REG_SZ /d 0 /f");
    }

    public static async Task DisableMouseHoverTime()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Control Panel\\Mouse\" /v MouseHoverTime /t REG_SZ /d 0 /f");
    }

    public static async Task DisableBackgroundApps()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications\" /v GlobalUserDisabled /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v BackgroundAppGlobalToggle /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy\" /v LetAppsRunInBackground /t REG_DWORD /d 0 /f");
    }

    public static async Task EnableAutoComplete()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\AutoComplete\" /v \"Append Completion\" /t REG_SZ /d yes /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\AutoComplete\" /v AutoSuggest /t REG_SZ /d yes /f");
    }

    public static async Task EnableCrashDump()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\CrashControl\" /v CrashDumpEnabled /t REG_DWORD /d 3 /f");
    }

    public static async Task DisableRemoteAssistance()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\System\\CurrentControlSet\\Control\\Remote Assistance\" /v fAllowToGetHelp /t REG_DWORD /d 0 /f");
    }

    public static async Task DisableWindowShake()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v DisallowShaking /t REG_DWORD /d 1 /f");
    }

    public static async Task AddCopyMoveContextMenu()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Classes\\AllFilesystemObjects\\shellex\\ContextMenuHandlers\\Copy To\" /ve /d \"{C2FBB630-2971-11D1-A18C-00C04FD75D13}\" /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Classes\\AllFilesystemObjects\\shellex\\ContextMenuHandlers\\Move To\" /ve /d \"{C2FBB631-2971-11D1-A18C-00C04FD75D13}\" /f");
    }

    public static async Task AdjustTaskTimeouts()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Control Panel\\Desktop\" /v AutoEndTasks /t REG_SZ /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Control Panel\\Desktop\" /v HungAppTimeout /t REG_SZ /d 1000 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Control Panel\\Desktop\" /v WaitToKillAppTimeout /t REG_SZ /d 2000 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Control Panel\\Desktop\" /v LowLevelHooksTimeout /t REG_SZ /d 1000 /f");
    }

    public static async Task EnableLowDiskSpaceChecks()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoLowDiskSpaceChecks /t REG_DWORD /d 00000000 /f");
    }

    public static async Task DisableLinkResolve()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v LinkResolveIgnoreLinkInfo /t REG_DWORD /d 00000001 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoResolveSearch /t REG_DWORD /d 00000001 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoResolveTrack /t REG_DWORD /d 00000001 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoInternetOpenWith /t REG_DWORD /d 00000001 /f");
    }

    public static async Task DecreaseServiceTimeouts()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\" /v WaitToKillServiceTimeout /t REG_SZ /d 2000 /f");
    }

    public static async Task DisableRemoteRegistry()
    {
        await OptimizationOptions.StartInCmd("sc config \"RemoteRegistry\" start= disabled");
    }

    public static async Task HideFileExtensionsAndHiddenFiles()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v HideFileExt /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v Hidden /t REG_DWORD /d 1 /f");
    }

    public static async Task OptimizeSystemProfile()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v SystemResponsiveness /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v NoLazyMode /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v AlwaysOn /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v NetworkThrottlingIndex /t REG_DWORD /d 0xffffffff /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"GPU Priority\" /t REG_DWORD /d 8 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v Priority /t REG_DWORD /d 8 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"Scheduling Category\" /t REG_SZ /d High /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"SFIO Priority\" /t REG_SZ /d High /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency\" /v \"GPU Priority\" /t REG_DWORD /d 8 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency\" /v Priority /t REG_DWORD /d 8 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency\" /v \"Scheduling Category\" /t REG_SZ /d High /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency\" /v \"SFIO Priority\" /t REG_SZ /d High /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\" /v HwSchMode /t REG_DWORD /d 2 /f");
    }

    public static async Task EnableMenuShowDelay()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Control Panel\\Desktop\" /v MenuShowDelay /f");
    }

    public static async Task EnableMouseHoverTime()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Control Panel\\Mouse\" /v MouseHoverTime /f");
    }

    public static async Task EnableBackgroundApps()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications\" /v GlobalUserDisabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v BackgroundAppGlobalToggle /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy\" /v LetAppsRunInBackground /f");
    }

    public static async Task DisableAutoComplete()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\AutoComplete\" /v \"Append Completion\" /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\AutoComplete\" /v AutoSuggest /f");
    }

    public static async Task DisableCrashDump()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\CrashControl\" /v CrashDumpEnabled /f");
    }

    public static async Task EnableRemoteAssistance()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\System\\CurrentControlSet\\Control\\Remote Assistance\" /v fAllowToGetHelp /f");
    }

    public static async Task EnableWindowShake()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v DisallowShaking /f");
    }

    public static async Task RemoveCopyMoveContextMenu()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Classes\\AllFilesystemObjects\\shellex\\ContextMenuHandlers\\Copy To\" /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Classes\\AllFilesystemObjects\\shellex\\ContextMenuHandlers\\Move To\" /f");
    }

    public static async Task IncreaseTaskTimeouts()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Control Panel\\Desktop\" /v AutoEndTasks /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Control Panel\\Desktop\" /v HungAppTimeout /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Control Panel\\Desktop\" /v WaitToKillAppTimeout /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Control Panel\\Desktop\" /v LowLevelHooksTimeout /f");
    }

    public static async Task DisableLowDiskSpaceChecks()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoLowDiskSpaceChecks /t REG_DWORD /d 00000001 /f");
    }

    public static async Task EnableLinkResolve()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v LinkResolveIgnoreLinkInfo /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoResolveSearch /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoResolveTrack /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoInternetOpenWith /f");
    }

    public static async Task RevertServiceTimeouts()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SYSTEM\\CurrentControlSet\\Control\" /v WaitToKillServiceTimeout /f");
    }

    public static async Task EnableRemoteRegistry()
    {
        await OptimizationOptions.StartInCmd("sc config \"RemoteRegistry\" start= demand");
    }

    public static async Task ShowFileExtensionsAndHiddenFiles()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v HideFileExt /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v Hidden /t REG_DWORD /d 0 /f");
    }

    public static async Task RevertSystemProfile()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v SystemResponsiveness /t REG_DWORD /d 20 /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v NoLazyMode /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v AlwaysOn /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v NetworkThrottlingIndex /t REG_DWORD /d 10 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"GPU Priority\" /t REG_DWORD /d 2 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v Priority /t REG_DWORD /d 2 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"Scheduling Category\" /t REG_SZ /d Medium /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"SFIO Priority\" /t REG_SZ /d Normal /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency\" /v \"GPU Priority\" /t REG_DWORD /d 2 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency\" /v Priority /t REG_DWORD /d 2 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency\" /v \"Scheduling Category\" /t REG_SZ /d Medium /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency\" /v \"SFIO Priority\" /t REG_SZ /d Normal /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\" /v HwSchMode /f");
    }

    internal static async Task DisableTelemetryServices()
    {
        await OptimizationOptions.StartInCmd("sc stop DiagTrack");
        await OptimizationOptions.StartInCmd("sc stop diagnosticshub.standardcollector.service");
        await OptimizationOptions.StartInCmd("sc stop dmwappushservice");
        await OptimizationOptions.StartInCmd("sc stop DcpSvc");
        await OptimizationOptions.StartInCmd("sc stop WdiServiceHost");
        await OptimizationOptions.StartInCmd("sc stop WdiSystemHost");

        string[] services = {
        "DiagTrack",
        "diagnosticshub.standardcollector.service",
        "dmwappushservice",
        "DcpSvc",
        "WdiServiceHost",
        "WdiSystemHost"
        };

        foreach (var svc in services)
        {
            await OptimizationOptions.StartInCmd($"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\{svc}\" /v Start /t REG_DWORD /d 4 /f");
        }

        string[] appCompatKeys = {
        "DisableEngine", "SbEnable", "AITEnable",
        "DisableInventory", "DisablePCA", "DisableUAR"
        };
        int[] appCompatValues = { 1, 0, 0, 1, 1, 1 };

        for (var i = 0; i < appCompatKeys.Length; i++)
        {
            await OptimizationOptions.StartInCmd($"reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppCompat\" /v {appCompatKeys[i]} /t REG_DWORD /d {appCompatValues[i]} /f");
            if (Environment.Is64BitOperatingSystem)
            {
                await OptimizationOptions.StartInCmd($"reg add \"HKLM\\SOFTWARE\\Wow6432Node\\Policies\\Microsoft\\Windows\\AppCompat\" /v {appCompatKeys[i]} /t REG_DWORD /d {appCompatValues[i]} /f");
            }
        }

        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection\" /v AllowTelemetry /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\SQMClient\\Windows\" /v CEIPEnable /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Siuf\\Rules\" /v NumberOfSIUFInPeriod /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\Software\\Microsoft\\PolicyManager\\default\\WiFi\\AllowAutoConnectToWiFiSenseHotspots\" /v value /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\Software\\Microsoft\\PolicyManager\\default\\WiFi\\AllowWiFiHotSpotReporting\" /v value /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Device Metadata\" /v PreventDeviceMetadataFromNetwork /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\MRT\" /v DontOfferThroughWUAU /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\WMI\\AutoLogger\\SQMLogger\" /v Start /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\System\" /v AllowExperimentation /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\" /v PublishUserActivities /t REG_DWORD /d 0 /f");

        string[] tasks = {
        "Microsoft\\Windows\\Application Experience\\Microsoft Compatibility Appraiser",
        "Microsoft\\Windows\\Application Experience\\ProgramDataUpdater",
        "Microsoft\\Windows\\Autochk\\Proxy",
        "Microsoft\\Windows\\Customer Experience Improvement Program\\Consolidator",
        "Microsoft\\Windows\\Customer Experience Improvement Program\\UsbCeip",
        "Microsoft\\Windows\\Customer Experience Improvement Program\\BthSQM",
        "Microsoft\\Windows\\DiskDiagnostic\\Microsoft-Windows-DiskDiagnosticDataCollector"
        };

        foreach (var task in tasks)
        {
            await OptimizationOptions.StartInCmd($"schtasks /Change /TN \"{task}\" /Disable");
        }

        var hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");

        string[] safeTelemetryHosts = {
        "vortex-win.data.microsoft.com",
        "settings-win.data.microsoft.com",
        "telemetry.microsoft.com",
        "watson.telemetry.microsoft.com",
        "oca.telemetry.microsoft.com",
        "sqm.telemetry.microsoft.com",
        "sqm.ppe.telemetry.microsoft.com",
        "watson.ppe.telemetry.microsoft.com",
        "df.telemetry.microsoft.com",
        "diagnostics.support.microsoft.com",
        "oca.microsoft.com",
        "oca.telemetry.microsoft.com.nsatc.net",
        "redir.metaservices.microsoft.com",
        "choice.microsoft.com",
        "choice.microsoft.com.nsatc.net",
        "ceuswatcab01.blob.core.windows.net"
        };

        var lines = new List<string>();
        lines.AddRange(File.ReadAllLines(hostsPath));

        foreach (var host in safeTelemetryHosts)
        {
            var entry = $"0.0.0.0 {host}";
            if (!lines.Contains(entry))
            {
                lines.Add(entry);
            }
        }

        File.WriteAllLines(hostsPath, lines);
    }

    internal static async Task EnableTelemetryServices()
    {
        string[] services = {
        "DiagTrack",
        "diagnosticshub.standardcollector.service",
        "dmwappushservice",
        "DcpSvc",
        "WdiSystemHost",
        "WdiServiceHost"
        };

        foreach (var svc in services)
        {
            await OptimizationOptions.StartInCmd($"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\{svc}\" /v Start /t REG_DWORD /d 2 /f");
            await OptimizationOptions.StartInCmd($"sc start {svc}");
        }

        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection\" /v AllowTelemetry /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\SQMClient\\Windows\" /v CEIPEnable /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\" /v PublishUserActivities /t REG_DWORD /d 1 /f");

        string[] tasks = {
        "Microsoft\\Windows\\Application Experience\\Microsoft Compatibility Appraiser",
        "Microsoft\\Windows\\Application Experience\\ProgramDataUpdater",
        "Microsoft\\Windows\\Autochk\\Proxy",
        "Microsoft\\Windows\\Customer Experience Improvement Program\\Consolidator",
        "Microsoft\\Windows\\Customer Experience Improvement Program\\UsbCeip",
        "Microsoft\\Windows\\Customer Experience Improvement Program\\BthSQM",
        "Microsoft\\Windows\\DiskDiagnostic\\Microsoft-Windows-DiskDiagnosticDataCollector"
        };

        foreach (var task in tasks)
        {
            await OptimizationOptions.StartInCmd($"schtasks /Change /TN \"{task}\" /Enable");
        }
        var hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");

        string[] safeTelemetryHosts = {
        "vortex-win.data.microsoft.com",
        "settings-win.data.microsoft.com",
        "telemetry.microsoft.com",
        "watson.telemetry.microsoft.com",
        "oca.telemetry.microsoft.com",
        "sqm.telemetry.microsoft.com",
        "sqm.ppe.telemetry.microsoft.com",
        "watson.ppe.telemetry.microsoft.com",
        "df.telemetry.microsoft.com",
        "diagnostics.support.microsoft.com",
        "oca.microsoft.com",
        "oca.telemetry.microsoft.com.nsatc.net",
        "redir.metaservices.microsoft.com",
        "choice.microsoft.com",
        "choice.microsoft.com.nsatc.net",
        "ceuswatcab01.blob.core.windows.net"
        };

        var lines = new List<string>(File.ReadAllLines(hostsPath));

        foreach (var host in safeTelemetryHosts)
        {
            var entry = $"0.0.0.0 {host}";
            lines.RemoveAll(line => line.Trim().Equals(entry, StringComparison.OrdinalIgnoreCase));
        }

        File.WriteAllLines(hostsPath, lines);
    }

    internal static async Task DisableMediaPlayerSharing()
    {
        await OptimizationOptions.StartInCmd("sc stop WMPNetworkSvc");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\WMPNetworkSvc\" /v Start /t REG_DWORD /d 4 /f");

    }

    internal static async Task EnableMediaPlayerSharing()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\WMPNetworkSvc\" /v Start /t REG_DWORD /d 2 /f");
        await OptimizationOptions.StartInCmd("sc start WMPNetworkSvc");

    }

    internal static async Task DisableHomeGroup()
    {
        await OptimizationOptions.StartInCmd("sc stop HomeGroupListener");
        await OptimizationOptions.StartInCmd("sc stop HomeGroupProvider");

        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\HomeGroup\" /v DisableHomeGroup /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\HomeGroupListener\" /v Start /t REG_DWORD /d 4 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\HomeGroupProvider\" /v Start /t REG_DWORD /d 4 /f");

    }

    internal static async Task EnableHomeGroup()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\HomeGroupListener\" /v Start /t REG_DWORD /d 2 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\HomeGroupProvider\" /v Start /t REG_DWORD /d 2 /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\HomeGroup\" /v DisableHomeGroup /f");
        await OptimizationOptions.StartInCmd("sc start HomeGroupListener");
        await OptimizationOptions.StartInCmd("sc start HomeGroupProvider");
    }

    internal static async Task DisablePrintService()
    {
        await OptimizationOptions.StartInCmd("sc stop Spooler");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\Spooler\" /v Start /t REG_DWORD /d 4 /f");
    }

    internal static async Task EnablePrintService()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\Spooler\" /v Start /t REG_DWORD /d 2 /f");
        await OptimizationOptions.StartInCmd("cmd /c sc start Spooler");
    }

    internal static async Task DisableSysMain()
    {
        await OptimizationOptions.StartInCmd("sc stop SysMain");

        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\SysMain\" /v Start /t REG_DWORD /d 4 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters\" /v EnableSuperfetch /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters\" /v EnablePrefetcher /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters\" /v SfTracingState /t REG_DWORD /d 1 /f");
    }

    internal static async Task EnableSysMain()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\SysMain\" /v Start /t REG_DWORD /d 2 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters\" /v EnableSuperfetch /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters\" /v EnablePrefetcher /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters\" /v SfTracingState /f");

        await OptimizationOptions.StartInCmd("sc start SysMain");
    }

    internal static async Task EnableCompatibilityAssistant()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\PcaSvc\" /v Start /t REG_DWORD /d 2 /f");
        await OptimizationOptions.StartInCmd("sc start PcaSvc");
    }

    internal static async Task DisableCompatibilityAssistant()
    {
        await OptimizationOptions.StartInCmd("sc stop PcaSvc");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\PcaSvc\" /v Start /t REG_DWORD /d 4 /f");
    }

    internal static async Task DisableWindowsTransparency()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v EnableTransparency /t REG_DWORD /d 0 /f");
    }
    internal static async Task EnableWindowsTransparency()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v EnableTransparency /t REG_DWORD /d 1 /f");
    }

    internal static async Task EnableWindowsDarkMode()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v AppsUseLightTheme /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v SystemUsesLightTheme /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("taskkill /f /im explorer.exe");
        await OptimizationOptions.StartInCmd("start explorer.exe");
    }
    internal static async Task DisableWindowsDarkMode()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v AppsUseLightTheme /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v SystemUsesLightTheme /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("taskkill /f /im explorer.exe");
        await OptimizationOptions.StartInCmd("start explorer.exe");
    }

    internal static async Task EnableVerboseLogon()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v VerboseStatus /t REG_DWORD /d 1 /f");
    }

    internal static async Task DisableVerboseLogon()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v VerboseStatus /t REG_DWORD /d 0 /f");
    }

    internal static async Task EnableClassicContextMenu()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\\InprocServer32\" /f");
        await OptimizationOptions.StartInCmd("taskkill /F /IM explorer.exe & start explorer");
    }

    internal static async Task DisableClassicContextMenu()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\" /f");
        await OptimizationOptions.StartInCmd("taskkill /F /IM explorer.exe & start explorer");
    }

    internal static async Task DisableSystemRestore()
    {
        await OptimizationOptions.StartInCmd("vssadmin delete shadows /for=c: /all /quiet");
        await OptimizationOptions.StartInCmd("sc stop VSS");

        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\SystemRestore\" /v DisableSR /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\SystemRestore\" /v DisableConfig /t REG_DWORD /d 1 /f");
    }

    internal static async Task EnableSystemRestore()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\SystemRestore\" /v DisableSR /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\SystemRestore\" /v DisableConfig /f");

        await OptimizationOptions.StartInCmd("sc start VSS");
    }

    internal static async Task DisableSearch()
    {
        await OptimizationOptions.StartInCmd("sc stop WSearch");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\WSearch\" /v Start /t REG_DWORD /d 4 /f");
    }

    internal static async Task EnableSearch()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\WSearch\" /v Start /t REG_DWORD /d 2 /f");
        await OptimizationOptions.StartInCmd("sc start WSearch");
    }

    internal static async Task DisableSMB(string v)
    {
        await OptimizationOptions.StartInCmd($"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\LanmanServer\\Parameters\" /v SMB{v} /t REG_DWORD /d 0 /f");
    }

    internal static async Task EnableSMB(string v)
    {
        await OptimizationOptions.StartInCmd($"reg delete \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\LanmanServer\\Parameters\" /v SMB{v} /f");
    }

    internal static async Task DisableErrorReporting()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Error Reporting\" /v Disabled /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\PCHealth\\ErrorReporting\" /v DoReport /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\" /v Disabled /t REG_DWORD /d 1 /f");

        await OptimizationOptions.StartInCmd("sc stop WerSvc");
        await OptimizationOptions.StartInCmd("sc stop wercplsupport");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\WerSvc\" /v Start /t REG_DWORD /d 4 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\wercplsupport\" /v Start /t REG_DWORD /d 4 /f");
    }

    internal static async Task EnableErrorReporting()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Error Reporting\" /v Disabled /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\PCHealth\\ErrorReporting\" /v DoReport /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\" /v Disabled /f");

        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\wercplsupport\" /v Start /t REG_DWORD /d 3 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\WerSvc\" /v Start /t REG_DWORD /d 2 /f");
        await OptimizationOptions.StartInCmd("sc start WerSvc");
        await OptimizationOptions.StartInCmd("sc start wercplsupport");
    }

    internal static async Task EnableLegacyVolumeSlider()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\Software\\Microsoft\\Windows NT\\CurrentVersion\\MTCUVC\" /v EnableMtcUvc /t REG_DWORD /d 1 /f");
    }

    internal static async Task DisableLegacyVolumeSlider()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\Software\\Microsoft\\Windows NT\\CurrentVersion\\MTCUVC\" /v EnableMtcUvc /t REG_DWORD /d 0 /f");
    }

    internal static async Task DisableCortana()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\SearchSettings\" /v IsDeviceSearchHistoryEnabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v AllowCortana /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v DisableWebSearch /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v ConnectedSearchUseWeb /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v ConnectedSearchUseWebOverMeteredConnections /t REG_DWORD /d 0 /f");

        await OptimizationOptions.StartInCmd("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Search\" /v HistoryViewEnabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Search\" /v DeviceHistoryEnabled /t REG_DWORD /d 0 /f");

        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v AllowSearchToUseLocation /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v BingSearchEnabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v CortanaConsent /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v AllowCloudSearch /t REG_DWORD /d 0 /f");
    }

    internal static async Task EnableCortana()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v AllowCortana /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v DisableWebSearch /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v ConnectedSearchUseWeb /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v ConnectedSearchUseWebOverMeteredConnections /t REG_DWORD /d 1 /f");

        await OptimizationOptions.StartInCmd("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Search\" /v HistoryViewEnabled /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Search\" /v DeviceHistoryEnabled /t REG_DWORD /d 1 /f");

        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v AllowSearchToUseLocation /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v BingSearchEnabled /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v CortanaConsent /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v AllowCloudSearch /t REG_DWORD /d 1 /f");
    }

    internal static async Task EnableGamingMode()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\" /v HwSchMode /t REG_DWORD /d 2 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\GameBar\" /v AllowAutoGameMode /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\GameBar\" /v AutoGameModeEnabled /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\System\\GameConfigStore\" /v GameDVR_FSEBehaviorMode /t REG_DWORD /d 2 /f");
    }

    internal static async Task DisableGamingMode()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\" /v HwSchMode /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\GameBar\" /v AllowAutoGameMode /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\GameBar\" /v AutoGameModeEnabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\System\\GameConfigStore\" /v GameDVR_FSEBehaviorMode /t REG_DWORD /d 0 /f");
    }

    internal static async Task DisableAutomaticUpdates()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v NoAutoUpdate /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\" /v DoNotConnectToWindowsUpdateInternetLocations /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v AUOptions /t REG_DWORD /d 2 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v NoAutoRebootWithLoggedOnUsers /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\DeliveryOptimization\\Config\" /v DODownloadMode /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\DoSvc\" /v Start /t REG_DWORD /d 4 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Speech\" /v AllowSpeechModelUpdate /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Schedule\\Maintenance\" /v MaintenanceDisabled /t REG_DWORD /d 1 /f");

        await OptimizationOptions.StartInCmd("sc config wuauserv start= disabled");
        await OptimizationOptions.StartInCmd("sc config UsoSvc start= disabled");
        await OptimizationOptions.StartInCmd("sc config BITS start= disabled");
        await OptimizationOptions.StartInCmd("sc config WaaSMedicSvc start= disabled");
        await OptimizationOptions.StartInCmd("sc config DoSvc start= disabled");

        await OptimizationOptions.StartInCmd("sc stop wuauserv");
        await OptimizationOptions.StartInCmd("sc stop UsoSvc");
        await OptimizationOptions.StartInCmd("sc stop BITS");
        await OptimizationOptions.StartInCmd("sc stop WaaSMedicSvc");
        await OptimizationOptions.StartInCmd("sc stop DoSvc");
    }

    internal static async Task EnableAutomaticUpdates()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\" /v DoNotConnectToWindowsUpdateInternetLocations /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v AUOptions /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v NoAutoUpdate /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v NoAutoRebootWithLoggedOnUsers /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\DeliveryOptimization\\Config\" /v DODownloadMode /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Speech\" /v AllowSpeechModelUpdate /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Schedule\\Maintenance\" /v MaintenanceDisabled /f");

        await OptimizationOptions.StartInCmd("sc config wuauserv start= demand");
        await OptimizationOptions.StartInCmd("sc config UsoSvc start= demand");
        await OptimizationOptions.StartInCmd("sc config BITS start= demand");
        await OptimizationOptions.StartInCmd("sc config WaaSMedicSvc start= demand");
        await OptimizationOptions.StartInCmd("sc config DoSvc start= demand");

        await OptimizationOptions.StartInCmd("sc start wuauserv");
        await OptimizationOptions.StartInCmd("sc start UsoSvc");
        await OptimizationOptions.StartInCmd("sc start BITS");
        await OptimizationOptions.StartInCmd("sc start WaaSMedicSvc");
        await OptimizationOptions.StartInCmd("sc start DoSvc");
    }

    internal static async Task DisableStoreUpdates()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v SilentInstalledAppsEnabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent\" /v DisableSoftLanding /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v PreInstalledAppsEnabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent\" /v DisableWindowsConsumerFeatures /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v OemPreInstalledAppsEnabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsStore\" /v AutoDownload /t REG_DWORD /d 2 /f");
    }

    internal static async Task EnableStoreUpdates()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v SilentInstalledAppsEnabled /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent\" /v DisableSoftLanding /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v PreInstalledAppsEnabled /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent\" /v DisableWindowsConsumerFeatures /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v OemPreInstalledAppsEnabled /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsStore\" /v AutoDownload /f");
    }


    internal static async Task DisableOneDrive()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\OneDrive /v DisableFileSyncNGSC /t REG_DWORD /d 1 /f");
    }

    internal static async Task EnableOneDrive()
    {
        await OptimizationOptions.StartInCmd("reg delete HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\OneDrive /v DisableFileSyncNGSC /f");
    }

    internal static async Task EnableSensorServices()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SYSTEM\\CurrentControlSet\\Services\\SensrSvc /v Start /t REG_DWORD /d 2 /f");
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SYSTEM\\CurrentControlSet\\Services\\SensorService /v Start /t REG_DWORD /d 2 /f");
        await OptimizationOptions.StartInCmd("sc start SensrSvc");
        await OptimizationOptions.StartInCmd("sc start SensorService");
    }

    internal static async Task DisableSensorServices()
    {
        await OptimizationOptions.StartInCmd("sc stop SensrSvc");
        await OptimizationOptions.StartInCmd("sc stop SensorService");
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SYSTEM\\CurrentControlSet\\Services\\SensrSvc /v Start /t REG_DWORD /d 4 /f");
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SYSTEM\\CurrentControlSet\\Services\\SensorService /v Start /t REG_DWORD /d 4 /f");
    }

    internal static async Task DisableNewsAndInterests()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Feeds /v EnableFeeds /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\default\\NewsAndInterests\\AllowNewsAndInterests /v value /t REG_DWORD /d 0 /f");
    }

    internal static async Task DisableSpotlightFeatures()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v RotatingLockScreenOverlayEnabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v RotatingLockScreenEnabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v DisableWindowsSpotlightFeatures /t REG_DWORD /d 1 /f");
    }

    internal static async Task DisableTailoredExperiences()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v DisableTailoredExperiencesWithDiagnosticData /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Privacy /v TailoredExperiencesWithDiagnosticDataEnabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("REG ADD HKEY_USERS\\.DEFAULT\\Software\\Microsoft\\Windows\\CurrentVersion\\Privacy /v TailoredExperiencesWithDiagnosticDataEnabled /t REG_DWORD /d 0 /f");
    }

    internal static async Task DisableCloudOptimizedContent()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent /v DisableCloudOptimizedContent /t REG_DWORD /d 1 /f");
    }

    internal static async Task DisableFeedbackNotifications()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection /v DoNotShowFeedbackNotifications /t REG_DWORD /d 1 /f");
    }

    internal static async Task DisableAdvertisingID()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo /v Enabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AdvertisingInfo /v DisabledByGroupPolicy /t REG_DWORD /d 1 /f");
    }

    internal static async Task DisableBluetoothAdvertising()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Bluetooth /v AllowAdvertising /t REG_DWORD /d 0 /f");
    }

    internal static async Task DisableAutomaticRestartSignOn()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System /v DisableAutomaticRestartSignOn /t REG_DWORD /d 1 /f");
    }

    internal static async Task DisableHandwritingDataSharing()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\TabletPC /v PreventHandwritingDataSharing /t REG_DWORD /d 1 /f");
    }

    internal static async Task DisableTextInputDataCollection()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\TextInput /v AllowLinguisticDataCollection /t REG_DWORD /d 0 /f");
    }

    internal static async Task DisableInputPersonalization()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\InputPersonalization /v AllowInputPersonalization /t REG_DWORD /d 0 /f");
    }

    internal static async Task DisableSafeSearchMode()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\SearchSettings /v SafeSearchMode /t REG_DWORD /d 0 /f");
    }

    internal static async Task DisableActivityUploads()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v UploadUserActivities /t REG_DWORD /d 0 /f");
    }

    internal static async Task DisableClipboardSync()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v AllowCrossDeviceClipboard /t REG_DWORD /d 0 /f");
    }

    internal static async Task DisableMessageSync()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\Software\\Policies\\Microsoft\\Windows\\Messaging /v AllowMessageSync /t REG_DWORD /d 0 /f");
    }

    internal static async Task DisableSettingSync()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\Software\\Policies\\Microsoft\\Windows\\SettingSync /v DisableCredentialsSettingSync /t REG_DWORD /d 2 /f");
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\Software\\Policies\\Microsoft\\Windows\\SettingSync /v DisableCredentialsSettingSyncUserOverride /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\Software\\Policies\\Microsoft\\Windows\\SettingSync /v DisableApplicationSettingSync /t REG_DWORD /d 2 /f");
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\Software\\Policies\\Microsoft\\Windows\\SettingSync /v DisableApplicationSettingSyncUserOverride /t REG_DWORD /d 1 /f");
    }

    internal static async Task DisableVoiceActivation()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy /v LetAppsActivateWithVoice /t REG_DWORD /d 2 /f");
    }

    internal static async Task DisableFindMyDevice()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\FindMyDevice /v AllowFindMyDevice /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Microsoft\\Settings\\FindMyDevice /v LocationSyncEnabled /t REG_DWORD /d 0 /f");
    }

    internal static async Task DisableActivityFeed()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v EnableActivityFeed /t REG_DWORD /d 0 /f");
    }

    internal static async Task DisableCdp()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v EnableCdp /t REG_DWORD /d 0 /f");
    }
    internal static async Task DisableDiagnosticsToast()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Diagnostics\\DiagTrack /v ShowedToastAtLevel /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("REG ADD HKEY_USERS\\.DEFAULT\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Diagnostics\\DiagTrack /v ShowedToastAtLevel /t REG_DWORD /d 1 /f");
    }

    internal static async Task DisableOnlineSpeechPrivacy()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Speech_OneCore\\Settings\\OnlineSpeechPrivacy /v HasAccepted /t REG_DWORD /d 0 /f");
    }

    internal static async Task DisableLocationFeatures()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\location /v Value /t REG_SZ /d Deny /f");
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\LocationAndSensors /v DisableLocation /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\LocationAndSensors /v DisableWindowsLocationProvider /t REG_DWORD /d 1 /f");
    }

    internal static async Task EnableNewsAndInterests()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Feeds /v EnableFeeds /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\default\\NewsAndInterests\\AllowNewsAndInterests /v value /t REG_DWORD /d 1 /f");
    }

    internal static async Task EnableSpotlightFeatures()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v RotatingLockScreenOverlayEnabled /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v RotatingLockScreenEnabled /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v DisableWindowsSpotlightFeatures /t REG_DWORD /d 0 /f");
    }

    internal static async Task EnableTailoredExperiences()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v DisableTailoredExperiencesWithDiagnosticData /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Privacy /v TailoredExperiencesWithDiagnosticDataEnabled /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("REG ADD HKEY_USERS\\.DEFAULT\\Software\\Microsoft\\Windows\\CurrentVersion\\Privacy /v TailoredExperiencesWithDiagnosticDataEnabled /t REG_DWORD /d 1 /f");
    }

    internal static async Task EnableCloudOptimizedContent()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent /v DisableCloudOptimizedContent /t REG_DWORD /d 0 /f");
    }

    internal static async Task EnableFeedbackNotifications()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection /v DoNotShowFeedbackNotifications /t REG_DWORD /d 0 /f");
    }

    internal static async Task EnableAdvertisingID()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo /v Enabled /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AdvertisingInfo /v DisabledByGroupPolicy /t REG_DWORD /d 0 /f");
    }

    internal static async Task EnableBluetoothAdvertising()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Bluetooth /v AllowAdvertising /t REG_DWORD /d 1 /f");
    }

    internal static async Task EnableAutomaticRestartSignOn()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System /v DisableAutomaticRestartSignOn /t REG_DWORD /d 0 /f");
    }

    internal static async Task EnableHandwritingDataSharing()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\TabletPC /v PreventHandwritingDataSharing /t REG_DWORD /d 0 /f");
    }

    internal static async Task EnableTextInputDataCollection()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\TextInput /v AllowLinguisticDataCollection /t REG_DWORD /d 1 /f");
    }

    internal static async Task EnableInputPersonalization()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\InputPersonalization /v AllowInputPersonalization /t REG_DWORD /d 1 /f");
    }

    internal static async Task EnableSafeSearchMode()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\SearchSettings /v SafeSearchMode /t REG_DWORD /d 1 /f");
    }

    internal static async Task EnableActivityUploads()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v UploadUserActivities /t REG_DWORD /d 1 /f");
    }

    internal static async Task EnableClipboardSync()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v AllowCrossDeviceClipboard /t REG_DWORD /d 1 /f");
    }

    internal static async Task EnableMessageSync()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\Software\\Policies\\Microsoft\\Windows\\Messaging /v AllowMessageSync /t REG_DWORD /d 1 /f");
    }

    internal static async Task EnableSettingSync()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\Software\\Policies\\Microsoft\\Windows\\SettingSync /v DisableCredentialsSettingSync /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\Software\\Policies\\Microsoft\\Windows\\SettingSync /v DisableCredentialsSettingSyncUserOverride /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\Software\\Policies\\Microsoft\\Windows\\SettingSync /v DisableApplicationSettingSync /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\Software\\Policies\\Microsoft\\Windows\\SettingSync /v DisableApplicationSettingSyncUserOverride /t REG_DWORD /d 0 /f");
    }

    internal static async Task EnableVoiceActivation()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy /v LetAppsActivateWithVoice /t REG_DWORD /d 1 /f");
    }

    internal static async Task EnableFindMyDevice()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\FindMyDevice /v AllowFindMyDevice /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Microsoft\\Settings\\FindMyDevice /v LocationSyncEnabled /t REG_DWORD /d 1 /f");
    }

    internal static async Task EnableActivityFeed()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v EnableActivityFeed /t REG_DWORD /d 1 /f");
    }

    internal static async Task EnableCdp()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v EnableCdp /t REG_DWORD /d 1 /f");
    }

    internal static async Task EnableDiagnosticsToast()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Diagnostics\\DiagTrack /v ShowedToastAtLevel /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("REG ADD HKEY_USERS\\.DEFAULT\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Diagnostics\\DiagTrack /v ShowedToastAtLevel /t REG_DWORD /d 0 /f");
    }

    internal static async Task EnableOnlineSpeechPrivacy()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Speech_OneCore\\Settings\\OnlineSpeechPrivacy /v HasAccepted /t REG_DWORD /d 1 /f");
    }

    internal static async Task EnableLocationFeatures()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\location /v Value /t REG_SZ /d Allow /f");
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\LocationAndSensors /v DisableLocation /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\LocationAndSensors /v DisableWindowsLocationProvider /t REG_DWORD /d 0 /f");
    }

    internal static async Task DisableBiometrics()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Biometrics /v Enabled /t REG_DWORD /d 0 /f");
    }

    internal static async Task EnableBiometrics()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Biometrics\" /v \"Enabled\" /f");
    }

    internal static async Task DisableGameBar()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR /v AppCaptureEnabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR /v AudioCaptureEnabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR /v CursorCaptureEnabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\GameBar /v UseNexusForGameBarEnabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\GameBar /v ShowStartupPanel /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\System\\GameConfigStore /v GameDVR_Enabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\Software\\Policies\\Microsoft\\Windows\\GameDVR /v AllowGameDVR /t REG_DWORD /d 0 /f");
    }

    internal static async Task EnableGameBar()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR /v AppCaptureEnabled /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR /v AudioCaptureEnabled /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR /v CursorCaptureEnabled /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\GameBar /v UseNexusForGameBarEnabled /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\GameBar /v ShowStartupPanel /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\System\\GameConfigStore /v GameDVR_Enabled /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\Software\\Policies\\Microsoft\\Windows\\GameDVR /v AllowGameDVR /t REG_DWORD /d 1 /f");
    }

    internal static async Task DisableQuickAccessHistory()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Feeds /v ShellFeedsTaskbarViewMode /t REG_DWORD /d 2 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Feeds /v IsFeedsAvailable /t REG_DWORD /d 0 /f");

        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced /v ShowTaskViewButton /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\OperationStatusManager /v EnthusiastMode /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced /v ShowSyncProviderNotifications /t REG_DWORD /d 0 /f");

        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer /v ShowFrequent /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer /v ShowRecent /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced /v LaunchTo /t REG_DWORD /d 1 /f");

        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer /v HideSCAMeetNow /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer /v HideSCAMeetNow /t REG_DWORD /d 1 /f");

        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\FileHistory /v Disabled /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\File History /v Disabled /t REG_DWORD /d 1 /f");
    }

    internal static async Task EnableQuickAccessHistory()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\OperationStatusManager /v EnthusiastMode /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced /v ShowSyncProviderNotifications /t REG_DWORD /d 1 /f");

        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer /v ShowFrequent /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer /v ShowRecent /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced /v LaunchTo /t REG_DWORD /d 2 /f");
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced /v ShowTaskViewButton /f");

        await OptimizationOptions.StartInCmd("reg delete HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\FileHistory /v Disabled /f");
        await OptimizationOptions.StartInCmd("reg delete HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\File History /v Disabled /f");

        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search /v SearchboxTaskbarMode /f");
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Feeds /v ShellFeedsTaskbarViewMode /f");
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Feeds /v IsFeedsAvailable /f");

        await OptimizationOptions.StartInCmd("reg delete HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer /v HideSCAMeetNow /f");
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer /v HideSCAMeetNow /f");
    }

    internal static async Task DisableStartMenuAds()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-88000326Enabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\UserProfileEngagement /v ScoobeSystemSettingEnabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v ContentDeliveryAllowed /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v RemediationRequired /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v PreInstalledAppsEverEnabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SilentInstalledAppsEnabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-314559Enabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-338387Enabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-338389Enabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SystemPaneSuggestionsEnabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-338393Enabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-353694Enabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-353696Enabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-310093Enabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-338388Enabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContentEnabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SoftLandingEnabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v FeatureManagementEnabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Policies\\Microsoft\\Windows\\Explorer /v DisableSearchBoxSuggestions /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer /v AllowOnlineTips /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer /v DisableSearchBoxSuggestions /t REG_DWORD /d 1 /f");
    }

    internal static async Task EnableStartMenuAds()
    {
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-88000326Enabled /f");
        await OptimizationOptions.StartInCmd("reg delete HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\UserProfileEngagement /v ScoobeSystemSettingEnabled /f");
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v ContentDeliveryAllowed /f");
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v RemediationRequired /f");
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v PreInstalledAppsEverEnabled /f");
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SilentInstalledAppsEnabled /f");
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-314559Enabled /f");
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-338387Enabled /f");
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-338389Enabled /f");
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SystemPaneSuggestionsEnabled /f");
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-338393Enabled /f");
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-353694Enabled /f");
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-353696Enabled /f");
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-310093Enabled /f");
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContentEnabled /f");
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-338388Enabled /f");
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SoftLandingEnabled /f");
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v FeatureManagementEnabled /f");
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Policies\\Microsoft\\Windows\\Explorer /v DisableSearchBoxSuggestions /f");
        await OptimizationOptions.StartInCmd("reg delete HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer /v AllowOnlineTips /f");
        await OptimizationOptions.StartInCmd("reg delete HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer /v DisableSearchBoxSuggestions /f");
    }

    internal static async Task DisableMyPeople()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\People /v PeopleBand /t REG_DWORD /d 0 /f");
    }

    internal static async Task EnableMyPeople()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\People /v PeopleBand /t REG_DWORD /d 1 /f");
    }

    internal static async Task ExcludeDrivers()
    {
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\default\\Update\\ExcludeWUDriversInQualityUpdate /v value /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\default\\Update /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Update /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Device Metadata /v PreventDeviceMetadataFromNetwork /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\DriverSearching /v SearchOrderConfig /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\DriverSearching /v DontSearchWindowsUpdate /t REG_DWORD /d 1 /f");
    }

    internal static async Task IncludeDrivers()
    {
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\default\\Update\\ExcludeWUDriversInQualityUpdate /v value /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\default\\Update /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Update /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Device Metadata /v PreventDeviceMetadataFromNetwork /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\DriverSearching /v SearchOrderConfig /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\DriverSearching /v DontSearchWindowsUpdate /t REG_DWORD /d 0 /f");
    }

    internal static async Task DisableWindowsInk()
    {
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsInkWorkspace /v AllowWindowsInkWorkspace /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsInkWorkspace /v AllowSuggestedAppsInWindowsInkWorkspace /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableInkingWithTouch /t REG_DWORD /d 0 /f");
    }

    internal static async Task EnableWindowsInk()
    {
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsInkWorkspace /v AllowWindowsInkWorkspace /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsInkWorkspace /v AllowSuggestedAppsInWindowsInkWorkspace /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableInkingWithTouch /t REG_DWORD /d 1 /f");
    }

    internal static async Task DisableSpellingAndTypingFeatures()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableAutocorrection /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableSpellchecking /t REG_DWORD /d 0 /f");

        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Input\\Settings /v InsightsEnabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableDoubleTapSpace /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnablePredictionSpaceInsertion /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableTextPrediction /t REG_DWORD /d 0 /f");
    }

    internal static async Task EnableSpellingAndTypingFeatures()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableAutocorrection /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableSpellchecking /t REG_DWORD /d 1 /f");

        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Input\\Settings /v InsightsEnabled /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableDoubleTapSpace /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnablePredictionSpaceInsertion /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableTextPrediction /t REG_DWORD /d 1 /f");
    }

    internal static async Task EnableFaxService()
    {
        await OptimizationOptions.StartInCmd("reg add HKLM\\SYSTEM\\CurrentControlSet\\Services\\Fax /v Start /t REG_DWORD /d 3 /f");
    }

    internal static async Task DisableFaxService()
    {
        await OptimizationOptions.StartInCmd("sc stop Fax");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SYSTEM\\CurrentControlSet\\Services\\Fax /v Start /t REG_DWORD /d 4 /f");
    }

    internal static async Task EnableInsiderService()
    {
        await OptimizationOptions.StartInCmd("reg add HKLM\\SYSTEM\\CurrentControlSet\\Services\\wisvc /v Start /t REG_DWORD /d 3 /f");
        await OptimizationOptions.StartInCmd("sc start wisvc");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\PreviewBuilds /v AllowBuildPreview /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\PreviewBuilds /v EnableConfigFlighting /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\PreviewBuilds /v EnableExperimentation /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg delete HKLM\\SOFTWARE\\Microsoft\\WindowsSelfHost\\UI\\Visibility /v HideInsiderPage /f");
    }

    internal static async Task DisableInsiderService()
    {
        await OptimizationOptions.StartInCmd("sc stop wisvc");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SYSTEM\\CurrentControlSet\\Services\\wisvc /v Start /t REG_DWORD /d 4 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\PreviewBuilds /v AllowBuildPreview /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\PreviewBuilds /v EnableConfigFlighting /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\PreviewBuilds /v EnableExperimentation /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\WindowsSelfHost\\UI\\Visibility /v HideInsiderPage /t REG_DWORD /d 1 /f");
    }


    internal static async Task DisableSmartScreen()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Attachments /v SaveZoneInformation /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Attachments /v ScanWithAntiVirus /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v ShellSmartScreenLevel /t REG_SZ /d Warn /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v EnableSmartScreen /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer /v SmartScreenEnabled /t REG_SZ /d Off /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\Internet Explorer\\PhishingFilter /v EnabledV9 /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\AppHost /v PreventOverride /t REG_DWORD /d 0 /f");

        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Notifications\\Settings\\Windows.SystemToast.SecurityAndMaintenance /v Enabled /t REG_DWORD /d 0 /f");
    }

    internal static async Task EnableSmartScreen()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Attachments /v SaveZoneInformation /t REG_DWORD /d 2 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Attachments /v ScanWithAntiVirus /t REG_DWORD /d 2 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v EnableSmartScreen /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer /v SmartScreenEnabled /t REG_SZ /d On /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\Internet Explorer\\PhishingFilter /v EnabledV9 /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\AppHost /v PreventOverride /f");

    }

    internal static async Task DisableCloudClipboard()
    {
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v AllowClipboardHistory /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v AllowCrossDeviceClipboard /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Clipboard /v EnableClipboardHistory /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add HKLM\\Software\\Microsoft\\Clipboard /v EnableClipboardHistory /t REG_DWORD /d 0 /f");
    }

    internal static async Task EnableCloudClipboard()
    {
        await OptimizationOptions.StartInCmd("reg delete HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v AllowClipboardHistory /f");
        await OptimizationOptions.StartInCmd("reg delete HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v AllowCrossDeviceClipboard /f");
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Clipboard /v EnableClipboardHistory /f");
        await OptimizationOptions.StartInCmd("reg delete HKLM\\Software\\Microsoft\\Clipboard /v EnableClipboardHistory /f");
    }

    internal static async Task DisableStickyKeys()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\Control Panel\\Accessibility\\StickyKeys /v Flags /t REG_SZ /d 506 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Control Panel\\Accessibility\\Keyboard Response /v Flags /t REG_SZ /d 122 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Control Panel\\Accessibility\\ToggleKeys /v Flags /t REG_SZ /d 58 /f");
        await OptimizationOptions.StartInCmd("reg add HKEY_USERS\\.DEFAULT\\Control Panel\\Accessibility\\StickyKeys /v Flags /t REG_SZ /d 506 /f");
        await OptimizationOptions.StartInCmd("reg add HKEY_USERS\\.DEFAULT\\Control Panel\\Accessibility\\Keyboard Response /v Flags /t REG_SZ /d 122 /f");
        await OptimizationOptions.StartInCmd("reg add HKEY_USERS\\.DEFAULT\\Control Panel\\Accessibility\\ToggleKeys /v Flags /t REG_SZ /d 58 /f");
    }

    internal static async Task EnableStickyKeys()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\Control Panel\\Accessibility\\StickyKeys /v Flags /t REG_SZ /d 510 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Control Panel\\Accessibility\\Keyboard Response /v Flags /t REG_SZ /d 126 /f");
        await OptimizationOptions.StartInCmd("reg add HKCU\\Control Panel\\Accessibility\\ToggleKeys /v Flags /t REG_SZ /d 62 /f");
        await OptimizationOptions.StartInCmd("reg add HKEY_USERS\\.DEFAULT\\Control Panel\\Accessibility\\StickyKeys /v Flags /t REG_SZ /d 510 /f");
        await OptimizationOptions.StartInCmd("reg add HKEY_USERS\\.DEFAULT\\Control Panel\\Accessibility\\Keyboard Response /v Flags /t REG_SZ /d 126 /f");
        await OptimizationOptions.StartInCmd("reg add HKEY_USERS\\.DEFAULT\\Control Panel\\Accessibility\\ToggleKeys /v Flags /t REG_SZ /d 62 /f");
    }

    internal static async Task RemoveCastToDevice()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked\" /V {7AD84985-87B4-4a16-BE58-8B72A5B390F7} /T REG_SZ /D \"Play to Menu\" /F");
    }

    internal static async Task AddCastToDevice()
    {
        await OptimizationOptions.StartInCmd("REG DELETE \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked\" /V {7AD84985-87B4-4a16-BE58-8B72A5B390F7} /F");
    }

    internal static async Task DisableVBS()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\" /V EnableVirtualizationBasedSecurity /T REG_DWORD /D 0 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity\" /V Enabled /T REG_DWORD /D 0 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\KernelShadowStacks\" /V Enabled /T REG_DWORD /D 0 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\CredentialGuard\" /V Enabled /T REG_DWORD /D 0 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Lsa\" /V RunAsPPL /T REG_DWORD /D 0 /F");
    }

    internal static async Task EnableVBS()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\" /V EnableVirtualizationBasedSecurity /T REG_DWORD /D 1 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity\" /V Enabled /T REG_DWORD /D 1 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\KernelShadowStacks\" /V Enabled /T REG_DWORD /D 1 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\CredentialGuard\" /V Enabled /T REG_DWORD /D 1 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Lsa\" /V RunAsPPL /T REG_DWORD /D 1 /F");
    }

    internal static async Task AlignTaskbarToLeft()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v TaskbarAl /t REG_DWORD /d 0 /f");
    }

    internal static async Task AlignTaskbarToCenter()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v TaskbarAl /t REG_DWORD /d 1 /f");
    }

    internal static async Task DisableSnapAssist()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V EnableSnapAssistFlyout /T REG_DWORD /D 0 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V EnableSnapBar /T REG_DWORD /D 0 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Control Panel\\Desktop\" /V DockMoving /T REG_SZ /D 0 /F");
    }

    internal static async Task EnableSnapAssist()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V EnableSnapAssistFlyout /T REG_DWORD /D 1 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V EnableSnapBar /T REG_DWORD /D 1 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Control Panel\\Desktop\" /V DockMoving /T REG_SZ /D 1 /F");
    }

    internal static async Task DisableWidgets()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V TaskbarDa /T REG_DWORD /D 0 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Feeds\" /V EnableFeeds /T REG_DWORD /D 0 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Dsh\" /V AllowNewsAndInterests /T REG_DWORD /D 0 /F");
    }

    internal static async Task EnableWidgets()
    {
        await OptimizationOptions.StartInCmd("REG Delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V TaskbarDa /F");
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Feeds\" /V EnableFeeds /F");
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Dsh\" /V AllowNewsAndInterests /F");
    }

    internal static async Task DisableChat()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V TaskbarMn /T REG_DWORD /D 0 /F");
    }

    internal static async Task EnableChat()
    {
        await OptimizationOptions.StartInCmd("REG Delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V TaskbarMn /F");
    }

    internal static async Task DisableShowMoreOptions()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\\InprocServer32\" /V \"\" /F");
    }

    internal static async Task EnableShowMoreOptions()
    {
        await OptimizationOptions.StartInCmd("REG Delete \"HKCU\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\" /F");
    }

    internal static async Task EnableFilesCompactMode()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V UseCompactMode /T REG_DWORD /D 1 /F");
    }

    internal static async Task DisableFilesCompactMode()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V UseCompactMode /T REG_DWORD /D 0 /F");
    }

    internal static async Task DisableStickers()
    {
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Stickers\" /V EnableStickers /F");
    }

    internal static async Task EnableStickers()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Stickers\" /V EnableStickers /T REG_DWORD /D 1 /F");
    }


    internal static async Task DisableEdgeDiscoverBar()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V HubsSidebarEnabled /T REG_DWORD /D 0 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V HubsSidebarEnabled /T REG_DWORD /D 0 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V WebWidgetAllowed /T REG_DWORD /D 0 /F");
    }

    internal static async Task EnableEdgeDiscoverBar()
    {
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V HubsSidebarEnabled /F");
        await OptimizationOptions.StartInCmd("REG Delete \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V HubsSidebarEnabled /F");
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V WebWidgetAllowed /F");
    }

    internal static async Task DisableEdgeTelemetry()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V PersonalizationReportingEnabled /T REG_DWORD /D 0 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V PersonalizationReportingEnabled /T REG_DWORD /D 0 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V UserFeedbackAllowed /T REG_DWORD /D 0 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V UserFeedbackAllowed /T REG_DWORD /D 0 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V MetricsReportingEnabled /T REG_DWORD /D 0 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V MetricsReportingEnabled /T REG_DWORD /D 0 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\MicrosoftEdge\\BooksLibrary\" /V EnableExtendedBooksTelemetry /T REG_DWORD /D 0 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\SOFTWARE\\Policies\\Microsoft\\MicrosoftEdge\\BooksLibrary\" /V EnableExtendedBooksTelemetry /T REG_DWORD /D 0 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\Edge\" /V SmartScreenEnabled /T REG_DWORD /D 0 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\Edge\" /V SmartScreenPuaEnabled /T REG_DWORD /D 0 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V SpotlightExperiencesAndRecommendationsEnabled /T REG_DWORD /D 0 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V SpotlightExperiencesAndRecommendationsEnabled /T REG_DWORD /D 0 /F");
    }

    internal static async Task EnableEdgeTelemetry()
    {
        await OptimizationOptions.StartInCmd("REG Delete \"HKCU\\Software\\Microsoft\\Edge\" /V SmartScreenEnabled /F");
        await OptimizationOptions.StartInCmd("REG Delete \"HKCU\\Software\\Microsoft\\Edge\" /V SmartScreenPuaEnabled /F");
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V MetricsReportingEnabled /F");
        await OptimizationOptions.StartInCmd("REG Delete \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V MetricsReportingEnabled /F");
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\MicrosoftEdge\\BooksLibrary\" /V EnableExtendedBooksTelemetry /F");
        await OptimizationOptions.StartInCmd("REG Delete \"HKCU\\SOFTWARE\\Policies\\Microsoft\\MicrosoftEdge\\BooksLibrary\" /V EnableExtendedBooksTelemetry /F");
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V PersonalizationReportingEnabled /F");
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V UserFeedbackAllowed /F");
        await OptimizationOptions.StartInCmd("REG Delete \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V PersonalizationReportingEnabled /F");
        await OptimizationOptions.StartInCmd("REG Delete \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V UserFeedbackAllowed /F");
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V SpotlightExperiencesAndRecommendationsEnabled /F");
        await OptimizationOptions.StartInCmd("REG Delete \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V SpotlightExperiencesAndRecommendationsEnabled /F");
    }


    internal static async Task DisableCoPilotAI()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Policies\\Microsoft\\Windows\\WindowsCopilot\" /V TurnOffWindowsCopilot /T REG_DWORD /D 1 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V ShowCopilotButton /T REG_DWORD /D 0 /F");
        await OptimizationOptions.StartInCmd("powershell \"Get-AppxPackage *Copilot* | Remove-AppxPackage\"");
    }

    internal static async Task EnableCoPilotAI()
    {
        await OptimizationOptions.StartInCmd("REG Delete \"HKCU\\Software\\Policies\\Microsoft\\Windows\\WindowsCopilot\" /V TurnOffWindowsCopilot /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V ShowCopilotButton /T REG_DWORD /D 1 /F");
        await OptimizationOptions.StartInCmd("powershell \"Get-AppxPackage *Copilot* | Add-AppxPackage\"");
    }


    internal static async Task DisableVisualStudioTelemetry()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\VisualStudio\\Telemetry\" /V TurnOffSwitch /T REG_DWORD /D 1 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\Feedback\" /V DisableFeedbackDialog /T REG_DWORD /D 1 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\Feedback\" /V DisableEmailInput /T REG_DWORD /D 1 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\Feedback\" /V DisableScreenshotCapture /T REG_DWORD /D 1 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\SQM\" /V OptIn /T REG_DWORD /D 0 /F");
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\Setup\" /V ConcurrentDownloads /T REG_DWORD /D 2 /F");

        if (Environment.Is64BitOperatingSystem)
        {
            await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Microsoft\\VSCommon\\14.0\\SQM\" /V OptIn /T REG_DWORD /D 0 /F");
            await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Microsoft\\VSCommon\\15.0\\SQM\" /V OptIn /T REG_DWORD /D 0 /F");
            await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Microsoft\\VSCommon\\16.0\\SQM\" /V OptIn /T REG_DWORD /D 0 /F");
        }
        else
        {
            await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Wow6432Node\\Microsoft\\VSCommon\\14.0\\SQM\" /V OptIn /T REG_DWORD /D 0 /F");
            await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Wow6432Node\\Microsoft\\VSCommon\\15.0\\SQM\" /V OptIn /T REG_DWORD /D 0 /F");
            await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Wow6432Node\\Microsoft\\VSCommon\\16.0\\SQM\" /V OptIn /T REG_DWORD /D 0 /F");
        }
        await OptimizationOptions.StartInCmd("SC Config VSStandardCollectorService150 Start= disabled");
    }

    internal static async Task EnableVisualStudioTelemetry()
    {
        await OptimizationOptions.StartInCmd("REG Delete \"HKCU\\Software\\Microsoft\\VisualStudio\\Telemetry\" /V TurnOffSwitch /F");
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\Feedback\" /V DisableFeedbackDialog /F");
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\Feedback\" /V DisableEmailInput /F");
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\Feedback\" /V DisableScreenshotCapture /F");
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\SQM\" /V OptIn /F");
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\Setup\" /V ConcurrentDownloads /F");

        if (Environment.Is64BitOperatingSystem)
        {
            await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Microsoft\\VSCommon\\14.0\\SQM\" /V OptIn /F");
            await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Microsoft\\VSCommon\\15.0\\SQM\" /V OptIn /F");
            await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Microsoft\\VSCommon\\16.0\\SQM\" /V OptIn /F");
        }
        else
        {
            await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Wow6432Node\\Microsoft\\VSCommon\\14.0\\SQM\" /V OptIn /F");
            await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Wow6432Node\\Microsoft\\VSCommon\\15.0\\SQM\" /V OptIn /F");
            await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Wow6432Node\\Microsoft\\VSCommon\\16.0\\SQM\" /V OptIn /F");
        }
        await OptimizationOptions.StartInCmd("SC Config VSStandardCollectorService150 Start= demand");
    }

    internal static async Task DisableNvidiaTelemetry()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\NvTelemetryContainer\" /v Start /t REG_DWORD /d 4 /f");
        await OptimizationOptions.StartInCmd("schtasks.exe /change /tn NvTmRepOnLogon_{B2FE1952-0186-46C3-BAEC-A80AA35AC5B8} /disable");
        await OptimizationOptions.StartInCmd("schtasks.exe /change /tn NvTmRep_{B2FE1952-0186-46C3-BAEC-A80AA35AC5B8} /disable");
        await OptimizationOptions.StartInCmd("schtasks.exe /change /tn NvTmMon_{B2FE1952-0186-46C3-BAEC-A80AA35AC5B8} /disable");
        await OptimizationOptions.StartInCmd("net.exe stop NvTelemetryContainer");
        await OptimizationOptions.StartInCmd("sc.exe config NvTelemetryContainer start= disabled");
        await OptimizationOptions.StartInCmd("sc.exe stop NvTelemetryContainer");
    }

    internal static async Task EnableNvidiaTelemetry()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\NvTelemetryContainer\" /v Start /t REG_DWORD /d 2 /f");
        await OptimizationOptions.StartInCmd("schtasks.exe /change /tn NvTmRepOnLogon_{B2FE1952-0186-46C3-BAEC-A80AA35AC5B8} /enable");
        await OptimizationOptions.StartInCmd("schtasks.exe /change /tn NvTmRep_{B2FE1952-0186-46C3-BAEC-A80AA35AC5B8} /enable");
        await OptimizationOptions.StartInCmd("schtasks.exe /change /tn NvTmMon_{B2FE1952-0186-46C3-BAEC-A80AA35AC5B8} /enable");
        await OptimizationOptions.StartInCmd("net.exe start NvTelemetryContainer");
        await OptimizationOptions.StartInCmd("sc.exe config NvTelemetryContainer start= auto");
        await OptimizationOptions.StartInCmd("sc.exe start NvTelemetryContainer");
    }

    internal static async Task DisableChromeTelemetry()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Google\\Chrome\" /v MetricsReportingEnabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Google\\Chrome\" /v ChromeCleanupReportingEnabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Google\\Chrome\" /v ChromeCleanupEnabled /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Google\\Chrome\" /v UserFeedbackAllowed /t REG_DWORD /d 0 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Google\\Chrome\" /v DeviceMetricsReportingEnabled /t REG_DWORD /d 0 /f");
    }

    internal static async Task EnableChromeTelemetry()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Google\\Chrome\" /v MetricsReportingEnabled /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Google\\Chrome\" /v ChromeCleanupReportingEnabled /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Google\\Chrome\" /v ChromeCleanupEnabled /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Google\\Chrome\" /v UserFeedbackAllowed /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Google\\Chrome\" /v DeviceMetricsReportingEnabled /f");
    }

    internal static async Task DisableFirefoxTelemetry()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Mozilla\\Firefox\" /v DisableTelemetry /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Mozilla\\Firefox\" /v DisableDefaultBrowserAgent /t REG_DWORD /d 1 /f");
        await OptimizationOptions.StartInCmd("schtasks.exe /change /disable /tn \"\\Mozilla\\Firefox Default Browser Agent 308046B0AF4A39CB\"");
        await OptimizationOptions.StartInCmd("schtasks.exe /change /disable /tn \"\\Mozilla\\Firefox Default Browser Agent D2CEEC440E2074BD\"");
    }

    internal static async Task EnableFirefoxTelemetry()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Mozilla\\Firefox\" /v DisableTelemetry /f");
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Mozilla\\Firefox\" /v DisableDefaultBrowserAgent /f");
        await OptimizationOptions.StartInCmd("schtasks.exe /change /enable /tn \"\\Mozilla\\Firefox Default Browser Agent 308046B0AF4A39CB\"");
        await OptimizationOptions.StartInCmd("schtasks.exe /change /enable /tn \"\\Mozilla\\Firefox Default Browser Agent D2CEEC440E2074BD\"");
    }
    internal static async Task DisableHibernation()
    {
        await OptimizationOptions.StartInCmd("powercfg -h off");
    }

    internal static async Task EnableHibernation()
    {
        await OptimizationOptions.StartInCmd("powercfg -h on");
    }

    internal static async Task EnableEndTask()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\TaskbarDeveloperSettings /v TaskbarEndTask /t REG_DWORD /d 1 /f");
    }

    internal static async Task DisableEndTask()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\TaskbarDeveloperSettings /v TaskbarEndTask /t REG_DWORD /d 0 /f");
    }

    public static async Task<bool> RemoveTempFiles()
    {
        // Stop Explorer
        await OptimizationOptions.StartInCmd("taskkill /F /IM explorer.exe");

        try
        {

            // List of commands to remove temporary files
            var tempCommands = new[]
            {
            "rd /S /Q %windir%\\Temp",
            "rd /S /Q %TEMP%",
            "rd /S /Q %localappdata%\\Temp",
            "rd /S /Q %windir%\\SoftwareDistribution\\Download",
            "rd /S /Q %windir%\\SoftwareDistribution\\DeliveryOptimization",
            "del /F /S /Q %windir%\\Logs\\CBS\\*",
            "del /F /S /Q %windir%\\MEMORY.DMP",
            "del /F /S /Q %windir%\\Minidump\\*.dmp",
            "del /F /S /Q %windir%\\Temp\\WindowsUpdate.log",
            "rd /S /Q %windir%\\Prefetch",
            "rd /S /Q %programdata%\\Microsoft\\Windows\\WER\\ReportQueue",
            "rd /S /Q %localappdata%\\Microsoft\\Windows\\WER\\ReportArchive",
            "rd /S /Q %localappdata%\\Microsoft\\Windows\\INetCache",
            "del /A /Q %localappdata%\\Microsoft\\Windows\\Explorer\\iconcache*",
            "del /A /Q %localappdata%\\Microsoft\\Windows\\Explorer\\thumbcache*",
            "rd /S /Q %systemdrive%\\Windows.old",
            "rd /S /Q %systemdrive%\\MSOCache",
            "del /F /S /Q %systemdrive%\\*.tmp",
            "del /F /S /Q %systemdrive%\\*._mp",
            "del /F /S /Q %systemdrive%\\*.log",
            "del /F /S /Q %systemdrive%\\*.chk",
            "del /F /S /Q %systemdrive%\\*.old",
            "del /F /S /Q %systemdrive%\\found.*",
            "del /F /S /Q %userprofile%\\recent\\*.*",
            "del /F /S /Q \"%userprofile%\\Local Settings\\Temporary Internet Files\\*.*\"",
            "PowerShell.exe -NoProfile -Command \"Remove-Item -Path $env:LOCALAPPDATA\\Google\\Chrome\\User Data\\Default\\* -Include 'Cache', 'Cookies', 'History', 'Visited Links', 'Archived History', 'Web Data', 'Current Session', 'Last Session' -Recurse -Force -ErrorAction SilentlyContinue\"",
            "PowerShell.exe -NoProfile -Command \"Remove-Item -Path $env:LOCALAPPDATA\\Microsoft\\Edge\\User Data\\Default\\Cache -Recurse -Force -ErrorAction SilentlyContinue\"",
            "PowerShell.exe -NoProfile -Command \"Remove-Item -Path $env:APPDATA\\Mozilla\\Firefox\\Profiles\\*\\cache2 -Recurse -Force -ErrorAction SilentlyContinue\"",
            "PowerShell.exe -NoProfile -Command \"Remove-Item -Path $env:APPDATA\\Moonchild Productions\\Pale Moon\\Profiles\\*\\cache2\\entries -Recurse -Force -ErrorAction SilentlyContinue\"",
            "PowerShell.exe -NoProfile -Command \"Clear-RecycleBin -Force\"",
            "PowerShell.exe -NoProfile -Command \"wevtutil cl System\"",
            "PowerShell.exe -NoProfile -Command \"wevtutil cl Application\"",
            "ipconfig /flushdns",
            "dism /Online /Cleanup-Image /StartComponentCleanup"
            };

            // Execute all commands
            var tempTasks = tempCommands.AsParallel().Select(cmd => OptimizationOptions.StartInCmd(cmd));
            await Task.WhenAll(tempTasks);

            // Start explorer
            await OptimizationOptions.StartInCmd("start explorer.exe");

            return true;
        }
        catch
        {
            return false;
        }
    }
}