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

    /*internal static async Task RemoveEdgeScript(string powerShellScript)
    {
        try
        {
            // Write the PowerShell script content to a temporary file
            var tempPowerShellFile = Path.GetTempFileName() + ".ps1";
            File.WriteAllText(tempPowerShellFile, powerShellScript);

            // Execute the PowerShell script asynchronously
            // Use Task.Run to start the PowerShell process on a thread pool thread
            await Task.Run(() =>
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{tempPowerShellFile}\"",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }).WaitForExit();
            });

            // Clean up the temporary file
            File.Delete(tempPowerShellFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
        }
    }*/

    internal static async Task ExecuteBatchFileAsync()
    {
        var batchFileName = "RemoveEdge.bat"; // Name of your .bat file
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var batchFilePath = Path.Combine(appDirectory, "Helpers", batchFileName);
        using Process process = new Process();
        process.StartInfo.FileName = "cmd.exe";
        process.StartInfo.Arguments = $"/c \"{batchFilePath}\""; // /c option carries out the command specified by the string and then terminates
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine($"Error: {e.Data}");
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await Task.Run(() => process.WaitForExit()); // Wait for the process to exit
    }

    internal static async Task<int> StartInCmd(string command)
    {
        try
        {
            using var p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.Arguments = $"/C {command}";
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            // Start the process in a separate Task
            Task startTask = Task.Run(() => p.Start());

            // Await the start task to ensure process starts
            await startTask;

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
    public static void XamlSwitches(ToggleSwitch toggleSwitch, bool isAutomated = true)
    {
        if (!isAutomated && toggleSwitch != null && toggleSwitch.Tag != null)
        {
            switch (toggleSwitch.Tag)
            {
                case "PerformanceTweaks":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.EnablePerformanceTweaks();
                        
                    }
                    else
                    {
                        OptimizeSystemHelper.DisablePerformanceTweaks();
                        
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
                case "Superfetch":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableSuperfetch();
                        
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableSuperfetch();
                        
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
                case "ClassicStartMenu":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.EnableClassicStartMenu();
                        
                    }
                    else
                    {
                        OptimizeSystemHelper.DisableClassicStartMenu();
                        
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
                case "NTFSTimeStamp":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableNTFSTimeStamp();
                        
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableNTFSTimeStamp();
                        
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
                case "LegacyVolumeSlider":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableLegacyVolumeSlider();
                        
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableLegacyVolumeSlider();
                        
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
                case "Privacy":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.EnhancePrivacy();
                        
                    }
                    else
                    {
                        OptimizeSystemHelper.CompromisePrivacy();
                        
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
                case "StartMenuAds":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableStartMenuAds();
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableStartMenuAds();
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
                case "ShowMoreOptions":
                    if (toggleSwitch.IsOn)
                    {
                        OptimizeSystemHelper.DisableShowMoreOptions();
                        
                    }
                    else
                    {
                        OptimizeSystemHelper.EnableShowMoreOptions();
                        
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
            }
        }
    }
}
