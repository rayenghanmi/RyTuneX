using System.Diagnostics;
using System.Management.Automation;
using System.ServiceProcess;
using Microsoft.UI.Xaml.Controls;

namespace RyTuneX.Helpers;
internal class OptimizationOptions
{
    public static List<KeyValuePair<string, string>> GetUWPApps(bool uninstallableOnly)
    {
        var installedApps = new List<KeyValuePair<string, string>>();

        string command;
        if (uninstallableOnly)
        {
            command = @"powershell.exe -Command ""Get-AppxPackage | Where-Object { $_.NonRemovable -eq $false } | Select-Object Name,InstallLocation""";
        }
        else
        {
            command = @"powershell.exe -Command ""Get-AppxPackage | Select-Object Name,InstallLocation""";
        }

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C {command}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            {
                process.Start();

                // Read the output directly
                while (!process.StandardOutput.EndOfStream)
                {
                    var line = process.StandardOutput.ReadLine();
                    var parts = line?.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts?.Length == 2 && !installedApps.Exists(i => i.Key == parts[0]))
                    {
                        installedApps.Add(new KeyValuePair<string, string>(parts[0], parts[1]));
                    }
                }

                process.WaitForExit();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return installedApps;
        }

        LogHelper.Log("Returning Installed Apps [GetUWPApps]");
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

    internal static async Task ExecuteBatchFileAsync()
    {
        try
        {
            // Get the path to the PowerShell script file
            var scriptFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "RemoveEdge.ps1");



            if (!File.Exists(scriptFilePath))
            {
                await LogHelper.Log($"Script file not found: {scriptFilePath}");
                return;
            }

            // Read the content of the script file
            var scriptContent = File.ReadAllText(scriptFilePath);

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
                    await LogHelper.Log($"PowerShell Error: {error}");
                }
            }
            else
            {
                await LogHelper.Log("PowerShell script executed successfully.");
            }
        }
        catch (Exception ex)
        {
            await LogHelper.Log($"Error: {ex.Message}");
        }
    }

    internal static void RestartExplorer()
    {
        var ExplorerProcess = Process.GetProcessesByName("explorer");
        foreach (var p in ExplorerProcess)
        {
            p.Kill();
        }
        ExplorerProcess = Process.GetProcessesByName("explorer");
        if (ExplorerProcess.Length == 0)
        {
            Process.Start("explorer.exe");
        }
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
