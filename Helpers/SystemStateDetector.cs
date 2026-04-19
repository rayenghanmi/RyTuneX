using Microsoft.Win32;

namespace RyTuneX.Helpers;

// Detects the actual system state for each toggle switch
internal static class SystemStateDetector
{
    private static readonly RegistryView RegView =
        Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
            ? RegistryView.Registry64
            : RegistryView.Default;

    // Returns true if the optimization is active, false if not, null if unknown.
    public static bool? DetectState(string tag)
    {
        return tag switch
        {
            // Optimize System Page

            "RecommendedSectionStartMenu" => All(
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\Explorer", "HideRecommendedSection", 1),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\PolicyManager\current\device\Start", "HideRecommendedSection", 1),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\PolicyManager\current\device\Education", "IsEducationEnvironment", 1)),

            "LegacyBootMenu" => null, // fall back to stored state

            "OptimizeNTFS" => DwordEquals(RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\FileSystem", "NtfsMftZoneReservation", 2),

            "PrioritizeForegroundApplications" => DwordEquals(RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 42),

            "WPBT" => DwordEquals(RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\Session Manager", "DisableWpbtExecution", 1),

            "ServiceHostSplitting" => DwordEquals(RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control", "SvcHostSplitThresholdInKB", unchecked((int)0xFFFFFFFF)),

            "MenuShowDelay" => StringEquals(RegistryHive.CurrentUser,
                @"Control Panel\Desktop", "MenuShowDelay", "0"),

            "MouseHoverTime" => StringEquals(RegistryHive.CurrentUser,
                @"Control Panel\Mouse", "MouseHoverTime", "0"),

            "BackgroundApps" => All(
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", 1),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsRunInBackground", 0),
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Search", "BackgroundAppGlobalToggle", 0)),

            "AutoComplete" => Not(StringEquals(RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoComplete", "AutoSuggest", "yes")),

            "CrashDump" => DwordEquals(RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\CrashControl", "CrashDumpEnabled", 3),

            "RemoteAssistance" => DwordEquals(RegistryHive.LocalMachine,
                @"System\CurrentControlSet\Control\Remote Assistance", "fAllowToGetHelp", 0),

            "WindowShake" => DwordEquals(RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "DisallowShaking", 1),

            "CopyMoveContextMenu" => All(
                ValueExists(RegistryHive.LocalMachine,
                    @"SOFTWARE\Classes\AllFilesystemObjects\shellex\ContextMenuHandlers\Copy To"),
                ValueExists(RegistryHive.LocalMachine,
                    @"SOFTWARE\Classes\AllFilesystemObjects\shellex\ContextMenuHandlers\Move To")),

            "TaskTimeouts" => All(
                StringEquals(RegistryHive.CurrentUser,
                    @"Control Panel\Desktop", "AutoEndTasks", "1"),
                StringEquals(RegistryHive.CurrentUser,
                    @"Control Panel\Desktop", "HungAppTimeout", "1000"),
                StringEquals(RegistryHive.CurrentUser,
                    @"Control Panel\Desktop", "WaitToKillAppTimeout", "2000"),
                StringEquals(RegistryHive.CurrentUser,
                    @"Control Panel\Desktop", "LowLevelHooksTimeout", "1000")),

            "LowDiskSpaceChecks" => DwordEquals(RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoLowDiskSpaceChecks", 0),

            "LinkResolve" => All(
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "LinkResolveIgnoreLinkInfo", 1),
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoResolveSearch", 1),
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoResolveTrack", 1),
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoInternetOpenWith", 1)),

            "ServiceTimeouts" => StringEquals(RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control", "WaitToKillServiceTimeout", "2000"),

            "RemoteRegistry" => ServiceDisabled("RemoteRegistry"),

            "FileExtensionsAndHiddenFiles" => All(
                DwordEquals(RegistryHive.CurrentUser,
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", 1),
                DwordEquals(RegistryHive.CurrentUser,
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Hidden", 1)),

            "SystemProfile" => All(
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 10),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NoLazyMode", 1),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "AlwaysOn", 1),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "GPU Priority", 8)),

            "TelemetryServices" => All(
                ServiceDisabled("DiagTrack"),
                ServiceDisabled("dmwappushservice"),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection", "AllowTelemetry", 0)),

            "HomeGroup" => All(
                ServiceDisabled("HomeGroupListener"),
                ServiceDisabled("HomeGroupProvider")),

            "PrintService" => ServiceDisabled("Spooler"),

            "SysMain" => All(
                ServiceDisabled("SysMain"),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnableSuperfetch", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnablePrefetcher", 0)),

            "CompatibilityAssistant" => All(
                ServiceDisabled("PcaSvc"),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\AppCompat", "DisableUAR", 1),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\AppCompat", "AITEnable", 0)),

            "SystemRestore" => All(
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows NT\SystemRestore", "DisableSR", 1),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows NT\SystemRestore", "DisableConfig", 1)),

            "WindowsTransparency" => DwordEquals(RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 0),

            "WindowsDarkMode" => All(
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 0),
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme", 0)),

            "VerboseLogon" => DwordEquals(RegistryHive.LocalMachine,
                @"Software\Microsoft\Windows\CurrentVersion\Policies\System", "VerboseStatus", 1),

            "ClassicContextMenu" => KeyExists(RegistryHive.CurrentUser,
                @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32"),

            "Search" => ServiceDisabled("WSearch"),

            "Biometrics" => DwordEquals(RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Biometrics", "Enabled", 0),

            "SMBv1" => DwordEquals(RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters", "SMB1", 0),

            "SMBv2" => DwordEquals(RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters", "SMB2", 0),

            "ErrorReporting" => All(
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows\Windows Error Reporting", "Disabled", 1),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows\Windows Error Reporting", "AutoApproveOSDumps", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows\Windows Error Reporting", "DontShowUI", 1),
                ServiceDisabled("WerSvc")),

            "Cortana" => All(
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "DisableWebSearch", 1),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "ConnectedSearchUseWeb", 0),
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 0)),

            "GamingMode" => All(
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\GameBar", "AutoGameModeEnabled", 1),
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\GameBar", "AllowAutoGameMode", 1),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2)),

            "StoreUpdates" => All(
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SilentInstalledAppsEnabled", 0),
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "PreInstalledAppsEnabled", 0),
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "OemPreInstalledAppsEnabled", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures", 1)),

            "OneDrive" => DwordEquals(RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\OneDrive", "DisableFileSyncNGSC", 1),

            "SensorServices" => All(
                ServiceDisabled("SensrSvc"),
                ServiceDisabled("SensorService")),

            "NewsAndInterests" => All(
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\Windows Feeds", "EnableFeeds", 0)),

            "Hibernation" => DwordEquals(RegistryHive.LocalMachine,
                @"System\CurrentControlSet\Control\Power", "PlatformAoAcOverride", 0),

            "EndTask" => DwordEquals(RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings", "TaskbarEndTask", 1),

            "MediaPlayerSharing" => ServiceDisabled("WMPNetworkSvc"),

            // Privacy Page

            "SpotlightFeatures" => All(
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "DisableWindowsSpotlightFeatures", 1),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "ConfigureWindowsSpotlight", 2),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableThirdPartySuggestions", 1)),

            "TailoredExperiences" => All(
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Privacy", "TailoredExperiencesWithDiagnosticDataEnabled", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableTailoredExperiencesWithDiagnosticData", 1)),

            "CloudOptimizedContent" => DwordEquals(RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableCloudOptimizedContent", 1),

            "FeedbackNotifications" => DwordEquals(RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "DoNotShowFeedbackNotifications", 1),

            "AdvertisingID" => All(
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo", "DisabledByGroupPolicy", 1)),

            "BluetoothAdvertising" => DwordEquals(RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\PolicyManager\current\device\Bluetooth", "AllowAdvertising", 0),

            "AutomaticRestartSignOn" => DwordEquals(RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "DisableAutomaticRestartSignOn", 1),

            "HandwritingDataSharing" => DwordEquals(RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\TabletPc", "PreventHandwritingDataSharing", 1),

            "TextInputDataCollection" => DwordEquals(RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\TextInput", "AllowLinguisticDataCollection", 0),

            "InputPersonalization" => DwordEquals(RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\InputPersonalization", "AllowInputPersonalization", 0),

            "SafeSearchMode" => DwordEquals(RegistryHive.CurrentUser,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\SearchSettings", "SafeSearchMode", 0),

            "ActivityUploads" => DwordEquals(RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\System", "UploadUserActivities", 0),

            "ClipboardSync" => DwordEquals(RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\System", "AllowCrossDeviceClipboard", 0),

            "MessageSync" => DwordEquals(RegistryHive.LocalMachine,
                @"Software\Policies\Microsoft\Windows\Messaging", "AllowMessageSync", 0),

            "SettingSync" => All(
                DwordEquals(RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\SettingSync", "DisableCredentialsSettingSync", 2),
                DwordEquals(RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\SettingSync", "DisableCredentialsSettingSyncUserOverride", 2),
                DwordEquals(RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\SettingSync", "DisableApplicationSettingSync", 2)),

            "VoiceActivation" => DwordEquals(RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsActivateWithVoice", 2),

            "FindMyDevice" => All(
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\FindMyDevice", "AllowFindMyDevice", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\FindMyDevice", "LocationSyncEnabled", 0)),

            "ActivityFeed" => DwordEquals(RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed", 0),

            "Cdp" => DwordEquals(RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\System", "EnableCdp", 0),

            "DiagnosticsToast" => DwordEquals(RegistryHive.CurrentUser,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Diagnostics\DiagTrack", "ShowedToastAtLevel", 1),

            "OnlineSpeechPrivacy" => DwordEquals(RegistryHive.CurrentUser,
                @"Software\Microsoft\Speech_OneCore\Settings\OnlineSpeechPrivacy", "HasAccepted", 0),

            "LocationAccess" or "LocationFeatures" => All(
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocation", 1),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableWindowsLocationProvider", 1)),

            // Features Page

            "GameBar" => All(
                DwordEquals(RegistryHive.CurrentUser,
                    @"System\GameConfigStore", "GameDVR_Enabled", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", 0),
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0),
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\GameBar", "UseNexusForGameBarEnabled", 0)),

            "QuickAccessHistory" => All(
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer", "ShowRecent", 0),
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer", "ShowFrequent", 0),
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowTaskViewButton", 0),
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo", 1)),

            "StartMenuAds" => All(
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SystemPaneSuggestionsEnabled", 0),
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "ContentDeliveryAllowed", 0),
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338389Enabled", 0)),

            "MyPeople" => DwordEquals(RegistryHive.CurrentUser,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced\People", "PeopleBand", 0),

            "Drivers" => All(
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "ExcludeWUDriversInQualityUpdate", 1),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\Device Metadata", "PreventDeviceMetadataFromNetwork", 1),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching", "SearchOrderConfig", 0)),

            "WindowsInk" => All(
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\WindowsInkWorkspace", "AllowWindowsInkWorkspace", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\WindowsInkWorkspace", "AllowSuggestedAppsInWindowsInkWorkspace", 0)),

            "SpellingAndTypingFeatures" => All(
                DwordEquals(RegistryHive.CurrentUser,
                    @"SOFTWARE\Microsoft\TabletTip\1.7", "EnableAutocorrection", 0),
                DwordEquals(RegistryHive.CurrentUser,
                    @"SOFTWARE\Microsoft\TabletTip\1.7", "EnableSpellchecking", 0),
                DwordEquals(RegistryHive.CurrentUser,
                    @"SOFTWARE\Microsoft\TabletTip\1.7", "EnableTextPrediction", 0)),

            "FaxService" => ServiceDisabled("Fax"),

            "InsiderService" => All(
                ServiceDisabled("wisvc"),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\PreviewBuilds", "AllowBuildPreview", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\PreviewBuilds", "EnableConfigFlighting", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\PreviewBuilds", "EnableExperimentation", 0)),

            "SmartScreen" => All(
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\System", "EnableSmartScreen", 0),
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Edge", "SmartScreenEnabled", 0),
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\Attachments", "SaveZoneInformation", 1)),

            "CloudClipboard" => All(
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\System", "AllowClipboardHistory", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\System", "AllowCrossDeviceClipboard", 0),
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Clipboard", "EnableClipboardHistory", 0)),

            "StickyKeys" => All(
                StringEquals(RegistryHive.CurrentUser,
                    @"Control Panel\Accessibility\StickyKeys", "Flags", "506"),
                StringEquals(RegistryHive.CurrentUser,
                    @"Control Panel\Accessibility\Keyboard Response", "Flags", "122"),
                StringEquals(RegistryHive.CurrentUser,
                    @"Control Panel\Accessibility\ToggleKeys", "Flags", "58")),

            "CastToDevice" => ValueExists(RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked",
                "{7AD84985-87B4-4a16-BE58-8B72A5B390F7}"),

            "VBS" => All(
                DwordEquals(RegistryHive.LocalMachine,
                    @"SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\CredentialGuard", "Enabled", 0)),

            "TaskbarToLeft" => DwordEquals(RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAl", 0),

            "SnapAssist" => All(
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "EnableSnapAssistFlyout", 0),
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "EnableSnapBar", 0),
                StringEquals(RegistryHive.CurrentUser,
                    @"Control Panel\Desktop", "DockMoving", "0")),

            "Widgets" => All(
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarDa", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\Windows Feeds", "EnableFeeds", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests", 0)),

            "Chat" => DwordEquals(RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarMn", 0),

            "FilesCompactMode" => DwordEquals(RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "UseCompactMode", 1),

            "Stickers" => Not(DwordEquals(RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\PolicyManager\current\device\Stickers", "EnableStickers", 1)),

            "EdgeDiscoverBar" => All(
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Edge", "HubsSidebarEnabled", 0),
                DwordEquals(RegistryHive.CurrentUser,
                    @"SOFTWARE\Policies\Microsoft\Edge", "HubsSidebarEnabled", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Edge", "WebWidgetAllowed", 0)),

            "EdgeTelemetry" => All(
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Edge", "MetricsReportingEnabled", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Edge", "PersonalizationReportingEnabled", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Edge", "UserFeedbackAllowed", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Edge", "StartupBoostEnabled", 0),
                ServiceDisabled("edgeupdate")),

            "CoPilotAI" => All(
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1),
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1),
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowCopilotButton", 0)),

            "WindowsRecall" => All(
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "DisableAIDataAnalysis", 1),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "AllowRecallEnablement", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "TurnOffSavingSnapshots", 1)),

            "VisualStudioTelemetry" => All(
                DwordEquals(RegistryHive.CurrentUser,
                    @"Software\Microsoft\VisualStudio\Telemetry", "TurnOffSwitch", 1),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\VisualStudio\Feedback", "DisableFeedbackDialog", 1),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\VisualStudio\SQM", "OptIn", 0),
                ServiceDisabled("VSStandardCollectorService150")),

            "NvidiaTelemetry" => ServiceDisabled("NvTelemetryContainer"),

            "ChromeTelemetry" => All(
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Google\Chrome", "MetricsReportingEnabled", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Google\Chrome", "ChromeCleanupReportingEnabled", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Google\Chrome", "ChromeCleanupEnabled", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Google\Chrome", "UserFeedbackAllowed", 0),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Google\Chrome", "DeviceMetricsReportingEnabled", 0)),

            "FirefoxTelemetry" => All(
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Mozilla\Firefox", "DisableTelemetry", 1),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Mozilla\Firefox", "DisableDefaultBrowserAgent", 1)),

            "WindowsAI" => All(
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "DisableAgentConnectors", 1),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "DisableAIDataAnalysis", 1),
                DwordEquals(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "AllowRecallEnablement", 0)),

            _ => null
        };
    }

    // Bulk-detect states for all known tags.
    public static Dictionary<string, bool> DetectAll(IEnumerable<string> tags)
    {
        var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in tags)
        {
            try
            {
                var state = DetectState(tag);
                if (state.HasValue)
                {
                    result[tag] = state.Value;
                }
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"SystemStateDetector.DetectState({tag}) failed: {ex.Message}");
            }
        }
        return result;
    }

    // Detects system state for all known toggles and writes results to the RyTuneX registry key
    public static void SyncToRegistry()
    {
        try
        {
            var detected = DetectAll(AllTags);
            if (detected.Count == 0)
            {
                return;
            }

            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegView);
            using var key = baseKey.CreateSubKey(@"SOFTWARE\RyTuneX\Optimizations");
            if (key == null)
            {
                return;
            }

            foreach (var (tag, isOn) in detected)
            {
                key.SetValue(tag, isOn ? 1 : 0, RegistryValueKind.DWord);
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"SystemStateDetector.SyncToRegistry failed: {ex.Message}");
        }
    }

    private static readonly string[] AllTags =
    [
        // Optimize System Page
        "RecommendedSectionStartMenu", "LegacyBootMenu", "OptimizeNTFS",
        "PrioritizeForegroundApplications", "WPBT", "ServiceHostSplitting",
        "MenuShowDelay", "MouseHoverTime", "BackgroundApps", "AutoComplete",
        "CrashDump", "RemoteAssistance", "WindowShake", "CopyMoveContextMenu",
        "TaskTimeouts", "LowDiskSpaceChecks", "LinkResolve", "ServiceTimeouts",
        "RemoteRegistry", "FileExtensionsAndHiddenFiles", "SystemProfile",
        "TelemetryServices", "HomeGroup", "PrintService", "SysMain",
        "CompatibilityAssistant", "SystemRestore", "WindowsTransparency",
        "WindowsDarkMode", "VerboseLogon", "ClassicContextMenu", "Search",
        "Biometrics", "SMBv1", "SMBv2", "ErrorReporting", "Cortana",
        "GamingMode", "StoreUpdates", "OneDrive", "SensorServices",
        "NewsAndInterests", "Hibernation", "EndTask", "MediaPlayerSharing",

        // Privacy Page
        "SpotlightFeatures", "TailoredExperiences", "CloudOptimizedContent",
        "FeedbackNotifications", "AdvertisingID", "BluetoothAdvertising",
        "AutomaticRestartSignOn", "HandwritingDataSharing", "TextInputDataCollection",
        "InputPersonalization", "SafeSearchMode", "ActivityUploads", "ClipboardSync",
        "MessageSync", "SettingSync", "VoiceActivation", "FindMyDevice",
        "ActivityFeed", "Cdp", "DiagnosticsToast", "OnlineSpeechPrivacy",
        "LocationAccess", "LocationFeatures",

        // Features Page
        "GameBar", "QuickAccessHistory", "StartMenuAds", "MyPeople",
        "Drivers", "WindowsInk", "SpellingAndTypingFeatures", "FaxService",
        "InsiderService", "SmartScreen", "CloudClipboard", "StickyKeys",
        "CastToDevice", "VBS", "TaskbarToLeft", "SnapAssist", "Widgets",
        "Chat", "FilesCompactMode", "Stickers", "EdgeDiscoverBar",
        "EdgeTelemetry", "CoPilotAI", "WindowsRecall", "VisualStudioTelemetry",
        "NvidiaTelemetry", "ChromeTelemetry", "FirefoxTelemetry", "WindowsAI"
    ];

    // Registry helpers
    private static bool? DwordEquals(RegistryHive hive, string keyPath, string valueName, int expected)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegView);
            using var key = baseKey.OpenSubKey(keyPath, writable: false);
            if (key == null) return null;

            var val = key.GetValue(valueName);
            if (val is int intVal) return intVal == expected;
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static bool? StringEquals(RegistryHive hive, string keyPath, string valueName, string expected)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegView);
            using var key = baseKey.OpenSubKey(keyPath, writable: false);
            if (key == null) return null;

            var val = key.GetValue(valueName);
            if (val is string strVal) return strVal.Equals(expected, StringComparison.OrdinalIgnoreCase);
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static bool? KeyExists(RegistryHive hive, string keyPath)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegView);
            using var key = baseKey.OpenSubKey(keyPath, writable: false);
            return key != null;
        }
        catch
        {
            return null;
        }
    }

    private static bool? ValueExists(RegistryHive hive, string keyPath, string? valueName = null)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegView);
            using var key = baseKey.OpenSubKey(keyPath, writable: false);
            if (key == null) return false;
            if (valueName == null) return true;
            return key.GetValue(valueName) != null;
        }
        catch
        {
            return null;
        }
    }

    private static bool? ServiceDisabled(string serviceName)
    {
        return DwordEquals(RegistryHive.LocalMachine,
            $@"SYSTEM\CurrentControlSet\Services\{serviceName}", "Start", 4);
    }

    private static bool? Not(bool? value) => value.HasValue ? !value.Value : null;

    // Returns true only when all non-null checks are true.
    private static bool? All(params bool?[] checks)
    {
        var seenTrue = false;
        foreach (var c in checks)
        {
            if (c == false) return false;
            if (c == true) seenTrue = true;
        }
        return seenTrue ? true : null;
    }
}
