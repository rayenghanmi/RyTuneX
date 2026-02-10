namespace RyTuneX.Helpers;

public static partial class OptimizeSystemHelper
{
    // Get OS build to handle version-specific behavior
    private static readonly int build = Environment.OSVersion.Version.Build;

    public static async Task DisableWindowsAI()
    {
        // Gaming, Studio Effects, & System App AI Registry
        var cmds = new List<string> {
            "REG ADD \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameBar\" /V UseGamingCopilot /T REG_DWORD /D 0 /F",
            "REG ADD \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR\" /V AppCaptureEnabled /T REG_DWORD /D 0 /F",
            "REG ADD \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Control Panel\\Glass\" /V IsEyeContactEnabled /T REG_DWORD /D 0 /F",
            "REG ADD \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Control Panel\\Glass\" /V IsVoiceFocusEnabled /T REG_DWORD /D 0 /F",
            "REG ADD \"HKCU\\Software\\Microsoft\\Speech_OneCore\\Settings\\VoiceActivation\\AppLaunchAllowed\" /V AgentAllowed /T REG_DWORD /D 0 /F",
            "REG ADD \"HKCU\\Software\\Microsoft\\Notepad\" /V ShowRewriteButton /T REG_DWORD /D 0 /F",
            "REG ADD \"HKLM\\SOFTWARE\\Policies\\WindowsNotepad\" /V DisableAIFeatures /T REG_DWORD /D 1 /F",
            "REG ADD \"HKCU\\Software\\Microsoft\\OneDrive\" /V EnablePeopleProcessing /T REG_DWORD /D 0 /F",
            "REG ADD \"HKCU\\Software\\Policies\\Microsoft\\Windows\\Paint\" /V AllowCocreator /T REG_DWORD /D 0 /F",
            "REG ADD \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Paint\" /V DisableImageCreator /T REG_DWORD /D 1 /F",
            "REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Intelligence\" /V AllowWindowsIntelligence /T REG_DWORD /D 0 /F",
            "REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V ComposeEnabled /T REG_DWORD /D 0 /F",
            "REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V HubsSidebarEnabled /T REG_DWORD /D 0 /F",
            "REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V GenAILocalFoundationalModelSettings /T REG_DWORD /D 1 /F",
            "REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /V EnableDynamicContentInSearchBox /T REG_DWORD /D 0 /F",
            "REG ADD \"HKCU\\Software\\Policies\\Microsoft\\Windows\\Explorer\" /V DisableSearchBoxSuggestions /T REG_DWORD /D 1 /F",
            "REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI\" /V DisableAgentConnectors /T REG_DWORD /D 1 /F"
        };
        foreach (var c in cmds) await OptimizationOptions.StartInCmd(c);

        // Hide AI Settings Component
        await ToggleSettingsAIVisibility(hide: true);

        // Remove AI System Packages (CBS) and Machine Learning DLLs
        await RemoveAISystemComponents();

        await OptimizationOptions.StartInCmd("taskkill /F /IM explorer.exe & start %SystemRoot%\\explorer.exe").ConfigureAwait(false);
    }

    public static async Task EnableWindowsAI()
    {
        var cmds = new List<string> {
            "REG DELETE \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameBar\" /V UseGamingCopilot /F",
            "REG ADD \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR\" /V AppCaptureEnabled /T REG_DWORD /D 1 /F",
            "REG DELETE \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Control Panel\\Glass\" /V IsEyeContactEnabled /F",
            "REG DELETE \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Control Panel\\Glass\" /V IsVoiceFocusEnabled /F",
            "REG ADD \"HKCU\\Software\\Microsoft\\Speech_OneCore\\Settings\\VoiceActivation\\AppLaunchAllowed\" /V AgentAllowed /T REG_DWORD /D 1 /F",
            "REG DELETE \"HKCU\\Software\\Microsoft\\Notepad\" /V ShowRewriteButton /F",
            "REG DELETE \"HKLM\\SOFTWARE\\Policies\\WindowsNotepad\" /V DisableAIFeatures /F",
            "REG ADD \"HKCU\\Software\\Microsoft\\OneDrive\" /V EnablePeopleProcessing /T REG_DWORD /D 1 /F",
            "REG DELETE \"HKCU\\Software\\Policies\\Microsoft\\Windows\\Paint\" /V AllowCocreator /F",
            "REG DELETE \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Paint\" /V DisableImageCreator /F",
            "REG DELETE \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Intelligence\" /V AllowWindowsIntelligence /F",
            "REG DELETE \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V ComposeEnabled /F",
            "REG DELETE \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V HubsSidebarEnabled /F",
            "REG DELETE \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V GenAILocalFoundationalModelSettings /F",
            "REG DELETE \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /V EnableDynamicContentInSearchBox /F",
            "REG DELETE \"HKCU\\Software\\Policies\\Microsoft\\Windows\\Explorer\" /V DisableSearchBoxSuggestions /F"
        };
        foreach (var c in cmds) await OptimizationOptions.StartInCmd(c);

        await ToggleSettingsAIVisibility(hide: false);

        // Restore General AI Packages
        var psScript = "Get-AppxPackage -allusers *AIX* | foreach {Add-AppxPackage -register \"$($_.InstallLocation)\\appxmanifest.xml\" -DisableDevelopmentMode}";
        await OptimizationOptions.StartInCmd($"powershell -NoProfile -Command \"{psScript}\"").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("taskkill /F /IM explorer.exe & start %SystemRoot%\\explorer.exe").ConfigureAwait(false);
    }

    private static async Task ToggleCopilotJsonPolicy(bool isDisabled)
    {
        var state = isDisabled ? "disabled" : "enabled";

        var system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var policyFile = Path.Combine(system32, "IntegratedServicesRegionPolicySet.json");

        if (!File.Exists(policyFile)) return;

        var psScript = $@"
        $path = '{policyFile}';
        try {{
            takeown /f $path /a;
            icacls $path /grant *S-1-5-32-544:F;
            $rawJson = Get-Content $path -Raw;
            if ([string]::IsNullOrWhiteSpace($rawJson)) {{
                Write-Error 'JSON file is empty';
                return;
            }}
            $json = $rawJson | ConvertFrom-Json;
            if ($null -eq $json -or $null -eq $json.policies) {{
                Write-Error 'Invalid JSON structure';
                return;
            }}
            $modified = $false;
            foreach ($p in $json.policies) {{
                if ($p.'$comment' -like '*CoPilot*') {{
                    $p.defaultState = '{state}';
                    $modified = $true;
                }}
            }}
            if ($modified) {{
                $json | ConvertTo-Json -Depth 100 | Set-Content $path -Encoding UTF8 -Force;
            }}
        }} catch {{
            Write-Error ""Failed to process JSON: $($_.Exception.Message)"";
        }}";
        var escapedScript = psScript.Replace("\"", "\\\"");
        await OptimizationOptions.StartInCmd($"powershell -NoProfile -ExecutionPolicy Bypass -Command \"{escapedScript}\"").ConfigureAwait(false);
    }

    private static async Task ToggleSettingsAIVisibility(bool hide)
    {
        var psScript = hide
            ? @"$p='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer'; $n='SettingsPageVisibility'; $v=(Get-ItemProperty $p $n -EA 0).$n; if($v -notlike '*hide:aicomponents*'){$nv=$v+';hide:aicomponents;appactions'; Set-ItemProperty $p $n $nv -Force}"
            : @"$p='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer'; $n='SettingsPageVisibility'; $v=(Get-ItemProperty $p $n -EA 0).$n; if($v){$nv=$v -replace ';hide:aicomponents;appactions',''; Set-ItemProperty $p $n $nv -Force}";

        await OptimizationOptions.StartInCmd($"powershell -NoProfile -Command \"{psScript}\"").ConfigureAwait(false);
    }

    private static async Task RemoveAISystemComponents()
    {
        // This handles CBS packages (CoreAI, AIX) and deleting machine learning DLLs
        var psScript = @"
            # CBS Package Removal (Forced)
            $cbsPath = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\Packages';
            $targets = @('*UserExperience-AIX*', '*CoreAI*');
            Get-ChildItem $cbsPath | Where-Object { $n=$_.PSChildName; $targets | Where-Object { $n -like $_ } } | ForEach-Object {
                Set-ItemProperty $_.PSPath 'Visibility' 1; Remove-Item ""$($_.PSPath)\Owners"" -Recurse -Force -EA 0;
                $pn=$_.PSChildName; dism /Online /Remove-Package /PackageName:$pn /NoRestart /Quiet
            }

            # Aggressive DLL Deletion
            $files = @(""$env:SystemRoot\System32\Windows.AI.MachineLearning.dll"", ""$env:SystemRoot\System32\SettingsHandlers_Copilot.dll"");
            foreach($f in $files) { if(Test-Path $f){ takeown /F $f /A; icacls $f /grant *S-1-5-32-544:F; Remove-Item $f -Force } }
        ";
        await OptimizationOptions.StartInCmd($"powershell -NoProfile -Command \"{psScript.Replace("\"", "\\\"")}\"").ConfigureAwait(false);
    }

    public static async Task DisableWindowsRecall()
    {
        // Registry Lockout
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI\" /V DisableAIDataAnalysis /T REG_DWORD /D 1 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Policies\\Microsoft\\Windows\\WindowsAI\" /V DisableAIDataAnalysis /T REG_DWORD /D 1 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI\" /V TurnOffSavingSnapshots /T REG_DWORD /D 1 /F").ConfigureAwait(false);

        // Services & Tasks
        await OptimizationOptions.StartInCmd("powershell -Command \"Stop-Service -Name 'WSAIFabricSvc' -ErrorAction SilentlyContinue; Set-Service -Name 'WSAIFabricSvc' -StartupType Disabled\"").ConfigureAwait(false);

        var taskDisable = @"
            $tasks = @('\Microsoft\Windows\WindowsAI\Recall\InitialConfiguration', '\Microsoft\Windows\WindowsAI\Recall\PolicyConfiguration');
            foreach($t in $tasks) { 
                if (Get-ScheduledTask -TaskName $t -ErrorAction SilentlyContinue) {
                    Disable-ScheduledTask -TaskName $t -ErrorAction SilentlyContinue 
                }
            }
        ";
        await OptimizationOptions.StartInCmd($"powershell -NoProfile -Command \"{taskDisable}\"").ConfigureAwait(false);

        // DISM Feature & Appx
        await OptimizationOptions.StartInCmd("dism /Online /Disable-Feature /FeatureName:Recall /Remove /NoRestart /Quiet").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("powershell \"Get-AppxPackage -AllUsers *AiFabric* | Remove-AppxPackage -AllUsers\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("powershell \"Get-AppxPackage -AllUsers *WindowsIntelligence* | Remove-AppxPackage -AllUsers\"").ConfigureAwait(false);
    }

    public static async Task EnableWindowsRecall()
    {
        await OptimizationOptions.StartInCmd("REG DELETE \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI\" /V DisableAIDataAnalysis /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG DELETE \"HKCU\\Software\\Policies\\Microsoft\\Windows\\WindowsAI\" /V DisableAIDataAnalysis /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG DELETE \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI\" /V TurnOffSavingSnapshots /F").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("powershell -Command \"Set-Service -Name 'WSAIFabricSvc' -StartupType Manual\"").ConfigureAwait(false);

        var taskEnable = @"
            $tasks = @('\Microsoft\Windows\WindowsAI\Recall\InitialConfiguration', '\Microsoft\Windows\WindowsAI\Recall\PolicyConfiguration');
            foreach($t in $tasks) { 
                if (Get-ScheduledTask -TaskName $t -ErrorAction SilentlyContinue) {
                    Enable-ScheduledTask -TaskName $t -ErrorAction SilentlyContinue 
                }
            }
        ";
        await OptimizationOptions.StartInCmd($"powershell -NoProfile -Command \"{taskEnable}\"").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("dism /Online /Enable-Feature /FeatureName:Recall /NoRestart").ConfigureAwait(false);

        var psScript = "Get-AppxPackage -allusers *AiFabric* | foreach {Add-AppxPackage -register \"$($_.InstallLocation)\\appxmanifest.xml\" -DisableDevelopmentMode}";
        await OptimizationOptions.StartInCmd($"powershell -NoProfile -Command \"{psScript}\"").ConfigureAwait(false);
    }

    public static async Task DisableRecommendedSectionStartMenu()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Start\" /v HideRecommendedSection /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Education\" /v IsEducationEnvironment /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer\" /v HideRecommendedSection /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task EnableRecommendedSectionStartMenu()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Start\" /v HideRecommendedSection /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Education\" /v IsEducationEnvironment /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer\" /v HideRecommendedSection /f").ConfigureAwait(false);
    }

    public static async Task DisableWPBT()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\" /v DisableWpbtExecution /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task EnableWPBT()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\" /v DisableWpbtExecution /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task EnablePrioritizeForegroundApplications()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl\" /v Win32PrioritySeparation /t REG_DWORD /d 42 /f").ConfigureAwait(false);
    }

    public static async Task DisablePrioritizeForegroundApplications()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl\" /v Win32PrioritySeparation /t REG_DWORD /d 2 /f").ConfigureAwait(false);
    }

    public static async Task EnableOptimizeNTFS()
    {
        await OptimizationOptions.StartInCmd("fsutil behavior set disablelastaccess 1").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("fsutil behavior set disable8dot3 1").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem\" /v NtfsMftZoneReservation /t REG_DWORD /d 2 /f").ConfigureAwait(false);
    }

    public static async Task DisableOptimizeNTFS()
    {
        await OptimizationOptions.StartInCmd("fsutil behavior set disablelastaccess 0").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("fsutil behavior set disable8dot3 0").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem\" /v NtfsMftZoneReservation /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task EnableLegacyBootMenu()
    {
        await OptimizationOptions.StartInCmd("bcdedit /set bootmenupolicy legacy").ConfigureAwait(false);
    }

    public static async Task DisableLegacyBootMenu()
    {
        await OptimizationOptions.StartInCmd("bcdedit /set bootmenupolicy standard").ConfigureAwait(false);
    }

    public static async Task DisableServiceHostSplitting()
    {
        await OptimizationOptions.StartInCmd("Get-ChildItem 'HKLM:\\SYSTEM\\CurrentControlSet\\Services' | Where-Object { $_.Name -notmatch 'Xbl|Xbox' } | ForEach-Object { if ((Get-ItemProperty -Path $_.PSPath -ErrorAction SilentlyContinue).Start -ne $null) { Set-ItemProperty -Path $_.PSPath -Name 'SvcHostSplitDisable' -Value 1 -Type DWord -Force -ErrorAction SilentlyContinue } }").ConfigureAwait(false);
    }

    public static async Task EnableServiceHostSplitting()
    {
        await OptimizationOptions.StartInCmd("Get-ChildItem 'HKLM:\\SYSTEM\\CurrentControlSet\\Services' | Where-Object { $_.Name -notmatch 'Xbl|Xbox' } | ForEach-Object { if (Test-Path $_.PSPath) { Remove-ItemProperty -Path $_.PSPath -Name 'SvcHostSplitDisable' -ErrorAction SilentlyContinue } }").ConfigureAwait(false);
    }

    public static async Task DisableMenuShowDelay()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Control Panel\\Desktop\" /v MenuShowDelay /t REG_SZ /d 0 /f").ConfigureAwait(false);
    }

    public static async Task DisableMouseHoverTime()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Control Panel\\Mouse\" /v MouseHoverTime /t REG_SZ /d 0 /f").ConfigureAwait(false);
    }

    public static async Task DisableBackgroundApps()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications\" /v GlobalUserDisabled /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v BackgroundAppGlobalToggle /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy\" /v LetAppsRunInBackground /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task EnableAutoComplete()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\AutoComplete\" /v \"Append Completion\" /t REG_SZ /d yes /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\AutoComplete\" /v AutoSuggest /t REG_SZ /d yes /f").ConfigureAwait(false);
    }

    public static async Task EnableCrashDump()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\CrashControl\" /v CrashDumpEnabled /t REG_DWORD /d 3 /f").ConfigureAwait(false);
    }

    public static async Task DisableRemoteAssistance()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\System\\CurrentControlSet\\Control\\Remote Assistance\" /v fAllowToGetHelp /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task DisableWindowShake()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v DisallowShaking /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task AddCopyMoveContextMenu()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Classes\\AllFilesystemObjects\\shellex\\ContextMenuHandlers\\Copy To\" /ve /d \"{C2FBB630-2971-11D1-A18C-00C04FD75D13}\" /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Classes\\AllFilesystemObjects\\shellex\\ContextMenuHandlers\\Move To\" /ve /d \"{C2FBB631-2971-11D1-A18C-00C04FD75D13}\" /f").ConfigureAwait(false);
    }

    public static async Task AdjustTaskTimeouts()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Control Panel\\Desktop\" /v AutoEndTasks /t REG_SZ /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Control Panel\\Desktop\" /v HungAppTimeout /t REG_SZ /d 1000 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Control Panel\\Desktop\" /v WaitToKillAppTimeout /t REG_SZ /d 2000 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Control Panel\\Desktop\" /v LowLevelHooksTimeout /t REG_SZ /d 1000 /f").ConfigureAwait(false);
    }

    public static async Task EnableLowDiskSpaceChecks()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoLowDiskSpaceChecks /t REG_DWORD /d 00000000 /f").ConfigureAwait(false);
    }

    public static async Task DisableLinkResolve()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v LinkResolveIgnoreLinkInfo /t REG_DWORD /d 00000001 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoResolveSearch /t REG_DWORD /d 00000001 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoResolveTrack /t REG_DWORD /d 00000001 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoInternetOpenWith /t REG_DWORD /d 00000001 /f").ConfigureAwait(false);
    }

    public static async Task DecreaseServiceTimeouts()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\" /v WaitToKillServiceTimeout /t REG_SZ /d 2000 /f").ConfigureAwait(false);
    }

    public static async Task DisableRemoteRegistry()
    {
        await OptimizationOptions.StartInCmd("sc config \"RemoteRegistry\" start= disabled").ConfigureAwait(false);
    }

    public static async Task HideFileExtensionsAndHiddenFiles()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v HideFileExt /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v Hidden /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task OptimizeSystemProfile()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v SystemResponsiveness /t REG_DWORD /d 10 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v NoLazyMode /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v AlwaysOn /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v NetworkThrottlingIndex /t REG_DWORD /d 0xffffffff /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"GPU Priority\" /t REG_DWORD /d 8 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v Priority /t REG_DWORD /d 8 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"Scheduling Category\" /t REG_SZ /d High /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"SFIO Priority\" /t REG_SZ /d High /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency\" /v \"GPU Priority\" /t REG_DWORD /d 8 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency\" /v Priority /t REG_DWORD /d 8 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency\" /v \"Scheduling Category\" /t REG_SZ /d High /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency\" /v \"SFIO Priority\" /t REG_SZ /d High /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\" /v HwSchMode /t REG_DWORD /d 2 /f").ConfigureAwait(false);
    }

    public static async Task EnableMenuShowDelay()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Control Panel\\Desktop\" /v MenuShowDelay /f").ConfigureAwait(false);
    }

    public static async Task EnableMouseHoverTime()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Control Panel\\Mouse\" /v MouseHoverTime /f").ConfigureAwait(false);
    }

    public static async Task EnableBackgroundApps()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications\" /v GlobalUserDisabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v BackgroundAppGlobalToggle /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy\" /v LetAppsRunInBackground /f").ConfigureAwait(false);
    }

    public static async Task DisableAutoComplete()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\AutoComplete\" /v \"Append Completion\" /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\AutoComplete\" /v AutoSuggest /f").ConfigureAwait(false);
    }

    public static async Task DisableCrashDump()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\CrashControl\" /v CrashDumpEnabled /f").ConfigureAwait(false);
    }

    public static async Task EnableRemoteAssistance()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\System\\CurrentControlSet\\Control\\Remote Assistance\" /v fAllowToGetHelp /f").ConfigureAwait(false);
    }

    public static async Task EnableWindowShake()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v DisallowShaking /f").ConfigureAwait(false);
    }

    public static async Task RemoveCopyMoveContextMenu()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Classes\\AllFilesystemObjects\\shellex\\ContextMenuHandlers\\Copy To\" /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Classes\\AllFilesystemObjects\\shellex\\ContextMenuHandlers\\Move To\" /f").ConfigureAwait(false);
    }

    public static async Task IncreaseTaskTimeouts()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Control Panel\\Desktop\" /v AutoEndTasks /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Control Panel\\Desktop\" /v HungAppTimeout /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Control Panel\\Desktop\" /v WaitToKillAppTimeout /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Control Panel\\Desktop\" /v LowLevelHooksTimeout /f").ConfigureAwait(false);
    }

    public static async Task DisableLowDiskSpaceChecks()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoLowDiskSpaceChecks /t REG_DWORD /d 00000001 /f").ConfigureAwait(false);
    }

    public static async Task EnableLinkResolve()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v LinkResolveIgnoreLinkInfo /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoResolveSearch /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoResolveTrack /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoInternetOpenWith /f").ConfigureAwait(false);
    }

    public static async Task RevertServiceTimeouts()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SYSTEM\\CurrentControlSet\\Control\" /v WaitToKillServiceTimeout /f").ConfigureAwait(false);
    }

    public static async Task EnableRemoteRegistry()
    {
        await OptimizationOptions.StartInCmd("sc config \"RemoteRegistry\" start= demand").ConfigureAwait(false);
    }

    public static async Task ShowFileExtensionsAndHiddenFiles()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v HideFileExt /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v Hidden /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task RevertSystemProfile()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v SystemResponsiveness /t REG_DWORD /d 20 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v NoLazyMode /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v AlwaysOn /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v NetworkThrottlingIndex /t REG_DWORD /d 10 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"GPU Priority\" /t REG_DWORD /d 2 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v Priority /t REG_DWORD /d 2 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"Scheduling Category\" /t REG_SZ /d Medium /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"SFIO Priority\" /t REG_SZ /d Normal /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency\" /v \"GPU Priority\" /t REG_DWORD /d 2 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency\" /v Priority /t REG_DWORD /d 2 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency\" /v \"Scheduling Category\" /t REG_SZ /d Medium /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency\" /v \"SFIO Priority\" /t REG_SZ /d Normal /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\" /v HwSchMode /f").ConfigureAwait(false);
    }

    public static async Task DisableTelemetryServices()
    {
        await OptimizationOptions.StartInCmd("sc stop DiagTrack").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc stop diagnosticshub.standardcollector.service").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc stop dmwappushservice").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc stop DcpSvc").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc stop WdiServiceHost").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc stop WdiSystemHost").ConfigureAwait(false);

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
            await OptimizationOptions.StartInCmd($"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\{svc}\" /v Start /t REG_DWORD /d 4 /f").ConfigureAwait(false);
        }

        string[] appCompatKeys = {
        "DisableEngine", "SbEnable", "AITEnable",
        "DisableInventory", "DisablePCA", "DisableUAR"
        };
        int[] appCompatValues = { 1, 0, 0, 1, 1, 1 };

        for (var i = 0; i < appCompatKeys.Length; i++)
        {
            await OptimizationOptions.StartInCmd($"reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppCompat\" /v {appCompatKeys[i]} /t REG_DWORD /d {appCompatValues[i]} /f").ConfigureAwait(false);
            if (Environment.Is64BitOperatingSystem)
            {
                await OptimizationOptions.StartInCmd($"reg add \"HKLM\\SOFTWARE\\Wow6432Node\\Policies\\Microsoft\\Windows\\AppCompat\" /v {appCompatKeys[i]} /t REG_DWORD /d {appCompatValues[i]} /f").ConfigureAwait(false);
            }
        }

        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection\" /v AllowTelemetry /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\SQMClient\\Windows\" /v CEIPEnable /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Siuf\\Rules\" /v NumberOfSIUFInPeriod /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\Software\\Microsoft\\PolicyManager\\default\\WiFi\\AllowAutoConnectToWiFiSenseHotspots\" /v value /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\Software\\Microsoft\\PolicyManager\\default\\WiFi\\AllowWiFiHotSpotReporting\" /v value /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Device Metadata\" /v PreventDeviceMetadataFromNetwork /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\MRT\" /v DontOfferThroughWUAU /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\WMI\\AutoLogger\\SQMLogger\" /v Start /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\System\" /v AllowExperimentation /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\" /v PublishUserActivities /t REG_DWORD /d 0 /f").ConfigureAwait(false);

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
            await OptimizationOptions.StartInCmd($"schtasks /Change /TN \"{task}\" /Disable").ConfigureAwait(false);
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

    public static async Task EnableTelemetryServices()
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
            await OptimizationOptions.StartInCmd($"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\{svc}\" /v Start /t REG_DWORD /d 2 /f").ConfigureAwait(false);
            await OptimizationOptions.StartInCmd($"sc start {svc}").ConfigureAwait(false);
        }

        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection\" /v AllowTelemetry /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\SQMClient\\Windows\" /v CEIPEnable /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\" /v PublishUserActivities /t REG_DWORD /d 1 /f").ConfigureAwait(false);

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
            await OptimizationOptions.StartInCmd($"schtasks /Change /TN \"{task}\" /Enable").ConfigureAwait(false);
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

    public static async Task DisableMediaPlayerSharing()
    {
        await OptimizationOptions.StartInCmd("sc stop WMPNetworkSvc").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\WMPNetworkSvc\" /v Start /t REG_DWORD /d 4 /f").ConfigureAwait(false);

    }

    public static async Task EnableMediaPlayerSharing()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\WMPNetworkSvc\" /v Start /t REG_DWORD /d 2 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc start WMPNetworkSvc").ConfigureAwait(false);

    }

    public static async Task DisableHomeGroup()
    {
        await OptimizationOptions.StartInCmd("sc stop HomeGroupListener").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc stop HomeGroupProvider").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\HomeGroup\" /v DisableHomeGroup /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\HomeGroupListener\" /v Start /t REG_DWORD /d 4 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\HomeGroupProvider\" /v Start /t REG_DWORD /d 4 /f").ConfigureAwait(false);

    }

    public static async Task EnableHomeGroup()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\HomeGroupListener\" /v Start /t REG_DWORD /d 2 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\HomeGroupProvider\" /v Start /t REG_DWORD /d 2 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\HomeGroup\" /v DisableHomeGroup /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc start HomeGroupListener").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc start HomeGroupProvider").ConfigureAwait(false);
    }

    public static async Task DisablePrintService()
    {
        await OptimizationOptions.StartInCmd("sc stop Spooler").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\Spooler\" /v Start /t REG_DWORD /d 4 /f").ConfigureAwait(false);
    }

    public static async Task EnablePrintService()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\Spooler\" /v Start /t REG_DWORD /d 2 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("cmd /c sc start Spooler").ConfigureAwait(false);
    }

    public static async Task DisableSysMain()
    {
        await OptimizationOptions.StartInCmd("sc stop SysMain").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\SysMain\" /v Start /t REG_DWORD /d 4 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters\" /v EnableSuperfetch /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters\" /v EnablePrefetcher /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters\" /v SfTracingState /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task EnableSysMain()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\SysMain\" /v Start /t REG_DWORD /d 2 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters\" /v EnableSuperfetch /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters\" /v EnablePrefetcher /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters\" /v SfTracingState /f").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("sc start SysMain").ConfigureAwait(false);
    }

    public static async Task EnableCompatibilityAssistant()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\PcaSvc\" /v Start /t REG_DWORD /d 2 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc start PcaSvc").ConfigureAwait(false);
    }

    public static async Task DisableCompatibilityAssistant()
    {
        await OptimizationOptions.StartInCmd("sc stop PcaSvc").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\PcaSvc\" /v Start /t REG_DWORD /d 4 /f").ConfigureAwait(false);
    }

    public static async Task DisableWindowsTransparency()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v EnableTransparency /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }
    public static async Task EnableWindowsTransparency()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v EnableTransparency /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task EnableWindowsDarkMode()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v AppsUseLightTheme /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v SystemUsesLightTheme /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("taskkill /f /im explorer.exe").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("start %SystemRoot%\\explorer.exe").ConfigureAwait(false);
    }
    public static async Task DisableWindowsDarkMode()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v AppsUseLightTheme /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v SystemUsesLightTheme /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("taskkill /f /im explorer.exe").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("start %SystemRoot%\\explorer.exe").ConfigureAwait(false);
    }

    public static async Task EnableVerboseLogon()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v VerboseStatus /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task DisableVerboseLogon()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v VerboseStatus /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task EnableClassicContextMenu()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\\InprocServer32\" /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("taskkill /F /IM explorer.exe & start %SystemRoot%\\explorer").ConfigureAwait(false);
    }

    public static async Task DisableClassicContextMenu()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\" /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("taskkill /F /IM explorer.exe & start %SystemRoot%\\explorer").ConfigureAwait(false);
    }

    public static async Task DisableSystemRestore()
    {
        await OptimizationOptions.StartInCmd("vssadmin delete shadows /for=c: /all /quiet").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc stop VSS").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\SystemRestore\" /v DisableSR /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\SystemRestore\" /v DisableConfig /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task EnableSystemRestore()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\SystemRestore\" /v DisableSR /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\SystemRestore\" /v DisableConfig /f").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("sc start VSS").ConfigureAwait(false);
    }

    public static async Task DisableSearch()
    {
        await OptimizationOptions.StartInCmd("sc stop WSearch").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\WSearch\" /v Start /t REG_DWORD /d 4 /f").ConfigureAwait(false);
    }

    public static async Task EnableSearch()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\WSearch\" /v Start /t REG_DWORD /d 2 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc start WSearch").ConfigureAwait(false);
    }

    public static async Task DisableSMBAsync(string v)
    {
        await OptimizationOptions.StartInCmd($"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\LanmanServer\\Parameters\" /v SMB{v} /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task EnableSMBAsync(string v)
    {
        await OptimizationOptions.StartInCmd($"reg delete \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\LanmanServer\\Parameters\" /v SMB{v} /f").ConfigureAwait(false);
    }

    public static async Task DisableErrorReporting()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Error Reporting\" /v Disabled /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\PCHealth\\ErrorReporting\" /v DoReport /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\" /v Disabled /t REG_DWORD /d 1 /f").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("sc stop WerSvc").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc stop wercplsupport").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\WerSvc\" /v Start /t REG_DWORD /d 4 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\wercplsupport\" /v Start /t REG_DWORD /d 4 /f").ConfigureAwait(false);
    }

    public static async Task EnableErrorReporting()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Error Reporting\" /v Disabled /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\PCHealth\\ErrorReporting\" /v DoReport /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\" /v Disabled /f").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\wercplsupport\" /v Start /t REG_DWORD /d 3 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\WerSvc\" /v Start /t REG_DWORD /d 2 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc start WerSvc").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc start wercplsupport").ConfigureAwait(false);
    }

    public static async Task EnableLegacyVolumeSlider()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\Software\\Microsoft\\Windows NT\\CurrentVersion\\MTCUVC\" /v EnableMtcUvc /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task DisableLegacyVolumeSlider()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\Software\\Microsoft\\Windows NT\\CurrentVersion\\MTCUVC\" /v EnableMtcUvc /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task DisableCortana()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\SearchSettings\" /v IsDeviceSearchHistoryEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v AllowCortana /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v DisableWebSearch /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v ConnectedSearchUseWeb /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v ConnectedSearchUseWebOverMeteredConnections /t REG_DWORD /d 0 /f").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Search\" /v HistoryViewEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Search\" /v DeviceHistoryEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v AllowSearchToUseLocation /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v BingSearchEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v CortanaConsent /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v AllowCloudSearch /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task EnableCortana()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v AllowCortana /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v DisableWebSearch /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v ConnectedSearchUseWeb /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v ConnectedSearchUseWebOverMeteredConnections /t REG_DWORD /d 1 /f").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Search\" /v HistoryViewEnabled /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Search\" /v DeviceHistoryEnabled /t REG_DWORD /d 1 /f").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v AllowSearchToUseLocation /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v BingSearchEnabled /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v CortanaConsent /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v AllowCloudSearch /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task EnableGamingMode()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\" /v HwSchMode /t REG_DWORD /d 2 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\GameBar\" /v AllowAutoGameMode /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\GameBar\" /v AutoGameModeEnabled /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\System\\GameConfigStore\" /v GameDVR_FSEBehaviorMode /t REG_DWORD /d 2 /f").ConfigureAwait(false);
    }

    public static async Task DisableGamingMode()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\" /v HwSchMode /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\GameBar\" /v AllowAutoGameMode /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\GameBar\" /v AutoGameModeEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\System\\GameConfigStore\" /v GameDVR_FSEBehaviorMode /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task SetWindowsUpdatesDefault()
    {
        // Remove all policy overrides to restore default behavior
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'DoNotConnectToWindowsUpdateInternetLocations' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'DisableWindowsUpdateAccess' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);

        // Remove Automatic Updates policies
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU' -Name 'AUOptions' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU' -Name 'NoAutoUpdate' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU' -Name 'NoAutoRebootWithLoggedOnUsers' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);

        // Remove deferral policies (feature + quality updates)
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'DeferFeatureUpdates' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'DeferFeatureUpdatesPeriodInDays' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'DeferQualityUpdates' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'DeferQualityUpdatesPeriodInDays' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);

        // Remove AllowOptionalContent / SetAllowOptionalContent
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'AllowOptionalContent' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'SetAllowOptionalContent' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);

        // Remove Delivery Optimization override
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\DeliveryOptimization\\Config' -Name 'DODownloadMode' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);

        // Remove speech model update and maintenance blocks
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Speech' -Name 'AllowSpeechModelUpdate' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Schedule\\Maintenance' -Name 'MaintenanceDisabled' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);

        // Clean up UX Settings
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings' -Name 'AllowMUUpdateService' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings' -Name 'RestartNotificationsAllowed2' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings' -Name 'AllowOptionalContent' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings' -Name 'DeferFeatureUpdatesPeriodInDays' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings' -Name 'DeferQualityUpdatesPeriodInDays' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings' -Name 'IsContinuousInnovationOptedIn' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings' -Name 'IsExpedited' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);

        // Remove additional policy values
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'SetPolicyDrivenUpdateSourceForDriverUpdates' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'SetPolicyDrivenUpdateSourceForFeatureUpdates' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'SetPolicyDrivenUpdateSourceForQualityUpdates' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'SetPolicyDrivenUpdateSourceForOtherUpdates' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'ManagePreviewBuildsPolicyValue' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'BranchReadinessLevel' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'PauseFeatureUpdatesStartTime' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'PauseQualityUpdatesStartTime' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);

        // Clean up empty policy keys
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-Item 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU' -Recurse -Force -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-Item 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Recurse -Force -ErrorAction SilentlyContinue\"").ConfigureAwait(false);

        // Restore Delivery Optimization service startup
        await OptimizationOptions.StartInCmd("sc config DoSvc start= delayed-auto").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\DoSvc\" /v Start /t REG_DWORD /d 2 /f").ConfigureAwait(false);

        // Restore Windows Update related services to default startup
        await OptimizationOptions.StartInCmd("sc config wuauserv start= demand").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc config UsoSvc start= demand").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc config BITS start= delayed-auto").ConfigureAwait(false);

        // Restore WaaSMedicSvc behavior
        if (build >= 19041)
        {
            // Restore WaaSMedic service startup
            await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\WaaSMedicSvc\" /v Start /t REG_DWORD /d 3 /f").ConfigureAwait(false);

            // Restore original registry ACLs
            await OptimizationOptions.StartInCmd("PowerShell -Command \"$acl = Get-Acl 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\WaaSMedicSvc'; $acl.SetAccessRuleProtection($false,$true); Set-Acl 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\WaaSMedicSvc' $acl\"").ConfigureAwait(false);

            // Re-enable WaaSMedic scheduled tasks
            await OptimizationOptions.StartInCmd("PowerShell -Command \"Get-ScheduledTask -TaskPath '\\Microsoft\\Windows\\WaaSMedic\\*' -ErrorAction SilentlyContinue | Enable-ScheduledTask -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        }
        else
        {
            await OptimizationOptions.StartInCmd("sc config WaaSMedicSvc start= demand").ConfigureAwait(false);
        }

        // Re-enable Update Orchestrator and Windows Update scheduled tasks
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Get-ScheduledTask -TaskPath '\\Microsoft\\Windows\\UpdateOrchestrator\\*' -ErrorAction SilentlyContinue | Enable-ScheduledTask -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Get-ScheduledTask -TaskPath '\\Microsoft\\Windows\\WindowsUpdate\\*' -ErrorAction SilentlyContinue | Enable-ScheduledTask -ErrorAction SilentlyContinue\"").ConfigureAwait(false);

        // Reset Windows Update components to clear any cached policy state
        await OptimizationOptions.StartInCmd("net stop wuauserv").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("net stop bits").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("net stop cryptsvc").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("net start cryptsvc").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("net start bits").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("net start wuauserv").ConfigureAwait(false);

        // Force Group Policy refresh to clear cached policy state
        await OptimizationOptions.StartInCmd("gpupdate /force").ConfigureAwait(false);
    }

    public static async Task SetWindowsUpdatesSecurityOnly()
    {
        // Ensure the WindowsUpdate policy keys exist
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\" /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /f").ConfigureAwait(false);

        // AUOptions = 3: Auto download and notify for install
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v AUOptions /t REG_DWORD /d 3 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v NoAutoUpdate /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v NoAutoRebootWithLoggedOnUsers /t REG_DWORD /d 1 /f").ConfigureAwait(false);

        // Defer feature updates for maximum period (365 days)
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\" /v DeferFeatureUpdates /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\" /v DeferFeatureUpdatesPeriodInDays /t REG_DWORD /d 365 /f").ConfigureAwait(false);

        // Quality updates with minimal deferral
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\" /v DeferQualityUpdates /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\" /v DeferQualityUpdatesPeriodInDays /t REG_DWORD /d 0 /f").ConfigureAwait(false);

        // Remove blocking policy if it was set
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'DoNotConnectToWindowsUpdateInternetLocations' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);

        // Services should be on-demand for updates to work
        await OptimizationOptions.StartInCmd("sc config wuauserv start= demand").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc config UsoSvc start= demand").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc config BITS start= demand").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc config DoSvc start= demand").ConfigureAwait(false);
    }

    public static async Task SetWindowsUpdatesManually()
    {
        // Ensure the WindowsUpdate policy keys exist
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\" /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /f").ConfigureAwait(false);

        // AUOptions = 2: Notify for download and notify for install
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v AUOptions /t REG_DWORD /d 2 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v NoAutoUpdate /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v NoAutoRebootWithLoggedOnUsers /t REG_DWORD /d 1 /f").ConfigureAwait(false);

        // Remove deferral settings so user controls all updates
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'DeferFeatureUpdates' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'DeferFeatureUpdatesPeriodInDays' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'DeferQualityUpdates' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'DeferQualityUpdatesPeriodInDays' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);

        // Remove blocking policy if it was set
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'DoNotConnectToWindowsUpdateInternetLocations' -ErrorAction SilentlyContinue\"").ConfigureAwait(false);

        // Services should be on-demand
        await OptimizationOptions.StartInCmd("sc config wuauserv start= demand").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc config UsoSvc start= demand").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc config BITS start= demand").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc config DoSvc start= demand").ConfigureAwait(false);
    }

    public static async Task SetWindowsUpdatesDisabled()
    {
        var build = Environment.OSVersion.Version.Build;

        // Ensure the WindowsUpdate policy keys exist
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\" /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /f").ConfigureAwait(false);

        // Block automatic updates using Group Policy registry keys
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v NoAutoUpdate /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\" /v DoNotConnectToWindowsUpdateInternetLocations /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v AUOptions /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v NoAutoRebootWithLoggedOnUsers /t REG_DWORD /d 1 /f").ConfigureAwait(false);

        // Additional policy to disable Windows Update access
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\" /v DisableWindowsUpdateAccess /t REG_DWORD /d 1 /f").ConfigureAwait(false);

        // Disable Delivery Optimization
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\DeliveryOptimization\\Config\" /v DODownloadMode /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\DoSvc\" /v Start /t REG_DWORD /d 4 /f").ConfigureAwait(false);

        // Disable speech model updates and maintenance
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Speech\" /v AllowSpeechModelUpdate /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Schedule\\Maintenance\" /v MaintenanceDisabled /t REG_DWORD /d 1 /f").ConfigureAwait(false);

        // Disable services
        await OptimizationOptions.StartInCmd("sc config wuauserv start= disabled").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc config UsoSvc start= disabled").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc config BITS start= disabled").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc config DoSvc start= demand").ConfigureAwait(false);

        // Handle WaaSMedicSvc
        if (build >= 19041)
        {
            // Disable WaaSMedic scheduled tasks - this prevents it from re-enabling update services
            await OptimizationOptions.StartInCmd("PowerShell -Command \"Get-ScheduledTask -TaskPath '\\Microsoft\\Windows\\WaaSMedic\\*' -ErrorAction SilentlyContinue | Disable-ScheduledTask -ErrorAction SilentlyContinue\"").ConfigureAwait(false);

            // Also try to take ownership and modify the service registry
            await OptimizationOptions.StartInCmd("PowerShell -Command \"$acl = Get-Acl 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\WaaSMedicSvc' -ErrorAction SilentlyContinue; if($acl) { $rule = New-Object System.Security.AccessControl.RegistryAccessRule('Administrators','FullControl','ContainerInherit,ObjectInherit','None','Allow'); $acl.SetAccessRule($rule); Set-Acl 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\WaaSMedicSvc' $acl -ErrorAction SilentlyContinue }\"").ConfigureAwait(false);
            await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\WaaSMedicSvc\" /v Start /t REG_DWORD /d 4 /f").ConfigureAwait(false);
        }
        else
        {
            // On older Windows, we can directly disable the service
            await OptimizationOptions.StartInCmd("sc stop WaaSMedicSvc").ConfigureAwait(false);
            await OptimizationOptions.StartInCmd("sc config WaaSMedicSvc start= disabled").ConfigureAwait(false);
        }

        // Disable Update Orchestrator scheduled tasks
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Get-ScheduledTask -TaskPath '\\Microsoft\\Windows\\UpdateOrchestrator\\*' -ErrorAction SilentlyContinue | Disable-ScheduledTask -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("PowerShell -Command \"Get-ScheduledTask -TaskPath '\\Microsoft\\Windows\\WindowsUpdate\\*' -ErrorAction SilentlyContinue | Disable-ScheduledTask -ErrorAction SilentlyContinue\"").ConfigureAwait(false);
    }

    public static async Task DisableStoreUpdates()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v SilentInstalledAppsEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent\" /v DisableSoftLanding /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v PreInstalledAppsEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent\" /v DisableWindowsConsumerFeatures /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v OemPreInstalledAppsEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsStore\" /v AutoDownload /t REG_DWORD /d 2 /f").ConfigureAwait(false);
    }

    public static async Task EnableStoreUpdates()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v SilentInstalledAppsEnabled /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent\" /v DisableSoftLanding /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v PreInstalledAppsEnabled /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent\" /v DisableWindowsConsumerFeatures /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v OemPreInstalledAppsEnabled /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsStore\" /v AutoDownload /f").ConfigureAwait(false);
    }


    public static async Task DisableOneDrive()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\OneDrive /v DisableFileSyncNGSC /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task EnableOneDrive()
    {
        await OptimizationOptions.StartInCmd("reg delete HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\OneDrive /v DisableFileSyncNGSC /f").ConfigureAwait(false);
    }

    public static async Task EnableSensorServices()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SYSTEM\\CurrentControlSet\\Services\\SensrSvc /v Start /t REG_DWORD /d 2 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SYSTEM\\CurrentControlSet\\Services\\SensorService /v Start /t REG_DWORD /d 2 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc start SensrSvc").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc start SensorService").ConfigureAwait(false);
    }

    public static async Task DisableSensorServices()
    {
        await OptimizationOptions.StartInCmd("sc stop SensrSvc").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc stop SensorService").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SYSTEM\\CurrentControlSet\\Services\\SensrSvc /v Start /t REG_DWORD /d 4 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SYSTEM\\CurrentControlSet\\Services\\SensorService /v Start /t REG_DWORD /d 4 /f").ConfigureAwait(false);
    }

    public static async Task DisableNewsAndInterests()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Feeds /v ShellFeedsTaskbarViewMode /t REG_DWORD /d 2 /f").ConfigureAwait(false);
    }

    public static async Task DisableSpotlightFeatures()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v RotatingLockScreenOverlayEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v RotatingLockScreenEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v DisableWindowsSpotlightFeatures /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task DisableTailoredExperiences()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v DisableTailoredExperiencesWithDiagnosticData /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Privacy /v TailoredExperiencesWithDiagnosticDataEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKEY_USERS\\.DEFAULT\\Software\\Microsoft\\Windows\\CurrentVersion\\Privacy /v TailoredExperiencesWithDiagnosticDataEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task DisableCloudOptimizedContent()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent /v DisableCloudOptimizedContent /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task DisableFeedbackNotifications()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection /v DoNotShowFeedbackNotifications /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task DisableAdvertisingID()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo /v Enabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AdvertisingInfo /v DisabledByGroupPolicy /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task DisableBluetoothAdvertising()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Bluetooth /v AllowAdvertising /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task DisableAutomaticRestartSignOn()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System /v DisableAutomaticRestartSignOn /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task DisableHandwritingDataSharing()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\TabletPC /v PreventHandwritingDataSharing /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task DisableTextInputDataCollection()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\TextInput /v AllowLinguisticDataCollection /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task DisableInputPersonalization()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\InputPersonalization /v AllowInputPersonalization /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task DisableSafeSearchMode()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\SearchSettings /v SafeSearchMode /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task DisableActivityUploads()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v UploadUserActivities /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task DisableClipboardSync()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v AllowCrossDeviceClipboard /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task DisableMessageSync()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\Software\\Policies\\Microsoft\\Windows\\Messaging /v AllowMessageSync /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task DisableSettingSync()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\Software\\Policies\\Microsoft\\Windows\\SettingSync /v DisableCredentialsSettingSync /t REG_DWORD /d 2 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\Software\\Policies\\Microsoft\\Windows\\SettingSync /v DisableCredentialsSettingSyncUserOverride /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\Software\\Policies\\Microsoft\\Windows\\SettingSync /v DisableApplicationSettingSync /t REG_DWORD /d 2 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\Software\\Policies\\Microsoft\\Windows\\SettingSync /v DisableApplicationSettingSyncUserOverride /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task DisableVoiceActivation()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy /v LetAppsActivateWithVoice /t REG_DWORD /d 2 /f").ConfigureAwait(false);
    }

    public static async Task DisableFindMyDevice()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\FindMyDevice /v AllowFindMyDevice /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Microsoft\\Settings\\FindMyDevice /v LocationSyncEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task DisableActivityFeed()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v EnableActivityFeed /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task DisableCdp()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v EnableCdp /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }
    public static async Task DisableDiagnosticsToast()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Diagnostics\\DiagTrack /v ShowedToastAtLevel /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKEY_USERS\\.DEFAULT\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Diagnostics\\DiagTrack /v ShowedToastAtLevel /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task DisableOnlineSpeechPrivacy()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Speech_OneCore\\Settings\\OnlineSpeechPrivacy /v HasAccepted /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task DisableLocationFeatures()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\location /v Value /t REG_SZ /d Deny /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\LocationAndSensors /v DisableLocation /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\LocationAndSensors /v DisableWindowsLocationProvider /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task EnableNewsAndInterests()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Feeds /v ShellFeedsTaskbarViewMode /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task EnableSpotlightFeatures()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v RotatingLockScreenOverlayEnabled /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v RotatingLockScreenEnabled /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v DisableWindowsSpotlightFeatures /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task EnableTailoredExperiences()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v DisableTailoredExperiencesWithDiagnosticData /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Privacy /v TailoredExperiencesWithDiagnosticDataEnabled /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKEY_USERS\\.DEFAULT\\Software\\Microsoft\\Windows\\CurrentVersion\\Privacy /v TailoredExperiencesWithDiagnosticDataEnabled /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task EnableCloudOptimizedContent()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent /v DisableCloudOptimizedContent /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task EnableFeedbackNotifications()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection /v DoNotShowFeedbackNotifications /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task EnableAdvertisingID()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo /v Enabled /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AdvertisingInfo /v DisabledByGroupPolicy /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task EnableBluetoothAdvertising()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Bluetooth /v AllowAdvertising /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task EnableAutomaticRestartSignOn()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System /v DisableAutomaticRestartSignOn /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task EnableHandwritingDataSharing()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\TabletPC /v PreventHandwritingDataSharing /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task EnableTextInputDataCollection()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\TextInput /v AllowLinguisticDataCollection /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task EnableInputPersonalization()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\InputPersonalization /v AllowInputPersonalization /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task EnableSafeSearchMode()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\SearchSettings /v SafeSearchMode /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task EnableActivityUploads()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v UploadUserActivities /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task EnableClipboardSync()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v AllowCrossDeviceClipboard /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task EnableMessageSync()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\Software\\Policies\\Microsoft\\Windows\\Messaging /v AllowMessageSync /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task EnableSettingSync()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\Software\\Policies\\Microsoft\\Windows\\SettingSync /v DisableCredentialsSettingSync /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\Software\\Policies\\Microsoft\\Windows\\SettingSync /v DisableCredentialsSettingSyncUserOverride /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\Software\\Policies\\Microsoft\\Windows\\SettingSync /v DisableApplicationSettingSync /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\Software\\Policies\\Microsoft\\Windows\\SettingSync /v DisableApplicationSettingSyncUserOverride /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task EnableVoiceActivation()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy /v LetAppsActivateWithVoice /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task EnableFindMyDevice()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\FindMyDevice /v AllowFindMyDevice /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Microsoft\\Settings\\FindMyDevice /v LocationSyncEnabled /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task EnableActivityFeed()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v EnableActivityFeed /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task EnableCdp()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v EnableCdp /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task EnableDiagnosticsToast()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Diagnostics\\DiagTrack /v ShowedToastAtLevel /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKEY_USERS\\.DEFAULT\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Diagnostics\\DiagTrack /v ShowedToastAtLevel /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task EnableOnlineSpeechPrivacy()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Speech_OneCore\\Settings\\OnlineSpeechPrivacy /v HasAccepted /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task EnableLocationFeatures()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\location /v Value /t REG_SZ /d Allow /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\LocationAndSensors /v DisableLocation /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\LocationAndSensors /v DisableWindowsLocationProvider /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task DisableBiometrics()
    {
        await OptimizationOptions.StartInCmd("REG ADD HKLM\\SOFTWARE\\Policies\\Microsoft\\Biometrics /v Enabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task EnableBiometrics()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Biometrics\" /v \"Enabled\" /f").ConfigureAwait(false);
    }

    public static async Task DisableGameBar()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR /v AppCaptureEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR /v AudioCaptureEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR /v CursorCaptureEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\GameBar /v UseNexusForGameBarEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\GameBar /v ShowStartupPanel /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\System\\GameConfigStore /v GameDVR_Enabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\Software\\Policies\\Microsoft\\Windows\\GameDVR /v AllowGameDVR /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task EnableGameBar()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR /v AppCaptureEnabled /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR /v AudioCaptureEnabled /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR /v CursorCaptureEnabled /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\GameBar /v UseNexusForGameBarEnabled /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\GameBar /v ShowStartupPanel /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\System\\GameConfigStore /v GameDVR_Enabled /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\Software\\Policies\\Microsoft\\Windows\\GameDVR /v AllowGameDVR /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task DisableQuickAccessHistory()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Feeds /v ShellFeedsTaskbarViewMode /t REG_DWORD /d 2 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Feeds /v IsFeedsAvailable /t REG_DWORD /d 0 /f").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced /v ShowTaskViewButton /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\OperationStatusManager /v EnthusiastMode /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced /v ShowSyncProviderNotifications /t REG_DWORD /d 0 /f").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer /v ShowFrequent /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer /v ShowRecent /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced /v LaunchTo /t REG_DWORD /d 1 /f").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer /v HideSCAMeetNow /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer /v HideSCAMeetNow /t REG_DWORD /d 1 /f").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\FileHistory /v Disabled /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\File History /v Disabled /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task EnableQuickAccessHistory()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\OperationStatusManager /v EnthusiastMode /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced /v ShowSyncProviderNotifications /t REG_DWORD /d 1 /f").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer /v ShowFrequent /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer /v ShowRecent /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced /v LaunchTo /t REG_DWORD /d 2 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced /v ShowTaskViewButton /f").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("reg delete HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\FileHistory /v Disabled /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\File History /v Disabled /f").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search /v SearchboxTaskbarMode /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Feeds /v ShellFeedsTaskbarViewMode /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Feeds /v IsFeedsAvailable /f").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("reg delete HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer /v HideSCAMeetNow /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer /v HideSCAMeetNow /f").ConfigureAwait(false);
    }

    public static async Task DisableStartMenuAds()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-88000326Enabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\UserProfileEngagement /v ScoobeSystemSettingEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v ContentDeliveryAllowed /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v RemediationRequired /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v PreInstalledAppsEverEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SilentInstalledAppsEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-314559Enabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-338387Enabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-338389Enabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SystemPaneSuggestionsEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-338393Enabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-353694Enabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-353696Enabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-310093Enabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-338388Enabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContentEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SoftLandingEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v FeatureManagementEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Policies\\Microsoft\\Windows\\Explorer /v DisableSearchBoxSuggestions /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer /v AllowOnlineTips /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer /v DisableSearchBoxSuggestions /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task EnableStartMenuAds()
    {
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-88000326Enabled /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\UserProfileEngagement /v ScoobeSystemSettingEnabled /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v ContentDeliveryAllowed /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v RemediationRequired /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v PreInstalledAppsEverEnabled /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SilentInstalledAppsEnabled /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-314559Enabled /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-338387Enabled /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-338389Enabled /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SystemPaneSuggestionsEnabled /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-338393Enabled /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-353694Enabled /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-353696Enabled /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-310093Enabled /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContentEnabled /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SubscribedContent-338388Enabled /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v SoftLandingEnabled /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager /v FeatureManagementEnabled /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Policies\\Microsoft\\Windows\\Explorer /v DisableSearchBoxSuggestions /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer /v AllowOnlineTips /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer /v DisableSearchBoxSuggestions /f").ConfigureAwait(false);
    }

    public static async Task DisableMyPeople()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\People /v PeopleBand /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task EnableMyPeople()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\People /v PeopleBand /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task ExcludeDrivers()
    {
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\default\\Update\\ExcludeWUDriversInQualityUpdate /v value /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\default\\Update /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Update /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Device Metadata /v PreventDeviceMetadataFromNetwork /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\DriverSearching /v SearchOrderConfig /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\DriverSearching /v DontSearchWindowsUpdate /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task IncludeDrivers()
    {
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\default\\Update\\ExcludeWUDriversInQualityUpdate /v value /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\default\\Update /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Update /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Device Metadata /v PreventDeviceMetadataFromNetwork /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\DriverSearching /v SearchOrderConfig /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\DriverSearching /v DontSearchWindowsUpdate /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task DisableWindowsInk()
    {
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsInkWorkspace /v AllowWindowsInkWorkspace /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsInkWorkspace /v AllowSuggestedAppsInWindowsInkWorkspace /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableInkingWithTouch /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task EnableWindowsInk()
    {
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsInkWorkspace /v AllowWindowsInkWorkspace /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsInkWorkspace /v AllowSuggestedAppsInWindowsInkWorkspace /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableInkingWithTouch /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task DisableSpellingAndTypingFeatures()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableAutocorrection /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableSpellchecking /t REG_DWORD /d 0 /f").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Input\\Settings /v InsightsEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableDoubleTapSpace /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnablePredictionSpaceInsertion /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableTextPrediction /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task EnableSpellingAndTypingFeatures()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableAutocorrection /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableSpellchecking /t REG_DWORD /d 1 /f").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Input\\Settings /v InsightsEnabled /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableDoubleTapSpace /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnablePredictionSpaceInsertion /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\SOFTWARE\\Microsoft\\TabletTip\\1.7 /v EnableTextPrediction /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task EnableFaxService()
    {
        await OptimizationOptions.StartInCmd("reg add HKLM\\SYSTEM\\CurrentControlSet\\Services\\Fax /v Start /t REG_DWORD /d 3 /f").ConfigureAwait(false);
    }

    public static async Task DisableFaxService()
    {
        await OptimizationOptions.StartInCmd("sc stop Fax").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SYSTEM\\CurrentControlSet\\Services\\Fax /v Start /t REG_DWORD /d 4 /f").ConfigureAwait(false);
    }

    public static async Task EnableInsiderService()
    {
        await OptimizationOptions.StartInCmd("reg add HKLM\\SYSTEM\\CurrentControlSet\\Services\\wisvc /v Start /t REG_DWORD /d 3 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc start wisvc").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\PreviewBuilds /v AllowBuildPreview /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\PreviewBuilds /v EnableConfigFlighting /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\PreviewBuilds /v EnableExperimentation /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKLM\\SOFTWARE\\Microsoft\\WindowsSelfHost\\UI\\Visibility /v HideInsiderPage /f").ConfigureAwait(false);
    }

    public static async Task DisableInsiderService()
    {
        await OptimizationOptions.StartInCmd("sc stop wisvc").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SYSTEM\\CurrentControlSet\\Services\\wisvc /v Start /t REG_DWORD /d 4 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\PreviewBuilds /v AllowBuildPreview /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\PreviewBuilds /v EnableConfigFlighting /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\PreviewBuilds /v EnableExperimentation /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\WindowsSelfHost\\UI\\Visibility /v HideInsiderPage /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }


    public static async Task DisableSmartScreen()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Attachments /v SaveZoneInformation /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Attachments /v ScanWithAntiVirus /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v ShellSmartScreenLevel /t REG_SZ /d Warn /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v EnableSmartScreen /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer /v SmartScreenEnabled /t REG_SZ /d Off /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\Internet Explorer\\PhishingFilter /v EnabledV9 /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\AppHost /v PreventOverride /t REG_DWORD /d 0 /f").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Notifications\\Settings\\Windows.SystemToast.SecurityAndMaintenance /v Enabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task EnableSmartScreen()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Attachments /v SaveZoneInformation /t REG_DWORD /d 2 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Attachments /v ScanWithAntiVirus /t REG_DWORD /d 2 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v EnableSmartScreen /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer /v SmartScreenEnabled /t REG_SZ /d On /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Microsoft\\Internet Explorer\\PhishingFilter /v EnabledV9 /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\AppHost /v PreventOverride /f").ConfigureAwait(false);

    }

    public static async Task DisableCloudClipboard()
    {
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v AllowClipboardHistory /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v AllowCrossDeviceClipboard /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Clipboard /v EnableClipboardHistory /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKLM\\Software\\Microsoft\\Clipboard /v EnableClipboardHistory /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task EnableCloudClipboard()
    {
        await OptimizationOptions.StartInCmd("reg delete HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v AllowClipboardHistory /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System /v AllowCrossDeviceClipboard /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKCU\\Software\\Microsoft\\Clipboard /v EnableClipboardHistory /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete HKLM\\Software\\Microsoft\\Clipboard /v EnableClipboardHistory /f").ConfigureAwait(false);
    }

    public static async Task DisableStickyKeys()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\Control Panel\\Accessibility\\StickyKeys /v Flags /t REG_SZ /d 506 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Control Panel\\Accessibility\\Keyboard Response /v Flags /t REG_SZ /d 122 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Control Panel\\Accessibility\\ToggleKeys /v Flags /t REG_SZ /d 58 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKEY_USERS\\.DEFAULT\\Control Panel\\Accessibility\\StickyKeys /v Flags /t REG_SZ /d 506 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKEY_USERS\\.DEFAULT\\Control Panel\\Accessibility\\Keyboard Response /v Flags /t REG_SZ /d 122 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKEY_USERS\\.DEFAULT\\Control Panel\\Accessibility\\ToggleKeys /v Flags /t REG_SZ /d 58 /f").ConfigureAwait(false);
    }

    public static async Task EnableStickyKeys()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\Control Panel\\Accessibility\\StickyKeys /v Flags /t REG_SZ /d 510 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Control Panel\\Accessibility\\Keyboard Response /v Flags /t REG_SZ /d 126 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKCU\\Control Panel\\Accessibility\\ToggleKeys /v Flags /t REG_SZ /d 62 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKEY_USERS\\.DEFAULT\\Control Panel\\Accessibility\\StickyKeys /v Flags /t REG_SZ /d 510 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKEY_USERS\\.DEFAULT\\Control Panel\\Accessibility\\Keyboard Response /v Flags /t REG_SZ /d 126 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add HKEY_USERS\\.DEFAULT\\Control Panel\\Accessibility\\ToggleKeys /v Flags /t REG_SZ /d 62 /f").ConfigureAwait(false);
    }

    public static async Task RemoveCastToDevice()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked\" /V {7AD84985-87B4-4a16-BE58-8B72A5B390F7} /T REG_SZ /D \"Play to Menu\" /F").ConfigureAwait(false);
    }

    public static async Task AddCastToDevice()
    {
        await OptimizationOptions.StartInCmd("REG DELETE \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked\" /V {7AD84985-87B4-4a16-BE58-8B72A5B390F7} /F").ConfigureAwait(false);
    }

    public static async Task DisableVBS()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\" /V EnableVirtualizationBasedSecurity /T REG_DWORD /D 0 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity\" /V Enabled /T REG_DWORD /D 0 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\KernelShadowStacks\" /V Enabled /T REG_DWORD /D 0 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\CredentialGuard\" /V Enabled /T REG_DWORD /D 0 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Lsa\" /V RunAsPPL /T REG_DWORD /D 0 /F").ConfigureAwait(false);
    }

    public static async Task EnableVBS()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\" /V EnableVirtualizationBasedSecurity /T REG_DWORD /D 1 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity\" /V Enabled /T REG_DWORD /D 1 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\KernelShadowStacks\" /V Enabled /T REG_DWORD /D 1 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\CredentialGuard\" /V Enabled /T REG_DWORD /D 1 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Lsa\" /V RunAsPPL /T REG_DWORD /D 1 /F").ConfigureAwait(false);
    }

    public static async Task AlignTaskbarToLeft()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v TaskbarAl /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task AlignTaskbarToCenter()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v TaskbarAl /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task DisableSnapAssist()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V EnableSnapAssistFlyout /T REG_DWORD /D 0 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V EnableSnapBar /T REG_DWORD /D 0 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Control Panel\\Desktop\" /V DockMoving /T REG_SZ /D 0 /F").ConfigureAwait(false);
    }

    public static async Task EnableSnapAssist()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V EnableSnapAssistFlyout /T REG_DWORD /D 1 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V EnableSnapBar /T REG_DWORD /D 1 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Control Panel\\Desktop\" /V DockMoving /T REG_SZ /D 1 /F").ConfigureAwait(false);
    }

    public static async Task DisableWidgets()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V TaskbarDa /T REG_DWORD /D 0 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Feeds\" /V EnableFeeds /T REG_DWORD /D 0 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Dsh\" /V AllowNewsAndInterests /T REG_DWORD /D 0 /F").ConfigureAwait(false);
    }

    public static async Task EnableWidgets()
    {
        await OptimizationOptions.StartInCmd("REG Delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V TaskbarDa /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Feeds\" /V EnableFeeds /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Dsh\" /V AllowNewsAndInterests /F").ConfigureAwait(false);
    }

    public static async Task DisableChat()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V TaskbarMn /T REG_DWORD /D 0 /F").ConfigureAwait(false);
    }

    public static async Task EnableChat()
    {
        await OptimizationOptions.StartInCmd("REG Delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V TaskbarMn /F").ConfigureAwait(false);
    }

    public static async Task DisableShowMoreOptions()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\\InprocServer32\" /V \"\" /F").ConfigureAwait(false);
    }

    public static async Task EnableShowMoreOptions()
    {
        await OptimizationOptions.StartInCmd("REG Delete \"HKCU\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\" /F").ConfigureAwait(false);
    }

    public static async Task EnableFilesCompactMode()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V UseCompactMode /T REG_DWORD /D 1 /F").ConfigureAwait(false);
    }

    public static async Task DisableFilesCompactMode()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V UseCompactMode /T REG_DWORD /D 0 /F").ConfigureAwait(false);
    }

    public static async Task DisableStickers()
    {
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Stickers\" /V EnableStickers /F").ConfigureAwait(false);
    }

    public static async Task EnableStickers()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Stickers\" /V EnableStickers /T REG_DWORD /D 1 /F").ConfigureAwait(false);
    }


    public static async Task DisableEdgeDiscoverBar()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V HubsSidebarEnabled /T REG_DWORD /D 0 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V HubsSidebarEnabled /T REG_DWORD /D 0 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V WebWidgetAllowed /T REG_DWORD /D 0 /F").ConfigureAwait(false);
    }

    public static async Task EnableEdgeDiscoverBar()
    {
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V HubsSidebarEnabled /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG Delete \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V HubsSidebarEnabled /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V WebWidgetAllowed /F").ConfigureAwait(false);
    }

    public static async Task DisableEdgeTelemetry()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V PersonalizationReportingEnabled /T REG_DWORD /D 0 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V PersonalizationReportingEnabled /T REG_DWORD /D 0 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V UserFeedbackAllowed /T REG_DWORD /D 0 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V UserFeedbackAllowed /T REG_DWORD /D 0 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V MetricsReportingEnabled /T REG_DWORD /D 0 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V MetricsReportingEnabled /T REG_DWORD /D 0 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\MicrosoftEdge\\BooksLibrary\" /V EnableExtendedBooksTelemetry /T REG_DWORD /D 0 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\SOFTWARE\\Policies\\Microsoft\\MicrosoftEdge\\BooksLibrary\" /V EnableExtendedBooksTelemetry /T REG_DWORD /D 0 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\Edge\" /V SmartScreenEnabled /T REG_DWORD /D 0 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\Edge\" /V SmartScreenPuaEnabled /T REG_DWORD /D 0 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V SpotlightExperiencesAndRecommendationsEnabled /T REG_DWORD /D 0 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V SpotlightExperiencesAndRecommendationsEnabled /T REG_DWORD /D 0 /F").ConfigureAwait(false);
    }

    public static async Task EnableEdgeTelemetry()
    {
        await OptimizationOptions.StartInCmd("REG Delete \"HKCU\\Software\\Microsoft\\Edge\" /V SmartScreenEnabled /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG Delete \"HKCU\\Software\\Microsoft\\Edge\" /V SmartScreenPuaEnabled /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V MetricsReportingEnabled /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG Delete \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V MetricsReportingEnabled /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\MicrosoftEdge\\BooksLibrary\" /V EnableExtendedBooksTelemetry /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG Delete \"HKCU\\SOFTWARE\\Policies\\Microsoft\\MicrosoftEdge\\BooksLibrary\" /V EnableExtendedBooksTelemetry /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V PersonalizationReportingEnabled /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V UserFeedbackAllowed /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG Delete \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V PersonalizationReportingEnabled /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG Delete \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V UserFeedbackAllowed /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V SpotlightExperiencesAndRecommendationsEnabled /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG Delete \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Edge\" /V SpotlightExperiencesAndRecommendationsEnabled /F").ConfigureAwait(false);
    }

    public static async Task DisableCoPilotAI()
    {
        // Registry Policies & Button
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsCopilot\" /V TurnOffWindowsCopilot /T REG_DWORD /D 1 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Policies\\Microsoft\\Windows\\WindowsCopilot\" /V TurnOffWindowsCopilot /T REG_DWORD /D 1 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\Windows\\CurrentVe\rsion\\Explorer\\Advanced\" /V ShowCopilotButton /T REG_DWORD /D 0 /F").ConfigureAwait(false);

        // Block the Shell Extension (Prevents Copilot UI from loading in the Shell)
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked\" /V \"{64134153-2E11-492F-8181-314091BA79A3}\" /T REG_SZ /D \"Copilot\" /F").ConfigureAwait(false);

        // Edit the Region Policy JSON
        await ToggleCopilotJsonPolicy(isDisabled: true);

        // Stop and remove/disable services related to Copilot/AI
        await OptimizationOptions.StartInCmd("powershell -Command \"Stop-Service -Name 'WSAIFabricSvc' -ErrorAction SilentlyContinue; sc.exe delete WSAIFabricSvc -ErrorAction SilentlyContinue\"").ConfigureAwait(false);

        // Remove Voice Access executable and StartMenu link if present (requires elevated/more privileges)
        var voiceAccessCmd = "Remove-Item -Path $env:windir\\System32\\voiceaccess.exe -Force -ErrorAction SilentlyContinue; Remove-Item \"$env:appdata\\Microsoft\\Windows\\Start Menu\\Programs\\Accessibility\\VoiceAccess.lnk\" -Force -ErrorAction SilentlyContinue";
        await OptimizationOptions.StartInCmd($"powershell -NoProfile -Command \"{voiceAccessCmd}\"").ConfigureAwait(false);

        // Appx Removal (Uninstall + Deprovision)
        var psScript = @"
            $targets = @('Microsoft.Windows.Copilot', 'MicrosoftWindows.Client.Copilot', 'Microsoft.Windows.Ai.Copilot.Provider', 'Microsoft.Copilot');
            foreach ($t in $targets) {
                Get-AppxPackage -AllUsers -Name ""*$t*"" | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue;
                Get-AppxProvisionedPackage -Online | Where-Object { $_.DisplayName -like ""*$t*"" } | Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue;
            }
        ";
        await OptimizationOptions.StartInCmd($"powershell -NoProfile -Command \"{psScript}\"").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("taskkill /F /IM explorer.exe & start %SystemRoot%\\explorer.exe").ConfigureAwait(false);
    }

    public static async Task EnableCoPilotAI()
    {
        await OptimizationOptions.StartInCmd("REG DELETE \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsCopilot\" /V TurnOffWindowsCopilot /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG DELETE \"HKCU\\Software\\Policies\\Microsoft\\Windows\\WindowsCopilot\" /V TurnOffWindowsCopilot /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG DELETE \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked\" /V \"{64134153-2E11-492F-8181-314091BA79A3}\" /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /V ShowCopilotButton /T REG_DWORD /D 1 /F").ConfigureAwait(false);

        await ToggleCopilotJsonPolicy(isDisabled: false);

        var psScript = "Get-AppxPackage -allusers *Copilot* | foreach {Add-AppxPackage -register \"$($_.InstallLocation)\\appxmanifest.xml\" -DisableDevelopmentMode}";
        await OptimizationOptions.StartInCmd($"powershell -NoProfile -Command \"{psScript}\"").ConfigureAwait(false);

        await OptimizationOptions.StartInCmd("taskkill /F /IM explorer.exe & start %SystemRoot%\\explorer.exe").ConfigureAwait(false);
    }

    public static async Task DisableVisualStudioTelemetry()
    {
        await OptimizationOptions.StartInCmd("REG ADD \"HKCU\\Software\\Microsoft\\VisualStudio\\Telemetry\" /V TurnOffSwitch /T REG_DWORD /D 1 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\Feedback\" /V DisableFeedbackDialog /T REG_DWORD /D 1 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\Feedback\" /V DisableEmailInput /T REG_DWORD /D 1 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\Feedback\" /V DisableScreenshotCapture /T REG_DWORD /D 1 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\SQM\" /V OptIn /T REG_DWORD /D 0 /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\Setup\" /V ConcurrentDownloads /T REG_DWORD /D 2 /F").ConfigureAwait(false);

        if (Environment.Is64BitOperatingSystem)
        {
            await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Microsoft\\VSCommon\\14.0\\SQM\" /V OptIn /T REG_DWORD /D 0 /F").ConfigureAwait(false);
            await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Microsoft\\VSCommon\\15.0\\SQM\" /V OptIn /T REG_DWORD /D 0 /F").ConfigureAwait(false);
            await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Microsoft\\VSCommon\\16.0\\SQM\" /V OptIn /T REG_DWORD /D 0 /F").ConfigureAwait(false);
        }
        else
        {
            await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Wow6432Node\\Microsoft\\VSCommon\\14.0\\SQM\" /V OptIn /T REG_DWORD /D 0 /F").ConfigureAwait(false);
            await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Wow6432Node\\Microsoft\\VSCommon\\15.0\\SQM\" /V OptIn /T REG_DWORD /D 0 /F").ConfigureAwait(false);
            await OptimizationOptions.StartInCmd("REG ADD \"HKLM\\SOFTWARE\\Wow6432Node\\Microsoft\\VSCommon\\16.0\\SQM\" /V OptIn /T REG_DWORD /D 0 /F").ConfigureAwait(false);
        }
        await OptimizationOptions.StartInCmd("SC Config VSStandardCollectorService150 Start= disabled").ConfigureAwait(false);
    }

    public static async Task EnableVisualStudioTelemetry()
    {
        await OptimizationOptions.StartInCmd("REG Delete \"HKCU\\Software\\Microsoft\\VisualStudio\\Telemetry\" /V TurnOffSwitch /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\Feedback\" /V DisableFeedbackDialog /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\Feedback\" /V DisableEmailInput /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\Feedback\" /V DisableScreenshotCapture /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\SQM\" /V OptIn /F").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\VisualStudio\\Setup\" /V ConcurrentDownloads /F").ConfigureAwait(false);

        if (Environment.Is64BitOperatingSystem)
        {
            await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Microsoft\\VSCommon\\14.0\\SQM\" /V OptIn /F").ConfigureAwait(false);
            await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Microsoft\\VSCommon\\15.0\\SQM\" /V OptIn /F").ConfigureAwait(false);
            await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Microsoft\\VSCommon\\16.0\\SQM\" /V OptIn /F").ConfigureAwait(false);
        }
        else
        {
            await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Wow6432Node\\Microsoft\\VSCommon\\14.0\\SQM\" /V OptIn /F").ConfigureAwait(false);
            await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Wow6432Node\\Microsoft\\VSCommon\\15.0\\SQM\" /V OptIn /F").ConfigureAwait(false);
            await OptimizationOptions.StartInCmd("REG Delete \"HKLM\\SOFTWARE\\Wow6432Node\\Microsoft\\VSCommon\\16.0\\SQM\" /V OptIn /F").ConfigureAwait(false);
        }
        await OptimizationOptions.StartInCmd("SC Config VSStandardCollectorService150 Start= demand").ConfigureAwait(false);
    }

    public static async Task DisableNvidiaTelemetry()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\NvTelemetryContainer\" /v Start /t REG_DWORD /d 4 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("schtasks.exe /change /tn NvTmRepOnLogon_{B2FE1952-0186-46C3-BAEC-A80AA35AC5B8} /disable").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("schtasks.exe /change /tn NvTmRep_{B2FE1952-0186-46C3-BAEC-A80AA35AC5B8} /disable").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("schtasks.exe /change /tn NvTmMon_{B2FE1952-0186-46C3-BAEC-A80AA35AC5B8} /disable").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("net.exe stop NvTelemetryContainer").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc.exe config NvTelemetryContainer start= disabled").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc.exe stop NvTelemetryContainer").ConfigureAwait(false);
    }

    public static async Task EnableNvidiaTelemetry()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\NvTelemetryContainer\" /v Start /t REG_DWORD /d 2 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("schtasks.exe /change /tn NvTmRepOnLogon_{B2FE1952-0186-46C3-BAEC-A80AA35AC5B8} /enable").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("schtasks.exe /change /tn NvTmRep_{B2FE1952-0186-46C3-BAEC-A80AA35AC5B8} /enable").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("schtasks.exe /change /tn NvTmMon_{B2FE1952-0186-46C3-BAEC-A80AA35AC5B8} /enable").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("net.exe start NvTelemetryContainer").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc.exe config NvTelemetryContainer start= auto").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("sc.exe start NvTelemetryContainer").ConfigureAwait(false);
    }

    public static async Task DisableChromeTelemetry()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Google\\Chrome\" /v MetricsReportingEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Google\\Chrome\" /v ChromeCleanupReportingEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Google\\Chrome\" /v ChromeCleanupEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Google\\Chrome\" /v UserFeedbackAllowed /t REG_DWORD /d 0 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Google\\Chrome\" /v DeviceMetricsReportingEnabled /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task EnableChromeTelemetry()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Google\\Chrome\" /v MetricsReportingEnabled /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Google\\Chrome\" /v ChromeCleanupReportingEnabled /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Google\\Chrome\" /v ChromeCleanupEnabled /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Google\\Chrome\" /v UserFeedbackAllowed /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Google\\Chrome\" /v DeviceMetricsReportingEnabled /f").ConfigureAwait(false);
    }

    public static async Task DisableFirefoxTelemetry()
    {
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Mozilla\\Firefox\" /v DisableTelemetry /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Mozilla\\Firefox\" /v DisableDefaultBrowserAgent /t REG_DWORD /d 1 /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("schtasks.exe /change /disable /tn \"\\Mozilla\\Firefox Default Browser Agent 308046B0AF4A39CB\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("schtasks.exe /change /disable /tn \"\\Mozilla\\Firefox Default Browser Agent D2CEEC440E2074BD\"").ConfigureAwait(false);
    }

    public static async Task EnableFirefoxTelemetry()
    {
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Mozilla\\Firefox\" /v DisableTelemetry /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Mozilla\\Firefox\" /v DisableDefaultBrowserAgent /f").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("schtasks.exe /change /enable /tn \"\\Mozilla\\Firefox Default Browser Agent 308046B0AF4A39CB\"").ConfigureAwait(false);
        await OptimizationOptions.StartInCmd("schtasks.exe /change /enable /tn \"\\Mozilla\\Firefox Default Browser Agent D2CEEC440E2074BD\"").ConfigureAwait(false);
    }
    public static async Task DisableHibernation()
    {
        await OptimizationOptions.StartInCmd("powercfg -h off").ConfigureAwait(false);
    }

    public static async Task EnableHibernation()
    {
        await OptimizationOptions.StartInCmd("powercfg -h on").ConfigureAwait(false);
    }

    public static async Task EnableEndTask()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\TaskbarDeveloperSettings /v TaskbarEndTask /t REG_DWORD /d 1 /f").ConfigureAwait(false);
    }

    public static async Task DisableEndTask()
    {
        await OptimizationOptions.StartInCmd("reg add HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\TaskbarDeveloperSettings /v TaskbarEndTask /t REG_DWORD /d 0 /f").ConfigureAwait(false);
    }

    public static async Task<bool> RemoveTempFiles()
    {
        try
        {
            // List of commands to remove temporary files
            var tempCommands = new[]
            {
            "rd /S /Q %windir%\\Temp",
            "rd /S /Q %TEMP%",
            "rd /S /Q %windir%\\SoftwareDistribution\\Download",
            "rd /S /Q %windir%\\SoftwareDistribution\\DeliveryOptimization",
            "del /F /S /Q %windir%\\Logs\\CBS\\*",
            "del /F /S /Q %windir%\\MEMORY.DMP",
            "del /F /S /Q %windir%\\Minidump\\*.dmp",
            "del /F /S /Q %windir%\\Temp\\WindowsUpdate.log",
            "rd /S /Q %programdata%\\Microsoft\\Windows\\WER\\ReportQueue",
            "rd /S /Q %localappdata%\\Microsoft\\Windows\\WER\\ReportArchive",
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
            "PowerShell.exe -NoProfile -Command \"& { Remove-Item -Path \"$env:LOCALAPPDATA\\Google\\Chrome\\User Data\\Default\\*\" -Include 'Cache','Cookies','History','Visited Links','Archived History','Web Data','Current Session','Last Session' -Recurse -Force -ErrorAction SilentlyContinue }\"",
            "PowerShell.exe -NoProfile -Command \"& { Remove-Item -Path \"$env:LOCALAPPDATA\\Microsoft\\Edge\\User Data\\Default\\Cache\" -Recurse -Force -ErrorAction SilentlyContinue }\"",
            "PowerShell.exe -NoProfile -Command \"& { Remove-Item -Path \"$env:APPDATA\\Mozilla\\Firefox\\Profiles\\*\\cache2\" -Recurse -Force -ErrorAction SilentlyContinue }\"",
            "PowerShell.exe -NoProfile -Command \"& { Remove-Item -Path \"$env:APPDATA\\Moonchild Productions\\Pale Moon\\Profiles\\*\\cache2\\entries\" -Recurse -Force -ErrorAction SilentlyContinue }\"",
            "PowerShell.exe -NoProfile -Command \"Clear-RecycleBin -Force\"",
            "PowerShell.exe -NoProfile -Command \"wevtutil cl System\"",
            "PowerShell.exe -NoProfile -Command \"wevtutil cl Application\"",
            "ipconfig /flushdns",
            //"dism /Online /Cleanup-Image /StartComponentCleanup /Quiet"
            };

            // Commands that require Explorer to be killed
            var explorerDependentCommands = new[]
            {
            "rd /S /Q %localappdata%\\Temp",
            "rd /S /Q %localappdata%\\Microsoft\\Windows\\INetCache",
            "del /A /Q %localappdata%\\Microsoft\\Windows\\Explorer\\iconcache*",
            "del /A /Q %localappdata%\\Microsoft\\Windows\\Explorer\\thumbcache*",
            "rd /S /Q %windir%\\Prefetch"
            };

            // Kill Explorer to release file locks
            await OptimizationOptions.StartInCmd("taskkill /F /IM explorer.exe").ConfigureAwait(false);

            // Execute commands that depend on Explorer being closed
            foreach (var cmd in explorerDependentCommands)
            {
                await OptimizationOptions.StartInCmd(cmd).ConfigureAwait(false);
            }

            // Restart Explorer immediately
            await OptimizationOptions.StartInCmd("start %SystemRoot%\\explorer.exe").ConfigureAwait(false);

            // Execute remaining commands
            foreach (var cmd in tempCommands)
            {
                await OptimizationOptions.StartInCmd(cmd).ConfigureAwait(false);
            }

            return true;
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error removing temp files: {ex.Message}");
            return false;
        }
    }
}