using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Collections.ObjectModel;
using System.Management.Automation;
using Windows.Storage;
using Microsoft.UI.Xaml.Controls;

namespace RyTuneX.Helpers;
internal class OptimizationOptions
{
    public static List<KeyValuePair<string, string>> GetUWPApps(bool uninstallableOnly)
    {
        var installedApps = new List<KeyValuePair<string, string>>();

        using (var PowerShellInstance = PowerShell.Create())
        {
            LogHelper.Log("Getting Installed Apps [OptimizationOptions.cs]");
            PowerShellInstance.AddScript("Set-ExecutionPolicy RemoteSigned -Scope Process");
            PowerShellInstance.AddScript("Import-Module Appx")
                .AddArgument("-ExecutionPolicy Bypass");

            if (uninstallableOnly)
            {
                PowerShellInstance.AddScript(@"Get-AppxPackage | Where {$_.NonRemovable -like ""False""} | Select  Name,InstallLocation");
            }
            else
            {
                PowerShellInstance.AddScript("Get-AppxPackage | Select Name,InstallLocation");
            }

            string[] tmp;
            Collection<PSObject> psResult;
            try
            {
                psResult = PowerShellInstance.Invoke();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return installedApps;
            }

            if (psResult == null)
            {
                return installedApps;
            }
            foreach (var x in psResult)
            {
                tmp = x.ToString().Replace("@", string.Empty).Replace("{", string.Empty).Replace("}", string.Empty).Replace("Name=", string.Empty).Replace("InstallLocation=", string.Empty).Trim().Split(';');
                if (!installedApps.Exists(i => i.Key == tmp[0]))
                {
                    installedApps.Add(new KeyValuePair<string, string>(tmp[0], tmp[1]));
                }
            }
        }
        LogHelper.Log("Returning Installed Apps [OptimizationOptions.cs]");
        return installedApps;
    }

    internal static bool ServiceExists(string serviceName)
    {
        return Array.Exists(ServiceController.GetServices(), (serviceController => serviceController.ServiceName.Equals(serviceName)));
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
    internal static void StartInCmd(string command)
    {
        try
        {
            using var p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.Arguments = $"/C {command}";
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.Start();
            p.WaitForExit();

            var output = p.StandardOutput.ReadToEnd();
            var error = p.StandardError.ReadToEnd();

            Debug.WriteLine($"Command Output: {output}");
            Debug.WriteLine($"Command Error: {error}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error running command: {ex.Message}");
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
    public static void XamlSwitches(ToggleSwitch toggleSwitch)
    {
        if (toggleSwitch != null && toggleSwitch.Tag != null)
        {
            switch (toggleSwitch.Tag)
            {
                case "PerformanceTweaks":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.EnablePerformanceTweaks();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.DisablePerformanceTweaks();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "TelemetryServices":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableTelemetryServices();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableTelemetryServices();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "HomeGroup":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableHomeGroup();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableHomeGroup();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "PrintService":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisablePrintService();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnablePrintService();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "Superfetch":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSuperfetch();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSuperfetch();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "CompatibilityAssistant":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableCompatibilityAssistant();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableCompatibilityAssistant();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "SystemRestore":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSystemRestore();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSystemRestore();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "Search":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSearch();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSearch();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "SMBv1":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSMB("1");
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSMB("1");
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "SMBv2":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSMB("2");
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSMB("2");
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "SMBv3":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSMB("3");
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSMB("3");
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "NTFSTimeStamp":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableNTFSTimeStamp();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableNTFSTimeStamp();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "ErrorReporting":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableErrorReporting();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableErrorReporting();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "LegacyVolumeSlider":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableLegacyVolumeSlider();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableLegacyVolumeSlider();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "Cortana":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableCortana();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableCortana();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "GamingMode":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.EnableGamingMode();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.DisableGamingMode();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "AutomaticUpdates":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableAutomaticUpdates();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableAutomaticUpdates();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "StoreUpdates":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableStoreUpdates();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableStoreUpdates();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "OneDrive":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableOneDrive();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableOneDrive();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "SensorServices":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSensorServices();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSensorServices();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "Privacy":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.EnhancePrivacy();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.CompromisePrivacy();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "GameBar":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableGameBar();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableGameBar();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "QuickAccessHistory":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableQuickAccessHistory();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableQuickAccessHistory();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "StartMenuAds":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableStartMenuAds();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableStartMenuAds();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "MyPeople":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableMyPeople();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableMyPeople();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "Drivers":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.ExcludeDrivers();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.IncludeDrivers();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "WindowsInk":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableWindowsInk();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableWindowsInk();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "SpellingAndTypingFeatures":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSpellingAndTypingFeatures();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSpellingAndTypingFeatures();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "FaxService":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableFaxService();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableFaxService();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "InsiderService":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableInsiderService();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableInsiderService();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "SmartScreen":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSmartScreen();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSmartScreen();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "CloudClipboard":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableCloudClipboard();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableCloudClipboard();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "StickyKeys":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableStickyKeys();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableStickyKeys();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "CastToDevice":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.RemoveCastToDevice();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.AddCastToDevice();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "VBS":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableVBS();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableVBS();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "TaskbarToLeft":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.AlignTaskbarToLeft();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.AlignTaskbarToCenter();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "SnapAssist":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSnapAssist();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSnapAssist();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "Widgets":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableWidgets();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableWidgets();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "Chat":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableChat();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableChat();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "ShowMoreOptions":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableShowMoreOptions();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableShowMoreOptions();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "TPMCheck":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableTPMCheck();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableTPMCheck();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "FilesCompactMode":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.EnableFilesCompactMode();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.DisableFilesCompactMode();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "Stickers":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableStickers();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableStickers();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "EdgeDiscoverBar":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableEdgeDiscoverBar();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableEdgeDiscoverBar();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "EdgeTelemetry":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableEdgeTelemetry();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableEdgeTelemetry();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "CoPilotAI":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableCoPilotAI();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableCoPilotAI();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "VisualStudioTelemetry":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableVisualStudioTelemetry();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableVisualStudioTelemetry();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "NvidiaTelemetry":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableNvidiaTelemetry();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableNvidiaTelemetry();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "ChromeTelemetry":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableChromeTelemetry();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableChromeTelemetry();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "FirefoxTelemetry":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableFirefoxTelemetry();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableFirefoxTelemetry();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
                case "Hibernation":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableHibernation();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = true;
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableHibernation();
                        ApplicationData.Current.LocalSettings.Values[(string)toggleSwitch.Tag] = false;
                    }
                    break;
            }
        }
    }
}
