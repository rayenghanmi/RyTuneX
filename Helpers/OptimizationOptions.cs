using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Collections.ObjectModel;
using System.Management.Automation;

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
        if (string.IsNullOrEmpty(command))
        {
            return;
        }
        LogHelper.Log("Starting CMD Command");
        using var p = new Process();
        p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        p.StartInfo.FileName = "cmd.exe";
        p.StartInfo.Arguments = "/C " + command;
        p.StartInfo.CreateNoWindow = true;
        p.Start();
        p.WaitForExit();
        p.Close();
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
}
