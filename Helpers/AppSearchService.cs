using RyTuneX.Models;
using RyTuneX.Views;

namespace RyTuneX.Helpers;

//Service that builds searchable items from resource strings at startup and provides localized search with the same icons as the UI toggles.
public static class AppSearchService
{
    // Mapping of resource key prefix to their page, category, glyph (matching XAML), and toggle tag
    private static readonly List<(string ResourceKeyPrefix, Type PageType, string Category, string Glyph, string ToggleTag)> FeatureMap =
    [
        // OptimizeSystemPage - Basic (icons match OptimizeSystemPage.xaml)
        ("Feature_PowerMode", typeof(OptimizeSystemPage), "Optimize", "\uEBB7", "PowerMode"),
        ("Feature_AddUltimatePowerPlan", typeof(OptimizeSystemPage), "Optimize", "\uE945", "AddUltimatePowerPlan"),
        ("Feature_MenuShowDelay", typeof(OptimizeSystemPage), "Optimize", "\uF182", "MenuShowDelay"),
        ("Feature_MouseHoverTime", typeof(OptimizeSystemPage), "Optimize", "\uE8B0", "MouseHoverTime"),
        ("Feature_BackgroundApps", typeof(OptimizeSystemPage), "Optimize", "\uE8BE", "BackgroundApps"),
        ("Feature_AutoComplete", typeof(OptimizeSystemPage), "Optimize", "\uE8A1", "AutoComplete"),
        ("Feature_CrashDump", typeof(OptimizeSystemPage), "Optimize", "\uE74D", "CrashDump"),
        ("Feature_RemoteAssistance", typeof(OptimizeSystemPage), "Optimize", "\uE8AF", "RemoteAssistance"),
        ("Feature_WindowShake", typeof(OptimizeSystemPage), "Optimize", "\uE746", "WindowShake"),
        ("Feature_CopyMoveContextMenu", typeof(OptimizeSystemPage), "Optimize", "\uE8C8", "CopyMoveContextMenu"),
        ("Feature_TaskTimeouts", typeof(OptimizeSystemPage), "Optimize", "\uE916", "TaskTimeouts"),
        ("Feature_LowDiskSpaceChecks", typeof(OptimizeSystemPage), "Optimize", "\uE73E", "LowDiskSpaceChecks"),
        ("Feature_LinkResolve", typeof(OptimizeSystemPage), "Optimize", "\uE8F5", "LinkResolve"),
        ("Feature_ServiceTimeouts", typeof(OptimizeSystemPage), "Optimize", "\uE823", "ServiceTimeouts"),
        ("Feature_RemoteRegistry", typeof(OptimizeSystemPage), "Optimize", "\uED5C", "RemoteRegistry"),
        ("Feature_FileExtensionsAndHiddenFiles", typeof(OptimizeSystemPage), "Optimize", "\uF19D", "FileExtensionsAndHiddenFiles"),
        
        // OptimizeSystemPage - Advanced
        ("Feature_SystemProfile", typeof(OptimizeSystemPage), "Optimize", "\uE9F5", "SystemProfile"),
        ("Feature_DisableSysMain", typeof(OptimizeSystemPage), "Optimize", "\uEC4A", "SysMain"),
        ("Feature_EnableGamingMode", typeof(OptimizeSystemPage), "Optimize", "\uE7FC", "GamingMode"),
        ("Feature_ExcludeDriversFromWindowsUpdates", typeof(OptimizeSystemPage), "Optimize", "\uE772", "Drivers"),
        ("OptimizePage_CompressOS", typeof(OptimizeSystemPage), "Optimize", "\uEB05", "CompressOS"),
        ("Feature_ServiceHostSplitting", typeof(OptimizeSystemPage), "Optimize", "\uE15E", "ServiceHostSplitting"),
        ("Feature_LegacyBootMenu", typeof(OptimizeSystemPage), "Optimize", "\uF16A", "LegacyBootMenu"),
        ("Feature_OptimizeNTFS", typeof(OptimizeSystemPage), "Optimize", "\uE9F3", "OptimizeNTFS"),
        ("Feature_PrioritizeForegroundApplications", typeof(OptimizeSystemPage), "Optimize", "\uE9D9", "PrioritizeForegroundApplications"),
        ("Feature_WPBT", typeof(OptimizeSystemPage), "Optimize", "\uE9E9", "WPBT"),
        
        // OptimizeSystemPage - Other
        ("Feature_DisableSystemRestore", typeof(OptimizeSystemPage), "Optimize", "\uE777", "SystemRestore"),
        ("Feature_DisableCortana", typeof(OptimizeSystemPage), "Optimize", "\uED66", "Cortana"),
        ("Feature_DisableStoreUpdates", typeof(OptimizeSystemPage), "Optimize", "\uE719", "StoreUpdates"),
        ("Feature_WindowsUpdates", typeof(OptimizeSystemPage), "Optimize", "\uE8D8", "AutomaticUpdates"),
        ("Feature_DisableSmartScreen", typeof(OptimizeSystemPage), "Optimize", "\uF8A5", "SmartScreen"),

        // PrivacyPage - Advertising (glyphs match PrivacyPage.xaml)
        ("Feature_DisableAdvertisingID", typeof(PrivacyPage), "Privacy", "\uEE57", "AdvertisingID"),
        ("Feature_DisableBluetoothAdvertising", typeof(PrivacyPage), "Privacy", "\uE702", "BluetoothAdvertising"),
        ("Feature_DisableNewsAndInterests", typeof(PrivacyPage), "Privacy", "\uF586", "NewsAndInterests"),
        ("Feature_DisableSpotlightFeatures", typeof(PrivacyPage), "Privacy", "\uE786", "SpotlightFeatures"),
        ("Feature_DisableTailoredExperiences", typeof(PrivacyPage), "Privacy", "\uEADF", "TailoredExperiences"),
        ("Feature_DisableCloudOptimizedContent", typeof(PrivacyPage), "Privacy", "\uE753", "CloudOptimizedContent"),
        ("Feature_DisableFeedbackNotifications", typeof(PrivacyPage), "Privacy", "\uED15", "FeedbackNotifications"),

        // PrivacyPage - Telemetry (glyphs match PrivacyPage.xaml)
        ("Feature_DisableTelemetryServices", typeof(PrivacyPage), "Privacy", "\uE9F9", "TelemetryServices"),
        ("Feature_DisableEdgeTelemetry", typeof(PrivacyPage), "Privacy", "\uE9F9", "EdgeTelemetry"),
        ("Feature_DisableVisualStudioTelemetry", typeof(PrivacyPage), "Privacy", "\uE9F9", "VisualStudioTelemetry"),
        ("Feature_DisableNvidiaTelemetry", typeof(PrivacyPage), "Privacy", "\uE9F9", "NvidiaTelemetry"),
        ("Feature_DisableChromeTelemetry", typeof(PrivacyPage), "Privacy", "\uE9F9", "ChromeTelemetry"),
        ("Feature_DisableFirefoxTelemetry", typeof(PrivacyPage), "Privacy", "\uE9F9", "FirefoxTelemetry"),
        ("Feature_DisableActivityFeed", typeof(PrivacyPage), "Privacy", "\uE779", "ActivityFeed"),
        ("Feature_DisableCdp", typeof(PrivacyPage), "Privacy", "\uEF58", "Cdp"),
        ("Feature_DisableDiagnosticsToast", typeof(PrivacyPage), "Privacy", "\uE9D9", "DiagnosticsToast"),
        ("Feature_DisableOnlineSpeechPrivacy", typeof(PrivacyPage), "Privacy", "\uE720", "OnlineSpeechPrivacy"),
        ("Feature_DisableLocationFeatures", typeof(PrivacyPage), "Privacy", "\uE809", "LocationFeatures"),
        ("Feature_DisableBiometrics", typeof(PrivacyPage), "Privacy", "\uE928", "Biometrics"),
        
        // PrivacyPage - Other Privacy (glyphs match PrivacyPage.xaml)
        ("Feature_DisableAutomaticRestartSignOn", typeof(PrivacyPage), "Privacy", "\uE777", "AutomaticRestartSignOn"),
        ("Feature_DisableHandwritingDataSharing", typeof(PrivacyPage), "Privacy", "\uE929", "HandwritingDataSharing"),
        ("Feature_DisableTextInputDataCollection", typeof(PrivacyPage), "Privacy", "\uE961", "TextInputDataCollection"),
        ("Feature_DisableInputPersonalization", typeof(PrivacyPage), "Privacy", "\uF180", "InputPersonalization"),
        ("Feature_DisableSafeSearchMode", typeof(PrivacyPage), "Privacy", "\uE773", "SafeSearchMode"),
        ("Feature_DisableActivityUploads", typeof(PrivacyPage), "Privacy", "\uE8FD", "ActivityUploads"),
        ("Feature_DisableClipboardSync", typeof(PrivacyPage), "Privacy", "\uF0E3", "ClipboardSync"),
        ("Feature_DisableMessageSync", typeof(PrivacyPage), "Privacy", "\uE90A", "MessageSync"),
        ("Feature_DisableSettingSync", typeof(PrivacyPage), "Privacy", "\uE895", "SettingSync"),
        ("Feature_DisableVoiceActivation", typeof(PrivacyPage), "Privacy", "\uEFA9", "VoiceActivation"),
        ("Feature_DisableFindMyDevice", typeof(PrivacyPage), "Privacy", "\uE707", "FindMyDevice"),
        ("Feature_DisableSMBv1", typeof(PrivacyPage), "Privacy", "\uF193", "SMBv1"),
        ("Feature_DisableSMBv2", typeof(PrivacyPage), "Privacy", "\uF193", "SMBv2"),

        // FeaturesPage - System Features (glyphs match FeaturesPage.xaml)
        ("Feature_DisableWindowsTransparency", typeof(FeaturesPage), "Features", "\uF5ED", "WindowsTransparency"),
        ("Feature_EnableWindowsDarkMode", typeof(FeaturesPage), "Features", "\uE790", "WindowsDarkMode"),
        ("Feature_EnableVerboseLogon", typeof(FeaturesPage), "Features", "\uE946", "VerboseLogon"),
        ("Feature_DisableHibernation", typeof(FeaturesPage), "Features", "\uE708", "Hibernation"),
        ("Feature_DisableHomeGroup", typeof(FeaturesPage), "Features", "\uE902", "HomeGroup"),
        ("Feature_DisablePrintService", typeof(FeaturesPage), "Features", "\uE749", "PrintService"),
        ("Feature_DisableCompatibilityAssistant", typeof(FeaturesPage), "Features", "\uE83D", "CompatibilityAssistant"),
        ("Feature_DisableSearch", typeof(FeaturesPage), "Features", "\uE721", "Search"),
        ("Feature_DisableErrorReporting", typeof(FeaturesPage), "Features", "\uE9F9", "ErrorReporting"),
        ("Feature_DisableGameBar", typeof(FeaturesPage), "Features", "\uE990", "GameBar"),
        ("Feature_DisableQuickAccessHistory", typeof(FeaturesPage), "Features", "\uE81C", "QuickAccessHistory"),
        ("Feature_DisableMyPeople", typeof(FeaturesPage), "Features", "\uE716", "MyPeople"),
        ("Feature_DisableSensorServices", typeof(FeaturesPage), "Features", "\uE957", "SensorServices"),
        ("Feature_DisableWindowsInk", typeof(FeaturesPage), "Features", "\uEDC6", "WindowsInk"),
        ("Feature_DisableSpellingAndTypingFeatures", typeof(FeaturesPage), "Features", "\uF87B", "SpellingAndTypingFeatures"),
        ("Feature_DisableFaxService", typeof(FeaturesPage), "Features", "\uEF40", "FaxService"),
        ("Feature_DisableInsiderService", typeof(FeaturesPage), "Features", "\uF1AD", "InsiderService"),
        ("Feature_DisableCloudClipboard", typeof(FeaturesPage), "Features", "\uEBC3", "CloudClipboard"),
        ("Feature_DisableStickyKeys", typeof(FeaturesPage), "Features", "\uE765", "StickyKeys"),
        ("Feature_DisableCastToDevice", typeof(FeaturesPage), "Features", "\uEC15", "CastToDevice"),
        
        // FeaturesPage - Windows 11 Exclusive (glyphs match FeaturesPage.xaml)
        ("Feature_DisableVBS", typeof(FeaturesPage), "Features", "\uF552", "VBS"),
        ("Feature_EnableEndTask", typeof(FeaturesPage), "Features", "\uE25B", "EndTask"),
        ("Feature_EnableClassicContextMenu", typeof(FeaturesPage), "Features", "\uE8C8", "ClassicContextMenu"),
        ("Feature_DisableRecommendedSectionStartMenu", typeof(FeaturesPage), "Features", "\uE8FC", "RecommendedSectionStartMenu"),
        ("Feature_MoveTaskbarToLeft", typeof(FeaturesPage), "Features", "\uE112", "TaskbarToLeft"),
        ("Feature_DisableSnapAssist", typeof(FeaturesPage), "Features", "\uE7C4", "SnapAssist"),
        ("Feature_DisableWidgets", typeof(FeaturesPage), "Features", "\uE1AC", "Widgets"),
        ("Feature_DisableChat", typeof(FeaturesPage), "Features", "\uE8BD", "Chat"),
        ("Feature_EnableFilesCompactMode", typeof(FeaturesPage), "Features", "\uE8FD", "FilesCompactMode"),
        ("Feature_DisableStickers", typeof(FeaturesPage), "Features", "\uF4AA", "Stickers"),
        ("Feature_DisableEdgeDiscoverBar", typeof(FeaturesPage), "Features", "\uF6FA", "EdgeDiscoverBar"),
        ("Feature_DisableCoPilotAI", typeof(FeaturesPage), "Features", "\uE99A", "CoPilotAI"),
        ("Feature_DisableWindowsRecall", typeof(FeaturesPage), "Features", "\uE82F", "WindowsRecall"),
    ];

    // Navigation pages with their resource keys for localization
    private static readonly List<(string ResourceKey, Type PageType, string Glyph)> NavigationPages =
    [
        ("Shell_Home", typeof(HomePage), "\uE80F"),
        ("Shell_OptimizeSystem", typeof(OptimizeSystemPage), "\uF259"),
        ("Shell_Repair", typeof(RepairPage), "\uE90F"),
        ("Shell_Debloat", typeof(DebloatSystemPage), "\uE74D"),
        ("Shell_Privacy", typeof(PrivacyPage), "\uE7B3"),
        ("Shell_Features", typeof(FeaturesPage), "\uE74C"),
        ("Shell_Network", typeof(NetworkPage), "\uE968"),
        ("Shell_Security", typeof(SecurityPage), "\uEA18"),
        ("Shell_GroupPolicy", typeof(GroupPolicyPage), "\uE9D5"),
        ("Shell_Processes", typeof(ProcessesPage), "\uECAA"),
        ("Shell_Services", typeof(ServicesPage), "\uEA86"),
        ("Shell_SystemInfo", typeof(SystemInfoPage), "\uE770"),
        ("Settings", typeof(SettingsPage), "\uE713")
    ];

    private static readonly object CacheLock = new();
    private static SearchableItem[]? _cachedItems;

    // Initializes the search cache from resource strings.
    public static void InitializeCache()
    {
        if (_cachedItems != null)
        {
            return;
        }

        SearchableItem[]? initializedItems = null;
        lock (CacheLock)
        {
            if (_cachedItems == null)
            {
                initializedItems = BuildCache();
                Volatile.Write(ref _cachedItems, initializedItems);
            }
        }

        if (initializedItems != null)
        {
            _ = LogHelper.Log($"Search cache initialized with {initializedItems.Length} items");
        }
    }

    private static SearchableItem[] BuildCache()
    {
        var items = new List<SearchableItem>(NavigationPages.Count + FeatureMap.Count);

        // Add navigation pages
        foreach (var (resourceKey, pageType, glyph) in NavigationPages)
        {
            // Try with .Content suffix first (Shell_Home.Content)
            var displayName = $"{resourceKey}.Content".TryGetLocalized();
            if (string.IsNullOrEmpty(displayName))
            {
                // Fallback to page name extraction
                displayName = pageType.FullName!.Split('.').Last().Replace("Page", "");
            }

            items.Add(new SearchableItem
            {
                DisplayName = displayName,
                Glyph = glyph,
                PageTypeName = pageType.FullName!,
                Category = "Navigation"
            });
        }

        // Add feature items from resources using the slash format for x:Uid
        foreach (var (resourceKeyPrefix, pageType, category, glyph, toggleTag) in FeatureMap)
        {
            // WinUI x:Uid resources use slash format: "Feature_MenuShowDelay/Header"
            // but ResourceLoader.GetString uses dot format: "Feature_MenuShowDelay.Header"
            var header = $"{resourceKeyPrefix}/Header".TryGetLocalized();

            // If slash format fails, try dot format
            if (string.IsNullOrEmpty(header))
            {
                header = $"{resourceKeyPrefix}.Header".TryGetLocalized();
            }

            if (string.IsNullOrEmpty(header))
            {
                _ = LogHelper.Log($"Resource not found for: {resourceKeyPrefix}");
                continue;
            }

            var description = $"{resourceKeyPrefix}/Description".TryGetLocalized();
            if (string.IsNullOrEmpty(description))
            {
                description = $"{resourceKeyPrefix}.Description".TryGetLocalized();
            }

            items.Add(new SearchableItem
            {
                DisplayName = header,
                Description = description,
                Glyph = glyph,
                PageTypeName = pageType.FullName!,
                OptionTag = toggleTag,
                Category = category
            });
        }

        return items.ToArray();
    }

    // Searches for items matching the query.
    public static IEnumerable<SearchableItem> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<SearchableItem>();
        }

        InitializeCache();
        var cachedItems = _cachedItems;
        if (cachedItems == null || cachedItems.Length == 0)
        {
            return Array.Empty<SearchableItem>();
        }

        var queryLower = query.ToLowerInvariant();

        return cachedItems
            .Select(item => new
            {
                Item = item,
                Score = CalculateMatchScore(item, queryLower)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Item.DisplayName)
            .Select(x => x.Item)
            .Take(10);
    }

    private static int CalculateMatchScore(SearchableItem item, string queryLower)
    {
        var score = 0;

        // Exact match in display name gets highest score
        if (item.DisplayName.Contains(queryLower, StringComparison.OrdinalIgnoreCase))
        {
            score += item.DisplayName.StartsWith(queryLower, StringComparison.OrdinalIgnoreCase) ? 100 : 50;
        }

        // Match in description
        if (!string.IsNullOrEmpty(item.Description) &&
            item.Description.Contains(queryLower, StringComparison.OrdinalIgnoreCase))
        {
            score += 25;
        }

        // Match in category
        if (!string.IsNullOrEmpty(item.Category) &&
            item.Category.Contains(queryLower, StringComparison.OrdinalIgnoreCase))
        {
            score += 15;
        }

        // Match in option tag
        if (!string.IsNullOrEmpty(item.OptionTag) &&
            item.OptionTag.Contains(queryLower, StringComparison.OrdinalIgnoreCase))
        {
            score += 20;
        }

        return score;
    }
}
