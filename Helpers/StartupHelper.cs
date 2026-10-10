using Microsoft.Win32;
using RyTuneX.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace RyTuneX.Helpers;

public static class StartupHelper
{
    private const string StartupApprovedRunHKCU = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string StartupApprovedRunHKLM = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string StartupApprovedRun32HKLM = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run32";
    private const string StartupApprovedFolderHKCU = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder";
    private const string StartupApprovedFolderHKLM = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder";

    public static async Task<List<StartupItem>> GetStartupItemsAsync()
    {
        return await Task.Run(() =>
        {
            var list = new List<StartupItem>();

            try
            {
                // HKCU Run & RunOnce
                ReadRegistryRunKeys(list, RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", StartupLocationType.HKCU_Run, "Registry (HKCU Run)", RegistryView.Default, StartupApprovedRunHKCU);
                ReadRegistryRunKeys(list, RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", StartupLocationType.HKCU_RunOnce, "Registry (HKCU RunOnce)", RegistryView.Default, StartupApprovedRunHKCU);

                // HKLM Run & RunOnce (64-bit and 32-bit)
                ReadRegistryRunKeys(list, RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", StartupLocationType.HKLM_Run, "Registry (HKLM Run)", RegistryView.Registry64, StartupApprovedRunHKLM);
                ReadRegistryRunKeys(list, RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", StartupLocationType.HKLM_RunOnce, "Registry (HKLM RunOnce)", RegistryView.Registry64, StartupApprovedRunHKLM);

                if (Environment.Is64BitOperatingSystem)
                {
                    ReadRegistryRunKeys(list, RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run", StartupLocationType.HKLM_Run, "Registry (HKLM Run 32-bit)", RegistryView.Registry32, StartupApprovedRun32HKLM);
                }

                // User Startup Folder
                var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                ReadStartupFolder(list, userFolder, StartupLocationType.UserStartupFolder, "Startup Folder (User)", RegistryHive.CurrentUser, StartupApprovedFolderHKCU);

                // Common Startup Folder
                var commonFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup);
                ReadStartupFolder(list, commonFolder, StartupLocationType.CommonStartupFolder, "Startup Folder (Common)", RegistryHive.LocalMachine, StartupApprovedFolderHKLM);

                // Scheduled Tasks with Logon/Startup triggers
                ReadScheduledTasks(list);

                // UWP / AppX Packaged Startup Tasks
                ReadUwpStartupTasks(list);

                // Direct StartupApproved keys enumeration (captures disabled apps tracked by Task Manager)
                ReadStartupApprovedOrphanedItems(list);
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Error fetching startup items: {ex.Message}");
            }

            return list;
        });
    }

    private static void ReadRegistryRunKeys(List<StartupItem> list, RegistryHive hive, string subKeyPath, StartupLocationType locationType, string locationLabel, RegistryView view, string startupApprovedPath)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var subKey = baseKey.OpenSubKey(subKeyPath);
            if (subKey == null) return;

            foreach (var valName in subKey.GetValueNames())
            {
                if (string.IsNullOrWhiteSpace(valName)) continue;
                var rawCommand = subKey.GetValue(valName)?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(rawCommand)) continue;

                var exePath = ExtractExecutablePath(rawCommand);
                var isValid = File.Exists(exePath);
                var publisher = GetPublisher(exePath);
                var description = GetDescription(exePath, valName);
                var fileSize = isValid ? new FileInfo(exePath).Length : 0;
                var isEnabled = IsStartupApprovedEnabled(hive, view, startupApprovedPath, valName);
                var impact = CalculateImpact(valName, exePath, publisher, isValid, fileSize);

                list.Add(new StartupItem
                {
                    Id = $"{hive}_{subKeyPath}_{valName}",
                    Name = valName,
                    Command = rawCommand,
                    ExecutablePath = exePath,
                    Publisher = publisher,
                    Description = description,
                    Location = locationLabel,
                    LocationType = locationType,
                    Hive = hive,
                    View = view,
                    RegistryPath = subKeyPath,
                    ValueName = valName,
                    FilePath = exePath,
                    IsEnabled = isEnabled,
                    IsValid = isValid,
                    Impact = impact,
                    FileSizeBytes = fileSize
                });
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogWarning($"ReadRegistryRunKeys failed for {subKeyPath}: {ex.Message}");
        }
    }

    private static void ReadStartupFolder(List<StartupItem> list, string folderPath, StartupLocationType locationType, string locationLabel, RegistryHive hive, string startupApprovedPath)
    {
        try
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return;

            var files = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                if (fileName.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)) continue;

                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext is not (".lnk" or ".exe" or ".bat" or ".cmd" or ".vbs" or ".url")) continue;

                var displayName = Path.GetFileNameWithoutExtension(file);
                var targetPath = ext == ".lnk" ? ResolveShortcutTarget(file) : file;
                var isValid = File.Exists(targetPath) || File.Exists(file);
                var publisher = GetPublisher(targetPath);
                var description = GetDescription(targetPath, displayName);
                var fileSize = File.Exists(targetPath) ? new FileInfo(targetPath).Length : (File.Exists(file) ? new FileInfo(file).Length : 0);
                var isEnabled = IsStartupApprovedEnabled(hive, RegistryView.Default, startupApprovedPath, fileName);
                var impact = CalculateImpact(displayName, targetPath, publisher, isValid, fileSize);

                list.Add(new StartupItem
                {
                    Id = $"Folder_{folderPath}_{fileName}",
                    Name = displayName,
                    Command = file,
                    ExecutablePath = targetPath,
                    Publisher = publisher,
                    Description = description,
                    Location = locationLabel,
                    LocationType = locationType,
                    Hive = hive,
                    View = RegistryView.Default,
                    FilePath = file,
                    ValueName = fileName,
                    IsEnabled = isEnabled,
                    IsValid = isValid,
                    Impact = impact,
                    FileSizeBytes = fileSize
                });
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogWarning($"ReadStartupFolder failed for {folderPath}: {ex.Message}");
        }
    }

    private static void ReadScheduledTasks(List<StartupItem> list)
    {
        try
        {
            var script = "Get-ScheduledTask | Where-Object { $_.State -ne 'Disabled' -or $_.Triggers } | Where-Object { $t = $_.Triggers; $t | Where-Object { $_.cimClass.CimClassName -like '*LogonTrigger*' -or $_.cimClass.CimClassName -like '*BootTrigger*' } } | Select-Object TaskName, TaskPath, State, @{N='Execute';E={($_.Actions | Where-Object { $_.Execute }).Execute -join ';'}}, @{N='Arguments';E={($_.Actions | Where-Object { $_.Execute }).Arguments -join ';'}} | ConvertTo-Json -Compress";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{script}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null) return;

            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(5000);

            if (string.IsNullOrWhiteSpace(output)) return;

            using var doc = JsonDocument.Parse(output);
            var root = doc.RootElement;

            List<JsonElement> taskElements = [];
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in root.EnumerateArray()) taskElements.Add(el);
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                taskElements.Add(root);
            }

            foreach (var el in taskElements)
            {
                var taskName = el.GetProperty("TaskName").GetString() ?? "";
                if (string.IsNullOrWhiteSpace(taskName)) continue;

                var taskPath = el.TryGetProperty("TaskPath", out var tp) ? tp.GetString() ?? "" : "";
                var trimmedPath = taskPath.Trim('\\');
                var fullTaskPath = string.IsNullOrEmpty(trimmedPath)
                    ? $@"\{taskName}"
                    : $@"\{trimmedPath}\{taskName}";

                var rawExe = el.TryGetProperty("Execute", out var exeProp) ? exeProp.GetString() ?? "" : "";
                if (rawExe.Contains(';'))
                {
                    rawExe = rawExe.Split(';')[0];
                }
                rawExe = Environment.ExpandEnvironmentVariables(rawExe.Trim('\"', ' '));
                var exePath = ExtractExecutablePath(rawExe);

                var rawArgs = el.TryGetProperty("Arguments", out var argProp) ? argProp.GetString() ?? "" : "";
                if (rawArgs.Contains(';'))
                {
                    rawArgs = rawArgs.Split(';')[0];
                }
                rawArgs = Environment.ExpandEnvironmentVariables(rawArgs.Trim());

                var isValid = string.IsNullOrEmpty(exePath) || File.Exists(exePath);
                var publisher = !string.IsNullOrEmpty(exePath) && File.Exists(exePath)
                    ? GetPublisher(exePath)
                    : "Scheduled Task";
                var description = !string.IsNullOrEmpty(exePath) && File.Exists(exePath)
                    ? GetDescription(exePath, taskName)
                    : (string.IsNullOrEmpty(trimmedPath) ? $"Windows Scheduled Task: {taskName}" : $"Windows Scheduled Task ({taskPath}): {taskName}");
                var fileSize = !string.IsNullOrEmpty(exePath) && File.Exists(exePath)
                    ? new FileInfo(exePath).Length
                    : 0;

                var command = !string.IsNullOrEmpty(exePath)
                    ? (!string.IsNullOrWhiteSpace(rawArgs) ? $"\"{exePath}\" {rawArgs}" : $"\"{exePath}\"")
                    : $"schtasks /Run /TN \"{fullTaskPath}\"";

                var stateStr = el.TryGetProperty("State", out var s) ? s.ToString() : "";
                var isEnabled = !stateStr.Equals("Disabled", StringComparison.OrdinalIgnoreCase);

                var impact = CalculateImpact(taskName, exePath, publisher, isValid, fileSize);

                list.Add(new StartupItem
                {
                    Id = $"Task_{fullTaskPath}",
                    Name = taskName,
                    Command = command,
                    ExecutablePath = !string.IsNullOrEmpty(exePath) ? exePath : "schtasks.exe",
                    Publisher = publisher,
                    Description = description,
                    Location = "Scheduled Task",
                    LocationType = StartupLocationType.ScheduledTask,
                    TaskName = fullTaskPath,
                    TaskPath = taskPath,
                    FilePath = !string.IsNullOrEmpty(exePath) && File.Exists(exePath) ? exePath : null,
                    IsEnabled = isEnabled,
                    IsValid = isValid,
                    Impact = impact,
                    FileSizeBytes = fileSize
                });
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogWarning($"ReadScheduledTasks failed: {ex.Message}");
        }
    }

    private static void ReadUwpStartupTasks(List<StartupItem> list)
    {
        try
        {
            var appModelKeyPath = @"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData";
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
            using var sysAppData = baseKey.OpenSubKey(appModelKeyPath);
            if (sysAppData == null) return;

            var existingNames = new HashSet<string>(list.Select(x => x.Name), StringComparer.OrdinalIgnoreCase);

            foreach (var pkgName in sysAppData.GetSubKeyNames())
            {
                using var pkgKey = sysAppData.OpenSubKey($@"{pkgName}\StartupTask");
                if (pkgKey == null) continue;

                foreach (var taskId in pkgKey.GetSubKeyNames())
                {
                    using var taskKey = pkgKey.OpenSubKey(taskId);
                    if (taskKey == null) continue;

                    var stateObj = taskKey.GetValue("State");
                    int stateVal = stateObj is int i ? i : 2;

                    var isEnabled = stateVal == 2;
                    var cleanName = taskId.Replace("StartupTask", "").Replace("Task", "");
                    if (string.IsNullOrWhiteSpace(cleanName)) cleanName = pkgName.Split('_')[0];

                    if (existingNames.Contains(cleanName) || existingNames.Contains(taskId)) continue;

                    if (IsStartupApprovedEnabled(RegistryHive.CurrentUser, RegistryView.Default, StartupApprovedRunHKCU, taskId) == false ||
                        IsStartupApprovedEnabled(RegistryHive.CurrentUser, RegistryView.Default, StartupApprovedRunHKCU, pkgName) == false)
                    {
                        isEnabled = false;
                    }

                    var impact = CalculateImpact(cleanName, cleanName, "Microsoft Store / UWP", true, 0);

                    list.Add(new StartupItem
                    {
                        Id = $"UWP_{pkgName}_{taskId}",
                        Name = cleanName,
                        Command = $"UWP AppX Package: {pkgName}",
                        ExecutablePath = "WinStoreApp",
                        Publisher = "Microsoft Store App",
                        Description = $"UWP AppX Startup Task ({taskId})",
                        Location = "UWP / Store App",
                        LocationType = StartupLocationType.HKCU_Run,
                        ValueName = taskId,
                        Hive = RegistryHive.CurrentUser,
                        IsEnabled = isEnabled,
                        IsValid = true,
                        Impact = impact
                    });

                    existingNames.Add(cleanName);
                }
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogWarning($"ReadUwpStartupTasks failed: {ex.Message}");
        }
    }

    private static void ReadStartupApprovedOrphanedItems(List<StartupItem> list)
    {
        var approvedKeys = new (RegistryHive Hive, RegistryView View, string KeyPath, StartupLocationType LocType, string LocLabel)[]
        {
            (RegistryHive.CurrentUser, RegistryView.Default, StartupApprovedRunHKCU, StartupLocationType.HKCU_Run, "Registry (HKCU)"),
            (RegistryHive.LocalMachine, RegistryView.Registry64, StartupApprovedRunHKLM, StartupLocationType.HKLM_Run, "Registry (HKLM)"),
            (RegistryHive.LocalMachine, RegistryView.Registry32, StartupApprovedRun32HKLM, StartupLocationType.HKLM_Run, "Registry (HKLM 32-bit)"),
            (RegistryHive.CurrentUser, RegistryView.Default, StartupApprovedFolderHKCU, StartupLocationType.UserStartupFolder, "Startup Folder (User)"),
            (RegistryHive.LocalMachine, RegistryView.Default, StartupApprovedFolderHKLM, StartupLocationType.CommonStartupFolder, "Startup Folder (Common)")
        };

        var existingNames = new HashSet<string>(list.Select(x => x.Name), StringComparer.OrdinalIgnoreCase);
        var existingValueNames = new HashSet<string>(list.Where(x => !string.IsNullOrEmpty(x.ValueName)).Select(x => x.ValueName!), StringComparer.OrdinalIgnoreCase);

        foreach (var (hive, view, keyPath, locType, locLabel) in approvedKeys)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                using var subKey = baseKey.OpenSubKey(keyPath);
                if (subKey == null) continue;

                foreach (var valName in subKey.GetValueNames())
                {
                    if (string.IsNullOrWhiteSpace(valName)) continue;
                    if (existingNames.Contains(valName) || existingValueNames.Contains(valName)) continue;

                    var bytes = subKey.GetValue(valName) as byte[];
                    if (bytes == null || bytes.Length == 0) continue;

                    // Byte 0: 0x02 or even = Enabled; 0x03, 0x01, 0x07, 0x00 or odd = Disabled
                    var isEnabled = (bytes[0] & 1) == 0;

                    var exePath = FindExecutableForApp(valName);
                    var rawCommand = !string.IsNullOrEmpty(exePath) ? $"\"{exePath}\"" : valName;
                    var isValid = File.Exists(exePath);
                    var publisher = GetPublisher(exePath);
                    var description = GetDescription(exePath, valName);
                    var fileSize = isValid ? new FileInfo(exePath).Length : 0;
                    var impact = CalculateImpact(valName, exePath, publisher, isValid, fileSize);

                    list.Add(new StartupItem
                    {
                        Id = $"Approved_{hive}_{keyPath}_{valName}",
                        Name = valName,
                        Command = rawCommand,
                        ExecutablePath = exePath,
                        Publisher = publisher,
                        Description = description,
                        Location = locLabel,
                        LocationType = locType,
                        Hive = hive,
                        View = view,
                        RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                        ValueName = valName,
                        FilePath = exePath,
                        IsEnabled = isEnabled,
                        IsValid = isValid,
                        Impact = impact,
                        FileSizeBytes = fileSize
                    });

                    existingNames.Add(valName);
                }
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogWarning($"ReadStartupApprovedOrphanedItems failed for {keyPath}: {ex.Message}");
            }
        }
    }

    private static string FindExecutableForApp(string appName)
    {
        try
        {
            // Check Win32_StartupCommand via WMI / ManagementObject
            using var searcher = new System.Management.ManagementObjectSearcher($"SELECT * FROM Win32_StartupCommand WHERE Name = '{appName.Replace("'", "''")}'");
            foreach (var obj in searcher.Get())
            {
                var cmd = obj["Command"]?.ToString();
                if (!string.IsNullOrWhiteSpace(cmd))
                {
                    var path = ExtractExecutablePath(cmd);
                    if (File.Exists(path)) return path;
                }
            }
        }
        catch { }

        try
        {
            // Check Registry Uninstall keys
            var hives = new[] { (RegistryHive.CurrentUser, RegistryView.Default), (RegistryHive.LocalMachine, RegistryView.Registry64), (RegistryHive.LocalMachine, RegistryView.Registry32) };
            foreach (var (hive, view) in hives)
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                using var unKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                if (unKey != null)
                {
                    foreach (var sub in unKey.GetSubKeyNames())
                    {
                        using var itemKey = unKey.OpenSubKey(sub);
                        var dispName = itemKey?.GetValue("DisplayName")?.ToString();
                        if (!string.IsNullOrEmpty(dispName) && dispName.Contains(appName, StringComparison.OrdinalIgnoreCase))
                        {
                            var loc = itemKey?.GetValue("InstallLocation")?.ToString();
                            if (!string.IsNullOrEmpty(loc) && Directory.Exists(loc))
                            {
                                var exes = Directory.GetFiles(loc, "*.exe", SearchOption.TopDirectoryOnly);
                                if (exes.Length > 0) return exes[0];
                            }
                        }
                    }
                }
            }
        }
        catch { }

        try
        {
            // Common AppData / Program Files paths
            string[] searchFolders = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appName),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appName),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), appName),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), appName)
            };

            foreach (var folder in searchFolders)
            {
                if (Directory.Exists(folder))
                {
                    var exes = Directory.GetFiles(folder, "*.exe", SearchOption.TopDirectoryOnly);
                    if (exes.Length > 0) return exes[0];
                }
            }
        }
        catch { }

        return string.Empty;
    }

    private static bool IsStartupApprovedEnabled(RegistryHive hive, RegistryView view, string startupApprovedKeyPath, string valueName)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var subKey = baseKey.OpenSubKey(startupApprovedKeyPath);
            if (subKey != null)
            {
                var val = subKey.GetValue(valueName);
                if (val is byte[] bytes && bytes.Length > 0)
                {
                    // Byte 0: 0x02 or even = Enabled; 0x03, 0x01, 0x07, 0x00 or odd = Disabled
                    return (bytes[0] & 1) == 0;
                }
            }
        }
        catch
        {
        }
        return true;
    }

    public static async Task<bool> SetStartupItemEnabledAsync(StartupItem item, bool enable)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (item.LocationType == StartupLocationType.ScheduledTask)
                {
                    var (folderPath, leafName) = ResolveTaskFolderAndName(
                        !string.IsNullOrEmpty(item.TaskName) ? item.TaskName : item.Name,
                        item.TaskPath);
                    var fullPath = folderPath == @"\" ? $@"\{leafName}" : $@"{folderPath}\{leafName}";

                    // Native in-process COM Schedule.Service
                    try
                    {
                        var serviceType = Type.GetTypeFromProgID("Schedule.Service");
                        if (serviceType != null)
                        {
                            dynamic? service = Activator.CreateInstance(serviceType);
                            if (service != null)
                            {
                                try
                                {
                                    service.Connect();
                                    dynamic folder = service.GetFolder(folderPath);
                                    try
                                    {
                                        dynamic task = folder.GetTask(leafName);
                                        try
                                        {
                                            task.Enabled = enable;
                                            item.IsEnabled = enable;
                                            return true;
                                        }
                                        finally
                                        {
                                            Marshal.ReleaseComObject(task);
                                        }
                                    }
                                    finally
                                    {
                                        Marshal.ReleaseComObject(folder);
                                    }
                                }
                                finally
                                {
                                    Marshal.ReleaseComObject(service);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _ = LogHelper.LogWarning($"COM Schedule.Service failed for '{leafName}' in '{folderPath}': {ex.Message}");
                    }

                    // schtasks.exe /Change
                    var flag = enable ? "/Enable" : "/Disable";
                    var schtasksExe = Path.Combine(Environment.SystemDirectory, "schtasks.exe");
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = schtasksExe,
                            Arguments = $"/Change /TN \"{fullPath}\" {flag}",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };
                        using var p = Process.Start(psi);
                        if (p != null)
                        {
                            var stdout = p.StandardOutput.ReadToEnd();
                            var stderr = p.StandardError.ReadToEnd();
                            p.WaitForExit(4000);
                            if (p.ExitCode == 0)
                            {
                                item.IsEnabled = enable;
                                return true;
                            }
                            _ = LogHelper.LogWarning($"schtasks /Change failed for {fullPath} (Exit {p.ExitCode}): {stderr.Trim()}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _ = LogHelper.LogWarning($"schtasks /Change execution error for {fullPath}: {ex.Message}");
                    }

                    // PowerShell Enable/Disable-ScheduledTask
                    try
                    {
                        var psAction = enable ? "Enable-ScheduledTask" : "Disable-ScheduledTask";
                        var psFolder = folderPath.EndsWith('\\') ? folderPath : folderPath + @"\";
                        var psScript = $"{psAction} -TaskPath '{psFolder}' -TaskName '{leafName}'";
                        var psPsi = new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{psScript}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };
                        using var psProc = Process.Start(psPsi);
                        if (psProc != null)
                        {
                            var psOut = psProc.StandardOutput.ReadToEnd();
                            var psErr = psProc.StandardError.ReadToEnd();
                            psProc.WaitForExit(6000);
                            if (psProc.ExitCode == 0)
                            {
                                item.IsEnabled = enable;
                                return true;
                            }
                            _ = LogHelper.LogWarning($"PowerShell {psAction} failed for {fullPath}: {psErr.Trim()}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _ = LogHelper.LogWarning($"PowerShell fallback error for {fullPath}: {ex.Message}");
                    }

                    // Verify state
                    var verifiedState = IsScheduledTaskEnabled(leafName, folderPath);
                    if (verifiedState.HasValue && verifiedState.Value == enable)
                    {
                        item.IsEnabled = enable;
                        return true;
                    }

                    return false;
                }

                string approvedPath = item.LocationType switch
                {
                    StartupLocationType.UserStartupFolder => StartupApprovedFolderHKCU,
                    StartupLocationType.CommonStartupFolder => StartupApprovedFolderHKLM,
                    StartupLocationType.HKLM_Run when item.View == RegistryView.Registry32 => StartupApprovedRun32HKLM,
                    StartupLocationType.HKLM_Run or StartupLocationType.HKLM_RunOnce => StartupApprovedRunHKLM,
                    _ => StartupApprovedRunHKCU
                };

                var hive = item.Hive ?? RegistryHive.CurrentUser;
                var view = item.View;
                var name = item.ValueName ?? item.Name;

                try
                {
                    using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                    using var subKey = baseKey.CreateSubKey(approvedPath, true);
                    if (subKey != null)
                    {
                        byte[] bytes = enable
                            ? new byte[] { 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }
                            : new byte[] { 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

                        subKey.SetValue(name, bytes, RegistryValueKind.Binary);
                        item.IsEnabled = enable;
                        return true;
                    }
                }
                catch (UnauthorizedAccessException) when (hive == RegistryHive.LocalMachine)
                {
                    // Direct HKLM write denied: elevate via reg.exe with runas
                    try
                    {
                        var hexData = enable ? "020000000000000000000000" : "030000000000000000000000";
                        var regViewFlag = item.View == RegistryView.Registry32 ? " /reg:32" : " /reg:64";
                        var regArgs = $"add \"HKLM\\{approvedPath}\" /v \"{name}\" /t REG_BINARY /d \"{hexData}\" /f{regViewFlag}";
                        var elevatedPsi = new ProcessStartInfo
                        {
                            FileName = "reg.exe",
                            Arguments = regArgs,
                            UseShellExecute = true,
                            Verb = "runas",
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        };
                        using var elevatedProc = Process.Start(elevatedPsi);
                        elevatedProc?.WaitForExit(5000);
                        if (elevatedProc?.ExitCode == 0)
                        {
                            item.IsEnabled = enable;
                            return true;
                        }
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {
                        // UAC cancelled
                    }
                }
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Failed to set startup item state for {item.Name}: {ex.Message}");
            }
            return false;
        });
    }

    public static async Task<bool> RemoveStartupItemAsync(StartupItem item)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (item.LocationType == StartupLocationType.ScheduledTask)
                {
                    var (folderPath, leafName) = ResolveTaskFolderAndName(
                        !string.IsNullOrEmpty(item.TaskName) ? item.TaskName : item.Name,
                        item.TaskPath);
                    var fullPath = folderPath == @"\" ? $@"\{leafName}" : $@"{folderPath}\{leafName}";

                    // In-process COM DeleteTask
                    try
                    {
                        var serviceType = Type.GetTypeFromProgID("Schedule.Service");
                        if (serviceType != null)
                        {
                            dynamic? service = Activator.CreateInstance(serviceType);
                            if (service != null)
                            {
                                try
                                {
                                    service.Connect();
                                    dynamic folder = service.GetFolder(folderPath);
                                    try
                                    {
                                        folder.DeleteTask(leafName, 0);
                                        return true;
                                    }
                                    finally
                                    {
                                        Marshal.ReleaseComObject(folder);
                                    }
                                }
                                finally
                                {
                                    Marshal.ReleaseComObject(service);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _ = LogHelper.LogWarning($"COM DeleteTask failed for '{leafName}' in '{folderPath}': {ex.Message}");
                    }

                    // schtasks.exe /Delete
                    var schtasksExe = Path.Combine(Environment.SystemDirectory, "schtasks.exe");
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = schtasksExe,
                            Arguments = $"/Delete /TN \"{fullPath}\" /F",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };
                        using var p = Process.Start(psi);
                        if (p != null)
                        {
                            p.StandardOutput.ReadToEnd();
                            p.StandardError.ReadToEnd();
                            p.WaitForExit(4000);
                            if (p.ExitCode == 0) return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _ = LogHelper.LogWarning($"schtasks /Delete error for {fullPath}: {ex.Message}");
                    }

                    // PowerShell Unregister-ScheduledTask
                    try
                    {
                        var psFolder = folderPath.EndsWith('\\') ? folderPath : folderPath + @"\";
                        var psScript = $"Unregister-ScheduledTask -TaskPath '{psFolder}' -TaskName '{leafName}' -Confirm:$false";
                        var psPsi = new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{psScript}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };
                        using var psProc = Process.Start(psPsi);
                        if (psProc != null)
                        {
                            psProc.StandardOutput.ReadToEnd();
                            psProc.StandardError.ReadToEnd();
                            psProc.WaitForExit(6000);
                            if (psProc.ExitCode == 0) return true;
                        }
                    }
                    catch { }

                    // Verify if task still exists
                    var exists = IsScheduledTaskEnabled(leafName, folderPath);
                    return exists == null; // null means deleted / not found
                }

                if (item.LocationType is StartupLocationType.UserStartupFolder or StartupLocationType.CommonStartupFolder)
                {
                    if (!string.IsNullOrEmpty(item.FilePath) && File.Exists(item.FilePath))
                    {
                        File.Delete(item.FilePath);
                    }

                    string folderApprovedPath = item.LocationType == StartupLocationType.UserStartupFolder ? StartupApprovedFolderHKCU : StartupApprovedFolderHKLM;
                    var fHive = item.Hive ?? RegistryHive.CurrentUser;
                    var fName = item.ValueName ?? Path.GetFileName(item.FilePath ?? "");
                    if (!string.IsNullOrEmpty(fName))
                    {
                        using var fBaseKey = RegistryKey.OpenBaseKey(fHive, RegistryView.Default);
                        using var fSubKey = fBaseKey.OpenSubKey(folderApprovedPath, true);
                        fSubKey?.DeleteValue(fName, false);
                    }
                    return true;
                }

                if (item.Hive.HasValue && !string.IsNullOrEmpty(item.RegistryPath) && !string.IsNullOrEmpty(item.ValueName))
                {
                    using var baseKey = RegistryKey.OpenBaseKey(item.Hive.Value, item.View);
                    using var regKey = baseKey.OpenSubKey(item.RegistryPath, true);
                    regKey?.DeleteValue(item.ValueName, false);

                    string approvedPath = item.Hive == RegistryHive.LocalMachine ? StartupApprovedRunHKLM : StartupApprovedRunHKCU;
                    using var appKey = baseKey.OpenSubKey(approvedPath, true);
                    appKey?.DeleteValue(item.ValueName, false);
                    return true;
                }

                // If standalone approved item
                if (!string.IsNullOrEmpty(item.ValueName))
                {
                    var h = item.Hive ?? RegistryHive.CurrentUser;
                    using var bKey = RegistryKey.OpenBaseKey(h, item.View);
                    using var sKey = bKey.OpenSubKey(StartupApprovedRunHKCU, true);
                    sKey?.DeleteValue(item.ValueName, false);
                    using var sKeyL = bKey.OpenSubKey(StartupApprovedRunHKLM, true);
                    sKeyL?.DeleteValue(item.ValueName, false);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Failed to remove startup item {item.Name}: {ex.Message}");
            }
            return false;
        });
    }

    public static async Task<bool> AddStartupItemAsync(string name, string targetPath, string arguments, bool isUserScope)
    {
        return await Task.Run(() =>
        {
            try
            {
                var fullCommand = string.IsNullOrWhiteSpace(arguments) ? $"\"{targetPath}\"" : $"\"{targetPath}\" {arguments}";
                var hive = isUserScope ? RegistryHive.CurrentUser : RegistryHive.LocalMachine;
                var subKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
                using var regKey = baseKey.CreateSubKey(subKeyPath, true);
                if (regKey != null)
                {
                    regKey.SetValue(name, fullCommand, RegistryValueKind.String);

                    string approvedPath = isUserScope ? StartupApprovedRunHKCU : StartupApprovedRunHKLM;
                    using var appKey = baseKey.CreateSubKey(approvedPath, true);
                    appKey?.SetValue(name, new byte[] { 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary);

                    return true;
                }
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Failed to add startup item {name}: {ex.Message}");
            }
            return false;
        });
    }

    public static void OpenItemLocation(StartupItem item)
    {
        try
        {
            var target = !string.IsNullOrEmpty(item.FilePath) && File.Exists(item.FilePath)
                ? item.FilePath
                : item.ExecutablePath;

            if (!string.IsNullOrEmpty(target) && File.Exists(target))
            {
                Process.Start("explorer.exe", $"/select,\"{target}\"");
            }
            else if (!string.IsNullOrEmpty(target) && Directory.Exists(Path.GetDirectoryName(target)))
            {
                Process.Start("explorer.exe", $"\"{Path.GetDirectoryName(target)}\"");
            }
            else if (item.LocationType == StartupLocationType.ScheduledTask)
            {
                Process.Start("taskschd.msc");
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"OpenItemLocation failed for {item.Name}: {ex.Message}");
        }
    }

    private static string ExtractExecutablePath(string rawCommand)
    {
        if (string.IsNullOrWhiteSpace(rawCommand)) return string.Empty;
        var trimmed = rawCommand.Trim();
        if (trimmed.StartsWith("\""))
        {
            var nextQuote = trimmed.IndexOf('"', 1);
            if (nextQuote > 1)
            {
                return trimmed.Substring(1, nextQuote - 1);
            }
        }

        var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0)
        {
            var first = parts[0];
            if (File.Exists(first)) return first;
            for (int i = parts.Length; i >= 1; i--)
            {
                var candidate = string.Join(" ", parts.Take(i));
                if (File.Exists(candidate)) return candidate;
            }
            return first;
        }
        return trimmed;
    }

    private static string ResolveShortcutTarget(string shortcutPath)
    {
        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType != null)
            {
                dynamic? shell = Activator.CreateInstance(shellType);
                if (shell != null)
                {
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);
                    string targetPath = shortcut.TargetPath;
                    Marshal.ReleaseComObject(shortcut);
                    Marshal.ReleaseComObject(shell);
                    if (!string.IsNullOrEmpty(targetPath))
                    {
                        return targetPath;
                    }
                }
            }
        }
        catch
        {
        }
        return shortcutPath;
    }

    private static string GetPublisher(string exePath)
    {
        try
        {
            if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
            {
                var info = FileVersionInfo.GetVersionInfo(exePath);
                if (!string.IsNullOrWhiteSpace(info.CompanyName))
                {
                    return info.CompanyName;
                }
            }
        }
        catch
        {
        }
        return "Unknown Publisher";
    }

    private static string GetDescription(string exePath, string fallbackName)
    {
        try
        {
            if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
            {
                var info = FileVersionInfo.GetVersionInfo(exePath);
                if (!string.IsNullOrWhiteSpace(info.FileDescription))
                {
                    return info.FileDescription;
                }
            }
        }
        catch
        {
        }
        return fallbackName;
    }

    private static StartupImpact CalculateImpact(string name, string exePath, string publisher, bool isValid, long fileSize)
    {
        if (!isValid) return StartupImpact.Broken;

        var nameLower = name.ToLowerInvariant();
        var pathLower = exePath.ToLowerInvariant();
        var pubLower = publisher.ToLowerInvariant();

        string[] highImpactKeywords = ["discord", "steam", "epicgames", "spotify", "teams", "onedrive", "chrome", "edge", "firefox", "adobe", "itunes", "overwolf", "cortana", "skype", "viber", "slack", "battlenet", "origin", "uplay", "ea desktop"];

        if (highImpactKeywords.Any(k => nameLower.Contains(k) || pathLower.Contains(k)))
        {
            return StartupImpact.High;
        }

        if (fileSize > 15 * 1024 * 1024)
        {
            return StartupImpact.High;
        }

        string[] mediumImpactKeywords = ["update", "tray", "helper", "notifier", "agent", "audio", "realtek", "nvidia", "amd", "logitech", "razer", "steelseries", "corsair", "asus", "msi", "gigabyte", "intel"];

        if (mediumImpactKeywords.Any(k => nameLower.Contains(k) || pathLower.Contains(k) || pubLower.Contains(k)))
        {
            return StartupImpact.Medium;
        }

        return StartupImpact.Low;
    }

    public static bool IsProcessElevated()
    {
        try
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    public static void RestartAsAdministrator()
    {
        try
        {
            var aliasPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "WindowsApps", "RyTuneX.exe");

            string target = File.Exists(aliasPath)
                ? aliasPath
                : (Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "");

            if (!string.IsNullOrEmpty(target) && File.Exists(target))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = target,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(psi);
                App.MainWindow?.Close();
                Environment.Exit(0);
            }
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // UAC cancelled
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"RestartAsAdministrator failed: {ex.Message}");
        }
    }

    public static bool? IsScheduledTaskEnabled(string taskName, string? taskPath = null)
    {
        var (folderPath, leafName) = ResolveTaskFolderAndName(taskName, taskPath);

        // In-process COM attempt
        try
        {
            var serviceType = Type.GetTypeFromProgID("Schedule.Service");
            if (serviceType != null)
            {
                dynamic? service = Activator.CreateInstance(serviceType);
                if (service != null)
                {
                    try
                    {
                        service.Connect();
                        dynamic folder = service.GetFolder(folderPath);
                        try
                        {
                            dynamic task = folder.GetTask(leafName);
                            try
                            {
                                bool isEnabled = task.Enabled;
                                return isEnabled;
                            }
                            finally
                            {
                                Marshal.ReleaseComObject(task);
                            }
                        }
                        finally
                        {
                            Marshal.ReleaseComObject(folder);
                        }
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(service);
                    }
                }
            }
        }
        catch
        {
            // Fall through to schtasks.exe
        }

        // schtasks.exe fallback
        try
        {
            var fullPath = folderPath == @"\" ? $@"\{leafName}" : $@"{folderPath}\{leafName}";
            var psi = new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.SystemDirectory, "schtasks.exe"),
                Arguments = $"/Query /TN \"{fullPath}\" /FO CSV /NH",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var p = Process.Start(psi);
            if (p == null) return null;
            var output = p.StandardOutput.ReadToEnd();
            var err = p.StandardError.ReadToEnd();
            p.WaitForExit(3000);
            if (p.ExitCode != 0 || string.IsNullOrWhiteSpace(output)) return null;

            // CSV output: "TaskName","Next Run Time","Status"
            var parts = output.Trim().Split(',');
            if (parts.Length >= 3)
            {
                var status = parts[2].Trim('\"', ' ', '\r', '\n');
                return !status.Equals("Disabled", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogWarning($"IsScheduledTaskEnabled check failed for {taskName}: {ex.Message}");
        }
        return null;
    }

    private static (string FolderPath, string LeafName) ResolveTaskFolderAndName(string taskName, string? taskPath)
    {
        string leafName = taskName;
        string folderPath = @"\";

        if (!string.IsNullOrWhiteSpace(taskPath))
        {
            var trimmed = taskPath.Trim('\\');
            folderPath = string.IsNullOrEmpty(trimmed) ? @"\" : $@"\{trimmed}";
        }

        if (leafName.Contains('\\'))
        {
            var lastSlash = leafName.LastIndexOf('\\');
            if (string.IsNullOrWhiteSpace(taskPath) || folderPath == @"\")
            {
                var folderPart = leafName.Substring(0, lastSlash).Trim('\\');
                folderPath = string.IsNullOrEmpty(folderPart) ? @"\" : $@"\{folderPart}";
            }
            leafName = leafName.Substring(lastSlash + 1);
        }

        return (folderPath, leafName);
    }
}
