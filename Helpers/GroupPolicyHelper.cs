using Microsoft.Win32;

namespace RyTuneX.Helpers;

// Helper class for detecting and reverting Local Group Policy (gpedit.msc) changes.
// Only operates on known policy-backed registry paths under HKLM\Software\Policies and HKCU\Software\Policies.
public static class GroupPolicyHelper
{
    // Represents a known Group Policy entry with its registry location.
    public sealed record PolicyEntry
    {
        // Unique identifier for the policy.
        public required string Id
        {
            get; init;
        }

        // Human-readable name of the policy.
        public required string Name
        {
            get; init;
        }

        // Description of what the policy controls.
        public required string Description
        {
            get; init;
        }

        // Category for grouping policies in the UI.
        public required string Category
        {
            get; init;
        }

        // Registry hive (HKLM or HKCU).
        public required RegistryHive Hive
        {
            get; init;
        }

        // Registry path under Software\Policies.
        public required string RegistryPath
        {
            get; init;
        }

        // Registry value name.
        public required string ValueName
        {
            get; init;
        }

        // Expected registry value type.
        public required RegistryValueKind ValueKind
        {
            get; init;
        }

        // Minimum Windows build number where this policy applies (0 = all versions).
        public int MinWindowsBuild
        {
            get; init;
        }

        // Maximum Windows build number where this policy applies (0 = no limit).
        public int MaxWindowsBuild
        {
            get; init;
        }
    }

    // Represents the current state of a detected policy.
    public sealed record PolicyState
    {
        public required PolicyEntry Policy
        {
            get; init;
        }
        public required bool IsConfigured
        {
            get; init;
        }
        public object? CurrentValue
        {
            get; init;
        }
        public RegistryValueKind? ActualValueKind
        {
            get; init;
        }
    }

    // Known Group Policy entries that can be detected and reset.
    // These are all under Software\Policies paths which are policy-backed.
    private static readonly PolicyEntry[] KnownPolicies =
    [
        // Windows Update Policies
        new PolicyEntry
        {
            Id = "NoAutoUpdate",
            Name = "Disable Automatic Updates",
            Description = "Prevents Windows from automatically downloading and installing updates.",
            Category = "Windows Update",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU",
            ValueName = "NoAutoUpdate",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "AUOptions",
            Name = "Configure Automatic Updates",
            Description = "Configures how Windows handles automatic updates.",
            Category = "Windows Update",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU",
            ValueName = "AUOptions",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "NoAutoRebootWithLoggedOnUsers",
            Name = "No Auto-Restart With Logged On Users",
            Description = "Prevents automatic restart when users are logged on.",
            Category = "Windows Update",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU",
            ValueName = "NoAutoRebootWithLoggedOnUsers",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DoNotConnectToWindowsUpdateInternetLocations",
            Name = "Block Windows Update Internet Locations",
            Description = "Prevents connecting to Windows Update internet locations.",
            Category = "Windows Update",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate",
            ValueName = "DoNotConnectToWindowsUpdateInternetLocations",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "ExcludeWUDriversInQualityUpdate",
            Name = "Exclude Drivers From Windows Updates",
            Description = "Excludes driver updates from quality updates.",
            Category = "Windows Update",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate",
            ValueName = "ExcludeWUDriversInQualityUpdate",
            ValueKind = RegistryValueKind.DWord
        },

        // Telemetry & Privacy Policies
        new PolicyEntry
        {
            Id = "AllowTelemetry",
            Name = "Diagnostic Data Collection",
            Description = "Controls the level of diagnostic data sent to Microsoft.",
            Category = "Privacy & Telemetry",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
            ValueName = "AllowTelemetry",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DoNotShowFeedbackNotifications",
            Name = "Disable Feedback Notifications",
            Description = "Prevents Windows from showing feedback notifications.",
            Category = "Privacy & Telemetry",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
            ValueName = "DoNotShowFeedbackNotifications",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisableAdvertisingId",
            Name = "Disable Advertising ID",
            Description = "Disables the advertising ID for all users.",
            Category = "Privacy & Telemetry",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo",
            ValueName = "DisabledByGroupPolicy",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "CEIPEnable",
            Name = "Customer Experience Improvement Program",
            Description = "Controls participation in the Customer Experience Improvement Program.",
            Category = "Privacy & Telemetry",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\SQMClient\Windows",
            ValueName = "CEIPEnable",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "PublishUserActivities",
            Name = "Publish User Activities",
            Description = "Controls publishing of user activities.",
            Category = "Privacy & Telemetry",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\System",
            ValueName = "PublishUserActivities",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "UploadUserActivities",
            Name = "Upload User Activities",
            Description = "Controls uploading of user activities.",
            Category = "Privacy & Telemetry",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\System",
            ValueName = "UploadUserActivities",
            ValueKind = RegistryValueKind.DWord
        },

        // Cortana & Search Policies
        new PolicyEntry
        {
            Id = "AllowCortana",
            Name = "Allow Cortana",
            Description = "Controls whether Cortana is allowed.",
            Category = "Cortana & Search",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
            ValueName = "AllowCortana",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisableWebSearch",
            Name = "Disable Web Search",
            Description = "Disables web search in Windows Search.",
            Category = "Cortana & Search",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
            ValueName = "DisableWebSearch",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "ConnectedSearchUseWeb",
            Name = "Connected Search Use Web",
            Description = "Controls connected search web usage.",
            Category = "Cortana & Search",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
            ValueName = "ConnectedSearchUseWeb",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "AllowCloudSearch",
            Name = "Allow Cloud Search",
            Description = "Controls cloud search functionality.",
            Category = "Cortana & Search",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
            ValueName = "AllowCloudSearch",
            ValueKind = RegistryValueKind.DWord
        },

        // Windows Store Policies
        new PolicyEntry
        {
            Id = "AutoDownload",
            Name = "Store Auto-Download",
            Description = "Controls automatic downloading of Store apps.",
            Category = "Windows Store",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\WindowsStore",
            ValueName = "AutoDownload",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisableWindowsConsumerFeatures",
            Name = "Disable Consumer Features",
            Description = "Disables consumer features like suggested apps.",
            Category = "Windows Store",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\CloudContent",
            ValueName = "DisableWindowsConsumerFeatures",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisableSoftLanding",
            Name = "Disable Soft Landing",
            Description = "Disables soft landing experience for new features.",
            Category = "Windows Store",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\CloudContent",
            ValueName = "DisableSoftLanding",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisableCloudOptimizedContent",
            Name = "Disable Cloud Optimized Content",
            Description = "Disables cloud-optimized content.",
            Category = "Windows Store",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\CloudContent",
            ValueName = "DisableCloudOptimizedContent",
            ValueKind = RegistryValueKind.DWord
        },

        // OneDrive Policies
        new PolicyEntry
        {
            Id = "DisableFileSyncNGSC",
            Name = "Disable OneDrive File Sync",
            Description = "Prevents OneDrive from syncing files.",
            Category = "OneDrive",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\OneDrive",
            ValueName = "DisableFileSyncNGSC",
            ValueKind = RegistryValueKind.DWord
        },

        // Windows Defender / Security Policies
        new PolicyEntry
        {
            Id = "EnableSmartScreen",
            Name = "SmartScreen Filter",
            Description = "Controls the SmartScreen filter.",
            Category = "Security",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\System",
            ValueName = "EnableSmartScreen",
            ValueKind = RegistryValueKind.DWord
        },

        // Windows Error Reporting Policies
        new PolicyEntry
        {
            Id = "DisableErrorReporting",
            Name = "Disable Error Reporting",
            Description = "Disables Windows Error Reporting.",
            Category = "Error Reporting",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting",
            ValueName = "Disabled",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DoReport",
            Name = "PC Health Error Reporting",
            Description = "Controls PC health error reporting.",
            Category = "Error Reporting",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\PCHealth\ErrorReporting",
            ValueName = "DoReport",
            ValueKind = RegistryValueKind.DWord
        },

        // System Restore Policies
        new PolicyEntry
        {
            Id = "DisableSR",
            Name = "Disable System Restore",
            Description = "Disables System Restore functionality.",
            Category = "System Restore",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows NT\SystemRestore",
            ValueName = "DisableSR",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisableConfig",
            Name = "Disable System Restore Configuration",
            Description = "Prevents configuration of System Restore.",
            Category = "System Restore",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows NT\SystemRestore",
            ValueName = "DisableConfig",
            ValueKind = RegistryValueKind.DWord
        },

        // Windows Insider Policies
        new PolicyEntry
        {
            Id = "AllowBuildPreview",
            Name = "Allow Build Preview",
            Description = "Controls Windows Insider preview builds.",
            Category = "Windows Insider",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\PreviewBuilds",
            ValueName = "AllowBuildPreview",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "EnableConfigFlighting",
            Name = "Enable Config Flighting",
            Description = "Controls configuration flighting.",
            Category = "Windows Insider",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\PreviewBuilds",
            ValueName = "EnableConfigFlighting",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "EnableExperimentation",
            Name = "Enable Experimentation",
            Description = "Controls Windows experimentation features.",
            Category = "Windows Insider",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\PreviewBuilds",
            ValueName = "EnableExperimentation",
            ValueKind = RegistryValueKind.DWord
        },

        // Input Personalization Policies
        new PolicyEntry
        {
            Id = "AllowInputPersonalization",
            Name = "Allow Input Personalization",
            Description = "Controls input personalization features.",
            Category = "Input & Privacy",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\InputPersonalization",
            ValueName = "AllowInputPersonalization",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "PreventHandwritingDataSharing",
            Name = "Prevent Handwriting Data Sharing",
            Description = "Prevents sharing of handwriting data.",
            Category = "Input & Privacy",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\TabletPC",
            ValueName = "PreventHandwritingDataSharing",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "AllowLinguisticDataCollection",
            Name = "Allow Linguistic Data Collection",
            Description = "Controls linguistic data collection.",
            Category = "Input & Privacy",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\TextInput",
            ValueName = "AllowLinguisticDataCollection",
            ValueKind = RegistryValueKind.DWord
        },

        // App Privacy Policies
        new PolicyEntry
        {
            Id = "LetAppsRunInBackground",
            Name = "Let Apps Run In Background",
            Description = "Controls whether apps can run in the background.",
            Category = "App Privacy",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
            ValueName = "LetAppsRunInBackground",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "LetAppsActivateWithVoice",
            Name = "Let Apps Activate With Voice",
            Description = "Controls voice activation for apps.",
            Category = "App Privacy",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
            ValueName = "LetAppsActivateWithVoice",
            ValueKind = RegistryValueKind.DWord
        },

        // Windows Ink Policies
        new PolicyEntry
        {
            Id = "AllowWindowsInkWorkspace",
            Name = "Allow Windows Ink Workspace",
            Description = "Controls Windows Ink Workspace.",
            Category = "Windows Ink",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\WindowsInkWorkspace",
            ValueName = "AllowWindowsInkWorkspace",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "AllowSuggestedAppsInWindowsInkWorkspace",
            Name = "Allow Suggested Apps In Windows Ink",
            Description = "Controls suggested apps in Windows Ink Workspace.",
            Category = "Windows Ink",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\WindowsInkWorkspace",
            ValueName = "AllowSuggestedAppsInWindowsInkWorkspace",
            ValueKind = RegistryValueKind.DWord
        },

        // Biometrics Policies
        new PolicyEntry
        {
            Id = "BiometricsEnabled",
            Name = "Biometrics Enabled",
            Description = "Controls biometric authentication.",
            Category = "Biometrics",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Biometrics",
            ValueName = "Enabled",
            ValueKind = RegistryValueKind.DWord
        },

        // Location Policies
        new PolicyEntry
        {
            Id = "DisableLocation",
            Name = "Disable Location Services",
            Description = "Disables location services.",
            Category = "Location",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors",
            ValueName = "DisableLocation",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisableWindowsLocationProvider",
            Name = "Disable Windows Location Provider",
            Description = "Disables the Windows location provider.",
            Category = "Location",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors",
            ValueName = "DisableWindowsLocationProvider",
            ValueKind = RegistryValueKind.DWord
        },

        // Find My Device Policies
        new PolicyEntry
        {
            Id = "AllowFindMyDevice",
            Name = "Allow Find My Device",
            Description = "Controls Find My Device functionality.",
            Category = "Find My Device",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\FindMyDevice",
            ValueName = "AllowFindMyDevice",
            ValueKind = RegistryValueKind.DWord
        },

        // Messaging Policies
        new PolicyEntry
        {
            Id = "AllowMessageSync",
            Name = "Allow Message Sync",
            Description = "Controls message synchronization.",
            Category = "Messaging",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\Messaging",
            ValueName = "AllowMessageSync",
            ValueKind = RegistryValueKind.DWord
        },

        // Clipboard Policies
        new PolicyEntry
        {
            Id = "AllowClipboardHistory",
            Name = "Allow Clipboard History",
            Description = "Controls clipboard history.",
            Category = "Clipboard",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\System",
            ValueName = "AllowClipboardHistory",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "AllowCrossDeviceClipboard",
            Name = "Allow Cross-Device Clipboard",
            Description = "Controls cross-device clipboard sync.",
            Category = "Clipboard",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\System",
            ValueName = "AllowCrossDeviceClipboard",
            ValueKind = RegistryValueKind.DWord
        },

        // Speech Policies
        new PolicyEntry
        {
            Id = "AllowSpeechModelUpdate",
            Name = "Allow Speech Model Update",
            Description = "Controls speech recognition model updates.",
            Category = "Speech",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Speech",
            ValueName = "AllowSpeechModelUpdate",
            ValueKind = RegistryValueKind.DWord
        },

        // Activity Feed Policies
        new PolicyEntry
        {
            Id = "EnableActivityFeed",
            Name = "Enable Activity Feed",
            Description = "Controls the Activity Feed feature.",
            Category = "Activity History",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\System",
            ValueName = "EnableActivityFeed",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "EnableCdp",
            Name = "Enable Connected Devices Platform",
            Description = "Controls Connected Devices Platform.",
            Category = "Activity History",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\System",
            ValueName = "EnableCdp",
            ValueKind = RegistryValueKind.DWord
        },

        // Game DVR Policies
        new PolicyEntry
        {
            Id = "AllowGameDVR",
            Name = "Allow Game DVR",
            Description = "Controls Game DVR functionality.",
            Category = "Gaming",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\GameDVR",
            ValueName = "AllowGameDVR",
            ValueKind = RegistryValueKind.DWord
        },

        // Windows Feeds / Widgets Policies
        new PolicyEntry
        {
            Id = "EnableFeeds",
            Name = "Enable Windows Feeds",
            Description = "Controls Windows Feeds (News and Interests).",
            Category = "Widgets & Feeds",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\Windows Feeds",
            ValueName = "EnableFeeds",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "AllowNewsAndInterests",
            Name = "Allow News and Interests",
            Description = "Controls the News and Interests widget.",
            Category = "Widgets & Feeds",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Dsh",
            ValueName = "AllowNewsAndInterests",
            ValueKind = RegistryValueKind.DWord
        },

        // Copilot Policies (Windows 11)
        new PolicyEntry
        {
            Id = "TurnOffWindowsCopilot",
            Name = "Turn Off Windows Copilot",
            Description = "Disables Windows Copilot.",
            Category = "Copilot",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Policies\Microsoft\Windows\WindowsCopilot",
            ValueName = "TurnOffWindowsCopilot",
            ValueKind = RegistryValueKind.DWord,
            MinWindowsBuild = 22621 // Windows 11 22H2+
        },

        // Windows Recall Policies (Windows 11 24H2+)
        new PolicyEntry
        {
            Id = "DisableAIDataAnalysis_HKLM",
            Name = "Disable Windows Recall (Machine)",
            Description = "Disables Windows Recall AI data analysis for all users.",
            Category = "Windows Recall",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI",
            ValueName = "DisableAIDataAnalysis",
            ValueKind = RegistryValueKind.DWord,
            MinWindowsBuild = 26100 // Windows 11 24H2+
        },
        new PolicyEntry
        {
            Id = "DisableAIDataAnalysis_HKCU",
            Name = "Disable Windows Recall (User)",
            Description = "Disables Windows Recall AI data analysis for current user.",
            Category = "Windows Recall",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI",
            ValueName = "DisableAIDataAnalysis",
            ValueKind = RegistryValueKind.DWord,
            MinWindowsBuild = 26100 // Windows 11 24H2+
        },

        // Edge Browser Policies
        new PolicyEntry
        {
            Id = "HubsSidebarEnabled",
            Name = "Edge Sidebar Enabled",
            Description = "Controls the Edge browser sidebar.",
            Category = "Microsoft Edge",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Edge",
            ValueName = "HubsSidebarEnabled",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "PersonalizationReportingEnabled",
            Name = "Edge Personalization Reporting",
            Description = "Controls Edge personalization reporting.",
            Category = "Microsoft Edge",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Edge",
            ValueName = "PersonalizationReportingEnabled",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "EdgeMetricsReportingEnabled",
            Name = "Edge Metrics Reporting",
            Description = "Controls Edge metrics reporting.",
            Category = "Microsoft Edge",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Edge",
            ValueName = "MetricsReportingEnabled",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "EdgeUserFeedbackAllowed",
            Name = "Edge User Feedback",
            Description = "Controls Edge user feedback.",
            Category = "Microsoft Edge",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Edge",
            ValueName = "UserFeedbackAllowed",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "EdgeSpotlightExperiences",
            Name = "Edge Spotlight Experiences",
            Description = "Controls Edge spotlight experiences and recommendations.",
            Category = "Microsoft Edge",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Edge",
            ValueName = "SpotlightExperiencesAndRecommendationsEnabled",
            ValueKind = RegistryValueKind.DWord
        },

        // File History Policies
        new PolicyEntry
        {
            Id = "FileHistoryDisabled",
            Name = "File History Disabled",
            Description = "Disables File History.",
            Category = "File History",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\FileHistory",
            ValueName = "Disabled",
            ValueKind = RegistryValueKind.DWord
        },

        // MRT (Malicious Software Removal Tool) Policies
        new PolicyEntry
        {
            Id = "DontOfferThroughWUAU",
            Name = "Don't Offer MRT Through Windows Update",
            Description = "Prevents offering Malicious Software Removal Tool through Windows Update.",
            Category = "Security",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\MRT",
            ValueName = "DontOfferThroughWUAU",
            ValueKind = RegistryValueKind.DWord
        },

        // Explorer Policies (User)
        new PolicyEntry
        {
            Id = "DisableSearchBoxSuggestions_User",
            Name = "Disable Search Box Suggestions (User)",
            Description = "Disables search box suggestions for current user.",
            Category = "Search",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Policies\Microsoft\Windows\Explorer",
            ValueName = "DisableSearchBoxSuggestions",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisableSearchBoxSuggestions_Machine",
            Name = "Disable Search Box Suggestions (Machine)",
            Description = "Disables search box suggestions for all users.",
            Category = "Search",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\Explorer",
            ValueName = "DisableSearchBoxSuggestions",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "HideRecommendedSection",
            Name = "Hide Recommended Section",
            Description = "Hides the recommended section in Start Menu.",
            Category = "Start Menu",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\Explorer",
            ValueName = "HideRecommendedSection",
            ValueKind = RegistryValueKind.DWord,
            MinWindowsBuild = 22000 // Windows 11+
        },

        //  Windows Defender / Antivirus Policies
        new PolicyEntry
        {
            Id = "DisableAntiSpyware",
            Name = "Disable Windows Defender Antivirus",
            Description = "Disables the Windows Defender Antivirus engine.",
            Category = "Windows Defender",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows Defender",
            ValueName = "DisableAntiSpyware",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisableRealtimeMonitoring",
            Name = "Disable Real-Time Protection",
            Description = "Disables real-time monitoring of Windows Defender.",
            Category = "Windows Defender",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection",
            ValueName = "DisableRealtimeMonitoring",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisableBehaviorMonitoring",
            Name = "Disable Behavior Monitoring",
            Description = "Disables behavior monitoring in Windows Defender.",
            Category = "Windows Defender",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection",
            ValueName = "DisableBehaviorMonitoring",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisableIOAVProtection",
            Name = "Disable Scan Downloaded Files",
            Description = "Disables scanning of downloaded files and attachments.",
            Category = "Windows Defender",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection",
            ValueName = "DisableIOAVProtection",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisableOnAccessProtection",
            Name = "Disable On-Access Protection",
            Description = "Disables monitoring file and program activity.",
            Category = "Windows Defender",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection",
            ValueName = "DisableOnAccessProtection",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisableRoutinelyTakingAction",
            Name = "Disable Automatic Remediation",
            Description = "Disables Windows Defender from automatically taking action on detections.",
            Category = "Windows Defender",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows Defender",
            ValueName = "DisableRoutinelyTakingAction",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "SpynetReporting",
            Name = "Microsoft MAPS Reporting",
            Description = "Controls cloud-delivered protection reporting level.",
            Category = "Windows Defender",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows Defender\Spynet",
            ValueName = "SpynetReporting",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "SubmitSamplesConsent",
            Name = "Automatic Sample Submission",
            Description = "Controls automatic sample submission to Microsoft.",
            Category = "Windows Defender",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows Defender\Spynet",
            ValueName = "SubmitSamplesConsent",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "MpEnablePus",
            Name = "Potentially Unwanted Application Protection",
            Description = "Controls detection for potentially unwanted applications.",
            Category = "Windows Defender",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows Defender",
            ValueName = "PUAProtection",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisableBlockAtFirstSeen",
            Name = "Disable Block at First Seen",
            Description = "Disables the Block at First Seen cloud protection feature.",
            Category = "Windows Defender",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows Defender\Spynet",
            ValueName = "DisableBlockAtFirstSeen",
            ValueKind = RegistryValueKind.DWord
        },

        //  Windows Firewall Policies
        new PolicyEntry
        {
            Id = "FwDomainEnableFirewall",
            Name = "Domain Profile Firewall Enabled",
            Description = "Controls Windows Firewall state for the Domain profile.",
            Category = "Windows Firewall",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\WindowsFirewall\DomainProfile",
            ValueName = "EnableFirewall",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "FwStandardEnableFirewall",
            Name = "Private Profile Firewall Enabled",
            Description = "Controls Windows Firewall state for the Private profile.",
            Category = "Windows Firewall",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\WindowsFirewall\StandardProfile",
            ValueName = "EnableFirewall",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "FwPublicEnableFirewall",
            Name = "Public Profile Firewall Enabled",
            Description = "Controls Windows Firewall state for the Public profile.",
            Category = "Windows Firewall",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\WindowsFirewall\PublicProfile",
            ValueName = "EnableFirewall",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "FwDomainDisableNotifications",
            Name = "Domain Profile Firewall Notifications",
            Description = "Controls firewall notifications for the Domain profile.",
            Category = "Windows Firewall",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\WindowsFirewall\DomainProfile",
            ValueName = "DisableNotifications",
            ValueKind = RegistryValueKind.DWord
        },

        //  Remote Desktop Policies
        new PolicyEntry
        {
            Id = "fDenyTSConnections",
            Name = "Deny Remote Desktop Connections",
            Description = "Controls whether Remote Desktop connections are allowed.",
            Category = "Remote Desktop",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services",
            ValueName = "fDenyTSConnections",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "UserAuthentication_RDP",
            Name = "Require Network Level Authentication",
            Description = "Requires Network Level Authentication for Remote Desktop.",
            Category = "Remote Desktop",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services",
            ValueName = "UserAuthentication",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "MinEncryptionLevel",
            Name = "Remote Desktop Encryption Level",
            Description = "Sets the minimum encryption level for Remote Desktop connections.",
            Category = "Remote Desktop",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services",
            ValueName = "MinEncryptionLevel",
            ValueKind = RegistryValueKind.DWord
        },

        //  Remote Assistance Policies
        new PolicyEntry
        {
            Id = "fAllowToGetHelp",
            Name = "Allow Remote Assistance",
            Description = "Controls whether Remote Assistance can be requested.",
            Category = "Remote Assistance",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services",
            ValueName = "fAllowToGetHelp",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "fAllowUnsolicited",
            Name = "Allow Unsolicited Remote Assistance",
            Description = "Controls unsolicited Remote Assistance offers.",
            Category = "Remote Assistance",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services",
            ValueName = "fAllowUnsolicited",
            ValueKind = RegistryValueKind.DWord
        },

        //  Network / Wi-Fi / Hotspot Policies
        new PolicyEntry
        {
            Id = "NC_ShowSharedAccessUI",
            Name = "Prohibit Internet Connection Sharing",
            Description = "Controls the ability to share internet connections.",
            Category = "Network",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\Network Connections",
            ValueName = "NC_ShowSharedAccessUI",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "fMinimizeConnections",
            Name = "Minimize Simultaneous Connections",
            Description = "Prevents connecting to multiple networks simultaneously.",
            Category = "Network",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\WcmSvc\GroupPolicy",
            ValueName = "fMinimizeConnections",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisableDNSOverHTTPS",
            Name = "Disable DNS-over-HTTPS",
            Description = "Disables DNS-over-HTTPS (DoH) resolution.",
            Category = "Network",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows NT\DNSClient",
            ValueName = "DoHPolicy",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "EnableFontProviders",
            Name = "Enable Font Providers",
            Description = "Controls whether Windows downloads fonts from online font providers.",
            Category = "Network",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\System",
            ValueName = "EnableFontProviders",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "AllowNetworkConnectivityPassivePolling",
            Name = "Network Connectivity Passive Polling",
            Description = "Controls network connectivity passive polling (NCSI).",
            Category = "Network",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\NetworkConnectivityStatusIndicator",
            ValueName = "NoActiveProbe",
            ValueKind = RegistryValueKind.DWord
        },

        //  Power Management Policies
        new PolicyEntry
        {
            Id = "HibernateEnabled",
            Name = "Allow Hibernate",
            Description = "Controls whether hibernate is available as a power option.",
            Category = "Power Management",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Power\PowerSettings",
            ValueName = "HibernateEnabled",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "AllowStandby",
            Name = "Allow Standby States",
            Description = "Controls whether the system can use standby states.",
            Category = "Power Management",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Power\PowerSettings",
            ValueName = "AllowStandby",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "RequirePasswordOnWakeup",
            Name = "Require Password on Wake",
            Description = "Requires a password when the computer wakes from sleep.",
            Category = "Power Management",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Power\PowerSettings\0e796bdb-100d-47d6-a2d5-f7d2daa51f51",
            ValueName = "ACSettingIndex",
            ValueKind = RegistryValueKind.DWord
        },

        //  Device Installation / USB Policies
        new PolicyEntry
        {
            Id = "DenyAllDeviceInstall",
            Name = "Prevent Device Installation",
            Description = "Prevents installation of devices not described by other policy settings.",
            Category = "Device Installation",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\DeviceInstall\Restrictions",
            ValueName = "DenyUnspecified",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DenyRemovableDevices",
            Name = "Deny Removable Devices",
            Description = "Prevents installation of removable devices.",
            Category = "Device Installation",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\DeviceInstall\Restrictions",
            ValueName = "DenyRemovableDevices",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "Deny_Write_Removable",
            Name = "Deny Write Access to Removable Drives",
            Description = "Prevents writing data to removable storage devices.",
            Category = "Device Installation",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\RemovableStorageDevices\{53f5630d-b6bf-11d0-94f2-00a0c91efb8b}",
            ValueName = "Deny_Write",
            ValueKind = RegistryValueKind.DWord
        },

        //  Lock Screen Policies
        new PolicyEntry
        {
            Id = "NoLockScreen",
            Name = "Disable Lock Screen",
            Description = "Disables the Windows lock screen.",
            Category = "Lock Screen",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\Personalization",
            ValueName = "NoLockScreen",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "NoLockScreenCamera",
            Name = "Disable Lock Screen Camera",
            Description = "Disables the camera toggle on the lock screen.",
            Category = "Lock Screen",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\Personalization",
            ValueName = "NoLockScreenCamera",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "NoLockScreenSlideshow",
            Name = "Disable Lock Screen Slideshow",
            Description = "Prevents slideshow on the lock screen.",
            Category = "Lock Screen",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\Personalization",
            ValueName = "NoLockScreenSlideshow",
            ValueKind = RegistryValueKind.DWord
        },

        //  User Account Control (UAC) Policies
        new PolicyEntry
        {
            Id = "EnableLUA",
            Name = "User Account Control Enabled",
            Description = "Controls whether UAC is enabled.",
            Category = "User Account Control",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
            ValueName = "EnableLUA",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "ConsentPromptBehaviorAdmin",
            Name = "UAC Admin Consent Prompt",
            Description = "Controls the behavior of UAC prompts for administrators.",
            Category = "User Account Control",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
            ValueName = "ConsentPromptBehaviorAdmin",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "ConsentPromptBehaviorUser",
            Name = "UAC Standard User Prompt",
            Description = "Controls the behavior of UAC prompts for standard users.",
            Category = "User Account Control",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
            ValueName = "ConsentPromptBehaviorUser",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "PromptOnSecureDesktop",
            Name = "UAC Secure Desktop Prompt",
            Description = "Controls whether UAC prompts appear on the secure desktop.",
            Category = "User Account Control",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
            ValueName = "PromptOnSecureDesktop",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "EnableVirtualization",
            Name = "UAC File and Registry Virtualization",
            Description = "Controls UAC file and registry virtualization.",
            Category = "User Account Control",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
            ValueName = "EnableVirtualization",
            ValueKind = RegistryValueKind.DWord
        },

        //  Delivery Optimization Policies
        new PolicyEntry
        {
            Id = "DODownloadMode",
            Name = "Delivery Optimization Download Mode",
            Description = "Controls how Windows downloads updates using peer-to-peer.",
            Category = "Delivery Optimization",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization",
            ValueName = "DODownloadMode",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DOMaxUploadBandwidth",
            Name = "Delivery Optimization Max Upload Bandwidth",
            Description = "Limits the upload bandwidth used for Delivery Optimization.",
            Category = "Delivery Optimization",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization",
            ValueName = "DOMaxUploadBandwidth",
            ValueKind = RegistryValueKind.DWord
        },

        //  Notifications Policies
        new PolicyEntry
        {
            Id = "NoToastApplicationNotification",
            Name = "Disable Toast Notifications",
            Description = "Disables toast notifications for applications.",
            Category = "Notifications",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Policies\Microsoft\Windows\CurrentVersion\PushNotifications",
            ValueName = "NoToastApplicationNotification",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "NoCallsDuringQuietHours",
            Name = "Disable Calls During Quiet Hours",
            Description = "Blocks calls during quiet hours / focus assist.",
            Category = "Notifications",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Policies\Microsoft\Windows\CurrentVersion\QuietHours",
            ValueName = "AllowCalls",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "NoLockScreenNotifications",
            Name = "Disable Lock Screen Notifications",
            Description = "Prevents notifications from appearing on the lock screen.",
            Category = "Notifications",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\System",
            ValueName = "DisableLockScreenAppNotifications",
            ValueKind = RegistryValueKind.DWord
        },

        //  Windows Spotlight Policies
        new PolicyEntry
        {
            Id = "DisableWindowsSpotlightFeatures",
            Name = "Disable Windows Spotlight",
            Description = "Disables all Windows Spotlight features.",
            Category = "Windows Spotlight",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Policies\Microsoft\Windows\CloudContent",
            ValueName = "DisableWindowsSpotlightFeatures",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisableThirdPartySuggestions",
            Name = "Disable Third-Party Suggestions",
            Description = "Disables third-party content suggestions in Windows Spotlight.",
            Category = "Windows Spotlight",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Policies\Microsoft\Windows\CloudContent",
            ValueName = "DisableThirdPartySuggestions",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisableSpotlightOnActionCenter",
            Name = "Disable Spotlight on Action Center",
            Description = "Disables Windows Spotlight suggestions in Action Center.",
            Category = "Windows Spotlight",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Policies\Microsoft\Windows\CloudContent",
            ValueName = "DisableWindowsSpotlightOnActionCenter",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisableSpotlightOnSettings",
            Name = "Disable Spotlight on Settings",
            Description = "Disables Windows Spotlight suggestions in Settings.",
            Category = "Windows Spotlight",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Policies\Microsoft\Windows\CloudContent",
            ValueName = "DisableWindowsSpotlightOnSettings",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisableTailoredExperiences",
            Name = "Disable Tailored Experiences",
            Description = "Disables tailored experiences with diagnostic data.",
            Category = "Windows Spotlight",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Policies\Microsoft\Windows\CloudContent",
            ValueName = "DisableTailoredExperiencesWithDiagnosticData",
            ValueKind = RegistryValueKind.DWord
        },

        //  Autoplay Policies
        new PolicyEntry
        {
            Id = "NoDriveTypeAutoRun",
            Name = "Disable AutoPlay",
            Description = "Disables AutoPlay for all drives.",
            Category = "Autoplay",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer",
            ValueName = "NoDriveTypeAutoRun",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "NoAutorun",
            Name = "Disable Default AutoRun Behavior",
            Description = "Sets the default behavior for AutoRun commands.",
            Category = "Autoplay",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer",
            ValueName = "NoAutorun",
            ValueKind = RegistryValueKind.DWord
        },

        //  Offline Maps Policies
        new PolicyEntry
        {
            Id = "AutoDownloadAndUpdateMapData",
            Name = "Auto-Download Map Data",
            Description = "Controls automatic download and update of map data.",
            Category = "Offline Maps",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\Maps",
            ValueName = "AutoDownloadAndUpdateMapData",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "AllowUntriggeredNetworkTrafficOnSettingsPage",
            Name = "Allow Map Network Traffic",
            Description = "Controls network traffic triggered by the Offline Maps settings page.",
            Category = "Offline Maps",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\Maps",
            ValueName = "AllowUntriggeredNetworkTrafficOnSettingsPage",
            ValueKind = RegistryValueKind.DWord
        },

        //  Print / Spooler Policies
        new PolicyEntry
        {
            Id = "RegisterSpoolerRemoteRpcEndPoint",
            Name = "Allow Print Spooler RPC",
            Description = "Controls the Print Spooler RPC endpoint.",
            Category = "Print Spooler",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows NT\Printers",
            ValueName = "RegisterSpoolerRemoteRpcEndPoint",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisableWebPnPDownload",
            Name = "Disable Web-Based Printing",
            Description = "Disables downloading of print drivers over HTTP.",
            Category = "Print Spooler",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows NT\Printers",
            ValueName = "DisableWebPnPDownload",
            ValueKind = RegistryValueKind.DWord
        },

        //  Scheduled Maintenance Policies
        new PolicyEntry
        {
            Id = "MaintenanceDisabled",
            Name = "Disable Automatic Maintenance",
            Description = "Disables scheduled automatic maintenance.",
            Category = "Scheduled Maintenance",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\ScheduledDiagnostics",
            ValueName = "EnabledExecution",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisableDefrag",
            Name = "Disable Scheduled Defragmentation",
            Description = "Disables the scheduled disk defragmentation task.",
            Category = "Scheduled Maintenance",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\Defrag",
            ValueName = "DisableDefrag",
            ValueKind = RegistryValueKind.DWord
        },

        //  OEM / Manufacturer Preinstall Policies
        new PolicyEntry
        {
            Id = "DisableOEMApps",
            Name = "Disable OEM Pre-Installed Apps",
            Description = "Prevents pre-installed OEM apps from installing on new user profiles.",
            Category = "OEM & Preinstall",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\CloudContent",
            ValueName = "DisableWindowsConsumerFeatures",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "ContentDeliveryAllowed",
            Name = "Content Delivery Allowed",
            Description = "Controls whether Windows delivers content such as suggested apps.",
            Category = "OEM & Preinstall",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Policies\Microsoft\Windows\CloudContent",
            ValueName = "ContentDeliveryAllowed",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "SubscribedContentEnabled",
            Name = "Subscribed Content (Suggestions)",
            Description = "Controls suggested content in Settings and Start.",
            Category = "OEM & Preinstall",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Policies\Microsoft\Windows\CloudContent",
            ValueName = "SubscribedContent-338388Enabled",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "SilentInstalledApps",
            Name = "Silently Installed Apps",
            Description = "Controls whether Windows silently installs suggested apps.",
            Category = "OEM & Preinstall",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Policies\Microsoft\Windows\CloudContent",
            ValueName = "SilentInstalledAppsEnabled",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "PreInstalledAppsEnabled",
            Name = "Pre-Installed Apps Enabled",
            Description = "Controls whether pre-installed apps are enabled for new user profiles.",
            Category = "OEM & Preinstall",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\CloudContent",
            ValueName = "PreInstalledAppsEnabled",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "PreInstalledAppsEverEnabled",
            Name = "Pre-Installed Apps Ever Enabled",
            Description = "Controls whether pre-installed apps were ever enabled.",
            Category = "OEM & Preinstall",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\CloudContent",
            ValueName = "PreInstalledAppsEverEnabled",
            ValueKind = RegistryValueKind.DWord
        },

        //  Gaming / Game Bar / Game Mode Extended
        new PolicyEntry
        {
            Id = "AllowBroadcasting",
            Name = "Allow Game Broadcasting",
            Description = "Controls whether game broadcasting is allowed.",
            Category = "Gaming",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\GameDVR",
            ValueName = "AllowBroadcasting",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "AllowGameMode",
            Name = "Allow Game Mode",
            Description = "Controls whether Game Mode is available.",
            Category = "Gaming",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\GameDVR",
            ValueName = "AllowGameMode",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "XboxGameBarEnabled",
            Name = "Xbox Game Bar Enabled",
            Description = "Controls whether the Xbox Game Bar is enabled.",
            Category = "Gaming",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Policies\Microsoft\Windows\GameDVR",
            ValueName = "AppCaptureEnabled",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "GameBarShowStartupPanel",
            Name = "Game Bar Startup Panel",
            Description = "Controls whether Game Bar shows the startup panel.",
            Category = "Gaming",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Policies\Microsoft\Windows\GameDVR",
            ValueName = "GameBarShowStartupPanel",
            ValueKind = RegistryValueKind.DWord
        },

        //  Microsoft Office / 365 Telemetry Policies
        new PolicyEntry
        {
            Id = "OfficeUpdateDisable",
            Name = "Disable Office Automatic Updates",
            Description = "Controls automatic updates for Microsoft Office.",
            Category = "Microsoft Office",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\office\16.0\common\officeupdate",
            ValueName = "enableautomaticupdates",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "OfficeTelemetryEnabled",
            Name = "Office Telemetry",
            Description = "Controls telemetry data collection in Microsoft Office.",
            Category = "Microsoft Office",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Policies\Microsoft\office\common\clienttelemetry",
            ValueName = "SendTelemetry",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "OfficeConnectedExperiences",
            Name = "Office Connected Experiences",
            Description = "Controls optional connected experiences in Office.",
            Category = "Microsoft Office",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Policies\Microsoft\office\16.0\common\privacy",
            ValueName = "disconnectedstate",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "OfficeLinkedIn",
            Name = "Office LinkedIn Integration",
            Description = "Controls LinkedIn features in Microsoft Office applications.",
            Category = "Microsoft Office",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\office\16.0\common\linkedin",
            ValueName = "AllowLinkedInFeatures",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "OfficePersonalInsights",
            Name = "Office Personal Insights",
            Description = "Controls personal productivity insights in Office.",
            Category = "Microsoft Office",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Policies\Microsoft\office\16.0\common\privacy",
            ValueName = "controllerconnectedservicesenabled",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "OfficeFeedback",
            Name = "Office Feedback Collection",
            Description = "Controls whether Office apps collect user feedback.",
            Category = "Microsoft Office",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Policies\Microsoft\office\16.0\common\feedback",
            ValueName = "enabled",
            ValueKind = RegistryValueKind.DWord
        },

        //  Google Chrome Enterprise Policies
        new PolicyEntry
        {
            Id = "ChromeMetricsReporting",
            Name = "Chrome Metrics Reporting",
            Description = "Controls Chrome usage statistics and crash reports.",
            Category = "Google Chrome",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Google\Chrome",
            ValueName = "MetricsReportingEnabled",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "ChromeSafeBrowsing",
            Name = "Chrome Safe Browsing",
            Description = "Controls Chrome Safe Browsing protection level.",
            Category = "Google Chrome",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Google\Chrome",
            ValueName = "SafeBrowsingProtectionLevel",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "ChromeTranslateEnabled",
            Name = "Chrome Translation Service",
            Description = "Controls the Chrome integrated translation service.",
            Category = "Google Chrome",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Google\Chrome",
            ValueName = "TranslateEnabled",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "ChromeSearchSuggestEnabled",
            Name = "Chrome Search Suggestions",
            Description = "Controls search suggestions in Chrome address bar.",
            Category = "Google Chrome",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Google\Chrome",
            ValueName = "SearchSuggestEnabled",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "ChromeSpellCheckEnabled",
            Name = "Chrome Spellcheck Service",
            Description = "Controls the Chrome online spellcheck service.",
            Category = "Google Chrome",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Google\Chrome",
            ValueName = "SpellCheckServiceEnabled",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "ChromeSyncDisabled",
            Name = "Chrome Sync Disabled",
            Description = "Disables Chrome browser data synchronization.",
            Category = "Google Chrome",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Google\Chrome",
            ValueName = "SyncDisabled",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "ChromeBackgroundMode",
            Name = "Chrome Background Mode",
            Description = "Controls whether Chrome runs in the background.",
            Category = "Google Chrome",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Google\Chrome",
            ValueName = "BackgroundModeEnabled",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "ChromeNetworkPrediction",
            Name = "Chrome Network Prediction",
            Description = "Controls Chrome network prediction (prefetching).",
            Category = "Google Chrome",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Google\Chrome",
            ValueName = "NetworkPredictionOptions",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "ChromeAutoUpdate",
            Name = "Chrome Auto-Update Policy",
            Description = "Controls Google Chrome automatic update behavior.",
            Category = "Google Chrome",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Google\Update",
            ValueName = "AutoUpdateCheckPeriodMinutes",
            ValueKind = RegistryValueKind.DWord
        },

        //  Mozilla Firefox Enterprise Policies
        new PolicyEntry
        {
            Id = "FirefoxDisableTelemetry",
            Name = "Firefox Telemetry",
            Description = "Controls Mozilla Firefox telemetry data collection.",
            Category = "Mozilla Firefox",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Mozilla\Firefox",
            ValueName = "DisableTelemetry",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "FirefoxDisableFirefoxStudies",
            Name = "Firefox Studies",
            Description = "Controls Firefox Shield/Normandy studies.",
            Category = "Mozilla Firefox",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Mozilla\Firefox",
            ValueName = "DisableFirefoxStudies",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "FirefoxDisablePocket",
            Name = "Firefox Pocket Integration",
            Description = "Controls the Pocket integration in Firefox.",
            Category = "Mozilla Firefox",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Mozilla\Firefox",
            ValueName = "DisablePocket",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "FirefoxDisableDefaultBrowserAgent",
            Name = "Firefox Default Browser Agent",
            Description = "Controls the Firefox Default Browser Agent.",
            Category = "Mozilla Firefox",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Mozilla\Firefox",
            ValueName = "DisableDefaultBrowserAgent",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "FirefoxDontCheckDefaultBrowser",
            Name = "Firefox Default Browser Check",
            Description = "Controls whether Firefox checks if it is the default browser.",
            Category = "Mozilla Firefox",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Mozilla\Firefox",
            ValueName = "DontCheckDefaultBrowser",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "FirefoxDisableFormHistory",
            Name = "Firefox Form History",
            Description = "Controls whether Firefox saves form and search history.",
            Category = "Mozilla Firefox",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Mozilla\Firefox",
            ValueName = "DisableFormHistory",
            ValueKind = RegistryValueKind.DWord
        },

        //  Adobe Enterprise Policies
        new PolicyEntry
        {
            Id = "AdobeAutoUpdate",
            Name = "Adobe Reader Auto-Update",
            Description = "Controls Adobe Acrobat Reader automatic updates.",
            Category = "Adobe",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Adobe\Acrobat Reader\DC\FeatureLockDown",
            ValueName = "bUpdater",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "AdobeUsageStatistics",
            Name = "Adobe Usage Statistics",
            Description = "Controls Adobe usage statistics collection.",
            Category = "Adobe",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Adobe\Acrobat Reader\DC\FeatureLockDown",
            ValueName = "bUsageMeasurement",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "AdobeOnlineServices",
            Name = "Adobe Online Services",
            Description = "Controls Adobe online service access and cloud features.",
            Category = "Adobe",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Adobe\Acrobat Reader\DC\FeatureLockDown\cServices",
            ValueName = "bToggleAdobeDocumentServices",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "AdobeAcrobatAutoUpdate",
            Name = "Adobe Acrobat Pro Auto-Update",
            Description = "Controls Adobe Acrobat Pro automatic updates.",
            Category = "Adobe",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Adobe\Adobe Acrobat\DC\FeatureLockDown",
            ValueName = "bUpdater",
            ValueKind = RegistryValueKind.DWord
        },

        //  Microsoft Edge Extended Policies
        new PolicyEntry
        {
            Id = "EdgeStartupBoostEnabled",
            Name = "Edge Startup Boost",
            Description = "Controls Edge startup boost (prelaunch on login).",
            Category = "Microsoft Edge",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Edge",
            ValueName = "StartupBoostEnabled",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "EdgeBackgroundMode",
            Name = "Edge Background Mode",
            Description = "Controls whether Edge continues running in the background.",
            Category = "Microsoft Edge",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Edge",
            ValueName = "BackgroundModeEnabled",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "EdgeShoppingAssistant",
            Name = "Edge Shopping Assistant",
            Description = "Controls the Edge shopping assistant feature.",
            Category = "Microsoft Edge",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Edge",
            ValueName = "EdgeShoppingAssistantEnabled",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "EdgeSearchSuggestEnabled",
            Name = "Edge Search Suggestions",
            Description = "Controls search suggestions in Edge address bar.",
            Category = "Microsoft Edge",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Edge",
            ValueName = "SearchSuggestEnabled",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "EdgeSendSiteInfoToImproveServices",
            Name = "Edge Send Site Info",
            Description = "Controls sending browsing data to improve Microsoft services.",
            Category = "Microsoft Edge",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Edge",
            ValueName = "SendSiteInfoToImproveServices",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "EdgeNetworkPrediction",
            Name = "Edge Network Prediction",
            Description = "Controls Edge network prediction (prefetching).",
            Category = "Microsoft Edge",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Edge",
            ValueName = "NetworkPredictionOptions",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "EdgeAutoImportAtFirstRun",
            Name = "Edge Auto-Import at First Run",
            Description = "Controls automatic import of data from other browsers on first run.",
            Category = "Microsoft Edge",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Edge",
            ValueName = "AutoImportAtFirstRun",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "EdgeSyncDisabled",
            Name = "Edge Sync Disabled",
            Description = "Disables Microsoft Edge data synchronization.",
            Category = "Microsoft Edge",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Edge",
            ValueName = "SyncDisabled",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "EdgeResolveNavigationErrorsUseWebService",
            Name = "Edge Resolve Navigation Errors",
            Description = "Controls using a web service to resolve navigation errors.",
            Category = "Microsoft Edge",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Edge",
            ValueName = "ResolveNavigationErrorsUseWebService",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "EdgeCopilotEnabled",
            Name = "Edge Copilot Sidebar",
            Description = "Controls the Copilot sidebar in Microsoft Edge.",
            Category = "Microsoft Edge",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Edge",
            ValueName = "CopilotCDPPageContext",
            ValueKind = RegistryValueKind.DWord
        },

        //  Windows Update Extended Policies
        new PolicyEntry
        {
            Id = "DisableOSUpgrade",
            Name = "Disable OS Upgrade",
            Description = "Prevents Windows from upgrading to a new major version.",
            Category = "Windows Update",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate",
            ValueName = "DisableOSUpgrade",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DeferFeatureUpdates",
            Name = "Defer Feature Updates",
            Description = "Defers Windows feature updates for a specified number of days.",
            Category = "Windows Update",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate",
            ValueName = "DeferFeatureUpdates",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DeferQualityUpdates",
            Name = "Defer Quality Updates",
            Description = "Defers Windows quality updates for a specified number of days.",
            Category = "Windows Update",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate",
            ValueName = "DeferQualityUpdates",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "SetActiveHoursStart",
            Name = "Active Hours Start",
            Description = "Configures the start of active hours to prevent restarts.",
            Category = "Windows Update",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate",
            ValueName = "SetActiveHoursStart",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "SetActiveHoursEnd",
            Name = "Active Hours End",
            Description = "Configures the end of active hours to prevent restarts.",
            Category = "Windows Update",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate",
            ValueName = "SetActiveHoursEnd",
            ValueKind = RegistryValueKind.DWord
        },

        //  Windows Store Extended Policies
        new PolicyEntry
        {
            Id = "RemoveWindowsStore",
            Name = "Remove Windows Store Access",
            Description = "Removes access to the Windows Store application.",
            Category = "Windows Store",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\WindowsStore",
            ValueName = "RemoveWindowsStore",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisableStoreApps",
            Name = "Disable Store Apps",
            Description = "Disables all apps from the Windows Store.",
            Category = "Windows Store",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\WindowsStore",
            ValueName = "DisableStoreApps",
            ValueKind = RegistryValueKind.DWord
        },

        //  Telemetry Extended Policies
        new PolicyEntry
        {
            Id = "AllowDeviceNameInTelemetry",
            Name = "Allow Device Name in Telemetry",
            Description = "Controls whether the device name is included in telemetry data.",
            Category = "Privacy & Telemetry",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
            ValueName = "AllowDeviceNameInTelemetry",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "LimitDiagnosticLogCollection",
            Name = "Limit Diagnostic Log Collection",
            Description = "Limits the collection of diagnostic logs.",
            Category = "Privacy & Telemetry",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
            ValueName = "LimitDiagnosticLogCollection",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "LimitDumpCollection",
            Name = "Limit Crash Dump Collection",
            Description = "Limits the collection of crash dump data.",
            Category = "Privacy & Telemetry",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
            ValueName = "LimitDumpCollection",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "AllowCommercialDataPipeline",
            Name = "Allow Commercial Data Pipeline",
            Description = "Controls the Windows commercial data pipeline.",
            Category = "Privacy & Telemetry",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
            ValueName = "AllowCommercialDataPipeline",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisableEnterpriseAuthProxy",
            Name = "Disable Telemetry Auth Proxy",
            Description = "Disables authenticated proxy for Connected User Experiences.",
            Category = "Privacy & Telemetry",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
            ValueName = "DisableEnterpriseAuthProxy",
            ValueKind = RegistryValueKind.DWord
        },

        //  App Privacy Extended Policies
        new PolicyEntry
        {
            Id = "LetAppsAccessCamera",
            Name = "Let Apps Access Camera",
            Description = "Controls whether apps can access the camera.",
            Category = "App Privacy",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
            ValueName = "LetAppsAccessCamera",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "LetAppsAccessMicrophone",
            Name = "Let Apps Access Microphone",
            Description = "Controls whether apps can access the microphone.",
            Category = "App Privacy",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
            ValueName = "LetAppsAccessMicrophone",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "LetAppsAccessLocation",
            Name = "Let Apps Access Location",
            Description = "Controls whether apps can access the device location.",
            Category = "App Privacy",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
            ValueName = "LetAppsAccessLocation",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "LetAppsAccessContacts",
            Name = "Let Apps Access Contacts",
            Description = "Controls whether apps can access contacts.",
            Category = "App Privacy",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
            ValueName = "LetAppsAccessContacts",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "LetAppsAccessCalendar",
            Name = "Let Apps Access Calendar",
            Description = "Controls whether apps can access the calendar.",
            Category = "App Privacy",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
            ValueName = "LetAppsAccessCalendar",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "LetAppsAccessNotifications",
            Name = "Let Apps Access Notifications",
            Description = "Controls whether apps can access user notifications.",
            Category = "App Privacy",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
            ValueName = "LetAppsAccessNotifications",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "LetAppsAccessAccountInfo",
            Name = "Let Apps Access Account Info",
            Description = "Controls whether apps can access account information.",
            Category = "App Privacy",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
            ValueName = "LetAppsAccessAccountInfo",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "LetAppsAccessCallHistory",
            Name = "Let Apps Access Call History",
            Description = "Controls whether apps can access call history.",
            Category = "App Privacy",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
            ValueName = "LetAppsAccessCallHistory",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "LetAppsAccessEmail",
            Name = "Let Apps Access Email",
            Description = "Controls whether apps can access email.",
            Category = "App Privacy",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
            ValueName = "LetAppsAccessEmail",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "LetAppsAccessMessaging",
            Name = "Let Apps Access Messaging",
            Description = "Controls whether apps can read or send messages.",
            Category = "App Privacy",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
            ValueName = "LetAppsAccessMessaging",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "LetAppsAccessRadios",
            Name = "Let Apps Control Radios",
            Description = "Controls whether apps can control device radios.",
            Category = "App Privacy",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
            ValueName = "LetAppsAccessRadios",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "LetAppsAccessTasks",
            Name = "Let Apps Access Tasks",
            Description = "Controls whether apps can access the user's tasks.",
            Category = "App Privacy",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
            ValueName = "LetAppsAccessTasks",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "LetAppsGetDiagnosticInfo",
            Name = "Let Apps Access Diagnostics",
            Description = "Controls whether apps can access diagnostic information.",
            Category = "App Privacy",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
            ValueName = "LetAppsGetDiagnosticInfo",
            ValueKind = RegistryValueKind.DWord
        },

        //  Microsoft Teams Policies
        new PolicyEntry
        {
            Id = "TeamsPreventAutoStart",
            Name = "Prevent Teams Auto-Start",
            Description = "Prevents Microsoft Teams from starting automatically.",
            Category = "Microsoft Teams",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Teams",
            ValueName = "DisableAutoStart",
            ValueKind = RegistryValueKind.DWord
        },

        //  OneDrive Extended Policies
        new PolicyEntry
        {
            Id = "OneDrivePreventNetworkTraffic",
            Name = "Prevent OneDrive Network Traffic",
            Description = "Prevents OneDrive from generating network traffic until the user signs in.",
            Category = "OneDrive",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\OneDrive",
            ValueName = "PreventNetworkTrafficPreUserSignIn",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "OneDriveDisablePersonalSync",
            Name = "Disable OneDrive Personal Sync",
            Description = "Prevents users from syncing personal OneDrive accounts.",
            Category = "OneDrive",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\OneDrive",
            ValueName = "DisablePersonalSync",
            ValueKind = RegistryValueKind.DWord
        },

        //  BitLocker Policies
        new PolicyEntry
        {
            Id = "RequireDeviceEncryption",
            Name = "Require Device Encryption",
            Description = "Controls whether BitLocker device encryption is required.",
            Category = "BitLocker",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\FVE",
            ValueName = "RequireDeviceEncryption",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "OSRecoveryUsage",
            Name = "BitLocker OS Drive Recovery",
            Description = "Controls how BitLocker-protected OS drives are recovered.",
            Category = "BitLocker",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\FVE",
            ValueName = "OSRecovery",
            ValueKind = RegistryValueKind.DWord
        },

        //  Windows Hello / Credential Policies
        new PolicyEntry
        {
            Id = "AllowSignInOptions",
            Name = "Allow Sign-In Options",
            Description = "Controls available sign-in options (PIN, picture password, etc.).",
            Category = "Credentials",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
            ValueName = "AllowSignInOptions",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "AllowDomainPINLogon",
            Name = "Allow Domain PIN Logon",
            Description = "Controls whether domain users can sign in using a PIN.",
            Category = "Credentials",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\System",
            ValueName = "AllowDomainPINLogon",
            ValueKind = RegistryValueKind.DWord
        },

        //  Startup / Logon Policies
        new PolicyEntry
        {
            Id = "DisableStartupSound",
            Name = "Disable Startup Sound",
            Description = "Disables the Windows startup sound.",
            Category = "Startup",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
            ValueName = "DisableStartupSound",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisableLockWorkstation",
            Name = "Disable Lock Workstation",
            Description = "Prevents locking the workstation with Ctrl+Alt+Del.",
            Category = "Startup",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\System",
            ValueName = "DisableLockWorkstation",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisableTaskMgr",
            Name = "Disable Task Manager",
            Description = "Prevents access to the Task Manager.",
            Category = "Startup",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\System",
            ValueName = "DisableTaskMgr",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisableChangePassword",
            Name = "Disable Change Password",
            Description = "Prevents changing the Windows password from security screen.",
            Category = "Startup",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\System",
            ValueName = "DisableChangePassword",
            ValueKind = RegistryValueKind.DWord
        },

        //  Explorer / Shell Policies
        new PolicyEntry
        {
            Id = "NoControlPanel",
            Name = "Disable Control Panel",
            Description = "Prevents access to Control Panel and PC Settings.",
            Category = "Explorer",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
            ValueName = "NoControlPanel",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "NoRun",
            Name = "Disable Run Dialog",
            Description = "Removes the Run command from the Start Menu.",
            Category = "Explorer",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
            ValueName = "NoRun",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "DisallowCpl",
            Name = "Restrict Control Panel Items",
            Description = "Controls which Control Panel items are visible.",
            Category = "Explorer",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
            ValueName = "DisallowCpl",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "NoDrives",
            Name = "Hide Drives in My Computer",
            Description = "Hides specified drives from My Computer.",
            Category = "Explorer",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
            ValueName = "NoDrives",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "NoRecentDocsHistory",
            Name = "Disable Recent Documents History",
            Description = "Prevents Windows from tracking recently opened documents.",
            Category = "Explorer",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
            ValueName = "NoRecentDocsHistory",
            ValueKind = RegistryValueKind.DWord
        },

        //  Windows Script Host Policies
        new PolicyEntry
        {
            Id = "WSHEnabled",
            Name = "Windows Script Host Enabled",
            Description = "Controls whether Windows Script Host is enabled.",
            Category = "Security",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Microsoft\Windows Script Host\Settings",
            ValueName = "Enabled",
            ValueKind = RegistryValueKind.DWord
        },

        //  Attachment Manager Policies
        new PolicyEntry
        {
            Id = "SaveZoneInformation",
            Name = "Attachment Zone Information",
            Description = "Controls whether zone information is preserved for downloaded attachments.",
            Category = "Security",
            Hive = RegistryHive.CurrentUser,
            RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\Attachments",
            ValueName = "SaveZoneInformation",
            ValueKind = RegistryValueKind.DWord
        },

        //  Multimedia / Streaming Policies
        new PolicyEntry
        {
            Id = "DisableWindowsMediaDRM",
            Name = "Disable Windows Media DRM Internet Access",
            Description = "Prevents Windows Media DRM from accessing the internet.",
            Category = "Multimedia",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\WMDRM",
            ValueName = "DisableOnline",
            ValueKind = RegistryValueKind.DWord
        },
        new PolicyEntry
        {
            Id = "PreventCodecDownload",
            Name = "Prevent Codec Download",
            Description = "Prevents Windows Media Player from downloading codecs.",
            Category = "Multimedia",
            Hive = RegistryHive.LocalMachine,
            RegistryPath = @"SOFTWARE\Policies\Microsoft\WindowsMediaPlayer",
            ValueName = "PreventCodecDownload",
            ValueKind = RegistryValueKind.DWord
        }
    ];

    // Gets the current Windows build number.
    private static int GetWindowsBuildNumber()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key?.GetValue("CurrentBuildNumber") is string buildStr && int.TryParse(buildStr, out var build))
            {
                return build;
            }
        }
        catch
        {
            // Ignore errors
        }
        return 0;
    }

    // Gets all known policies that are applicable to the current Windows version.
    public static IReadOnlyList<PolicyEntry> GetApplicablePolicies()
    {
        var currentBuild = GetWindowsBuildNumber();
        return KnownPolicies
            .Where(p => (p.MinWindowsBuild == 0 || currentBuild >= p.MinWindowsBuild) &&
                        (p.MaxWindowsBuild == 0 || currentBuild <= p.MaxWindowsBuild))
            .ToList()
            .AsReadOnly();
    }

    // Detects the current state of all known policies.
    // A policy is considered "configured" if its registry value exists.
    public static async Task<IReadOnlyList<PolicyState>> DetectPolicyStatesAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var results = new List<PolicyState>();
            var applicablePolicies = GetApplicablePolicies();

            foreach (var policy in applicablePolicies)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var state = DetectPolicyState(policy);
                results.Add(state);
            }

            return results.AsReadOnly();
        }, cancellationToken).ConfigureAwait(false);
    }

    // Detects the current state of a single policy.
    private static PolicyState DetectPolicyState(PolicyEntry policy)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(policy.Hive,
                Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Default);
            using var subKey = baseKey.OpenSubKey(policy.RegistryPath, writable: false);

            if (subKey == null)
            {
                // Key doesn't exist - policy is not configured
                return new PolicyState
                {
                    Policy = policy,
                    IsConfigured = false,
                    CurrentValue = null,
                    ActualValueKind = null
                };
            }

            var value = subKey.GetValue(policy.ValueName);
            if (value == null)
            {
                // Value doesn't exist - policy is not configured
                return new PolicyState
                {
                    Policy = policy,
                    IsConfigured = false,
                    CurrentValue = null,
                    ActualValueKind = null
                };
            }

            // Value exists - policy is configured
            var actualKind = subKey.GetValueKind(policy.ValueName);
            return new PolicyState
            {
                Policy = policy,
                IsConfigured = true,
                CurrentValue = value,
                ActualValueKind = actualKind
            };
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error detecting policy state for {policy.Id}: {ex.Message}");
            return new PolicyState
            {
                Policy = policy,
                IsConfigured = false,
                CurrentValue = null,
                ActualValueKind = null
            };
        }
    }

    // Removes a policy override by deleting its registry value.
    // This returns the policy to "Not Configured" state.
    public static async Task<bool> RemovePolicyOverrideAsync(PolicyEntry policy)
    {
        return await Task.Run(async () =>
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(policy.Hive,
                    Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Default);
                using var subKey = baseKey.OpenSubKey(policy.RegistryPath, writable: true);

                if (subKey == null)
                {
                    // Key doesn't exist - nothing to remove
                    _ = LogHelper.Log($"Policy {policy.Id}: Registry key does not exist, nothing to remove.");
                    return true;
                }

                // Check if value exists
                if (subKey.GetValue(policy.ValueName) != null)
                {
                    subKey.DeleteValue(policy.ValueName, throwOnMissingValue: false);
                    _ = LogHelper.Log($"Policy {policy.Id}: Removed registry value {policy.ValueName}");
                }
                else
                {
                    _ = LogHelper.Log($"Policy {policy.Id}: Value does not exist, nothing to remove.");
                }

                // Clean up empty parent keys if possible (but be careful not to delete keys with other values)
                CleanupEmptyPolicyKey(policy.Hive, policy.RegistryPath);

                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                _ = LogHelper.LogError($"Access denied removing policy {policy.Id}: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Error removing policy {policy.Id}: {ex.Message}");
                return false;
            }
        }).ConfigureAwait(false);
    }

    // Removes multiple policy overrides.
    public static async Task<(int succeeded, int failed)> RemovePolicyOverridesAsync(
        IEnumerable<PolicyEntry> policies,
        IProgress<(string policyId, bool success)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var succeeded = 0;
        var failed = 0;

        foreach (var policy in policies)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var success = await RemovePolicyOverrideAsync(policy).ConfigureAwait(false);
            if (success)
                succeeded++;
            else
                failed++;

            progress?.Report((policy.Id, success));
        }

        return (succeeded, failed);
    }

    // Cleans up empty policy registry keys if they have no values or subkeys.
    private static void CleanupEmptyPolicyKey(RegistryHive hive, string path)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive,
                Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Default);
            using var subKey = baseKey.OpenSubKey(path, writable: false);

            if (subKey == null)
                return;

            // Only delete if empty (no values and no subkeys)
            if (subKey.GetValueNames().Length == 0 && subKey.GetSubKeyNames().Length == 0)
            {
                var parentPath = GetParentPath(path);
                if (!string.IsNullOrEmpty(parentPath) && parentPath.Contains("Policies", StringComparison.OrdinalIgnoreCase))
                {
                    using var parentKey = baseKey.OpenSubKey(parentPath, writable: true);
                    var keyName = Path.GetFileName(path);
                    parentKey?.DeleteSubKey(keyName, throwOnMissingSubKey: false);
                }
            }
        }
        catch
        {
            // Ignore cleanup errors - not critical
        }
    }

    private static string GetParentPath(string path)
    {
        var lastSeparator = path.LastIndexOf('\\');
        return lastSeparator > 0 ? path[..lastSeparator] : string.Empty;
    }

    // Gets all configured (changed) policies.
    public static async Task<IReadOnlyList<PolicyState>> GetConfiguredPoliciesAsync(CancellationToken cancellationToken = default)
    {
        var allStates = await DetectPolicyStatesAsync(cancellationToken).ConfigureAwait(false);
        return allStates.Where(s => s.IsConfigured).ToList().AsReadOnly();
    }

    // Gets a summary of policy states grouped by category.
    public static async Task<IReadOnlyDictionary<string, (int total, int configured)>> GetPolicySummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var allStates = await DetectPolicyStatesAsync(cancellationToken).ConfigureAwait(false);

        return allStates
            .GroupBy(s => s.Policy.Category)
            .ToDictionary(
                g => g.Key,
                g => (total: g.Count(), configured: g.Count(s => s.IsConfigured)))
            .AsReadOnly();
    }

    // Restarts Windows Explorer to apply certain policy changes.
    public static async Task RestartExplorerAsync()
    {
        try
        {
            await OptimizationOptions.StartInCmd("taskkill /F /IM explorer.exe").ConfigureAwait(false);
            await Task.Delay(500).ConfigureAwait(false);
            await OptimizationOptions.StartInCmd("start %SystemRoot%\\explorer.exe").ConfigureAwait(false);
            _ = LogHelper.Log("Explorer restarted to apply policy changes.");
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error restarting Explorer: {ex.Message}");
        }
    }
}
