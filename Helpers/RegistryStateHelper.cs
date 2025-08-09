using Microsoft.Win32;

namespace RyTuneX.Helpers;

public static partial class OptimizeSystemHelper
{
    private const string RegistryBaseKey = @"SOFTWARE\RyTuneX\Optimizations";

    public static bool GetFeatureState(string tagName)
    {
        try
        {
            switch (tagName)
            {
                // Features Page - System Features
                case "WindowsTransparency":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                    {
                        var value = key?.GetValue("EnableTransparency");
                        return !(value is int v && v == 0); // Transparency NOT disabled
                    }
                case "WindowsDarkMode":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                    {
                        var value = key?.GetValue("AppsUseLightTheme");
                        return value is int v && v == 0; // Dark Mode enabled
                    }
                case "VerboseLogon":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                    {
                        var value = key?.GetValue("VerboseStatus");
                        return value is int v && v == 1; // Verbose Logon enabled
                    }
                case "Hibernation":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power"))
                    {
                        var value = key?.GetValue("HibernateEnabled");
                        return value is int v && v == 0; // Hibernation disabled
                    }
                case "HomeGroup":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\HomeGroup"))
                    {
                        var value = key?.GetValue("DisableHomeGroup");
                        return value is int v && v == 1; // HomeGroup disabled
                    }
                case "PrintService":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Spooler"))
                    {
                        var value = key?.GetValue("Start");
                        return value is int v && v == 4; // Print Service disabled
                    }
                case "CompatibilityAssistant":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\PcaSvc"))
                    {
                        var value = key?.GetValue("Start");
                        return value is int v && v == 4; // Compatibility Assistant disabled
                    }
                case "Search":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\WSearch"))
                    {
                        var value = key?.GetValue("Start");
                        return value is int v && v == 4; // Search disabled
                    }
                case "ErrorReporting":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting"))
                    {
                        var value = key?.GetValue("Disabled");
                        return value is int v && v == 1; // Error Reporting disabled
                    }
                case "GameBar":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\GameDVR"))
                    {
                        var value = key?.GetValue("AppCaptureEnabled");
                        return value is int v && v == 0; // GameBar disabled
                    }
                case "QuickAccessHistory":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer"))
                    {
                        var value = key?.GetValue("ShowRecent");
                        return value is int v && v == 0; // Quick Access History disabled
                    }
                case "MyPeople":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced\People"))
                    {
                        var value = key?.GetValue("PeopleBand");
                        return value is int v && v == 0; // My People disabled
                    }
                case "SensorServices":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\SensrSvc"))
                    {
                        var value = key?.GetValue("Start");
                        return value is int v && v == 4; // Sensor Services disabled
                    }
                case "WindowsInk":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\WindowsInkWorkspace"))
                    {
                        var value = key?.GetValue("AllowWindowsInkWorkspace");
                        return value is int v && v == 0; // Windows Ink disabled
                    }
                case "SpellingAndTypingFeatures":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\TabletTip\1.7"))
                    {
                        var value = key?.GetValue("EnableAutocorrection");
                        return value is int v && v == 0; // Spelling and Typing disabled
                    }
                case "FaxService":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Fax"))
                    {
                        var value = key?.GetValue("Start");
                        return value is int v && v == 4; // Fax Service disabled
                    }
                case "InsiderService":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\wisvc"))
                    {
                        var value = key?.GetValue("Start");
                        return value is int v && v == 4; // Insider Service disabled
                    }
                case "CloudClipboard":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System"))
                    {
                        var value = key?.GetValue("AllowClipboardHistory");
                        return value is int v && v == 0; // Cloud Clipboard disabled
                    }
                case "StickyKeys":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Accessibility\StickyKeys"))
                    {
                        var value = key?.GetValue("Flags");
                        return value?.ToString() == "506"; // Sticky Keys disabled
                    }
                case "CastToDevice":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked"))
                    {
                        var value = key?.GetValue("{7AD84985-87B4-4a16-BE58-8B72A5B390F7}");
                        return value != null; // Cast to Device disabled
                    }

                // Windows 11 Features
                case "VBS":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard"))
                    {
                        var value = key?.GetValue("EnableVirtualizationBasedSecurity");
                        return value is int v && v == 0; // VBS disabled
                    }
                case "EndTask":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings"))
                    {
                        var value = key?.GetValue("TaskbarEndTask");
                        return value is int v && v == 1; // End Task enabled
                    }
                case "ClassicContextMenu":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32"))
                    {
                        return key != null; // Classic Context Menu enabled
                    }
                case "RecommendedSectionStartMenu":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\PolicyManager\current\device\Start"))
                    {
                        var value = key?.GetValue("HideRecommendedSection");
                        return value is int v && v == 1; // Recommended Section disabled
                    }
                case "TaskbarToLeft":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                    {
                        var value = key?.GetValue("TaskbarAl");
                        return value is int v && v == 0; // Taskbar aligned to left
                    }
                case "SnapAssist":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                    {
                        var value = key?.GetValue("EnableSnapAssistFlyout");
                        return value is int v && v == 0; // Snap Assist disabled
                    }
                case "Widgets":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                    {
                        var value = key?.GetValue("TaskbarDa");
                        return value is int v && v == 0; // Widgets disabled
                    }
                case "Chat":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                    {
                        var value = key?.GetValue("TaskbarMn");
                        return value is int v && v == 0; // Chat disabled
                    }
                case "FilesCompactMode":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                    {
                        var value = key?.GetValue("UseCompactMode");
                        return value is int v && v == 1; // Files Compact Mode enabled
                    }
                case "Stickers":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\PolicyManager\current\device\Stickers"))
                    {
                        var value = key?.GetValue("EnableStickers");
                        return value == null || (value is int v && v == 0); // Stickers disabled
                    }
                case "EdgeDiscoverBar":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Edge"))
                    {
                        var value = key?.GetValue("HubsSidebarEnabled");
                        return value is int v && v == 0; // Edge Discover Bar disabled
                    }
                case "CoPilotAI":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Policies\Microsoft\Windows\WindowsCopilot"))
                    {
                        var value = key?.GetValue("TurnOffWindowsCopilot");
                        return value is int v && v == 1; // CoPilot AI disabled
                    }
                case "WindowsRecall":
                    using (var ryTuneXKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                        Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                            ? RegistryView.Registry64
                            : RegistryView.Default).OpenSubKey(RegistryBaseKey))
                    {
                        var savedState = ryTuneXKey?.GetValue("WindowsRecall");
                        return savedState is int v && v == 1; // Windows Recall disabled
                    }

                // Privacy Page - Advertising
                case "AdvertisingID":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo"))
                    {
                        var value = key?.GetValue("Enabled");
                        return value is int v && v == 0; // Advertising ID disabled
                    }
                case "BluetoothAdvertising":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\PolicyManager\current\device\Bluetooth"))
                    {
                        var value = key?.GetValue("AllowAdvertising");
                        return value is int v && v == 0; // Bluetooth Advertising disabled
                    }
                case "NewsAndInterests":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Feeds"))
                    {
                        var value = key?.GetValue("EnableFeeds");
                        return value is int v && v == 0; // News and Interests disabled
                    }
                case "SpotlightFeatures":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"))
                    {
                        var value = key?.GetValue("DisableWindowsSpotlightFeatures");
                        return value is int v && v == 1; // Spotlight Features disabled
                    }
                case "TailoredExperiences":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"))
                    {
                        var value = key?.GetValue("DisableTailoredExperiencesWithDiagnosticData");
                        return value is int v && v == 1; // Tailored Experiences disabled
                    }
                case "CloudOptimizedContent":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\CloudContent"))
                    {
                        var value = key?.GetValue("DisableCloudOptimizedContent");
                        return value is int v && v == 1; // Cloud Optimized Content disabled
                    }
                case "FeedbackNotifications":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection"))
                    {
                        var value = key?.GetValue("DoNotShowFeedbackNotifications");
                        return value is int v && v == 1; // Feedback Notifications disabled
                    }

                // Privacy Page - Telemetry
                case "TelemetryServices":
                    // Use RyTuneX registry key to determine state
                    using (var ryTuneXKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                        Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                            ? RegistryView.Registry64
                            : RegistryView.Default).OpenSubKey(RegistryBaseKey))
                    {
                        var savedState = ryTuneXKey?.GetValue("TelemetryServices");
                        return savedState is int v && v == 1; // Telemetry Services disabled
                    }
                case "EdgeTelemetry":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Edge"))
                    {
                        var value = key?.GetValue("MetricsReportingEnabled");
                        return value is int v && v == 0; // Edge Telemetry disabled
                    }
                case "VisualStudioTelemetry":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\VisualStudio\Telemetry"))
                    {
                        var value = key?.GetValue("TurnOffSwitch");
                        return value is int v && v == 1; // Visual Studio Telemetry disabled
                    }
                case "NvidiaTelemetry":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\NvTelemetryContainer"))
                    {
                        var value = key?.GetValue("Start");
                        return value is int v && v == 4; // Nvidia Telemetry disabled
                    }
                case "ChromeTelemetry":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Google\Chrome"))
                    {
                        var value = key?.GetValue("MetricsReportingEnabled");
                        return value is int v && v == 0; // Chrome Telemetry disabled
                    }
                case "FirefoxTelemetry":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Mozilla\Firefox"))
                    {
                        var value = key?.GetValue("DisableTelemetry");
                        return value is int v && v == 1; // Firefox Telemetry disabled
                    }
                case "ActivityFeed":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System"))
                    {
                        var value = key?.GetValue("EnableActivityFeed");
                        return value is int v && v == 0; // Activity Feed disabled
                    }
                case "Cdp":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System"))
                    {
                        var value = key?.GetValue("EnableCdp");
                        return value is int v && v == 0; // CDP disabled
                    }
                case "DiagnosticsToast":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Diagnostics\DiagTrack"))
                    {
                        var value = key?.GetValue("ShowedToastAtLevel");
                        return value is int v && v == 1; // Diagnostics Toast disabled
                    }
                case "OnlineSpeechPrivacy":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Speech_OneCore\Settings\OnlineSpeechPrivacy"))
                    {
                        var value = key?.GetValue("HasAccepted");
                        return value is int v && v == 0; // Online Speech Privacy disabled
                    }
                case "LocationFeatures":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors"))
                    {
                        var value = key?.GetValue("DisableLocation");
                        return value is int v && v == 1; // Location Features disabled
                    }
                case "Biometrics":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Biometrics"))
                    {
                        var value = key?.GetValue("Enabled");
                        return value is int v && v == 0; // Biometrics disabled
                    }

                // Privacy Page - Other Settings
                case "AutomaticRestartSignOn":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                    {
                        var value = key?.GetValue("DisableAutomaticRestartSignOn");
                        return value is int v && v == 1; // Automatic Restart Sign-On disabled
                    }
                case "HandwritingDataSharing":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\TabletPC"))
                    {
                        var value = key?.GetValue("PreventHandwritingDataSharing");
                        return value is int v && v == 1; // Handwriting Data Sharing disabled
                    }
                case "TextInputDataCollection":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\TextInput"))
                    {
                        var value = key?.GetValue("AllowLinguisticDataCollection");
                        return value is int v && v == 0; // Text Input Data Collection disabled
                    }
                case "InputPersonalization":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\InputPersonalization"))
                    {
                        var value = key?.GetValue("AllowInputPersonalization");
                        return value is int v && v == 0; // Input Personalization disabled
                    }
                case "SafeSearchMode":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\SearchSettings"))
                    {
                        var value = key?.GetValue("SafeSearchMode");
                        return value is int v && v == 0; // Safe Search Mode disabled
                    }
                case "ActivityUploads":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System"))
                    {
                        var value = key?.GetValue("UploadUserActivities");
                        return value is int v && v == 0; // Activity Uploads disabled
                    }
                case "ClipboardSync":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System"))
                    {
                        var value = key?.GetValue("AllowCrossDeviceClipboard");
                        return value is int v && v == 0; // Clipboard Sync disabled
                    }
                case "MessageSync":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Policies\Microsoft\Windows\Messaging"))
                    {
                        var value = key?.GetValue("AllowMessageSync");
                        return value is int v && v == 0; // Message Sync disabled
                    }
                case "SettingSync":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Policies\Microsoft\Windows\SettingSync"))
                    {
                        var value = key?.GetValue("DisableCredentialsSettingSync");
                        return value is int v && v == 2; // Setting Sync disabled
                    }
                case "VoiceActivation":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy"))
                    {
                        var value = key?.GetValue("LetAppsActivateWithVoice");
                        return value is int v && v == 2; // Voice Activation disabled
                    }
                case "FindMyDevice":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\FindMyDevice"))
                    {
                        var value = key?.GetValue("AllowFindMyDevice");
                        return value is int v && v == 0; // Find My Device disabled
                    }
                case "SMBv1":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters"))
                    {
                        var value = key?.GetValue("SMB1");
                        return value is int v && v == 0; // SMBv1 disabled
                    }
                case "SMBv2":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters"))
                    {
                        var value = key?.GetValue("SMB2");
                        return value is int v && v == 0; // SMBv2 disabled
                    }

                // Optimize System Page - Basic Optimization
                case "MenuShowDelay":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop"))
                    {
                        var value = key?.GetValue("MenuShowDelay");
                        return value?.ToString() == "0"; // Menu Show Delay optimized
                    }
                case "MouseHoverTime":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse"))
                    {
                        var value = key?.GetValue("MouseHoverTime");
                        return value?.ToString() == "0"; // Mouse Hover Time optimized
                    }
                case "BackgroundApps":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications"))
                    {
                        var value = key?.GetValue("GlobalUserDisabled");
                        return value is int v && v == 1; // Background Apps disabled
                    }
                case "AutoComplete":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoComplete"))
                    {
                        var value = key?.GetValue("Append Completion");
                        return value?.ToString() == "yes"; // AutoComplete enabled
                    }
                case "CrashDump":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\CrashControl"))
                    {
                        var value = key?.GetValue("CrashDumpEnabled");
                        return value is int v && v == 3; // Crash Dump enabled
                    }
                case "RemoteAssistance":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Remote Assistance"))
                    {
                        var value = key?.GetValue("fAllowToGetHelp");
                        return value is int v && v == 0; // Remote Assistance disabled
                    }
                case "WindowShake":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                    {
                        var value = key?.GetValue("DisallowShaking");
                        return value is int v && v == 1; // Window Shake disabled
                    }
                case "CopyMoveContextMenu":
                    using (var key = Registry.ClassesRoot.OpenSubKey(@"AllFilesystemObjects\shellex\ContextMenuHandlers\Copy To"))
                    {
                        return key != null; // Copy/Move Context Menu enabled
                    }
                case "TaskTimeouts":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop"))
                    {
                        var value = key?.GetValue("AutoEndTasks");
                        return value?.ToString() == "1"; // Task Timeouts optimized
                    }
                case "LowDiskSpaceChecks":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"))
                    {
                        var value = key?.GetValue("NoLowDiskSpaceChecks");
                        return value is int v && v == 1; // Low Disk Space Checks disabled
                    }
                case "LinkResolve":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"))
                    {
                        var value = key?.GetValue("LinkResolveIgnoreLinkInfo");
                        return value is int v && v == 1; // Link Resolve disabled
                    }
                case "ServiceTimeouts":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control"))
                    {
                        var value = key?.GetValue("WaitToKillServiceTimeout");
                        return value?.ToString() == "2000"; // Service Timeouts optimized
                    }
                case "RemoteRegistry":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\RemoteRegistry"))
                    {
                        var value = key?.GetValue("Start");
                        return value is int v && v == 4; // Remote Registry disabled
                    }
                case "FileExtensionsAndHiddenFiles":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                    {
                        var value = key?.GetValue("HideFileExt");
                        return value is int v && v == 0; // File Extensions shown
                    }

                // Optimize System Page - Advanced Optimization
                case "SystemProfile":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"))
                    {
                        var value = key?.GetValue("SystemResponsiveness");
                        return value is int v && v == 1; // System Profile optimized
                    }
                case "GPUAndPrioritySettings":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"))
                    {
                        var value = key?.GetValue("GPU Priority");
                        return value is int v && v == 8; // GPU and Priority Settings optimized
                    }
                case "FrameServerMode":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows Media Foundation"))
                    {
                        var value = key?.GetValue("EnableFrameServerMode");
                        return value is int v && v == 0; // Frame Server Mode disabled
                    }
                case "LowLatencyGPUSettings":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Low Latency"))
                    {
                        var value = key?.GetValue("Priority");
                        return value is int v && v == 8; // Low Latency GPU Settings optimized
                    }
                case "NonBestEffortLimit":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Psched"))
                    {
                        var value = key?.GetValue("NonBestEffortLimit");
                        return value is int v && v == 0; // Non-Best Effort Limit optimized
                    }
                case "SysMain":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\SysMain"))
                    {
                        var value = key?.GetValue("Start");
                        return value is int v && v == 4; // SysMain disabled
                    }
                case "NTFSTimeStamp":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\FileSystem"))
                    {
                        var value = key?.GetValue("NtfsDisableLastAccessUpdate");
                        return value is int v && v == 1; // NTFS Time Stamp disabled
                    }
                case "GamingMode":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\GameBar"))
                    {
                        var value = key?.GetValue("AllowAutoGameMode");
                        // If the value is missing, treat it as enabled
                        return value == null || (value is int v && v == 1); // Gaming Mode enabled
                    }
                case "Drivers":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate"))
                    {
                        var value = key?.GetValue("ExcludeWUDriversInQualityUpdate");
                        return value is int v && v == 1; // Drivers excluded from Windows Update
                    }
                case "ServiceHostSplitting":
                    // Use RyTuneX registry key to determine state
                    using (var ryTuneXKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                        Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                            ? RegistryView.Registry64
                            : RegistryView.Default).OpenSubKey(RegistryBaseKey))
                    {
                        var savedState = ryTuneXKey?.GetValue("ServiceHostSplitting");
                        return savedState is int v && v == 1; // Service Host Splitting disabled
                    }
                case "LegacyBootMenu":
                    // Use RyTuneX registry key to determine state
                    using (var ryTuneXKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                        Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                            ? RegistryView.Registry64
                            : RegistryView.Default).OpenSubKey(RegistryBaseKey))
                    {
                        var savedState = ryTuneXKey?.GetValue("LegacyBootMenu");
                        return savedState is int v && v == 1; // Legacy Boot Menu enabled
                    }
                case "OptimizeNTFS":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\FileSystem"))
                    {
                        var value = key?.GetValue("NtfsDisableLastAccessUpdate");
                        return value is int v && v == 1; // NTFS optimized
                    }
                case "PagingSettings":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"))
                    {
                        var value = key?.GetValue("DisablePagingExecutive");
                        return value is int v && v == 1; // Paging Settings optimized
                    }
                case "PrioritizeForegroundApplications":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\PriorityControl"))
                    {
                        var value = key?.GetValue("Win32PrioritySeparation");
                        return value is int v && v == 38; // Foreground Applications prioritized
                    }
                case "WPBT":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager"))
                    {
                        var value = key?.GetValue("DisableWpbtExecution");
                        return value is int v && v == 1; // WPBT execution disabled
                    }

                // Optimize System Page - Other Settings
                case "SystemRestore":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows NT\SystemRestore"))
                    {
                        var value = key?.GetValue("DisableSR");
                        return value is int v && v == 1; // System Restore disabled
                    }
                case "Cortana":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search"))
                    {
                        var value = key?.GetValue("AllowCortana");
                        return value is int v && v == 0; // Cortana disabled
                    }
                case "StoreUpdates":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\CloudContent"))
                    {
                        var value = key?.GetValue("DisableSoftLanding");
                        return value is int v && v == 1; // Store Updates disabled
                    }
                case "AutomaticUpdates":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU"))
                    {
                        var value = key?.GetValue("NoAutoUpdate");
                        return value is int v && v == 1; // Automatic Updates disabled
                    }
                case "SmartScreen":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer"))
                    {
                        var value = key?.GetValue("SmartScreenEnabled");
                        return value?.ToString() == "Off"; // SmartScreen disabled
                    }

                default:
                    return false;
            }
        }
        catch
        {
            return false;
        }
    }
}
