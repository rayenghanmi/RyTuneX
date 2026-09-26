using Microsoft.Win32;
using RyTuneX.Helpers;
using RyTuneX.Models;

namespace RyTuneX.Services;

public static class IntelligentOptimizationEngine
{
    private static readonly List<OptimizationItemModel> MasterCatalog = new()
    {
        // Title, Description, and ImpactDescription are already localized but present here as a fallback
        new OptimizationItemModel
        {
            Tag = "BackgroundApps",
            Title = "Disable Background Apps",
            Category = OptimizationCategory.Performance,
            Description = "Prevents Windows UWP apps from running in the background and consuming CPU and memory.",
            ScoreWeight = 9,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Frees background CPU cycles and saves 200MB+ RAM.",
            TechnicalDetails = "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications -> GlobalUserDisabled = 1"
        },
        new OptimizationItemModel
        {
            Tag = "SystemProfile",
            Title = "Optimize Gaming & System Responsiveness",
            Category = OptimizationCategory.Performance,
            Description = "Prioritizes GPU processing and network throughput for active gaming and desktop applications.",
            ScoreWeight = 10,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Reduces micro-stuttering and improves framerate consistency.",
            TechnicalDetails = "HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile -> SystemResponsiveness = 10, GPU Priority = 8"
        },
        new OptimizationItemModel
        {
            Tag = "PrioritizeForegroundApplications",
            Title = "Prioritize Foreground Applications",
            Category = OptimizationCategory.Performance,
            Description = "Allocates longer CPU time quantum slices to the active foreground application window.",
            ScoreWeight = 8,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Enhances active app responsiveness under heavy background multitasking.",
            TechnicalDetails = "HKLM\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl -> Win32PrioritySeparation = 42"
        },
        new OptimizationItemModel
        {
            Tag = "GamingMode",
            Title = "Enable Windows Gaming Mode",
            Category = OptimizationCategory.Performance,
            Description = "Configures Windows Game Mode and GPU Hardware Scheduling for ultra-low latency.",
            ScoreWeight = 8,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Locks CPU cores to gaming processes and optimizes thread scheduling.",
            TechnicalDetails = "HKCU\\Software\\Microsoft\\GameBar -> AllowAutoGameMode = 1; HwSchMode = 2"
        },
        new OptimizationItemModel
        {
            Tag = "FullscreenOptimizations",
            Title = "Enable Fullscreen Optimizations",
            Category = OptimizationCategory.Performance,
            Description = "Configures Windows display pipeline for high performance borderless presentation.",
            ScoreWeight = 6,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Minimizes display latency while retaining rapid Alt+Tab switching.",
            TechnicalDetails = "HKCU\\System\\GameConfigStore -> GameDVR_FSEBehaviorMode = 2"
        },
        new OptimizationItemModel
        {
            Tag = "GpuDriverTweaks",
            Title = "Apply GPU Driver Tweaks",
            Category = OptimizationCategory.Performance,
            Description = "Optimizes GPU hardware driver power states and disables deep sleep throttling on graphics cards.",
            ScoreWeight = 8,
            Risk = RiskLevel.Moderate,
            ImpactDescription = "Prevents GPU power clock fluctuations during gaming and 3D rendering.",
            TechnicalDetails = "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e968...} -> ULPS = 0, PowerGating = 1"
        },
        new OptimizationItemModel
        {
            Tag = "MenuShowDelay",
            Title = "Instant Menu Response Time",
            Category = OptimizationCategory.Performance,
            Description = "Eliminates the artificial 400ms Windows delay when expanding context menus and sub-menus.",
            ScoreWeight = 5,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Instantaneous UI navigation feel across File Explorer and desktop.",
            TechnicalDetails = "HKCU\\Control Panel\\Desktop -> MenuShowDelay = 0"
        },
        new OptimizationItemModel
        {
            Tag = "MouseHoverTime",
            Title = "Instant Mouse Hover Time",
            Category = OptimizationCategory.Performance,
            Description = "Reduces mouse hover tooltip and thumbnail delay from 400ms to 0ms.",
            ScoreWeight = 4,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Immediate tooltip and taskbar thumbnail previews.",
            TechnicalDetails = "HKCU\\Control Panel\\Mouse -> MouseHoverTime = 0"
        },
        new OptimizationItemModel
        {
            Tag = "KeyboardLatency",
            Title = "Optimize Keyboard Latency",
            Category = OptimizationCategory.Performance,
            Description = "Maximizes keyboard repeat speed and minimizes initial key repeat delay.",
            ScoreWeight = 5,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Instantaneous key input recognition and faster character repeat.",
            TechnicalDetails = "HKCU\\Control Panel\\Keyboard -> KeyboardDelay = 0, KeyboardSpeed = 31"
        },
        new OptimizationItemModel
        {
            Tag = "MouseAcceleration",
            Title = "Disable Mouse Acceleration (Raw Input)",
            Category = OptimizationCategory.Performance,
            Description = "Disables Windows mouse curve acceleration for pure 1:1 hardware cursor precision.",
            ScoreWeight = 6,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Flawless cursor tracking consistency for gaming and precise design work.",
            TechnicalDetails = "HKCU\\Control Panel\\Mouse -> MouseSpeed = 0, MouseThreshold1 = 0, MouseThreshold2 = 0"
        },
        new OptimizationItemModel
        {
            Tag = "OptimizeNTFS",
            Title = "Optimize NTFS File System Zone",
            Category = OptimizationCategory.Performance,
            Description = "Expands the Master File Table (MFT) reserved buffer to prevent file system fragmentation.",
            ScoreWeight = 6,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Accelerates file directory indexing and disk metadata lookups.",
            TechnicalDetails = "HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem -> NtfsMftZoneReservation = 2"
        },
        new OptimizationItemModel
        {
            Tag = "ServiceHostSplitting",
            Title = "Optimize Service Host Memory Threshold",
            Category = OptimizationCategory.Performance,
            Description = "Groups system svchost.exe processes cleanly according to installed physical RAM.",
            ScoreWeight = 7,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Reduces total process count overhead by 30-50 processes on systems with 8GB+ RAM.",
            TechnicalDetails = "HKLM\\SYSTEM\\CurrentControlSet\\Control -> SvcHostSplitThresholdInKB based on RAM"
        },
        new OptimizationItemModel
        {
            Tag = "UsbPowerSaving",
            Title = "Disable USB Selective Suspend",
            Category = OptimizationCategory.Performance,
            Description = "Prevents Windows from powering down USB controllers and peripherals into low-power states.",
            ScoreWeight = 5,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Prevents USB audio dropouts, controller lag, and mouse disconnects.",
            TechnicalDetails = "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power -> DisableUsbPowerSaving"
        },
        new OptimizationItemModel
        {
            Tag = "PowerThrottling",
            Title = "Disable Power Throttling",
            Category = OptimizationCategory.Performance,
            Description = "Disables Windows power throttling algorithm on background execution threads.",
            ScoreWeight = 7,
            Risk = RiskLevel.Moderate,
            ImpactDescription = "Ensures background renders and compile jobs run at full CPU clock speeds.",
            TechnicalDetails = "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerThrottling -> PowerThrottlingOff = 1"
        },
        new OptimizationItemModel
        {
            Tag = "LowDiskSpaceChecks",
            Title = "Disable Low Disk Space Checks",
            Category = OptimizationCategory.Performance,
            Description = "Stops Windows from constantly polling disk drives for low space warnings.",
            ScoreWeight = 4,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Stops recurring disk polling interrupts.",
            TechnicalDetails = "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer -> NoLowDiskSpaceChecks = 0"
        },
        new OptimizationItemModel
        {
            Tag = "LinkResolve",
            Title = "Disable Link Resolve Tracking",
            Category = OptimizationCategory.Performance,
            Description = "Disables Windows shortcut resolution search across networks and slow volumes.",
            ScoreWeight = 4,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Speeds up broken shortcut opening without freezing Explorer.",
            TechnicalDetails = "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer -> LinkResolveIgnoreLinkInfo = 1"
        },
        new OptimizationItemModel
        {
            Tag = "AutoComplete",
            Title = "Disable Explorer AutoComplete Suggestions",
            Category = OptimizationCategory.Performance,
            Description = "Disables automatic suggestion query popups in Explorer address bars.",
            ScoreWeight = 3,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Faster address bar path navigation.",
            TechnicalDetails = "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\AutoComplete -> AutoSuggest = no"
        },
        new OptimizationItemModel
        {
            Tag = "TelemetryServices",
            Title = "Disable Windows Telemetry & Diagnostics",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Completely disables DiagTrack and dmwappushservice background diagnostic collection.",
            ScoreWeight = 10,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Stops automated background tracking, keystroke logging, and telemetry uploads.",
            TechnicalDetails = "Services DiagTrack & dmwappushservice disabled; AllowTelemetry = 0"
        },
        new OptimizationItemModel
        {
            Tag = "WindowsAI",
            Title = "Disable Windows AI Features & Mining",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Blocks Windows background AI analysis, Notepad/Paint AI integrations, and model query reporting.",
            ScoreWeight = 10,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Protects local file privacy and stops continuous cloud AI scraping.",
            TechnicalDetails = "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI -> DisableAIDataAnalysis = 1"
        },
        new OptimizationItemModel
        {
            Tag = "WindowsRecall",
            Title = "Disable Windows Recall Screen Snapshotting",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Blocks periodic continuous screenshotting of active desktops and OCR indexing.",
            ScoreWeight = 10,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Guarantees sensitive banking, passwords, and private documents are never recorded.",
            TechnicalDetails = "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI -> DisableRecall = 1, TurnOffSavingSnapshots = 1"
        },
        new OptimizationItemModel
        {
            Tag = "CoPilotAI",
            Title = "Disable Copilot AI Assistant",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Disables Windows Copilot sidebar, taskbar button, and continuous cloud sync queries.",
            ScoreWeight = 8,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Frees taskbar space and eliminates background web view processes.",
            TechnicalDetails = "HKCU\\Software\\Policies\\Microsoft\\Windows\\WindowsCopilot -> TurnOffWindowsCopilot = 1"
        },
        new OptimizationItemModel
        {
            Tag = "AdvertisingID",
            Title = "Disable Windows Advertising ID",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Prevents Windows Store applications from tracking user activity for personalized advertising.",
            ScoreWeight = 6,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Stops unique advertising identifier profile tracking.",
            TechnicalDetails = "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo -> Enabled = 0"
        },
        new OptimizationItemModel
        {
            Tag = "BluetoothAdvertising",
            Title = "Disable Bluetooth Advertising Beacons",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Prevents Windows from broadcasting Bluetooth advertising beacons to nearby hardware.",
            ScoreWeight = 5,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Enhances wireless device privacy in public locations.",
            TechnicalDetails = "HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\current\\device\\Bluetooth -> AllowAdvertising = 0"
        },
        new OptimizationItemModel
        {
            Tag = "SpotlightFeatures",
            Title = "Disable Windows Spotlight Suggestions",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Disables promotional suggestions and background cloud image downloads on lockscreen.",
            ScoreWeight = 5,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Stops lock screen advertisements and background download traffic.",
            TechnicalDetails = "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager -> DisableWindowsSpotlightFeatures = 1"
        },
        new OptimizationItemModel
        {
            Tag = "TailoredExperiences",
            Title = "Disable Tailored Diagnostic Experiences",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Prevents Microsoft from using device diagnostic logs to target personalized tips and ads.",
            ScoreWeight = 5,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Eliminates tailored promotional popup notifications.",
            TechnicalDetails = "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Privacy -> TailoredExperiencesWithDiagnosticDataEnabled = 0"
        },
        new OptimizationItemModel
        {
            Tag = "CloudOptimizedContent",
            Title = "Disable Cloud Optimized Content",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Disables cloud-delivered promotional tiles and search widgets.",
            ScoreWeight = 5,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Cleans up Windows interface and eliminates cloud promotional fetches.",
            TechnicalDetails = "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent -> DisableCloudOptimizedContent = 1"
        },
        new OptimizationItemModel
        {
            Tag = "FeedbackNotifications",
            Title = "Disable Feedback Survey Notifications",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Stops Windows from displaying recurring feedback requests and rating popups.",
            ScoreWeight = 4,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Eliminates intrusive feedback toasts.",
            TechnicalDetails = "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection -> DoNotShowFeedbackNotifications = 1"
        },
        new OptimizationItemModel
        {
            Tag = "AutomaticRestartSignOn",
            Title = "Disable Automatic Restart Sign-On (ARSO)",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Prevents Windows from keeping user credentials in memory to auto-logon after updates.",
            ScoreWeight = 6,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Hardens logon security by requiring direct physical user authentication.",
            TechnicalDetails = "HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System -> DisableAutomaticRestartSignOn = 1"
        },
        new OptimizationItemModel
        {
            Tag = "HandwritingDataSharing",
            Title = "Disable Handwriting Data Sharing",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Prevents digital pen and handwriting recognition samples from uploading to Microsoft.",
            ScoreWeight = 5,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Protects handwritten notes and stylus sketches.",
            TechnicalDetails = "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\TabletPc -> PreventHandwritingDataSharing = 1"
        },
        new OptimizationItemModel
        {
            Tag = "TextInputDataCollection",
            Title = "Disable Text Input & Linguistic Data Collection",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Stops Windows from harvesting typed text samples and dictionary telemetry.",
            ScoreWeight = 7,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Keeps keystrokes and typing patterns confidential.",
            TechnicalDetails = "HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\TextInput -> AllowLinguisticDataCollection = 0"
        },
        new OptimizationItemModel
        {
            Tag = "InputPersonalization",
            Title = "Disable Inking & Typing Personalization",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Disables cloud synchronization of custom user dictionaries and phrase data.",
            ScoreWeight = 6,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Dictionary and speech models remain local to the machine.",
            TechnicalDetails = "HKLM\\SOFTWARE\\Policies\\Microsoft\\InputPersonalization -> AllowInputPersonalization = 0"
        },
        new OptimizationItemModel
        {
            Tag = "SafeSearchMode",
            Title = "Disable SafeSearch Query Interception",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Disables Microsoft cloud search filtering and logging for local search queries.",
            ScoreWeight = 5,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Stops local search terms from sending to Bing search endpoints.",
            TechnicalDetails = "HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\SearchSettings -> SafeSearchMode = 0"
        },
        new OptimizationItemModel
        {
            Tag = "ActivityUploads",
            Title = "Disable Activity History Cloud Sync",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Prevents uploading user app launch history and browsing history to Microsoft cloud.",
            ScoreWeight = 7,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Device usage history is never broadcast across linked cloud accounts.",
            TechnicalDetails = "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System -> UploadUserActivities = 0"
        },
        new OptimizationItemModel
        {
            Tag = "ClipboardSync",
            Title = "Disable Cross-Device Clipboard Cloud Sync",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Stops clipboard copy buffer from automatically uploading to cloud servers.",
            ScoreWeight = 7,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Protects sensitive copied text, passwords, and tokens from cloud exposure.",
            TechnicalDetails = "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System -> AllowCrossDeviceClipboard = 0"
        },
        new OptimizationItemModel
        {
            Tag = "MessageSync",
            Title = "Disable Messaging Cloud Sync",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Disables Windows SMS/text message synchronization to Microsoft cloud servers.",
            ScoreWeight = 6,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Messages stay strictly local on device.",
            TechnicalDetails = "HKLM\\Software\\Policies\\Microsoft\\Windows\\Messaging -> AllowMessageSync = 0"
        },
        new OptimizationItemModel
        {
            Tag = "SettingSync",
            Title = "Disable Windows Settings Cloud Sync",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Stops desktop wallpapers, credentials, and app preferences from syncing to OneDrive.",
            ScoreWeight = 5,
            Risk = RiskLevel.Moderate,
            ImpactDescription = "Keeps OS configuration isolated to this local PC only.",
            TechnicalDetails = "HKLM\\Software\\Policies\\Microsoft\\Windows\\SettingSync -> DisableCredentialsSettingSync = 2"
        },
        new OptimizationItemModel
        {
            Tag = "VoiceActivation",
            Title = "Disable Voice Activation When Locked",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Blocks applications from listening to microphones while the computer screen is locked.",
            ScoreWeight = 6,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Prevents unauthorized audio eavesdropping on locked workstations.",
            TechnicalDetails = "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy -> LetAppsActivateWithVoice = 2"
        },
        new OptimizationItemModel
        {
            Tag = "FindMyDevice",
            Title = "Disable Find My Device Location Beaconing",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Disables continuous background GPS/Wi-Fi location triangulation and cloud reporting.",
            ScoreWeight = 6,
            Risk = RiskLevel.Moderate,
            ImpactDescription = "Stops constant geo-location tracking and saves battery/network bandwidth.",
            TechnicalDetails = "HKLM\\SOFTWARE\\Policies\\Microsoft\\FindMyDevice -> AllowFindMyDevice = 0"
        },
        new OptimizationItemModel
        {
            Tag = "ActivityFeed",
            Title = "Disable Activity Feed & Timeline",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Disables local timeline tracking of opened files, tabs, and documents.",
            ScoreWeight = 6,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Stops persistent forensic logging of opened files.",
            TechnicalDetails = "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System -> EnableActivityFeed = 0"
        },
        new OptimizationItemModel
        {
            Tag = "Cdp",
            Title = "Disable Connected Devices Platform (CDP)",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Disables cross-device background sync tasks and telemetry discovery broadcast.",
            ScoreWeight = 6,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Frees background CPU cycles and closes local network discovery ports.",
            TechnicalDetails = "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System -> EnableCdp = 0"
        },
        new OptimizationItemModel
        {
            Tag = "DiagnosticsToast",
            Title = "Disable Diagnostic Prompt Toasts",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Suppresses diagnostic data collection prompts and popup banners.",
            ScoreWeight = 4,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Cleaner uninterrupted notifications.",
            TechnicalDetails = "HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Diagnostics\\DiagTrack -> ShowedToastAtLevel = 1"
        },
        new OptimizationItemModel
        {
            Tag = "OnlineSpeechPrivacy",
            Title = "Disable Online Speech Recognition",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Disables cloud transmission of voice dictation and speech samples to Microsoft servers.",
            ScoreWeight = 7,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Voice samples are never transmitted over the internet.",
            TechnicalDetails = "HKCU\\Software\\Microsoft\\Speech_OneCore\\Settings\\OnlineSpeechPrivacy -> HasAccepted = 0"
        },
        new OptimizationItemModel
        {
            Tag = "LocationAccess",
            Title = "Disable Location Access & Sensors",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Disables Windows location provider service and location sensor APIs.",
            ScoreWeight = 7,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Prevents web browsers and desktop apps from pinpointing physical coordinates.",
            TechnicalDetails = "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\LocationAndSensors -> DisableLocation = 1"
        },
        new OptimizationItemModel
        {
            Tag = "NvidiaTelemetry",
            Title = "Disable NVIDIA Telemetry Container",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Disables NVIDIA driver background telemetry tasks and tracking services.",
            ScoreWeight = 7,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Stops GPU analytics uploads and frees 100MB+ background RAM.",
            TechnicalDetails = "Service NvTelemetryContainer -> Start = Disabled"
        },
        new OptimizationItemModel
        {
            Tag = "EdgeTelemetry",
            Title = "Disable Microsoft Edge Telemetry",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Disables Edge browser diagnostics, crash reporting, and startup background boost.",
            ScoreWeight = 7,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Prevents Edge from keeping persistent background processes alive.",
            TechnicalDetails = "HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge -> MetricsReportingEnabled = 0, StartupBoostEnabled = 0"
        },
        new OptimizationItemModel
        {
            Tag = "ChromeTelemetry",
            Title = "Disable Google Chrome Telemetry",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Disables Chrome metrics reporting, cleanup tool tracking, and device diagnostics.",
            ScoreWeight = 6,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Stops Chrome browser background telemetry.",
            TechnicalDetails = "HKLM\\SOFTWARE\\Policies\\Google\\Chrome -> MetricsReportingEnabled = 0"
        },
        new OptimizationItemModel
        {
            Tag = "FirefoxTelemetry",
            Title = "Disable Mozilla Firefox Telemetry",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Disables Firefox usage metrics and default browser background agent reporting.",
            ScoreWeight = 6,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Stops Firefox usage data collection.",
            TechnicalDetails = "HKLM\\SOFTWARE\\Policies\\Mozilla\\Firefox -> DisableTelemetry = 1"
        },
        new OptimizationItemModel
        {
            Tag = "VisualStudioTelemetry",
            Title = "Disable Visual Studio Telemetry",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Disables Microsoft Visual Studio usage data collection and standard collector service.",
            ScoreWeight = 6,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Stops IDE background data logging and frees collector memory.",
            TechnicalDetails = "HKCU\\Software\\Microsoft\\VisualStudio\\Telemetry -> TurnOffSwitch = 1"
        },
        new OptimizationItemModel
        {
            Tag = "Cortana",
            Title = "Disable Cortana Virtual Assistant",
            Category = OptimizationCategory.SystemResourcesAndBloat,
            Description = "Completely disables legacy Cortana background processes and auto-start services.",
            ScoreWeight = 8,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Frees 150MB+ RAM and eliminates unused assistant processes.",
            TechnicalDetails = "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search -> AllowCortana = 0"
        },
        new OptimizationItemModel
        {
            Tag = "OneDrive",
            Title = "Disable OneDrive Auto-Sync & Background Startup",
            Category = OptimizationCategory.SystemResourcesAndBloat,
            Description = "Stops OneDrive cloud sync engine from continuously running in the background.",
            ScoreWeight = 7,
            Risk = RiskLevel.Moderate,
            ImpactDescription = "Saves disk I/O, CPU cycles, and network bandwidth (Recommended if not using OneDrive).",
            TechnicalDetails = "HKLM\\SOFTWARE\\Policies\\Microsoft\\OneDrive -> DisableFileSyncNGSC = 1"
        },
        new OptimizationItemModel
        {
            Tag = "Widgets",
            Title = "Disable Windows 11 Widgets Board",
            Category = OptimizationCategory.SystemResourcesAndBloat,
            Description = "Disables the taskbar Widgets feed and background Edge WebView processes.",
            ScoreWeight = 9,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Frees 300MB+ RAM and stops constant Edge background rendering.",
            TechnicalDetails = "HKLM\\SOFTWARE\\Policies\\Microsoft\\Dsh -> AllowNewsAndInterests = 0"
        },
        new OptimizationItemModel
        {
            Tag = "NewsAndInterests",
            Title = "Disable News & Taskbar Feed",
            Category = OptimizationCategory.SystemResourcesAndBloat,
            Description = "Turns off the taskbar news feed widget on Windows 10/11 taskbars.",
            ScoreWeight = 6,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Frees taskbar space and network bandwidth.",
            TechnicalDetails = "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Feeds -> EnableFeeds = 0"
        },
        new OptimizationItemModel
        {
            Tag = "SysMain",
            Title = "Optimize SysMain (Superfetch)",
            Category = OptimizationCategory.SystemResourcesAndBloat,
            Description = "Adjusts or disables SysMain service to stop unnecessary preloading on SSDs.",
            ScoreWeight = 8,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Eliminates high background disk usage spikes and unnecessary SSD write cycles.",
            TechnicalDetails = "Service SysMain Start = Disabled; EnableSuperfetch = 0"
        },
        new OptimizationItemModel
        {
            Tag = "TaskTimeouts",
            Title = "Optimize Application Termination Timeouts",
            Category = OptimizationCategory.SystemResourcesAndBloat,
            Description = "Reduces shutdown wait time for hanging applications from 20 seconds to 2 seconds.",
            ScoreWeight = 6,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Dramatically speeds up PC shutdown and restart speed.",
            TechnicalDetails = "HKCU\\Control Panel\\Desktop -> AutoEndTasks = 1, WaitToKillAppTimeout = 2000"
        },
        new OptimizationItemModel
        {
            Tag = "ServiceTimeouts",
            Title = "Optimize Service Termination Timeouts",
            Category = OptimizationCategory.SystemResourcesAndBloat,
            Description = "Reduces service stop timeout from 20 seconds to 2 seconds during reboot.",
            ScoreWeight = 5,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Faster system reboot without hanging on sluggish background services.",
            TechnicalDetails = "HKLM\\SYSTEM\\CurrentControlSet\\Control -> WaitToKillServiceTimeout = 2000"
        },
        new OptimizationItemModel
        {
            Tag = "StoreUpdates",
            Title = "Disable Automatic Store Silent App Installs",
            Category = OptimizationCategory.SystemResourcesAndBloat,
            Description = "Prevents Windows Store from silently downloading sponsored games and promotional apps.",
            ScoreWeight = 7,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Stops Windows from auto-installing Candy Crush, TikTok, and other sponsored bloat.",
            TechnicalDetails = "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager -> SilentInstalledAppsEnabled = 0"
        },
        new OptimizationItemModel
        {
            Tag = "Hibernation",
            Title = "Disable Windows Hibernation",
            Category = OptimizationCategory.SystemResourcesAndBloat,
            Description = "Disables hiberfil.sys and releases gigabytes of SSD storage matching physical RAM size.",
            ScoreWeight = 7,
            Risk = RiskLevel.Moderate,
            ImpactDescription = "Instantly frees 8GB to 64GB of SSD storage capacity.",
            TechnicalDetails = "HKLM\\System\\CurrentControlSet\\Control\\Power -> PlatformAoAcOverride = 0"
        },
        new OptimizationItemModel
        {
            Tag = "PrintService",
            Title = "Disable Print Spooler Service",
            Category = OptimizationCategory.SystemResourcesAndBloat,
            Description = "Disables the Windows Print Spooler background service (Spooler).",
            ScoreWeight = 5,
            Risk = RiskLevel.Caution,
            ImpactDescription = "Frees RAM and mitigates PrintNightmare vulnerabilities (Caution: Disable only if you have no printer).",
            TechnicalDetails = "Service Spooler -> Start = Disabled"
        },
        new OptimizationItemModel
        {
            Tag = "HomeGroup",
            Title = "Disable Legacy HomeGroup Services",
            Category = OptimizationCategory.SystemResourcesAndBloat,
            Description = "Disables discontinued HomeGroup background listener and provider services.",
            ScoreWeight = 5,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Eliminates legacy dead service overhead.",
            TechnicalDetails = "Services HomeGroupListener & HomeGroupProvider disabled"
        },
        new OptimizationItemModel
        {
            Tag = "FaxService",
            Title = "Disable Windows Fax Service",
            Category = OptimizationCategory.SystemResourcesAndBloat,
            Description = "Disables ancient fax modem background service.",
            ScoreWeight = 4,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Stops unused legacy service.",
            TechnicalDetails = "Service Fax -> Start = Disabled"
        },
        new OptimizationItemModel
        {
            Tag = "RemoteRegistry",
            Title = "Disable Remote Registry Service",
            Category = OptimizationCategory.SecurityAndPolicies,
            Description = "Blocks remote users and network machines from editing your local Windows Registry.",
            ScoreWeight = 9,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Closes major network vulnerability exploited by malware and ransomware.",
            TechnicalDetails = "Service RemoteRegistry Start = Disabled"
        },
        new OptimizationItemModel
        {
            Tag = "RemoteAssistance",
            Title = "Disable Windows Remote Assistance",
            Category = OptimizationCategory.SecurityAndPolicies,
            Description = "Prevents unauthorized remote desktop help sessions and unsolicited incoming connections.",
            ScoreWeight = 8,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Hardens machine against social engineering and remote takeovers.",
            TechnicalDetails = "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Remote Assistance -> fAllowToGetHelp = 0"
        },
        new OptimizationItemModel
        {
            Tag = "SMBv1",
            Title = "Disable Legacy SMBv1 Network Protocol",
            Category = OptimizationCategory.SecurityAndPolicies,
            Description = "Disables insecure 30-year-old SMBv1 file sharing protocol (EternalBlue / WannaCry attack vector).",
            ScoreWeight = 10,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Critical security hardening against network worm exploits.",
            TechnicalDetails = "HKLM\\SYSTEM\\CurrentControlSet\\Services\\LanmanServer\\Parameters -> SMB1 = 0"
        },
        new OptimizationItemModel
        {
            Tag = "WPBT",
            Title = "Disable Windows Platform Binary Table (WPBT)",
            Category = OptimizationCategory.SecurityAndPolicies,
            Description = "Blocks motherboard BIOS firmware from forcibly injecting executables and OEM bloat into Windows.",
            ScoreWeight = 9,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Prevents rootkit-level OEM bloatware persistence.",
            TechnicalDetails = "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager -> DisableWpbtExecution = 1"
        },
        new OptimizationItemModel
        {
            Tag = "CrashDump",
            Title = "Disable Automatic Memory Crash Dumps",
            Category = OptimizationCategory.SecurityAndPolicies,
            Description = "Prevents Windows from dumping full RAM contents onto disk during BSOD crashes.",
            ScoreWeight = 5,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Protects confidential memory data and saves several gigabytes of SSD space.",
            TechnicalDetails = "HKLM\\SYSTEM\\CurrentControlSet\\Control\\CrashControl -> CrashDumpEnabled = 3"
        },
        new OptimizationItemModel
        {
            Tag = "VBS",
            Title = "Optimize Virtualization-Based Security (VBS)",
            Category = OptimizationCategory.SecurityAndPolicies,
            Description = "Configures Virtualization-Based Security and Memory Integrity.",
            ScoreWeight = 8,
            Risk = RiskLevel.Advanced,
            ImpactDescription = "Provides 5-15% gaming CPU boost on older architectures (Power User / Advanced).",
            TechnicalDetails = "HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard -> EnableVirtualizationBasedSecurity = 0"
        },
        new OptimizationItemModel
        {
            Tag = "SystemRestore",
            Title = "Disable System Restore",
            Category = OptimizationCategory.SecurityAndPolicies,
            Description = "Turns off automated Volume Shadow Copy system restore checkpoints.",
            ScoreWeight = 5,
            Risk = RiskLevel.Caution,
            ImpactDescription = "Saves disk writes and SSD storage (Caution: Recommended to leave ON unless managing your own backups).",
            TechnicalDetails = "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\SystemRestore -> DisableSR = 1"
        },
        new OptimizationItemModel
        {
            Tag = "SmartScreen",
            Title = "Disable Windows Defender SmartScreen",
            Category = OptimizationCategory.SecurityAndPolicies,
            Description = "Disables SmartScreen file and website reputation checks.",
            ScoreWeight = 5,
            Risk = RiskLevel.Caution,
            ImpactDescription = "Prevents false-positive blocking of custom tools (Caution: Only recommended for advanced power users).",
            TechnicalDetails = "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System -> EnableSmartScreen = 0"
        },
        new OptimizationItemModel
        {
            Tag = "ClassicContextMenu",
            Title = "Restore Classic Windows 10 Right-Click Menu",
            Category = OptimizationCategory.FeaturesAndUsability,
            Description = "Restores the full instant right-click context menu in Windows 11 without clicking 'Show more options'.",
            ScoreWeight = 8,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Saves 1 click on every right-click action in Explorer.",
            TechnicalDetails = "HKCU\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\\InprocServer32"
        },
        new OptimizationItemModel
        {
            Tag = "EndTask",
            Title = "Enable 'End Task' on Taskbar App Icons",
            Category = OptimizationCategory.FeaturesAndUsability,
            Description = "Adds direct right-click 'End Task' action to taskbar items for instant process termination.",
            ScoreWeight = 7,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Instantly terminate frozen or hung programs without opening Task Manager.",
            TechnicalDetails = "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\TaskbarDeveloperSettings -> TaskbarEndTask = 1"
        },
        new OptimizationItemModel
        {
            Tag = "FileExtensionsAndHiddenFiles",
            Title = "Show File Extensions & System Files",
            Category = OptimizationCategory.FeaturesAndUsability,
            Description = "Reveals file extensions (.exe, .bat, .dll) and system hidden files in Explorer.",
            ScoreWeight = 7,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Protects against spoofed double-extension malware (.pdf.exe).",
            TechnicalDetails = "HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced -> HideFileExt = 0"
        },
        new OptimizationItemModel
        {
            Tag = "RecommendedSectionStartMenu",
            Title = "Hide Recommended Section in Start Menu",
            Category = OptimizationCategory.FeaturesAndUsability,
            Description = "Removes the recent files and recommended pane in the Windows 11 Start Menu.",
            ScoreWeight = 6,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Cleaner, uncluttered Start Menu with more room for pinned apps.",
            TechnicalDetails = "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer -> HideRecommendedSection = 1"
        },
        new OptimizationItemModel
        {
            Tag = "WindowShake",
            Title = "Disable Aero Shake Window Minimization",
            Category = OptimizationCategory.FeaturesAndUsability,
            Description = "Prevents accidental minimization of all open windows when shaking a titlebar.",
            ScoreWeight = 5,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Eliminates frustrating accidental window hiding.",
            TechnicalDetails = "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced -> DisallowShaking = 1"
        },
        new OptimizationItemModel
        {
            Tag = "CopyMoveContextMenu",
            Title = "Add 'Copy To' & 'Move To' Context Menu Items",
            Category = OptimizationCategory.FeaturesAndUsability,
            Description = "Adds instant folder destination pickers to right-click menus.",
            ScoreWeight = 5,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Faster file management in File Explorer.",
            TechnicalDetails = "Adds Copy To and Move To ContextMenuHandlers"
        },
        new OptimizationItemModel
        {
            Tag = "TaskbarToLeft",
            Title = "Align Taskbar Icons to the Left",
            Category = OptimizationCategory.FeaturesAndUsability,
            Description = "Moves Windows 11 Start button and taskbar icons to the classic bottom-left position.",
            ScoreWeight = 5,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Classic muscle memory alignment.",
            TechnicalDetails = "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced -> TaskbarAl = 0"
        },
        new OptimizationItemModel
        {
            Tag = "FilesCompactMode",
            Title = "Enable Compact View in File Explorer",
            Category = OptimizationCategory.FeaturesAndUsability,
            Description = "Reduces vertical padding between rows in File Explorer for higher file density.",
            ScoreWeight = 5,
            Risk = RiskLevel.Safe,
            ImpactDescription = "View more files per screen without scrolling.",
            TechnicalDetails = "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced -> UseCompactMode = 1"
        },
        new OptimizationItemModel
        {
            Tag = "StickyKeys",
            Title = "Disable Sticky Keys Prompt",
            Category = OptimizationCategory.FeaturesAndUsability,
            Description = "Disables the popup dialog when pressing Shift 5 times in games.",
            ScoreWeight = 6,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Stops gaming interruptions from Shift key tapping.",
            TechnicalDetails = "HKCU\\Control Panel\\Accessibility\\StickyKeys -> Flags = 506"
        },
        new OptimizationItemModel
        {
            Tag = "CloudClipboard",
            Title = "Enable Enhanced Clipboard History",
            Category = OptimizationCategory.FeaturesAndUsability,
            Description = "Configures local Win+V clipboard history without cloud sync.",
            ScoreWeight = 5,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Multi-item clipboard history accessible with Win+V.",
            TechnicalDetails = "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System -> AllowClipboardHistory = 1"
        },
        new OptimizationItemModel
        {
            Tag = "EdgeDiscoverBar",
            Title = "Disable Edge Discover Sidebar & Widgets",
            Category = OptimizationCategory.FeaturesAndUsability,
            Description = "Disables the Edge browser sidebar button and floating search widget.",
            ScoreWeight = 6,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Removes clutter in Edge browser.",
            TechnicalDetails = "HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge -> HubsSidebarEnabled = 0"
        },
        // Timer Resolution & Latency
        new OptimizationItemModel
        {
            Tag = "GlobalTimerResolution",
            Title = "Enable Global Timer Resolution",
            Category = OptimizationCategory.Performance,
            Description = "Allows applications to request higher timer resolution globally, reducing scheduling latency.",
            ScoreWeight = 6,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Improves frame pacing consistency and reduces micro-stuttering in games.",
            TechnicalDetails = "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel -> GlobalTimerResolutionRequests = 1"
        },
        new OptimizationItemModel
        {
            Tag = "HPET",
            Title = "Disable HPET (High Precision Event Timer)",
            Category = OptimizationCategory.Performance,
            Description = "Disables the HPET hardware timer and switches to TSC for lower latency timing.",
            ScoreWeight = 5,
            Risk = RiskLevel.Moderate,
            ImpactDescription = "May reduce DPC latency on some systems; requires reboot.",
            TechnicalDetails = "bcdedit /deletevalue useplatformclock; bcdedit /set useplatformtick yes"
        },
        new OptimizationItemModel
        {
            Tag = "DynamicTick",
            Title = "Disable Dynamic Tick",
            Category = OptimizationCategory.Performance,
            Description = "Forces the system timer to fire at a constant rate instead of dynamically adjusting, reducing jitter.",
            ScoreWeight = 5,
            Risk = RiskLevel.Moderate,
            ImpactDescription = "Provides more consistent timer interrupts for latency-sensitive workloads; may increase power usage.",
            TechnicalDetails = "bcdedit /set disabledynamictick yes"
        },
        // Memory Optimization
        new OptimizationItemModel
        {
            Tag = "Prefetch",
            Title = "Disable Prefetch & Superfetch",
            Category = OptimizationCategory.Performance,
            Description = "Disables Windows Prefetcher and Superfetch preloading to free RAM and reduce disk I/O on SSD systems.",
            ScoreWeight = 7,
            Risk = RiskLevel.Moderate,
            ImpactDescription = "Frees RAM used by preloaded data; best for SSD users where prefetch provides minimal benefit.",
            TechnicalDetails = "HKLM\\...\\PrefetchParameters -> EnablePrefetcher = 0, EnableSuperfetch = 0"
        },
        new OptimizationItemModel
        {
            Tag = "LargeSystemCache",
            Title = "Enable Large System Cache",
            Category = OptimizationCategory.Performance,
            Description = "Allocates more RAM for file system caching, improving disk read performance for file-heavy workloads.",
            ScoreWeight = 5,
            Risk = RiskLevel.Moderate,
            ImpactDescription = "Boosts file I/O throughput; may reduce available RAM for applications on systems with limited memory.",
            TechnicalDetails = "HKLM\\...\\Memory Management -> LargeSystemCache = 1"
        },
        new OptimizationItemModel
        {
            Tag = "NDU",
            Title = "Disable Network Data Usage Monitor (NDU)",
            Category = OptimizationCategory.Performance,
            Description = "Disables the Windows NDU service known to cause non-paged pool memory leaks on some systems.",
            ScoreWeight = 7,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Prevents gradual memory leak that can consume GBs of non-paged pool over uptime.",
            TechnicalDetails = "HKLM\\SYSTEM\\CurrentControlSet\\Services\\Ndu -> Start = 4"
        },
        new OptimizationItemModel
        {
            Tag = "PageFileEncryption",
            Title = "Disable Page File Encryption",
            Category = OptimizationCategory.Performance,
            Description = "Disables encryption of the Windows page file to reduce CPU overhead during paging operations.",
            ScoreWeight = 4,
            Risk = RiskLevel.Moderate,
            ImpactDescription = "Reduces CPU usage during memory paging; slightly reduces security of swapped data.",
            TechnicalDetails = "fsutil behavior set encryptpagingfile 0"
        },
        // Notification Controls
        new OptimizationItemModel
        {
            Tag = "LockScreenNotifications",
            Title = "Disable Lock Screen Notifications",
            Category = OptimizationCategory.FeaturesAndUsability,
            Description = "Prevents toast notifications from appearing on the lock screen for improved privacy.",
            ScoreWeight = 4,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Hides notification content from passersby when your device is locked.",
            TechnicalDetails = "HKCU\\...\\PushNotifications -> NoToastApplicationNotificationOnLockScreen = 1"
        },
        new OptimizationItemModel
        {
            Tag = "ToastNotifications",
            Title = "Disable All Toast Notifications",
            Category = OptimizationCategory.FeaturesAndUsability,
            Description = "Completely disables all pop-up toast notifications from apps and system.",
            ScoreWeight = 3,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Eliminates all notification pop-ups for a distraction-free experience.",
            TechnicalDetails = "HKCU\\...\\PushNotifications -> NoToastApplicationNotification = 1"
        },
        new OptimizationItemModel
        {
            Tag = "NotificationCenter",
            Title = "Disable Notification Center",
            Category = OptimizationCategory.FeaturesAndUsability,
            Description = "Removes the Notification Center (Action Center) from the taskbar and system tray.",
            ScoreWeight = 3,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Frees taskbar space and eliminates notification badge distractions.",
            TechnicalDetails = "HKCU\\...\\Explorer -> DisableNotificationCenter = 1"
        },
        new OptimizationItemModel
        {
            Tag = "SuggestedActions",
            Title = "Disable Suggested Actions",
            Category = OptimizationCategory.FeaturesAndUsability,
            Description = "Disables the Windows smart clipboard suggested actions flyout that appears when copying content.",
            ScoreWeight = 3,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Prevents unwanted popup menus when copying dates, phone numbers, or addresses.",
            TechnicalDetails = "HKCU\\...\\SmartClipboard -> Disabled = 1"
        },
        // Privacy Hardening
        new OptimizationItemModel
        {
            Tag = "WiFiSense",
            Title = "Disable Wi-Fi Sense",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Disables automatic connection to suggested open Wi-Fi hotspots and shared networks.",
            ScoreWeight = 6,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Prevents automatic connection to potentially insecure shared Wi-Fi networks.",
            TechnicalDetails = "HKLM\\...\\wifinetworkmanager\\config -> AutoConnectAllowedOEM = 0"
        },
        new OptimizationItemModel
        {
            Tag = "WebSearchResults",
            Title = "Disable Web Results in Search",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Prevents Windows Search from sending queries to Bing and displaying web results.",
            ScoreWeight = 8,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Keeps search local and private; speeds up Start menu search results.",
            TechnicalDetails = "HKCU\\...\\Explorer -> DisableSearchBoxSuggestions = 1; BingSearchEnabled = 0"
        },
        new OptimizationItemModel
        {
            Tag = "AppLaunchTracking",
            Title = "Disable App Launch Tracking",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Stops Windows from tracking which apps you launch for personalization of the Start menu.",
            ScoreWeight = 5,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Prevents Windows from recording app usage history; Start menu Most Used list may be affected.",
            TechnicalDetails = "HKCU\\...\\Explorer\\Advanced -> Start_TrackProgs = 0"
        },
        new OptimizationItemModel
        {
            Tag = "TimelineHistory",
            Title = "Disable Timeline & Activity History",
            Category = OptimizationCategory.PrivacyAndTelemetry,
            Description = "Disables Windows Timeline activity feed and prevents activity data from being published or uploaded.",
            ScoreWeight = 7,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Stops collection and cloud sync of activity history across devices.",
            TechnicalDetails = "HKLM\\...\\System -> EnableActivityFeed = 0, PublishUserActivities = 0, UploadUserActivities = 0"
        },
        // Context Menu Customization
        new OptimizationItemModel
        {
            Tag = "TakeOwnership",
            Title = "Add 'Take Ownership' to Context Menu",
            Category = OptimizationCategory.FeaturesAndUsability,
            Description = "Adds a 'Take Ownership' option to the right-click context menu for files and folders.",
            ScoreWeight = 3,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Quick one-click file ownership transfer for locked system files and folders.",
            TechnicalDetails = "HKCR\\*\\shell\\runas + HKCR\\Directory\\shell\\runas -> takeown + icacls"
        },
        new OptimizationItemModel
        {
            Tag = "OpenCmdHere",
            Title = "Add 'Open Command Prompt Here' to Context Menu",
            Category = OptimizationCategory.FeaturesAndUsability,
            Description = "Adds an 'Open Command Prompt Here' option to the folder background right-click context menu.",
            ScoreWeight = 2,
            Risk = RiskLevel.Safe,
            ImpactDescription = "Quick access to CMD at the current folder location from Explorer.",
            TechnicalDetails = "HKCR\\Directory\\Background\\shell\\OpenCmdHere"
        },
        new OptimizationItemModel
        {
            Tag = "CopyFilePath",
            Title = "Add 'Copy File Path' to Context Menu",
            Category = OptimizationCategory.FeaturesAndUsability,
            Description = "Adds a 'Copy File Path' option to the right-click context menu for quick path copying to clipboard.",
            ScoreWeight = 2,
            Risk = RiskLevel.Safe,
            ImpactDescription = "One-click copy of the full file path to clipboard.",
            TechnicalDetails = "HKCR\\*\\shell\\CopyFilePath -> cmd.exe /c echo \"%1\" | clip"
        }
    };

    public static List<OptimizationItemModel> GetCatalog()
    {
        return MasterCatalog.Select(item => new OptimizationItemModel
        {
            Tag = item.Tag,
            Title = item.Title,
            Category = item.Category,
            Description = item.Description,
            ScoreWeight = item.ScoreWeight,
            Risk = item.Risk,
            ImpactDescription = item.ImpactDescription,
            TechnicalDetails = item.TechnicalDetails,
            IsApplied = item.IsApplied,
            IsRecommended = item.IsRecommended,
            RecommendationReason = item.RecommendationReason,
            IsSelectedForApply = item.IsSelectedForApply,
            VerificationStatus = item.VerificationStatus,
            RollbackAvailable = item.RollbackAvailable,
            BackupDate = item.BackupDate,
            PreApplyValue = item.PreApplyValue
        }).ToList();
    }

    public static OptimizationItemModel? GetItemByTag(string tag)
    {
        return MasterCatalog.FirstOrDefault(i => string.Equals(i.Tag, tag, StringComparison.OrdinalIgnoreCase));
    }

    // Real-time detection of actual system configuration and rollback points
    public static async Task<List<OptimizationItemModel>> ScanAsync(IProgress<string>? progress = null)
    {
        _ = LogHelper.Log("[IntelligentEngine] === STEP 1: SCAN SYSTEM START ===");
        progress?.Report("Scanning system hardware profile and registry state...");

        var scannedList = GetCatalog();
        var rollbackTags = ItemRollbackService.GetAvailableRollbackTags();

        await Task.Run(() =>
        {
            foreach (var item in scannedList)
            {
                progress?.Report($"Scanning {item.CategoryDisplay}: {item.Title}...");

                // Detect actual live Windows state
                var detectedState = SystemStateDetector.DetectState(item.Tag);

                if (detectedState.HasValue)
                {
                    item.IsApplied = detectedState.Value;
                }
                else
                {
                    // Fall back to saved RyTuneX registry value
                    var regState = GetSavedRegistryState(item.Tag);
                    item.IsApplied = regState == 1;
                }

                // Check for per-item rollback point
                if (rollbackTags.Contains(item.Tag))
                {
                    var (hasBackup, preState, backupDt, details) = ItemRollbackService.GetBackupInfo(item.Tag);
                    item.RollbackAvailable = hasBackup;
                    item.BackupDate = backupDt;
                    item.PreApplyValue = preState ? "Enabled (Original)" : "Disabled (Original)";
                }
                else
                {
                    item.RollbackAvailable = false;
                }
            }
        }).ConfigureAwait(false);

        _ = LogHelper.Log($"[IntelligentEngine] === STEP 1: SCAN FINISHED. Scanned {scannedList.Count} items ===");
        return scannedList;
    }

    // Evaluates current state against system hardware profile and computes 0-100 scores
    public static IntelligentScoreModel Analyse(List<OptimizationItemModel> items)
    {
        _ = LogHelper.Log("[IntelligentEngine] === STEP 2: ANALYSE START ===");

        var model = new IntelligentScoreModel
        {
            TotalItemsCount = items.Count,
            OptimalItemsCount = items.Count(i => i.IsApplied),
            RollbackableItemsCount = items.Count(i => i.RollbackAvailable),
            SystemSummary = GetHardwareProfileSummary()
        };

        model.PerformanceScore = CalculateCategoryScore(items, OptimizationCategory.Performance);
        model.PrivacyScore = CalculateCategoryScore(items, OptimizationCategory.PrivacyAndTelemetry);
        model.SecurityScore = CalculateCategoryScore(items, OptimizationCategory.SecurityAndPolicies);
        model.ResourcesScore = CalculateCategoryScore(items, OptimizationCategory.SystemResourcesAndBloat);
        model.FeaturesScore = CalculateCategoryScore(items, OptimizationCategory.FeaturesAndUsability);

        // Weighted overall score
        model.OverallScore = (int)Math.Round(
            (model.PerformanceScore * 0.25) +
            (model.PrivacyScore * 0.25) +
            (model.SecurityScore * 0.20) +
            (model.ResourcesScore * 0.20) +
            (model.FeaturesScore * 0.10)
        );

        // Calculate potential gain from recommended safe items
        var unappliedRecommended = items.Where(i => !i.IsApplied && (i.Risk == RiskLevel.Safe || i.IsRecommended));
        int gainPoints = unappliedRecommended.Sum(i => i.ScoreWeight);
        int maxWeight = items.Sum(i => i.ScoreWeight);

        model.PotentialScoreGain = maxWeight > 0 ? (int)Math.Round((double)gainPoints / maxWeight * 100) : 0;
        model.RecommendedItemsCount = items.Count(i => i.IsRecommended);

        _ = LogHelper.Log($"[IntelligentEngine] Analysis complete. OverallScore={model.OverallScore}%, PotentialGain=+{model.PotentialScoreGain}%");
        return model;
    }

    // Categorizes optimizations into Recommended, Safe, Moderate, Advanced, and provides context-aware reasons
    public static void Recommend(List<OptimizationItemModel> items)
    {
        _ = LogHelper.Log("[IntelligentEngine] === STEP 3: RECOMMEND START ===");

        var totalRamGb = MemoryHelper.GetTotalPhysicalMemory() / (1024 * 1024 * 1024);

        foreach (var item in items)
        {
            bool isRecommended = false;
            string reason;

            switch (item.Tag)
            {
                case "TelemetryServices":
                case "WindowsAI":
                case "WindowsRecall":
                    isRecommended = !item.IsApplied;
                    reason = "Critical Recommendation: Protects personal data, stops continuous desktop OCR snapshotting and telemetry mining.";
                    break;

                case "BackgroundApps":
                    isRecommended = !item.IsApplied;
                    reason = $"Recommended: Reclaims CPU cycles and memory for your PC ({totalRamGb}GB RAM detected).";
                    break;

                case "SystemProfile":
                case "PrioritizeForegroundApplications":
                    isRecommended = !item.IsApplied;
                    reason = "Recommended: Boosts system responsiveness and GPU priority for active games and applications.";
                    break;

                case "Widgets":
                case "Cortana":
                    isRecommended = !item.IsApplied;
                    reason = "Recommended: Saves 300MB+ background RAM by terminating unused Edge WebView components.";
                    break;

                case "SMBv1":
                case "RemoteRegistry":
                case "WPBT":
                    isRecommended = !item.IsApplied;
                    reason = "Security Recommendation: Closes legacy remote attack vectors and motherboard bloatware injection.";
                    break;

                case "ClassicContextMenu":
                case "EndTask":
                case "FileExtensionsAndHiddenFiles":
                    isRecommended = !item.IsApplied;
                    reason = "Usability Recommendation: Restores fast 1-click desktop workflow and reveals actual file extensions.";
                    break;

                default:
                    if (item.Risk == RiskLevel.Safe)
                    {
                        isRecommended = !item.IsApplied;
                        reason = isRecommended
                            ? $"Recommended: 100% safe {item.CategoryDisplay.ToLowerInvariant()} optimization with zero system risk."
                            : "Already Optimal: This setting is currently active.";
                    }
                    else if (item.Risk == RiskLevel.Moderate)
                    {
                        isRecommended = false;
                        reason = "Moderate: Mild behavioral change. Review before applying.";
                    }
                    else if (item.Risk == RiskLevel.Advanced)
                    {
                        isRecommended = false;
                        reason = "Advanced: Power user tweak. Use caution depending on hardware setup.";
                    }
                    else
                    {
                        isRecommended = false;
                        reason = "Caution: May disable features if you rely on them (e.g. printing or system restore).";
                    }
                    break;
            }

            item.IsRecommended = isRecommended;
            item.RecommendationReason = reason;
            item.IsSelectedForApply = isRecommended;
        }
    }

    // Generates structured preview of proposed changes
    public static (int SelectedCount, int ExpectedScoreGain, List<OptimizationItemModel> SelectedItems) Preview(List<OptimizationItemModel> items)
    {
        _ = LogHelper.Log("[IntelligentEngine] === STEP 4: PREVIEW ===");

        var selected = items.Where(i => i.IsSelectedForApply).ToList();
        int scoreGain = selected.Sum(i => i.ScoreWeight);

        return (selected.Count, scoreGain, selected);
    }

    // Creates pre-apply rollback snapshots and applies tweaks
    public static async Task ApplyAsync(List<OptimizationItemModel> itemsToApply, IProgress<(int Current, int Total, string Status)>? progress = null)
    {
        _ = LogHelper.Log($"[IntelligentEngine] === STEP 5: APPLY START ({itemsToApply.Count} items) ===");

        int total = itemsToApply.Count;
        int current = 0;

        foreach (var item in itemsToApply)
        {
            current++;
            progress?.Report((current, total, $"Applying: {item.Title}..."));

            try
            {
                // Capture pre-apply backup for per-item rollback
                bool preApplyState = item.IsApplied;
                ItemRollbackService.SavePreApplyBackup(item.Tag, preApplyState, item.TechnicalDetails);

                // Execute optimization toggle
                var fakeToggle = new Microsoft.UI.Xaml.Controls.ToggleSwitch
                {
                    Tag = item.Tag,
                    IsOn = true
                };

                await OptimizationOptions.XamlSwitchesAsync(fakeToggle).ConfigureAwait(false);

                item.IsApplied = true;
                item.RollbackAvailable = true;
                item.BackupDate = DateTime.Now;
                item.PreApplyValue = preApplyState ? "Enabled (Original)" : "Disabled (Original)";
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"[IntelligentEngine] Error applying {item.Tag}: {ex.Message}");
            }
        }

        await Task.Delay(400).ConfigureAwait(false);
        _ = LogHelper.Log("[IntelligentEngine] === STEP 5: APPLY FINISHED ===");
    }

    // Re-evaluates live registry/service states to confirm application status
    public static async Task VerifyAsync(List<OptimizationItemModel> itemsToVerify, IProgress<(int Current, int Total, string Status)>? progress = null)
    {
        _ = LogHelper.Log($"[IntelligentEngine] === STEP 6: VERIFY START ({itemsToVerify.Count} items) ===");

        int total = itemsToVerify.Count;
        int current = 0;

        await Task.Run(() =>
        {
            foreach (var item in itemsToVerify)
            {
                current++;
                progress?.Report((current, total, $"Verifying: {item.Title}..."));

                var state = SystemStateDetector.DetectState(item.Tag);

                if (state.HasValue)
                {
                    if (state.Value)
                    {
                        item.VerificationStatus = VerificationStatus.VerifiedActive;
                        item.IsApplied = true;
                    }
                    else
                    {
                        item.VerificationStatus = VerificationStatus.VerificationFailed;
                    }
                }
                else
                {
                    var regVal = GetSavedRegistryState(item.Tag);
                    if (regVal == 1)
                    {
                        item.VerificationStatus = VerificationStatus.RequiresRestart;
                        item.IsApplied = true;
                    }
                    else
                    {
                        item.VerificationStatus = VerificationStatus.VerificationFailed;
                    }
                }
            }
        }).ConfigureAwait(false);

        _ = LogHelper.Log("[IntelligentEngine] === STEP 6: VERIFY FINISHED ===");
    }

    // Restores a single setting to its pre-apply state without affecting any other settings
    public static async Task<bool> RollbackItemAsync(OptimizationItemModel item)
    {
        _ = LogHelper.Log($"[IntelligentEngine] === STEP 7: PER-ITEM ROLLBACK for '{item.Tag}' ===");

        bool success = await ItemRollbackService.RollbackItemAsync(item.Tag).ConfigureAwait(false);
        if (success)
        {
            var (hasBackup, preState, _, _) = ItemRollbackService.GetBackupInfo(item.Tag);
            item.IsApplied = preState;
            item.RollbackAvailable = false;
            item.BackupDate = null;
            item.PreApplyValue = null;
            item.VerificationStatus = VerificationStatus.NotVerified;

            _ = LogHelper.Log($"[IntelligentEngine] Rollback succeeded for '{item.Tag}' -> IsApplied={item.IsApplied}");
        }

        return success;
    }

    private static int CalculateCategoryScore(List<OptimizationItemModel> items, OptimizationCategory cat)
    {
        var catItems = items.Where(i => i.Category == cat).ToList();
        if (catItems.Count == 0) return 100;

        int appliedWeight = catItems.Where(i => i.IsApplied).Sum(i => i.ScoreWeight);
        int totalWeight = catItems.Sum(i => i.ScoreWeight);

        return totalWeight > 0 ? (int)Math.Round((double)appliedWeight / totalWeight * 100) : 100;
    }

    private static int GetSavedRegistryState(string tagName)
    {
        try
        {
            using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                    ? RegistryView.Registry64
                    : RegistryView.Default).OpenSubKey(@"SOFTWARE\RyTuneX\Optimizations");

            if (key?.GetValue(tagName) is int val)
            {
                return val;
            }
        }
        catch { }
        return 0;
    }

    private static string GetHardwareProfileSummary()
    {
        try
        {
            var ramGb = (int)Math.Round((double)MemoryHelper.GetTotalPhysicalMemory() / (1024 * 1024 * 1024));
            var cores = Environment.ProcessorCount;
            return $"{cores}-Core CPU • {ramGb} GB RAM • Windows {(Environment.OSVersion.Version.Build >= 22000 ? "11" : "10")}";
        }
        catch
        {
            return "Windows PC";
        }
    }
}
