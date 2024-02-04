using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace RyTuneX.Helpers;
internal class OptimizeSystemHelper
{
    internal static void EnablePerformanceTweaks()
    {
        OptimizationOptions.StartInCmd("reg add \"HKEY_CURRENT_USER\\Control Panel\\Desktop\" /v MenuShowDelay /t REG_SZ /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_CURRENT_USER\\Control Panel\\Mouse\" /v MouseHoverTime /t REG_SZ /d 0 /f");

        OptimizationOptions.StartInCmd("reg add \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\AutoComplete\" /v \"Append Completion\" /t REG_SZ /d yes /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\AutoComplete\" /v AutoSuggest /t REG_SZ /d yes /f");

        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\CrashControl\" /v CrashDumpEnabled /t REG_DWORD /d 3 /f");

        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\Control\\Remote Assistance\" /v fAllowToGetHelp /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("reg add \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v DisallowShaking /t REG_DWORD /d 1 /f");

        OptimizationOptions.StartInCmd("reg add \"HKEY_CLASSES_ROOT\\AllFilesystemObjects\\shellex\\ContextMenuHandlers\\Copy To\" /ve /d \"{C2FBB630-2971-11D1-A18C-00C04FD75D13}\" /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_CLASSES_ROOT\\AllFilesystemObjects\\shellex\\ContextMenuHandlers\\Move To\" /ve /d \"{C2FBB631-2971-11D1-A18C-00C04FD75D13}\" /f");

        OptimizationOptions.StartInCmd("reg add \"HKEY_CURRENT_USER\\Control Panel\\Desktop\" /v AutoEndTasks /t REG_SZ /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_CURRENT_USER\\Control Panel\\Desktop\" /v HungAppTimeout /t REG_SZ /d 1000 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_CURRENT_USER\\Control Panel\\Desktop\" /v WaitToKillAppTimeout /t REG_SZ /d 2000 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_CURRENT_USER\\Control Panel\\Desktop\" /v LowLevelHooksTimeout /t REG_SZ /d 1000 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoLowDiskSpaceChecks /t REG_DWORD /d 00000001 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v LinkResolveIgnoreLinkInfo /t REG_DWORD /d 00000001 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoResolveSearch /t REG_DWORD /d 00000001 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoResolveTrack /t REG_DWORD /d 00000001 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoInternetOpenWith /t REG_DWORD /d 00000001 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\" /v WaitToKillServiceTimeout /t REG_SZ /d 2000 /f");

        OptimizationOptions.StopService("DiagTrack");
        OptimizationOptions.StopService("diagsvc");
        OptimizationOptions.StopService("diagnosticshub.standardcollector.service");
        OptimizationOptions.StopService("dmwappushservice");

        OptimizationOptions.StartInCmd("sc config \"RemoteRegistry\" start= disabled");

        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\DiagTrack\" /v Start /t REG_DWORD /d 4 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\diagsvc\" /v Start /t REG_DWORD /d 4 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\diagnosticshub.standardcollector.service\" /v Start /t REG_DWORD /d 4 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\dmwappushservice\" /v Start /t REG_DWORD /d 4 /f");

        OptimizationOptions.StartInCmd("reg add \"HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v HideFileExt /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v Hidden /t REG_DWORD /d 1 /f");

        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v SystemResponsiveness /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v NoLazyMode /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v AlwaysOn /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"GPU Priority\" /t REG_DWORD /d 8 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v Priority /t REG_DWORD /d 6 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"Scheduling Category\" /t REG_SZ /d High /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"SFIO Priority\" /t REG_SZ /d High /f");

        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Microsoft\\Windows Media Foundation\" /v EnableFrameServerMode /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency\" /v \"GPU Priority\" /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency\" /v Priority /t REG_DWORD /d 8 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency\" /v \"Scheduling Category\" /t REG_SZ /d Medium /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency\" /v \"SFIO Priority\" /t REG_SZ /d High /f");

        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\Psched\" /v NonBestEffortLimit /t REG_DWORD /d 0 /f");
    }

    internal static void DisablePerformanceTweaks()
    {
        OptimizationOptions.StartInCmd("reg delete \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\AutoComplete\" /v \"Append Completion\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\AutoComplete\" /v AutoSuggest /f");

        OptimizationOptions.StartInCmd("reg delete \"SOFTWARE\\WOW6432Node\\Microsoft\\Windows Media Foundation\" /v EnableFrameServerMode /f");

        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\CrashControl\" /v CrashDumpEnabled /t REG_DWORD /d 7 /f");

        OptimizationOptions.StartInCmd("reg add \"Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\" /v EnableAutoTray /t REG_DWORD /d 1 /f");

        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\Control\\Remote Assistance\" /v fAllowToGetHelp /t REG_DWORD /d 1 /f");

        OptimizationOptions.StartInCmd("reg add \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v DisallowShaking /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("reg delete \"HKEY_CLASSES_ROOT\\AllFilesystemObjects\\shellex\\ContextMenuHandlers\\Copy To\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_CLASSES_ROOT\\AllFilesystemObjects\\shellex\\ContextMenuHandlers\\Move To\" /f");

        OptimizationOptions.StartInCmd("reg delete \"HKEY_CURRENT_USER\\Control Panel\\Desktop\" /v AutoEndTasks /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_CURRENT_USER\\Control Panel\\Desktop\" /v HungAppTimeout /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_CURRENT_USER\\Control Panel\\Desktop\" /v WaitToKillAppTimeout /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_CURRENT_USER\\Control Panel\\Desktop\" /v LowLevelHooksTimeout /f");

        OptimizationOptions.StartInCmd("reg add \"HKEY_CURRENT_USER\\Control Panel\\Desktop\" /v MenuShowDelay /t REG_SZ /d 400 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_CURRENT_USER\\Control Panel\\Mouse\" /v MouseHoverTime /t REG_SZ /d 400 /f");

        OptimizationOptions.StartInCmd("reg delete \"Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoLowDiskSpaceChecks /f");
        OptimizationOptions.StartInCmd("reg delete \"Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v LinkResolveIgnoreLinkInfo /f");
        OptimizationOptions.StartInCmd("reg delete \"Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoResolveSearch /f");
        OptimizationOptions.StartInCmd("reg delete \"Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoResolveTrack /f");
        OptimizationOptions.StartInCmd("reg delete \"Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoInternetOpenWith /f");

        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\" /v WaitToKillServiceTimeout /t REG_SZ /d 5000 /f");

        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\DiagTrack\" /v Start /t REG_DWORD /d 2 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\diagnosticshub.standardcollector.service\" /v Start /t REG_DWORD /d 2 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\dmwappushservice\" /v Start /t REG_DWORD /d 2 /f");

        OptimizationOptions.StartService("DiagTrack");
        OptimizationOptions.StartService("diagnosticshub.standardcollector.service");
        OptimizationOptions.StartService("dmwappushservice");

        OptimizationOptions.StartInCmd("reg add \"HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v HideFileExt /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v Hidden /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v SystemResponsiveness /t REG_DWORD /d 14 /f");
        OptimizationOptions.StartInCmd("reg delete \"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v NoLazyMode /f");
        OptimizationOptions.StartInCmd("reg delete \"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v AlwaysOn /f");
        OptimizationOptions.StartInCmd("reg delete \"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"GPU Priority\" /f");
        OptimizationOptions.StartInCmd("reg delete \"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v Priority /f");
        OptimizationOptions.StartInCmd("reg delete \"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"Scheduling Category\" /f");
        OptimizationOptions.StartInCmd("reg delete \"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"SFIO Priority\" /f");

        OptimizationOptions.StartInCmd("reg delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency\" /v \"GPU Priority\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency\" /v Priority /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency\" /v \"Scheduling Category\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency\" /v \"SFIO Priority\" /f");

        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\Psched\" /v NonBestEffortLimit /t REG_DWORD /d 80 /f");
        OptimizationOptions.StartInCmd("reg delete \"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v NetworkThrottlingIndex /f");

        OptimizationOptions.StartInCmd("reg delete \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\Dnscache\\Parameters\" /v MaxCacheTtl /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\Dnscache\\Parameters\" /v MaxNegativeCacheTtl /f");

    }

    internal static void DisableTelemetryServices()
    {
        OptimizationOptions.StopService("DiagTrack");
        OptimizationOptions.StopService("diagnosticshub.standardcollector.service");
        OptimizationOptions.StopService("dmwappushservice");
        OptimizationOptions.StopService("DcpSvc");

        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\DiagTrack\" /v Start /t REG_DWORD /d 4 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\diagnosticshub.standardcollector.service\" /v Start /t REG_DWORD /d 4 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\dmwappushservice\" /v Start /t REG_DWORD /d 4 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\DcpSvc\" /v Start /t REG_DWORD /d 4 /f");

        OptimizationOptions.StartInCmd("reg add \"HKLM\\Software\\Microsoft\\PolicyManager\\default\\WiFi\\AllowAutoConnectToWiFiSenseHotspots\" /v value /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKLM\\Software\\Microsoft\\PolicyManager\\default\\WiFi\\AllowWiFiHotSpotReporting\" /v value /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppCompat\" /v DisableEngine /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppCompat\" /v SbEnable /t REG_DWORD /d 0 /f");

        if (Environment.Is64BitOperatingSystem)
        {
            OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Policies\\Microsoft\\Windows\\AppCompat\" /v DisableEngine /t REG_DWORD /d 1 /f");
            OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Policies\\Microsoft\\Windows\\AppCompat\" /v SbEnable /t REG_DWORD /d 0 /f");
            OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Policies\\Microsoft\\Windows\\AppCompat\" /v DisablePCA /t REG_DWORD /d 1 /f");
        }

        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\" /v PublishUserActivities /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\SQMClient\\Windows\" /v CEIPEnable /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppCompat\" /v AITEnable /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppCompat\" /v DisableInventory /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppCompat\" /v DisablePCA /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppCompat\" /v DisableUAR /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Device Metadata\" /v PreventDeviceMetadataFromNetwork /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\MRT\" /v DontOfferThroughWUAU /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\WMI\\AutoLogger\\SQMLogger\" /v Start /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\System\" /v AllowExperimentation /d 0 /f");
        OptimizationOptions.StartInCmd("sc config WdiServiceHost start= disabled");


    }

    internal static void EnableTelemetryServices()
    {
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\DiagTrack\" /v Start /t REG_DWORD /d 2 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\diagnosticshub.standardcollector.service\" /v Start /t REG_DWORD /d 2 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\dmwappushservice\" /v Start /t REG_DWORD /d 2 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\DcpSvc\" /v Start /t REG_DWORD /d 2 /f");

        OptimizationOptions.StartInCmd("sc config WdiSystemHost start= auto");
        OptimizationOptions.StartInCmd("sc config WdiServiceHost start= auto");

        OptimizationOptions.StartInCmd("sc start DiagTrack");
        OptimizationOptions.StartInCmd("sc start diagnosticshub.standardcollector.service");
        OptimizationOptions.StartInCmd("sc start dmwappushservice");
        OptimizationOptions.StartInCmd("sc start DcpSvc");

    }

    internal static void DisableMediaPlayerSharing()
    {
        OptimizationOptions.StopService("WMPNetworkSvc");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\WMPNetworkSvc\" /v Start /t REG_DWORD /d 4 /f");

    }

    internal static void EnableMediaPlayerSharing()
    {
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\WMPNetworkSvc\" /v Start /t REG_DWORD /d 2 /f");
        OptimizationOptions.StartInCmd("sc start WMPNetworkSvc");

    }

    internal static void DisableHomeGroup()
    {
        OptimizationOptions.StopService("HomeGroupListener");
        OptimizationOptions.StopService("HomeGroupProvider");

        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\HomeGroup\" /v DisableHomeGroup /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\HomeGroupListener\" /v Start /t REG_DWORD /d 4 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\HomeGroupProvider\" /v Start /t REG_DWORD /d 4 /f");

    }

    internal static void EnableHomeGroup()
    {
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\HomeGroupListener\" /v Start /t REG_DWORD /d 2 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\HomeGroupProvider\" /v Start /t REG_DWORD /d 2 /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\HomeGroup\" /v DisableHomeGroup /f");
        OptimizationOptions.StartInCmd("sc start HomeGroupListener");
        OptimizationOptions.StartInCmd("sc start HomeGroupProvider");
    }

    internal static void DisablePrintService()
    {
        OptimizationOptions.StopService("Spooler");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\Spooler\" /v Start /t REG_DWORD /d 3 /f");
    }

    internal static void EnablePrintService()
    {
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\Spooler\" /v Start /t REG_DWORD /d 2 /f");
        OptimizationOptions.StartInCmd("cmd /c sc start Spooler");
    }

    internal static void DisableSuperfetch()
    {
        OptimizationOptions.StopService("SysMain");

        OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\SysMain\" /v Start /t REG_DWORD /d 4 /f");
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters\" /v EnableSuperfetch /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters\" /v EnablePrefetcher /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters\" /v SfTracingState /t REG_DWORD /d 1 /f");
    }

    internal static void EnableSuperfetch()
    {
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\SysMain\" /v Start /t REG_DWORD /d 2 /f");
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters\" /v EnableSuperfetch /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters\" /v EnablePrefetcher /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters\" /v SfTracingState /f");

        OptimizationOptions.StartService("SysMain");
    }

    internal static void EnableCompatibilityAssistant()
    {
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\PcaSvc\" /v Start /t REG_DWORD /d 2 /f");
        OptimizationOptions.StartService("PcaSvc");
    }

    internal static void DisableCompatibilityAssistant()
    {
        OptimizationOptions.StopService("PcaSvc");
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\PcaSvc\" /v Start /t REG_DWORD /d 4 /f");
    }

    internal static void EnableVerboseLogon()
    {
        OptimizationOptions.StartInCmd("reg add \"HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v VerboseStatus /t REG_DWORD /d 1 /f");
    }

    internal static void DisableVerboseLogon()
    {
        OptimizationOptions.StartInCmd("reg add \"HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v VerboseStatus /t REG_DWORD /d 0 /f");
    }

    internal static void EnableClassicContextMenu()
    {
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\\InprocServer32\\");
    }

    internal static void DisableClassicContextMenu()
    {
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\\InprocServer32\\");
    }

    internal static void EnableClassicStartMenu()
    {
        //Working on it
    }

    internal static void DisableClassicStartMenu()
    {
        //Working on it
    }

    internal static void DisableSystemRestore()
    {
        OptimizationOptions.StartInCmd("vssadmin delete shadows /for=c: /all /quiet");
        OptimizationOptions.StopService("VSS");

        OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\SystemRestore\" /v DisableSR /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\SystemRestore\" /v DisableConfig /t REG_DWORD /d 1 /f");
    }

    internal static void EnableSystemRestore()
    {
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\SystemRestore\" /v DisableSR /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\SystemRestore\" /v DisableConfig /f");

        OptimizationOptions.StartService("VSS");
    }

    internal static void DisableSearch()
    {
        OptimizationOptions.StopService("WSearch");
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\WSearch\" /v Start /t REG_DWORD /d 4 /f");
    }

    internal static void EnableSearch()
    {
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\WSearch\" /v Start /t REG_DWORD /d 2 /f");
        OptimizationOptions.StartService("WSearch");
    }

    internal static void DisableSMB(string v)
    {
        OptimizationOptions.StartInCmd($"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\LanmanServer\\Parameters\" /v SMB{v} /t REG_DWORD /d 0 /f");
    }

    internal static void EnableSMB(string v)
    {
        OptimizationOptions.StartInCmd($"reg delete \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\LanmanServer\\Parameters\" /v SMB{v} /f");
    }

    internal static void DisableNTFSTimeStamp()
    {
        OptimizationOptions.StartInCmd("fsutil behavior set disablelastaccess 1");
    }

    internal static void EnableNTFSTimeStamp()
    {
        OptimizationOptions.StartInCmd("fsutil behavior set disablelastaccess 2");
    }

    internal static void DisableErrorReporting()
    {
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Error Reporting\" /v Disabled /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\PCHealth\\ErrorReporting\" /v DoReport /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\" /v Disabled /t REG_DWORD /d 1 /f");

        OptimizationOptions.StopService("WerSvc");
        OptimizationOptions.StopService("wercplsupport");
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\WerSvc\" /v Start /t REG_DWORD /d 4 /f");
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\wercplsupport\" /v Start /t REG_DWORD /d 4 /f");
    }

    internal static void EnableErrorReporting()
    {
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Error Reporting\" /v Disabled /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\PCHealth\\ErrorReporting\" /v DoReport /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\" /v Disabled /f");

        OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\wercplsupport\" /v Start /t REG_DWORD /d 3 /f");
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\WerSvc\" /v Start /t REG_DWORD /d 2 /f");
        OptimizationOptions.StartService("WerSvc");
        OptimizationOptions.StartService("wercplsupport");
    }

    internal static void EnableLegacyVolumeSlider()
    {
        OptimizationOptions.StartInCmd("reg add \"HKLM\\Software\\Microsoft\\Windows NT\\CurrentVersion\\MTCUVC\" /v EnableMtcUvc /t REG_DWORD /d 0 /f");
    }

    internal static void DisableLegacyVolumeSlider()
    {
        OptimizationOptions.StartInCmd("reg add \"HKLM\\Software\\Microsoft\\Windows NT\\CurrentVersion\\MTCUVC\" /v EnableMtcUvc /t REG_DWORD /d 1 /f");
    }

    internal static void DisableCortana()
    {
        OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\SearchSettings\" /v IsDeviceSearchHistoryEnabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v AllowCortana /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v DisableWebSearch /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v ConnectedSearchUseWeb /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v ConnectedSearchUseWebOverMeteredConnections /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Search\" /v HistoryViewEnabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Search\" /v DeviceHistoryEnabled /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v AllowSearchToUseLocation /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v BingSearchEnabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v CortanaConsent /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v AllowCloudSearch /t REG_DWORD /d 0 /f");
    }

    internal static void EnableCortana()
    {
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v AllowCortana /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v DisableWebSearch /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v ConnectedSearchUseWeb /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v ConnectedSearchUseWebOverMeteredConnections /t REG_DWORD /d 1 /f");

        OptimizationOptions.StartInCmd("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Search\" /v HistoryViewEnabled /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Search\" /v DeviceHistoryEnabled /t REG_DWORD /d 1 /f");

        OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v AllowSearchToUseLocation /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v BingSearchEnabled /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v CortanaConsent /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v AllowCloudSearch /t REG_DWORD /d 1 /f");
    }

    internal static void EnableGamingMode()
    {
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\" /v HwSchMode /t REG_DWORD /d 2 /f");
        OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\GameBar\" /v AllowAutoGameMode /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\GameBar\" /v AutoGameModeEnabled /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKCU\\System\\GameConfigStore\" /v GameDVR_FSEBehaviorMode /t REG_DWORD /d 2 /f");
    }

    internal static void DisableGamingMode()
    {
        OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\" /v HwSchMode /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\GameBar\" /v AllowAutoGameMode /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\GameBar\" /v AutoGameModeEnabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKCU\\System\\GameConfigStore\" /v GameDVR_FSEBehaviorMode /t REG_DWORD /d 0 /f");
    }


    internal static void DisableAutomaticUpdates()
    {
        OptimizationOptions.StartInCmd("reg add \"HKEY_USERS\\S-1-5-20\\Software\\Microsoft\\Windows\\CurrentVersion\\DeliveryOptimization\\Settings\" /v DownloadMode /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings\" /v UxOption /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v NoAutoUpdate /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v AUOptions /t REG_DWORD /d 2 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v NoAutoRebootWithLoggedOnUsers /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\DeliveryOptimization\\Config\" /v DODownloadMode /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\DoSvc\" /v Start /t REG_DWORD /d 4 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Speech\" /v AllowSpeechModelUpdate /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Schedule\\Maintenance\" /v MaintenanceDisabled /t REG_DWORD /d 1 /f");
        OptimizationOptions.StopService("DoSvc");
    }

    internal static void EnableAutomaticUpdates()
    {
        OptimizationOptions.StartInCmd("reg delete \"HKEY_USERS\\S-1-5-20\\Software\\Microsoft\\Windows\\CurrentVersion\\DeliveryOptimization\\Settings\" /v DownloadMode /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings\" /v UxOption /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v AUOptions /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v NoAutoUpdate /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v NoAutoRebootWithLoggedOnUsers /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\DeliveryOptimization\\Config\" /v DODownloadMode /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Speech\" /v AllowSpeechModelUpdate /f");

        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\DoSvc\" /v Start /t REG_DWORD /d 3 /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Schedule\\Maintenance\" /v MaintenanceDisabled /f");
    }

    internal static void DisableStoreUpdates()
    {
        OptimizationOptions.StartInCmd("reg add \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v SilentInstalledAppsEnabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent\" /v DisableSoftLanding /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v PreInstalledAppsEnabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent\" /v DisableWindowsConsumerFeatures /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v OemPreInstalledAppsEnabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\WindowsStore\" /v AutoDownload /t REG_DWORD /d 2 /f");
    }

    internal static void EnableStoreUpdates()
    {
        OptimizationOptions.StartInCmd("reg delete \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v SilentInstalledAppsEnabled /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent\" /v DisableSoftLanding /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v PreInstalledAppsEnabled /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent\" /v DisableWindowsConsumerFeatures /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v OemPreInstalledAppsEnabled /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\WindowsStore\" /v AutoDownload /f");
    }


    internal static void DisableOneDrive()
    {
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\OneDrive /v DisableFileSyncNGSC /t REG_DWORD /d 1 /f");
    }

    internal static void EnableOneDrive()
    {
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\OneDrive /v DisableFileSyncNGSC /t REG_DWORD /d 0 /f");
    }

    internal static void EnableSensorServices()
    {
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\SensrSvc /v Start /t REG_DWORD /d 2 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\SensorService /v Start /t REG_DWORD /d 2 /f");
        OptimizationOptions.StartInCmd("sc start SensrSvc");
        OptimizationOptions.StartInCmd("sc start SensorService");
    }

    internal static void DisableSensorServices()
    {
        OptimizationOptions.StartInCmd("sc stop SensrSvc");
        OptimizationOptions.StartInCmd("sc stop SensorService");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\SensrSvc /v Start /t REG_DWORD /d 4 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\SensorService /v Start /t REG_DWORD /d 4 /f");
    }

    internal static void EnhancePrivacy()
    {
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Feeds /v EnableFeeds /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Feeds /v EnableFeeds /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\PolicyManager\\default\\NewsAndInterests\\AllowNewsAndInterests /v value /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v RotatingLockScreenOverlayEnabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v RotatingLockScreenEnabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v DisableWindowsSpotlightFeatures /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v DisableTailoredExperiencesWithDiagnosticData /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent /v DisableCloudOptimizedContent /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection /v DoNotShowFeedbackNotifications /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo /v Enabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Bluetooth /v AllowAdvertising /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System /v DisableAutomaticRestartSignOn /t REG_DWORD /d 1 /f");

        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\AdvertisingInfo /v DisabledByGroupPolicy /t REG_DWORD /d 1 /f");

        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\TabletPC /v PreventHandwritingDataSharing /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\TextInput /v AllowLinguisticDataCollection /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\InputPersonalization /v AllowInputPersonalization /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("REG ADD HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\SearchSettings /v SafeSearchMode /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v UploadUserActivities /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v AllowCrossDeviceClipboard /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\Software\\Policies\\Microsoft\\Windows\\Messaging /v AllowMessageSync /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\Software\\Policies\\Microsoft\\Windows\\SettingSync /v DisableCredentialsSettingSync /t REG_DWORD /d 2 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\Software\\Policies\\Microsoft\\Windows\\SettingSync /v DisableCredentialsSettingSyncUserOverride /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\Software\\Policies\\Microsoft\\Windows\\SettingSync /v DisableApplicationSettingSync /t REG_DWORD /d 2 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\Software\\Policies\\Microsoft\\Windows\\SettingSync /v DisableApplicationSettingSyncUserOverride /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy /v LetAppsActivateWithVoice /t REG_DWORD /d 2 /f");

        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\FindMyDevice /v AllowFindMyDevice /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Settings\\FindMyDevice /v LocationSyncEnabled /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v EnableActivityFeed /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v EnableCdp /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("REG ADD HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Privacy /v TailoredExperiencesWithDiagnosticDataEnabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_USERS\\.DEFAULT\\Software\\Microsoft\\Windows\\CurrentVersion\\Privacy /v TailoredExperiencesWithDiagnosticDataEnabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Diagnostics\\DiagTrack /v ShowedToastAtLevel /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_USERS\\.DEFAULT\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Diagnostics\\DiagTrack /v ShowedToastAtLevel /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_CURRENT_USER\\Software\\Microsoft\\Speech_OneCore\\Settings\\OnlineSpeechPrivacy /v HasAccepted /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\location /v Value /t REG_SZ /d Deny /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Settings\\FindMyDevice /v LocationSyncEnabled /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\LocationAndSensors /v DisableLocation /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\LocationAndSensors /v DisableLocationScripting /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\LocationAndSensors /v DisableWindowsLocationProvider /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Sensor\\Overrides\\{BFA794E4-F964-4FDB-90F6-51056BFE4B44} /v SensorPermissionState /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\Services\\lfsvc\\Service\\Configuration /v Status /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Biometrics /v Enabled /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("REG ADD HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Feeds /v ShellFeedsTaskbarOpenOnHover /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("REG ADD HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\CDP /v CdpSessionUserAuthzPolicy /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\CDP /v NearShareChannelUserAuthzPolicy /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\CDP /v RomeSdkChannelUserAuthzPolicy /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\Software\\Policies\\Microsoft\\Windows NT\\CurrentVersion\\Software Protection Platform /v NoGenTicket /t REG_DWORD /d 1 /f");

        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo /v Enabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AppHost /v EnableWebContentEvaluation /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AppHost\\EnableWebContentEvaluation /v Enabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_CURRENT_USER\\Control Panel\\International\\User Profile /v HttpAcceptLanguageOptOut /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\SmartGlass /v UserAuthPolicy /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("REG ADD HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Personalization\\Settings /v AcceptedPrivacyPolicy /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\SettingSync\\Groups\\Language /v Enabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\InputPersonalization /v RestrictImplicitTextCollection /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\InputPersonalization /v RestrictImplicitInkCollection /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\InputPersonalization\\TrainedDataStore /v HarvestContacts /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Input\\TIPC /v Enabled /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy /v LetAppsSyncWithDevices /t REG_DWORD /d 2 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\DeviceAccess\\Global\\LooselyCoupled /v Value /t REG_SZ /d Deny /f");

        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection /v MaxTelemetryAllowed /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v UploadUserActivities /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_CURRENT_USER\\Software\\Microsoft\\Siuf\\Rules /v PeriodInNanoSeconds /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_CURRENT_USER\\Software\\Microsoft\\Siuf\\Rules /v NumberOfSIUFInPeriod /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection /v AllowTelemetry /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection /v AllowTelemetry /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SYSTEM\\ControlSet001\\Control\\WMI\\AutoLogger\\AutoLogger-Diagtrack-Listener /v Start /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\WMI\\AutoLogger\\AutoLogger-Diagtrack-Listener /v Start /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WcmSvc\\wifinetworkmanager\\config /v AutoConnectAllowedOEM /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WcmSvc\\Tethering /v Hotspot2SignUp /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WlanSvc\\AnqpCache /v OsuRegistrationStatus /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WcmSvc\\Tethering /v RemoteStartupDisabled /t REG_DWORD /d 1 /f");

        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\Connect /v AllowProjectionToPC /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\Software\\Policies\\Microsoft\\Windows\\System /v EnableMmx /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("REG ADD HKEY_LOCAL_MACHINE\\Software\\Policies\\Microsoft\\Windows\\System /v RSoPLogging /t REG_DWORD /d 0 /f");

    }

    internal static void CompromisePrivacy()
    {
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v \"RotatingLockScreenOverlayEnabled\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v \"RotatingLockScreenEnabled\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v \"DisableWindowsSpotlightFeatures\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v \"DisableTailoredExperiencesWithDiagnosticData\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent\" /v \"DisableCloudOptimizedContent\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection\" /v \"DoNotShowFeedbackNotifications\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Feeds\" /v \"EnableFeeds\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\default\\NewsAndInterests\\AllowNewsAndInterests\" /v \"value\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo\" /v \"Enabled\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Bluetooth\" /v \"AllowAdvertising\" /f");

        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v \"DisableAutomaticRestartSignOn\" /f");

        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AdvertisingInfo\" /v \"DisabledByGroupPolicy\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v \"Start_TrackProgs\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\TabletPC\" /v \"PreventHandwritingDataSharing\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\TextInput\" /v \"AllowLinguisticDataCollection\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\InputPersonalization\" /v \"AllowInputPersonalization\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\" /v \"UploadUserActivities\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\" /v \"AllowCrossDeviceClipboard\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\Software\\Policies\\Microsoft\\Windows\\Messaging\" /v \"AllowMessageSync\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\Software\\Policies\\Microsoft\\Windows\\SettingSync\" /v \"DisableCredentialsSettingSync\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\Software\\Policies\\Microsoft\\Windows\\SettingSync\" /v \"DisableCredentialsSettingSyncUserOverride\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\Software\\Policies\\Microsoft\\Windows\\SettingSync\" /v \"DisableApplicationSettingSync\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\Software\\Policies\\Microsoft\\Windows\\SettingSync\" /v \"DisableApplicationSettingSyncUserOverride\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy\" /v \"LetAppsActivateWithVoice\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\SearchSettings\" /v \"SafeSearchMode\" /f");

        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\FindMyDevice\" /v \"AllowFindMyDevice\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Settings\\FindMyDevice\" /v \"LocationSyncEnabled\" /f");

        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\" /v \"EnableActivityFeed\" /f");

        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\" /v \"EnableCdp\" /f");

        OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Privacy\" /v \"TailoredExperiencesWithDiagnosticDataEnabled\" /f");
        OptimizationOptions.StartInCmd("reg delete \".DEFAULT\\Software\\Microsoft\\Windows\\CurrentVersion\\Privacy\" /v \"TailoredExperiencesWithDiagnosticDataEnabled\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Diagnostics\\DiagTrack\" /v \"ShowedToastAtLevel\" /f");
        OptimizationOptions.StartInCmd("reg delete \".DEFAULT\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Diagnostics\\DiagTrack\" /v \"ShowedToastAtLevel\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Speech_OneCore\\Settings\\OnlineSpeechPrivacy\" /v \"HasAccepted\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\location\" /v \"Value\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Settings\\FindMyDevice\" /v \"LocationSyncEnabled\" /f");

        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\LocationAndSensors\" /v \"DisableLocation\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\LocationAndSensors\" /v \"DisableLocationScripting\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\LocationAndSensors\" /v \"DisableWindowsLocationProvider\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Sensor\\Overrides\\{BFA794E4-F964-4FDB-90F6-51056BFE4B44}\" /v \"SensorPermissionState\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\System\\CurrentControlSet\\Services\\lfsvc\\Service\\Configuration\" /v \"Status\" /f");

        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Biometrics\" /v \"Enabled\" /f");

        OptimizationOptions.StartInCmd("reg delete \"HKLM\\Software\\Policies\\Microsoft\\Windows NT\\CurrentVersion\\Software Protection Platform\" /v \"NoGenTicket\" /f");

        OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Feeds\" /v \"ShellFeedsTaskbarOpenOnHover\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\CDP\" /v \"CdpSessionUserAuthzPolicy\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\CDP\" /v \"NearShareChannelUserAuthzPolicy\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\CDP\" /v \"RomeSdkChannelUserAuthzPolicy\" /f");

        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo\" /v \"Enabled\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AppHost\" /v \"EnableWebContentEvaluation\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AppHost\\EnableWebContentEvaluation\" /v \"Enabled\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\Control Panel\\International\\User Profile\" /v \"HttpAcceptLanguageOptOut\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\SmartGlass\" /v \"UserAuthPolicy\" /f");

        OptimizationOptions.StartInCmd("reg delete \"HKCU\\SOFTWARE\\Microsoft\\Personalization\\Settings\" /v \"AcceptedPrivacyPolicy\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\SettingSync\\Groups\\Language\" /v \"Enabled\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\SOFTWARE\\Microsoft\\InputPersonalization\" /v \"RestrictImplicitTextCollection\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\SOFTWARE\\Microsoft\\InputPersonalization\" /v \"RestrictImplicitInkCollection\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\SOFTWARE\\Microsoft\\InputPersonalization\\TrainedDataStore\" /v \"HarvestContacts\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\SOFTWARE\\Microsoft\\Input\\TIPC\" /v \"Enabled\" /f");

        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy\" /v \"LetAppsSyncWithDevices\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\DeviceAccess\\Global\\LooselyCoupled\" /v \"Value\" /f");

        OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Siuf\\Rules\" /v \"PeriodInNanoSeconds\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Siuf\\Rules\" /v \"NumberOfSIUFInPeriod\" /f");

        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection\" /v \"MaxTelemetryAllowed\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\" /v \"UploadUserActivities\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Siuf\\Rules\" /v \"PeriodInNanoSeconds\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Siuf\\Rules\" /v \"NumberOfSIUFInPeriod\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection\" /v \"AllowTelemetry\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection\" /v \"AllowTelemetry\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SYSTEM\\ControlSet001\\Control\\WMI\\AutoLogger\\AutoLogger-Diagtrack-Listener\" /v \"Start\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\WMI\\AutoLogger\\AutoLogger-Diagtrack-Listener\" /v \"Start\" /f");

        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\WcmSvc\\wifinetworkmanager\\config\" /v \"AutoConnectAllowedOEM\" /f");

        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\WcmSvc\\Tethering\" /v \"Hotspot2SignUp\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\WlanSvc\\AnqpCache\" /v \"OsuRegistrationStatus\" /f");

        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\WcmSvc\\Tethering\" /v \"RemoteStartupDisabled\" /f");


        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Connect\" /v \"AllowProjectionToPC\" /f");

        OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\" /v \"EnableMmx\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Policies\\Microsoft\\Windows\\System\" /v \"EnableMmx\" /f");
        OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Policies\\Microsoft\\Windows\\System\" /v \"RSoPLogging\" /f");

    }

    internal static void DisableGameBar()
    {
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR /v AppCaptureEnabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR /v AudioCaptureEnabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR /v CursorCaptureEnabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\GameBar /v UseNexusForGameBarEnabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\GameBar /v ShowStartupPanel /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\System\\GameConfigStore /v GameDVR_Enabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKLM\\Software\\Policies\\Microsoft\\Windows\\GameDVR /v AllowGameDVR /t REG_DWORD /d 0 /f");
    }

    internal static void EnableGameBar()
    {
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR /v AppCaptureEnabled /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR /v AudioCaptureEnabled /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR /v CursorCaptureEnabled /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\GameBar /v UseNexusForGameBarEnabled /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\GameBar /v ShowStartupPanel /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\System\\GameConfigStore /v GameDVR_Enabled /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add HKLM\\Software\\Policies\\Microsoft\\Windows\\GameDVR /v AllowGameDVR /t REG_DWORD /d 1 /f");
    }


    internal static void DisableQuickAccessHistory()
    {
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Feeds /v ShellFeedsTaskbarViewMode /t REG_DWORD /d 2 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Feeds /v IsFeedsAvailable /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search /v SearchboxTaskbarMode /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced /v ShowTaskViewButton /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\OperationStatusManager /v EnthusiastMode /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced /v ShowSyncProviderNotifications /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer /v ShowFrequent /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer /v ShowRecent /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced /v LaunchTo /t REG_DWORD /d 1 /f");

        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer /v HideSCAMeetNow /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer /v HideSCAMeetNow /t REG_DWORD /d 1 /f");

        OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\FileHistory /v Disabled /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\File History /v Disabled /t REG_DWORD /d 1 /f");
    }

    internal static void EnableQuickAccessHistory()
    {
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\OperationStatusManager /v EnthusiastMode /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced /v ShowSyncProviderNotifications /t REG_DWORD /d 1 /f");

        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer /v ShowFrequent /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer /v ShowRecent /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced /v LaunchTo /t REG_DWORD /d 2 /f");
        OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced /v ShowTaskViewButton /f");

        OptimizationOptions.StartInCmd("reg delete HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\FileHistory /v Disabled /f");
        OptimizationOptions.StartInCmd("reg delete HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\File History /v Disabled /f");

        OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search /v SearchboxTaskbarMode /f");
        OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Feeds /v ShellFeedsTaskbarViewMode /f");
        OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Feeds /v IsFeedsAvailable /f");

        OptimizationOptions.StartInCmd("reg delete HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer /v HideSCAMeetNow /f");
        OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer /v HideSCAMeetNow /f");
    }

    internal static void DisableStartMenuAds()
    {
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-88000326Enabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\UserProfileEngagement /v ScoobeSystemSettingEnabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v ContentDeliveryAllowed /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v PreInstalledAppsEverEnabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SilentInstalledAppsEnabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-314559Enabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-338387Enabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-338389Enabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SystemPaneSuggestionsEnabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-338393Enabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-353694Enabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-353696Enabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-310093Enabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-338388Enabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContentEnabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SoftLandingEnabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v FeatureManagementEnabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Policies\\Microsoft\\Windows\\Explorer /v DisableSearchBoxSuggestions /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer /v AllowOnlineTips /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer /v DisableSearchBoxSuggestions /t REG_DWORD /d 1 /f");
    }


    internal static void EnableStartMenuAds()
    {
        OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-88000326Enabled /f");
        OptimizationOptions.StartInCmd("reg delete HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\UserProfileEngagement /v ScoobeSystemSettingEnabled /f");
        OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v ContentDeliveryAllowed /f");
        OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v PreInstalledAppsEverEnabled /f");
        OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SilentInstalledAppsEnabled /f");
        OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-314559Enabled /f");
        OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-338387Enabled /f");
        OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-338389Enabled /f");
        OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SystemPaneSuggestionsEnabled /f");
        OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-338393Enabled /f");
        OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-353694Enabled /f");
        OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-353696Enabled /f");
        OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-310093Enabled /f");
        OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContentEnabled /f");
        OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-338388Enabled /f");
        OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SoftLandingEnabled /f");
        OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v FeatureManagementEnabled /f");
        OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Policies\\Microsoft\\Windows\\Explorer /v DisableSearchBoxSuggestions /f");
        OptimizationOptions.StartInCmd("reg delete HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer /v AllowOnlineTips /f");
        OptimizationOptions.StartInCmd("reg delete HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer /v DisableSearchBoxSuggestions /f");
    }


    internal static void DisableMyPeople()
    {
        OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\People /v PeopleBand /t REG_DWORD /d 0 /f");
    }

    internal static void EnableMyPeople()
    {
        OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\People /v PeopleBand /t REG_DWORD /d 1 /f");
    }


    internal static void ExcludeDrivers()
    {
        OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\default\\Update\\ExcludeWUDriversInQualityUpdate /v value /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\default\\Update /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Update /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 1 /f");
    }

    internal static void IncludeDrivers()
    {
        OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\default\\Update\\ExcludeWUDriversInQualityUpdate /v value /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\default\\Update /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Update /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 0 /f");
    }


    internal static void DisableWindowsInk()
    {
        OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsInkWorkspace /v AllowWindowsInkWorkspace /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsInkWorkspace /v AllowSuggestedAppsInWindowsInkWorkspace /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableInkingWithTouch /t REG_DWORD /d 0 /f");
    }

    internal static void EnableWindowsInk()
    {
        OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsInkWorkspace /v AllowWindowsInkWorkspace /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsInkWorkspace /v AllowSuggestedAppsInWindowsInkWorkspace /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableInkingWithTouch /t REG_DWORD /d 1 /f");
    }


    internal static void DisableSpellingAndTypingFeatures()
    {
        OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableAutocorrection /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableSpellchecking /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Input\\Settings /v InsightsEnabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableDoubleTapSpace /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnablePredictionSpaceInsertion /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableTextPrediction /t REG_DWORD /d 0 /f");
    }

    internal static void EnableSpellingAndTypingFeatures()
    {
        OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableAutocorrection /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableSpellchecking /t REG_DWORD /d 1 /f");

        OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Input\\Settings /v InsightsEnabled /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableDoubleTapSpace /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnablePredictionSpaceInsertion /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableTextPrediction /t REG_DWORD /d 1 /f");
    }


    internal static void EnableFaxService()
    {
        Registry.SetValue("HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\Fax", "Start", "3", RegistryValueKind.DWord);
    }

    internal static void DisableFaxService()
    {
        OptimizationOptions.StopService("Fax");
        Registry.SetValue("HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\Fax", "Start", "4", RegistryValueKind.DWord);
    }

    internal static void EnableInsiderService()
    {
        Registry.SetValue("HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\wisvc", "Start", "3", RegistryValueKind.DWord);
        OptimizationOptions.StartInCmd("sc start wisvc");
    }

    internal static void DisableInsiderService()
    {
        OptimizationOptions.StopService("wisvc");
        Registry.SetValue("HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\wisvc", "Start", "4", RegistryValueKind.DWord);
    }


    internal static void DisableSmartScreen()
    {
        OptimizationOptions.StartInCmd("reg add HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Attachments /v SaveZoneInformation /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add HKEY_LOCAL_MACHINE\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Attachments /v ScanWithAntiVirus /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v ShellSmartScreenLevel /t REG_SZ /d Warn /f");
        OptimizationOptions.StartInCmd("reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v EnableSmartScreen /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer /v SmartScreenEnabled /t REG_SZ /d Off /f");
        OptimizationOptions.StartInCmd("reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Internet Explorer\\PhishingFilter /v EnabledV9 /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\AppHost /v PreventOverride /t REG_DWORD /d 0 /f");

        OptimizationOptions.StartInCmd("reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Notifications\\Settings\\Windows.SystemToast.SecurityAndMaintenance /v Enabled /t REG_DWORD /d 0 /f");
    }

    internal static void EnableSmartScreen()
    {
        OptimizationOptions.StartInCmd("reg add HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Attachments /v SaveZoneInformation /t REG_DWORD /d 2 /f");
        OptimizationOptions.StartInCmd("reg add HKEY_LOCAL_MACHINE\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Attachments /v ScanWithAntiVirus /t REG_DWORD /d 2 /f");
        OptimizationOptions.StartInCmd("reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v EnableSmartScreen /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer /v SmartScreenEnabled /t REG_SZ /d On /f");
        OptimizationOptions.StartInCmd("reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Internet Explorer\\PhishingFilter /v EnabledV9 /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg delete HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\AppHost /v PreventOverride /f");

    }

    internal static void DisableCloudClipboard()
    {
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "AllowClipboardHistory", "0", RegistryValueKind.DWord);
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "AllowCrossDeviceClipboard", "0", RegistryValueKind.DWord);
        Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Clipboard", "EnableClipboardHistory", "0", RegistryValueKind.DWord);
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Clipboard", "EnableClipboardHistory", "0", RegistryValueKind.DWord);
    }

    internal static void EnableCloudClipboard()
    {
        OptimizationOptions.StartInCmd("reg delete HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v AllowClipboardHistory /f");
        OptimizationOptions.StartInCmd("reg delete HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v AllowCrossDeviceClipboard /f");
        OptimizationOptions.StartInCmd("reg delete HKEY_CURRENT_USER\\Software\\Microsoft\\Clipboard /v EnableClipboardHistory /f");
        OptimizationOptions.StartInCmd("reg delete HKEY_LOCAL_MACHINE\\Software\\Microsoft\\Clipboard /v EnableClipboardHistory /f");
    }

    internal static void DisableStickyKeys()
    {
        OptimizationOptions.StartInCmd("reg add HKEY_CURRENT_USER\\Control Panel\\Accessibility\\StickyKeys /v Flags /t REG_SZ /d 506 /f");
        OptimizationOptions.StartInCmd("reg add HKEY_CURRENT_USER\\Control Panel\\Accessibility\\Keyboard Response /v Flags /t REG_SZ /d 122 /f");
        OptimizationOptions.StartInCmd("reg add HKEY_CURRENT_USER\\Control Panel\\Accessibility\\ToggleKeys /v Flags /t REG_SZ /d 58 /f");
        OptimizationOptions.StartInCmd("reg add HKEY_USERS\\.DEFAULT\\Control Panel\\Accessibility\\StickyKeys /v Flags /t REG_SZ /d 506 /f");
        OptimizationOptions.StartInCmd("reg add HKEY_USERS\\.DEFAULT\\Control Panel\\Accessibility\\Keyboard Response /v Flags /t REG_SZ /d 122 /f");
        OptimizationOptions.StartInCmd("reg add HKEY_USERS\\.DEFAULT\\Control Panel\\Accessibility\\ToggleKeys /v Flags /t REG_SZ /d 58 /f");
    }

    internal static void EnableStickyKeys()
    {
        OptimizationOptions.StartInCmd("reg add HKEY_CURRENT_USER\\Control Panel\\Accessibility\\StickyKeys /v Flags /t REG_SZ /d 510 /f");
        OptimizationOptions.StartInCmd("reg add HKEY_CURRENT_USER\\Control Panel\\Accessibility\\Keyboard Response /v Flags /t REG_SZ /d 126 /f");
        OptimizationOptions.StartInCmd("reg add HKEY_CURRENT_USER\\Control Panel\\Accessibility\\ToggleKeys /v Flags /t REG_SZ /d 62 /f");
        OptimizationOptions.StartInCmd("reg add HKEY_USERS\\.DEFAULT\\Control Panel\\Accessibility\\StickyKeys /v Flags /t REG_SZ /d 510 /f");
        OptimizationOptions.StartInCmd("reg add HKEY_USERS\\.DEFAULT\\Control Panel\\Accessibility\\Keyboard Response /v Flags /t REG_SZ /d 126 /f");
        OptimizationOptions.StartInCmd("reg add HKEY_USERS\\.DEFAULT\\Control Panel\\Accessibility\\ToggleKeys /v Flags /t REG_SZ /d 62 /f");
    }

    internal static void RemoveCastToDevice()
    {
        OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked\" /V {7AD84985-87B4-4a16-BE58-8B72A5B390F7} /T REG_SZ /D \"Play to Menu\" /F");
    }

    internal static void AddCastToDevice()
    {
        OptimizationOptions.StartInCmd("REG DELETE \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked\" /V {7AD84985-87B4-4a16-BE58-8B72A5B390F7} /F");
    }

    internal static void DisableVBS()
    {
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\" /V EnableVirtualizationBasedSecurity /T REG_DWORD /D 0 /F");
    }

    internal static void EnableVBS()
    {
        OptimizationOptions.StartInCmd("REG DELETE \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\" /V EnableVirtualizationBasedSecurity /F");
    }

    internal static void AlignTaskbarToLeft()
    {
        OptimizationOptions.StartInCmd("reg add \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v TaskbarAl /t REG_DWORD /d 0 /f");
    }

    internal static void AlignTaskbarToCenter()
    {
        OptimizationOptions.StartInCmd("reg add \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v TaskbarAl /t REG_DWORD /d 1 /f");
    }

    internal static void DisableSnapAssist()
    {
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V EnableSnapAssistFlyout /T REG_DWORD /D 0 /F");
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_CURRENT_USER\\Control Panel\\Desktop\" /V DockMoving /T REG_SZ /D 0 /F");
    }

    internal static void EnableSnapAssist()
    {
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V EnableSnapAssistFlyout /T REG_DWORD /D 1 /F");
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_CURRENT_USER\\Control Panel\\Desktop\" /V DockMoving /T REG_SZ /D 1 /F");
    }

    internal static void DisableWidgets()
    {
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V TaskbarDa /T REG_DWORD /D 0 /F");
    }

    internal static void EnableWidgets()
    {
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V TaskbarDa /F");
    }

    internal static void DisableChat()
    {
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V TaskbarMn /T REG_DWORD /D 0 /F");
    }

    internal static void EnableChat()
    {
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V TaskbarMn /F");
    }

    internal static void DisableShowMoreOptions()
    {
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_CURRENT_USER\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\\InprocServer32\" /V \"\" /F");
    }

    internal static void EnableShowMoreOptions()
    {
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_CURRENT_USER\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\" /F");
    }

    internal static void DisableTPMCheck()
    {
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\SYSTEM\\Setup\\MoSetup\" /V AllowUpgradesWithUnsupportedTPMOrCPU /T REG_DWORD /D 1 /F");
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\SYSTEM\\Setup\\LabConfig\" /V BypassCPUCheck /T REG_DWORD /D 1 /F");
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\SYSTEM\\Setup\\LabConfig\" /V BypassStorageCheck /T REG_DWORD /D 1 /F");
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\SYSTEM\\Setup\\LabConfig\" /V BypassTPMCheck /T REG_DWORD /D 1 /F");
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\SYSTEM\\Setup\\LabConfig\" /V BypassRAMCheck /T REG_DWORD /D 1 /F");
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\SYSTEM\\Setup\\LabConfig\" /V BypassSecureBootCheck /T REG_DWORD /D 1 /F");
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_CURRENT_USER\\Control Panel\\UnsupportedHardwareNotificationCache\" /V SV2 /T REG_DWORD /D 0 /F");
    }

    internal static void EnableTPMCheck()
    {
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\SYSTEM\\Setup\\MoSetup\" /V AllowUpgradesWithUnsupportedTPMOrCPU /T REG_DWORD /D 0 /F");
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_LOCAL_MACHINE\\SYSTEM\\Setup\\LabConfig\" /V BypassTPMCheck /F");
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_LOCAL_MACHINE\\SYSTEM\\Setup\\LabConfig\" /V BypassRAMCheck /F");
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_LOCAL_MACHINE\\SYSTEM\\Setup\\LabConfig\" /V BypassSecureBootCheck /F");
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_LOCAL_MACHINE\\SYSTEM\\Setup\\LabConfig\" /V BypassStorageCheck /F");
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_LOCAL_MACHINE\\SYSTEM\\Setup\\LabConfig\" /V BypassCPUCheck /F");
    }

    internal static void EnableFilesCompactMode()
    {
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V UseCompactMode /T REG_DWORD /D 1 /F");
    }

    internal static void DisableFilesCompactMode()
    {
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V UseCompactMode /T REG_DWORD /D 0 /F");
    }

    internal static void DisableStickers()
    {
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Stickers\" /V EnableStickers /F");
    }

    internal static void EnableStickers()
    {
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Stickers\" /V EnableStickers /T REG_DWORD /D 1 /F");
    }


    internal static void DisableEdgeDiscoverBar()
    {
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V HubsSidebarEnabled /T REG_DWORD /D 0 /F");
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_CURRENT_USER\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V HubsSidebarEnabled /T REG_DWORD /D 0 /F");
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V WebWidgetAllowed /T REG_DWORD /D 0 /F");
    }

    internal static void EnableEdgeDiscoverBar()
    {
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V HubsSidebarEnabled /F");
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_CURRENT_USER\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V HubsSidebarEnabled /F");
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V WebWidgetAllowed /F");
    }

    internal static void DisableEdgeTelemetry()
    {
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V PersonalizationReportingEnabled /T REG_DWORD /D 0 /F");
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_CURRENT_USER\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V PersonalizationReportingEnabled /T REG_DWORD /D 0 /F");
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_CURRENT_USER\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V UserFeedbackAllowed /T REG_DWORD /D 0 /F");
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V UserFeedbackAllowed /T REG_DWORD /D 0 /F");
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V MetricsReportingEnabled /T REG_DWORD /D 0 /F");
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_CURRENT_USER\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V MetricsReportingEnabled /T REG_DWORD /D 0 /F");
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\MicrosoftEdge\\BooksLibrary\" /V EnableExtendedBooksTelemetry /T REG_DWORD /D 0 /F");
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_CURRENT_USER\\SOFTWARE\\Policies\\Microsoft\\MicrosoftEdge\\BooksLibrary\" /V EnableExtendedBooksTelemetry /T REG_DWORD /D 0 /F");
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_CURRENT_USER\\Software\\Microsoft\\Edge\\SmartScreenEnabled\" /V \"\" /T REG_DWORD /D 0 /F");
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_CURRENT_USER\\Software\\Microsoft\\Edge\\SmartScreenPuaEnabled\" /V \"\" /T REG_DWORD /D 0 /F");
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V SpotlightExperiencesAndRecommendationsEnabled /T REG_DWORD /D 0 /F");
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_CURRENT_USER\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V SpotlightExperiencesAndRecommendationsEnabled /T REG_DWORD /D 0 /F");
    }

    internal static void EnableEdgeTelemetry()
    {
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_CURRENT_USER\\Software\\Microsoft\\Edge\\SmartScreenEnabled\" /V \"\" /F");
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_CURRENT_USER\\Software\\Microsoft\\Edge\\SmartScreenPuaEnabled\" /V \"\" /F");
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V MetricsReportingEnabled /F");
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_CURRENT_USER\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V MetricsReportingEnabled /F");
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\MicrosoftEdge\\BooksLibrary\" /V EnableExtendedBooksTelemetry /F");
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_CURRENT_USER\\SOFTWARE\\Policies\\Microsoft\\MicrosoftEdge\\BooksLibrary\" /V EnableExtendedBooksTelemetry /F");
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V PersonalizationReportingEnabled /F");
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V UserFeedbackAllowed /F");
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_CURRENT_USER\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V PersonalizationReportingEnabled /F");
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_CURRENT_USER\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V UserFeedbackAllowed /F");
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V SpotlightExperiencesAndRecommendationsEnabled /F");
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_CURRENT_USER\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V SpotlightExperiencesAndRecommendationsEnabled /F");
    }


    internal static void DisableCoPilotAI()
    {
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_CURRENT_USER\\Software\\Policies\\Microsoft\\Windows\\WindowsCopilot\" /V TurnOffWindowsCopilot /T REG_DWORD /D 1 /F");
    }

    internal static void EnableCoPilotAI()
    {
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_CURRENT_USER\\Software\\Policies\\Microsoft\\Windows\\WindowsCopilot\" /V TurnOffWindowsCopilot /F");
    }


    internal static void DisableVisualStudioTelemetry()
    {
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_CURRENT_USER\\Software\\Microsoft\\VisualStudio\\Telemetry\" /V TurnOffSwitch /T REG_DWORD /D 1 /F");
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\Feedback\" /V DisableFeedbackDialog /T REG_DWORD /D 1 /F");
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\Feedback\" /V DisableEmailInput /T REG_DWORD /D 1 /F");
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\Feedback\" /V DisableScreenshotCapture /T REG_DWORD /D 1 /F");
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\Software\\Policies\\Microsoft\\VisualStudio\\SQM\" /V OptIn /T REG_DWORD /D 0 /F");
        OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\Setup\" /V ConcurrentDownloads /T REG_DWORD /D 2 /F");

        if (Environment.Is64BitOperatingSystem)
        {
            OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\VSCommon\\14.0\\SQM\" /V OptIn /T REG_DWORD /D 0 /F");
            OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\VSCommon\\15.0\\SQM\" /V OptIn /T REG_DWORD /D 0 /F");
            OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\VSCommon\\16.0\\SQM\" /V OptIn /T REG_DWORD /D 0 /F");
        }
        else
        {
            OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\VSCommon\\14.0\\SQM\" /V OptIn /T REG_DWORD /D 0 /F");
            OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\VSCommon\\15.0\\SQM\" /V OptIn /T REG_DWORD /D 0 /F");
            OptimizationOptions.StartInCmd("REG ADD \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\VSCommon\\16.0\\SQM\" /V OptIn /T REG_DWORD /D 0 /F");
        }
        OptimizationOptions.StartInCmd("SC Config VSStandardCollectorService150 Start= disabled");
    }

    internal static void EnableVisualStudioTelemetry()
    {
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_CURRENT_USER\\Software\\Microsoft\\VisualStudio\\Telemetry\" /V TurnOffSwitch /F");
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\Feedback\" /V DisableFeedbackDialog /F");
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\Feedback\" /V DisableEmailInput /F");
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\Feedback\" /V DisableScreenshotCapture /F");
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_LOCAL_MACHINE\\Software\\Policies\\Microsoft\\VisualStudio\\SQM\" /V OptIn /F");
        OptimizationOptions.StartInCmd("REG Delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\Setup\" /V ConcurrentDownloads /F");

        if (Environment.Is64BitOperatingSystem)
        {
            OptimizationOptions.StartInCmd("REG Delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\VSCommon\\14.0\\SQM\" /V OptIn /F");
            OptimizationOptions.StartInCmd("REG Delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\VSCommon\\15.0\\SQM\" /V OptIn /F");
            OptimizationOptions.StartInCmd("REG Delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\VSCommon\\16.0\\SQM\" /V OptIn /F");
        }
        else
        {
            OptimizationOptions.StartInCmd("REG Delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\VSCommon\\14.0\\SQM\" /V OptIn /F");
            OptimizationOptions.StartInCmd("REG Delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\VSCommon\\15.0\\SQM\" /V OptIn /F");
            OptimizationOptions.StartInCmd("REG Delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\VSCommon\\16.0\\SQM\" /V OptIn /F");
        }
        OptimizationOptions.StartInCmd("SC Config VSStandardCollectorService150 Start= demand");
    }

    internal static void DisableNvidiaTelemetry()
    {
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\NvTelemetryContainer\" /v Start /t REG_DWORD /d 4 /f");

        OptimizationOptions.StartInCmd("schtasks.exe /change /tn NvTmRepOnLogon_{B2FE1952-0186-46C3-BAEC-A80AA35AC5B8} /disable");
        OptimizationOptions.StartInCmd("schtasks.exe /change /tn NvTmRep_{B2FE1952-0186-46C3-BAEC-A80AA35AC5B8} /disable");
        OptimizationOptions.StartInCmd("schtasks.exe /change /tn NvTmMon_{B2FE1952-0186-46C3-BAEC-A80AA35AC5B8} /disable");
        OptimizationOptions.StartInCmd("net.exe stop NvTelemetryContainer");
        OptimizationOptions.StartInCmd("sc.exe config NvTelemetryContainer start= disabled");
        OptimizationOptions.StartInCmd("sc.exe stop NvTelemetryContainer");
    }

    internal static void EnableNvidiaTelemetry()
    {
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\NvTelemetryContainer\" /v Start /t REG_DWORD /d 2 /f");

        OptimizationOptions.StartInCmd("schtasks.exe /change /tn NvTmRepOnLogon_{B2FE1952-0186-46C3-BAEC-A80AA35AC5B8} /enable");
        OptimizationOptions.StartInCmd("schtasks.exe /change /tn NvTmRep_{B2FE1952-0186-46C3-BAEC-A80AA35AC5B8} /enable");
        OptimizationOptions.StartInCmd("schtasks.exe /change /tn NvTmMon_{B2FE1952-0186-46C3-BAEC-A80AA35AC5B8} /enable");
        OptimizationOptions.StartInCmd("net.exe start NvTelemetryContainer");
        OptimizationOptions.StartInCmd("sc.exe config NvTelemetryContainer start= enabled");
        OptimizationOptions.StartInCmd("sc.exe start NvTelemetryContainer");
    }

    internal static void DisableChromeTelemetry()
    {

        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Google\\Chrome\" /v MetricsReportingEnabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Google\\Chrome\" /v ChromeCleanupReportingEnabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Google\\Chrome\" /v ChromeCleanupEnabled /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Google\\Chrome\" /v UserFeedbackAllowed /t REG_DWORD /d 0 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Google\\Chrome\" /v DeviceMetricsReportingEnabled /t REG_DWORD /d 0 /f");
    }

    internal static void EnableChromeTelemetry()
    {
        OptimizationOptions.StartInCmd("reg delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Google\\Chrome\" /v MetricsReportingEnabled /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Google\\Chrome\" /v ChromeCleanupReportingEnabled /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Google\\Chrome\" /v ChromeCleanupEnabled /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Google\\Chrome\" /v UserFeedbackAllowed /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Google\\Chrome\" /v DeviceMetricsReportingEnabled /f");

    }

    internal static void DisableFirefoxTelemetry()
    {
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Mozilla\\Firefox\" /v DisableTelemetry /t REG_DWORD /d 1 /f");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Mozilla\\Firefox\" /v DisableDefaultBrowserAgent /t REG_DWORD /d 1 /f");

        OptimizationOptions.StartInCmd("schtasks.exe /change /disable /tn \"\\Mozilla\\Firefox Default Browser Agent 308046B0AF4A39CB\"");
        OptimizationOptions.StartInCmd("schtasks.exe /change /disable /tn \"\\Mozilla\\Firefox Default Browser Agent D2CEEC440E2074BD\"");
    }

    internal static void EnableFirefoxTelemetry()
    {
        OptimizationOptions.StartInCmd("reg delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Mozilla\\Firefox\" /v DisableTelemetry /f");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Mozilla\\Firefox\" /v DisableDefaultBrowserAgent /f");

        OptimizationOptions.StartInCmd("schtasks.exe /change /enable /tn \"\\Mozilla\\Firefox Default Browser Agent 308046B0AF4A39CB\"");
        OptimizationOptions.StartInCmd("schtasks.exe /change /enable /tn \"\\Mozilla\\Firefox Default Browser Agent D2CEEC440E2074BD\"");
    }
    internal static void DisableHibernation()
    {
        OptimizationOptions.StartInCmd("powercfg -h off");
        OptimizationOptions.StartInCmd("powercfg -h off");
        OptimizationOptions.StartInCmd("reg add \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Power\" /v HibernateEnabled /t REG_DWORD /d 0 /f");
    }

    internal static void EnableHibernation()
    {
        OptimizationOptions.StartInCmd("powercfg -h on");
        OptimizationOptions.StartInCmd("reg delete \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Power\" /v HibernateEnabled /f");
        OptimizationOptions.StartInCmd("powercfg -h on");
    }
}