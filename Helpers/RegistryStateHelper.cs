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
                case "WindowsRecall":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsAI"))
                    {
                        var value = key?.GetValue("DisableAIDataAnalysis");
                        return value is int v && v == 1;
                    }
                case "RecommendedSectionStartMenu":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\PolicyManager\current\device\Start"))
                    {
                        var value = key?.GetValue("HideRecommendedSection");
                        return value is int v && v == 1;
                    }
                case "WPBT":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager"))
                    {
                        var value = key?.GetValue("DisableWpbtExecution");
                        return value is int v && v == 1;
                    }
                case "PrioritizeForegroundApplications":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\PriorityControl"))
                    {
                        var value = key?.GetValue("Win32PrioritySeparation");
                        return value is int v && v == 38;
                    }
                case "PagingSettings":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"))
                    {
                        var pagingExecutive = key?.GetValue("DisablePagingExecutive");
                        var pageCombining = key?.GetValue("DisablePageCombining");
                        return (pagingExecutive is int v1 && v1 == 1) && (pageCombining is int v2 && v2 == 1);
                    }
                case "OptimizeNTFS":
                    try
                    {
                        using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\FileSystem");
                        var mftZone = key?.GetValue("NtfsMftZoneReservation");
                        return mftZone is int v && v == 2;
                    }
                    catch
                    {
                        return false;
                    }
                case "LegacyBootMenu":
                    using (var ryTuneXKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                        Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                            ? RegistryView.Registry64
                            : RegistryView.Default).OpenSubKey(RegistryBaseKey))
                    {
                        var savedState = ryTuneXKey?.GetValue("LegacyBootMenu");
                        return savedState is int v && v == 1;
                    }
                case "ServiceHostSplitting":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control"))
                    {
                        var value = key?.GetValue("SvcHostSplitThresholdInKB");
                        return value is long v && v == 4294967295;
                    }
                case "MenuShowDelay":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop"))
                    {
                        var value = key?.GetValue("MenuShowDelay");
                        return value?.ToString() == "0";
                    }
                case "MouseHoverTime":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse"))
                    {
                        var value = key?.GetValue("MouseHoverTime");
                        return value?.ToString() == "0";
                    }
                case "BackgroundApps":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications"))
                    {
                        var value = key?.GetValue("GlobalUserDisabled");
                        return value is int v && v == 1;
                    }
                case "AutoComplete":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoComplete"))
                    {
                        var appendCompletion = key?.GetValue("Append Completion");
                        return key == null || appendCompletion == null;
                    }
                case "CrashDump":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\CrashControl"))
                    {
                        var value = key?.GetValue("CrashDumpEnabled");
                        return value == null;
                    }
                case "RemoteAssistance":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Remote Assistance"))
                    {
                        var value = key?.GetValue("fAllowToGetHelp");
                        return value is int v && v == 0;
                    }
                case "WindowShake":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                    {
                        var value = key?.GetValue("DisallowShaking");
                        return value is int v && v == 1;
                    }
                case "CopyMoveContextMenu":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\AllFilesystemObjects\shellex\ContextMenuHandlers\Copy To"))
                    {
                        return key != null;
                    }
                case "TaskTimeouts":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop"))
                    {
                        var value = key?.GetValue("AutoEndTasks");
                        return value?.ToString() == "1";
                    }
                case "LowDiskSpaceChecks":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"))
                    {
                        var value = key?.GetValue("NoLowDiskSpaceChecks");
                        return value is int v && v == 0;
                    }
                case "LinkResolve":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"))
                    {
                        var value = key?.GetValue("LinkResolveIgnoreLinkInfo");
                        return value is int v && v == 1;
                    }
                case "ServiceTimeouts":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control"))
                    {
                        var value = key?.GetValue("WaitToKillServiceTimeout");
                        return value?.ToString() == "2000";
                    }
                case "RemoteRegistry":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\RemoteRegistry"))
                    {
                        var value = key?.GetValue("Start");
                        return value is int v && v == 4;
                    }
                case "FileExtensionsAndHiddenFiles":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                    {
                        var hideFileExt = key?.GetValue("HideFileExt");
                        var hidden = key?.GetValue("Hidden");
                        return (hideFileExt is int v1 && v1 == 1) && (hidden is int v2 && v2 == 1);
                    }
                case "SystemProfile":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"))
                    {
                        var value = key?.GetValue("SystemResponsiveness");
                        return value is int v && v == 1;
                    }
                case "TelemetryServices":
                    {
                        string[] services = {
                        "DiagTrack",
                        "diagnosticshub.standardcollector.service",
                        "dmwappushservice",
                        "DcpSvc",
                        "WdiServiceHost",
                        "WdiSystemHost",
                        "WerSvc",
                        "PcaSvc",
                        "RetailDemo"
                        };

                        foreach (var svc in services)
                        {
                            using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{svc}");
                            var value = key?.GetValue("Start");
                            if (!(value is int v && v == 4)) { return false; }
                        }
                        return true;
                    }
                case "HomeGroup":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\HomeGroup"))
                    {
                        var value = key?.GetValue("DisableHomeGroup");
                        return value is int v && v == 1;
                    }
                case "PrintService":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Spooler"))
                    {
                        var value = key?.GetValue("Start");
                        return value is int v && v == 4;
                    }
                case "SysMain":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\SysMain"))
                    {
                        var value = key?.GetValue("Start");
                        return value is int v && v == 4;
                    }
                case "CompatibilityAssistant":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\PcaSvc"))
                    {
                        var value = key?.GetValue("Start");
                        return value is int v && v == 4;
                    }
                case "Search":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\WSearch"))
                    {
                        var value = key?.GetValue("Start");
                        return value is int v && v == 4;
                    }
                case "ErrorReporting":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting"))
                    {
                        var value = key?.GetValue("Disabled");
                        return value is int v && v == 1;
                    }
                case "GameBar":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\GameDVR"))
                    {
                        var value = key?.GetValue("AppCaptureEnabled");
                        return value is int v && v == 0;
                    }
                case "QuickAccessHistory":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer"))
                    {
                        var showRecent = key?.GetValue("ShowRecent");
                        var showFrequent = key?.GetValue("ShowFrequent");
                        return (showRecent is int v1 && v1 == 0) && (showFrequent is int v2 && v2 == 0);
                    }
                case "MyPeople":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced\People"))
                    {
                        var value = key?.GetValue("PeopleBand");
                        return value is int v && v == 0;
                    }
                case "SensorServices":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\SensrSvc"))
                    {
                        var value = key?.GetValue("Start");
                        return value is int v && v == 4;
                    }
                case "WindowsInk":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\WindowsInkWorkspace"))
                    {
                        var value = key?.GetValue("AllowWindowsInkWorkspace");
                        return value is int v && v == 0;
                    }
                case "SpellingAndTypingFeatures":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\TabletTip\1.7"))
                    {
                        var autocorrection = key?.GetValue("EnableAutocorrection");
                        var spellcheck = key?.GetValue("EnableSpellchecking");
                        return (autocorrection is int v1 && v1 == 0) && (spellcheck is int v2 && v2 == 0);
                    }
                case "FaxService":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Fax"))
                    {
                        var value = key?.GetValue("Start");
                        return value is int v && v == 4;
                    }
                case "InsiderService":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\wisvc"))
                    {
                        var value = key?.GetValue("Start");
                        return value is int v && v == 4;
                    }
                case "CloudClipboard":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System"))
                    {
                        var clipboardHistory = key?.GetValue("AllowClipboardHistory");
                        var crossDeviceClipboard = key?.GetValue("AllowCrossDeviceClipboard");
                        return (clipboardHistory is int v1 && v1 == 0) && (crossDeviceClipboard is int v2 && v2 == 0);
                    }
                case "StickyKeys":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Accessibility\StickyKeys"))
                    {
                        var value = key?.GetValue("Flags");
                        return value?.ToString() == "506";
                    }
                case "CastToDevice":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked"))
                    {
                        var value = key?.GetValue("{7AD84985-87B4-4a16-BE58-8B72A5B390F7}");
                        return value != null;
                    }
                case "VBS":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard"))
                    {
                        var value = key?.GetValue("EnableVirtualizationBasedSecurity");
                        return value is int v && v == 0;
                    }
                case "EndTask":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings"))
                    {
                        var value = key?.GetValue("TaskbarEndTask");
                        return value is int v && v == 1;
                    }
                case "ClassicContextMenu":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32"))
                    {
                        return key != null;
                    }
                case "TaskbarToLeft":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                    {
                        var value = key?.GetValue("TaskbarAl");
                        return value is int v && v == 0;
                    }
                case "SnapAssist":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                    {
                        var snapAssistFlyout = key?.GetValue("EnableSnapAssistFlyout");
                        var snapBar = key?.GetValue("EnableSnapBar");
                        return (snapAssistFlyout is int v1 && v1 == 0) && (snapBar is int v2 && v2 == 0);
                    }
                case "Widgets":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                    {
                        var value = key?.GetValue("TaskbarDa");
                        return value is int v && v == 0;
                    }
                case "Chat":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                    {
                        var value = key?.GetValue("TaskbarMn");
                        return value is int v && v == 0;
                    }
                case "FilesCompactMode":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                    {
                        var value = key?.GetValue("UseCompactMode");
                        return value is int v && v == 1;
                    }
                case "Stickers":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\PolicyManager\current\device\Stickers"))
                    {
                        var value = key?.GetValue("EnableStickers");
                        return key == null || (value is int v && v != 1);
                    }
                case "EdgeDiscoverBar":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Edge"))
                    {
                        var value = key?.GetValue("HubsSidebarEnabled");
                        return value is int v && v == 0;
                    }
                case "CoPilotAI":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Policies\Microsoft\Windows\WindowsCopilot"))
                    {
                        var value = key?.GetValue("TurnOffWindowsCopilot");
                        return value is int v && v == 1;
                    }
                case "AdvertisingID":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo"))
                    {
                        var value = key?.GetValue("Enabled");
                        return value is int v && v == 0;
                    }
                case "BluetoothAdvertising":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\PolicyManager\current\device\Bluetooth"))
                    {
                        var value = key?.GetValue("AllowAdvertising");
                        return value is int v && v == 0;
                    }
                case "NewsAndInterests":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Feeds"))
                    {
                        var value = key?.GetValue("EnableFeeds");
                        return value is int v && v == 0;
                    }
                case "SpotlightFeatures":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"))
                    {
                        var value = key?.GetValue("DisableWindowsSpotlightFeatures");
                        return value is int v && v == 1;
                    }
                case "TailoredExperiences":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"))
                    {
                        var value = key?.GetValue("DisableTailoredExperiencesWithDiagnosticData");
                        return value is int v && v == 1;
                    }
                case "CloudOptimizedContent":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\CloudContent"))
                    {
                        var value = key?.GetValue("DisableCloudOptimizedContent");
                        return value is int v && v == 1;
                    }
                case "FeedbackNotifications":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection"))
                    {
                        var value = key?.GetValue("DoNotShowFeedbackNotifications");
                        return value is int v && v == 1;
                    }
                case "EdgeTelemetry":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Edge"))
                    {
                        var value = key?.GetValue("MetricsReportingEnabled");
                        return value is int v && v == 0;
                    }
                case "VisualStudioTelemetry":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\VisualStudio\Telemetry"))
                    {
                        var value = key?.GetValue("TurnOffSwitch");
                        return value is int v && v == 1;
                    }
                case "NvidiaTelemetry":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\NvTelemetryContainer"))
                    {
                        var value = key?.GetValue("Start");
                        return value is int v && v == 4;
                    }
                case "ChromeTelemetry":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Google\Chrome"))
                    {
                        var value = key?.GetValue("MetricsReportingEnabled");
                        return value is int v && v == 0;
                    }
                case "FirefoxTelemetry":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Mozilla\Firefox"))
                    {
                        var value = key?.GetValue("DisableTelemetry");
                        return value is int v && v == 1;
                    }
                case "ActivityFeed":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System"))
                    {
                        var value = key?.GetValue("EnableActivityFeed");
                        return value is int v && v == 0;
                    }
                case "Cdp":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System"))
                    {
                        var value = key?.GetValue("EnableCdp");
                        return value is int v && v == 0;
                    }
                case "DiagnosticsToast":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Diagnostics\DiagTrack"))
                    {
                        var value = key?.GetValue("ShowedToastAtLevel");
                        return value is int v && v == 1;
                    }
                case "OnlineSpeechPrivacy":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Speech_OneCore\Settings\OnlineSpeechPrivacy"))
                    {
                        var value = key?.GetValue("HasAccepted");
                        return value is int v && v == 0;
                    }
                case "LocationFeatures":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors"))
                    {
                        var value = key?.GetValue("DisableLocation");
                        return value is int v && v == 1;
                    }
                case "Biometrics":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Biometrics"))
                    {
                        var value = key?.GetValue("Enabled");
                        return value is int v && v == 0;
                    }
                case "AutomaticRestartSignOn":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                    {
                        var value = key?.GetValue("DisableAutomaticRestartSignOn");
                        return value is int v && v == 1;
                    }
                case "HandwritingDataSharing":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\TabletPC"))
                    {
                        var value = key?.GetValue("PreventHandwritingDataSharing");
                        return value is int v && v == 1;
                    }
                case "TextInputDataCollection":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\TextInput"))
                    {
                        var value = key?.GetValue("AllowLinguisticDataCollection");
                        return value is int v && v == 0;
                    }
                case "InputPersonalization":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\InputPersonalization"))
                    {
                        var value = key?.GetValue("AllowInputPersonalization");
                        return value is int v && v == 0;
                    }
                case "SafeSearchMode":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\SearchSettings"))
                    {
                        var value = key?.GetValue("SafeSearchMode");
                        return value is int v && v == 0;
                    }
                case "ActivityUploads":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System"))
                    {
                        var value = key?.GetValue("UploadUserActivities");
                        return value is int v && v == 0;
                    }
                case "ClipboardSync":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System"))
                    {
                        var value = key?.GetValue("AllowCrossDeviceClipboard");
                        return value is int v && v == 0;
                    }
                case "MessageSync":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Policies\Microsoft\Windows\Messaging"))
                    {
                        var value = key?.GetValue("AllowMessageSync");
                        return value is int v && v == 0;
                    }
                case "SettingSync":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Policies\Microsoft\Windows\SettingSync"))
                    {
                        var value = key?.GetValue("DisableCredentialsSettingSync");
                        return value is int v && v == 2;
                    }
                case "VoiceActivation":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy"))
                    {
                        var value = key?.GetValue("LetAppsActivateWithVoice");
                        return value is int v && v == 2;
                    }
                case "FindMyDevice":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\FindMyDevice"))
                    {
                        var value = key?.GetValue("AllowFindMyDevice");
                        return value is int v && v == 0;
                    }
                case "SMBv1":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters"))
                    {
                        var value = key?.GetValue("SMB1");
                        return value is int v && v == 0;
                    }
                case "SMBv2":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters"))
                    {
                        var value = key?.GetValue("SMB2");
                        return value is int v && v == 0;
                    }
                case "Drivers":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate"))
                    {
                        var value = key?.GetValue("ExcludeWUDriversInQualityUpdate");
                        return value is int v && v == 1;
                    }
                case "SystemRestore":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows NT\SystemRestore"))
                    {
                        var value = key?.GetValue("DisableSR");
                        return value is int v && v == 1;
                    }
                case "Cortana":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search"))
                    {
                        var value = key?.GetValue("AllowCortana");
                        return value is int v && v == 0;
                    }
                case "StoreUpdates":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\CloudContent"))
                    {
                        var value = key?.GetValue("DisableSoftLanding");
                        return value is int v && v == 1;
                    }
                case "AutomaticUpdates":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU"))
                    {
                        var value = key?.GetValue("NoAutoUpdate");
                        return value is int v && v == 1;
                    }
                case "SmartScreen":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer"))
                    {
                        var value = key?.GetValue("SmartScreenEnabled");
                        return value?.ToString() == "Off";
                    }
                case "WindowsTransparency":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                    {
                        var value = key?.GetValue("EnableTransparency");
                        return value is int v && v == 0;
                    }
                case "WindowsDarkMode":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                    {
                        var appsLightTheme = key?.GetValue("AppsUseLightTheme");
                        var systemLightTheme = key?.GetValue("SystemUsesLightTheme");
                        return (appsLightTheme is int v1 && v1 == 0) && (systemLightTheme is int v2 && v2 == 0);
                    }
                case "VerboseLogon":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                    {
                        var value = key?.GetValue("VerboseStatus");
                        return value is int v && v == 1;
                    }
                case "Hibernation":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power"))
                    {
                        var value = key?.GetValue("HibernateEnabled");
                        return value is int v && v == 0;
                    }
                case "OneDrive":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\OneDrive"))
                    {
                        var value = key?.GetValue("DisableFileSyncNGSC");
                        return value is int v && v == 1;
                    }
                case "GamingMode":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\GameBar"))
                    {
                        var value = key?.GetValue("AutoGameModeEnabled");
                        return (value is int v1 && v1 == 1 || value == null);
                    }
                case "StartMenuAds":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"))
                    {
                        var subscribedContent = key?.GetValue("SubscribedContent-88000326Enabled");
                        var contentDelivery = key?.GetValue("ContentDeliveryAllowed");
                        return (subscribedContent is int v1 && v1 == 0) && (contentDelivery is int v2 && v2 == 0);
                    }
                case "ShowMoreOptions":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32"))
                    {
                        return key != null;
                    }
                case "MediaPlayerSharing":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\WMPNetworkSvc"))
                    {
                        var value = key?.GetValue("Start");
                        return value is int v && v == 4;
                    }
                case "LegacyVolumeSlider":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\MTCUVC"))
                    {
                        var value = key?.GetValue("EnableMtcUvc");
                        return value is int v && v == 1;
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
