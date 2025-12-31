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
                    await LogHelper.Log($"Policy {policy.Id}: Registry key does not exist, nothing to remove.");
                    return true;
                }

                // Check if value exists
                if (subKey.GetValue(policy.ValueName) != null)
                {
                    subKey.DeleteValue(policy.ValueName, throwOnMissingValue: false);
                    await LogHelper.Log($"Policy {policy.Id}: Removed registry value {policy.ValueName}");
                }
                else
                {
                    await LogHelper.Log($"Policy {policy.Id}: Value does not exist, nothing to remove.");
                }

                // Clean up empty parent keys if possible (but be careful not to delete keys with other values)
                CleanupEmptyPolicyKey(policy.Hive, policy.RegistryPath);

                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                await LogHelper.LogError($"Access denied removing policy {policy.Id}: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                await LogHelper.LogError($"Error removing policy {policy.Id}: {ex.Message}");
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
            await OptimizationOptions.StartInCmd("start explorer.exe").ConfigureAwait(false);
            await LogHelper.Log("Explorer restarted to apply policy changes.");
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"Error restarting Explorer: {ex.Message}");
        }
    }
}
