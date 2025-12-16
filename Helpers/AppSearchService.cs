using RyTuneX.Models;

namespace RyTuneX.Helpers;

//Service that builds searchable items from resource strings at startup and provides localized search with the same icons as the UI toggles.
public static class AppSearchService
{
    // Mapping of resource key prefix to their page, category, glyph (matching XAML), and toggle tag
    private static readonly List<(string ResourceKeyPrefix, string PageTypeName, string Category, string Glyph, string ToggleTag)> FeatureMap =
    [
        // OptimizeSystemPage - Basic (icons match OptimizeSystemPage.xaml)
        ("Feature_MenuShowDelay", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uF182", "MenuShowDelay"),
        ("Feature_MouseHoverTime", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uE8B0", "MouseHoverTime"),
        ("Feature_BackgroundApps", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uE8BE", "BackgroundApps"),
        ("Feature_AutoComplete", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uE8A1", "AutoComplete"),
        ("Feature_CrashDump", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uE74D", "CrashDump"),
        ("Feature_RemoteAssistance", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uE8AF", "RemoteAssistance"),
        ("Feature_WindowShake", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uE746", "WindowShake"),
        ("Feature_CopyMoveContextMenu", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uE8C8", "CopyMoveContextMenu"),
        ("Feature_TaskTimeouts", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uE916", "TaskTimeouts"),
        ("Feature_LowDiskSpaceChecks", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uE73E", "LowDiskSpaceChecks"),
        ("Feature_LinkResolve", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uE8F5", "LinkResolve"),
        ("Feature_ServiceTimeouts", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uE823", "ServiceTimeouts"),
        ("Feature_RemoteRegistry", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uED5C", "RemoteRegistry"),
        ("Feature_FileExtensionsAndHiddenFiles", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uF19D", "FileExtensionsAndHiddenFiles"),
        
        // OptimizeSystemPage - Advanced
        ("Feature_SystemProfile", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uE9F5", "SystemProfile"),
        ("Feature_DisableSysMain", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uEC4A", "SysMain"),
        ("Feature_EnableGamingMode", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uE7FC", "GamingMode"),
        ("Feature_ExcludeDriversFromWindowsUpdates", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uE772", "Drivers"),
        ("Feature_ServiceHostSplitting", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uE15E", "ServiceHostSplitting"),
        ("Feature_LegacyBootMenu", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uF16A", "LegacyBootMenu"),
        ("Feature_OptimizeNTFS", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uE9F3", "OptimizeNTFS"),
        ("Feature_PrioritizeForegroundApplications", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uE9D9", "PrioritizeForegroundApplications"),
        ("Feature_WPBT", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uE9E9", "WPBT"),
        
        // OptimizeSystemPage - Other
        ("Feature_DisableSystemRestore", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uE777", "SystemRestore"),
        ("Feature_DisableCortana", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uED66", "Cortana"),
        ("Feature_DisableStoreUpdates", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uE719", "StoreUpdates"),
        ("Feature_DisableAutomaticUpdates", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uE8D8", "AutomaticUpdates"),
        ("Feature_DisableSmartScreen", "RyTuneX.Views.OptimizeSystemPage", "Optimize", "\uF8A5", "SmartScreen"),

        // PrivacyPage - Telemetry
        ("Feature_DisableTelemetryServices", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE9D9", "TelemetryServices"),
        ("Feature_DisableEdgeTelemetry", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "EdgeTelemetry"),
        ("Feature_DisableVisualStudioTelemetry", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "VisualStudioTelemetry"),
        ("Feature_DisableNvidiaTelemetry", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "NvidiaTelemetry"),
        ("Feature_DisableChromeTelemetry", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "ChromeTelemetry"),
        ("Feature_DisableFirefoxTelemetry", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "FirefoxTelemetry"),
        
        // PrivacyPage - Advertising
        ("Feature_DisableNewsAndInterests", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE12A", "NewsAndInterests"),
        ("Feature_DisableSpotlightFeatures", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "SpotlightFeatures"),
        ("Feature_DisableTailoredExperiences", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "TailoredExperiences"),
        ("Feature_DisableCloudOptimizedContent", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "CloudOptimizedContent"),
        ("Feature_DisableFeedbackNotifications", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "FeedbackNotifications"),
        ("Feature_DisableAdvertisingID", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "AdvertisingID"),
        ("Feature_DisableBluetoothAdvertising", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "BluetoothAdvertising"),
        
        // PrivacyPage - Other Privacy
        ("Feature_DisableAutomaticRestartSignOn", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "AutomaticRestartSignOn"),
        ("Feature_DisableHandwritingDataSharing", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "HandwritingDataSharing"),
        ("Feature_DisableTextInputDataCollection", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "TextInputDataCollection"),
        ("Feature_DisableInputPersonalization", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "InputPersonalization"),
        ("Feature_DisableSafeSearchMode", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "SafeSearchMode"),
        ("Feature_DisableActivityUploads", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "ActivityUploads"),
        ("Feature_DisableClipboardSync", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "ClipboardSync"),
        ("Feature_DisableMessageSync", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "MessageSync"),
        ("Feature_DisableSettingSync", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "SettingSync"),
        ("Feature_DisableVoiceActivation", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "VoiceActivation"),
        ("Feature_DisableFindMyDevice", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "FindMyDevice"),
        ("Feature_DisableActivityFeed", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "ActivityFeed"),
        ("Feature_DisableCdp", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "Cdp"),
        ("Feature_DisableDiagnosticsToast", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "DiagnosticsToast"),
        ("Feature_DisableOnlineSpeechPrivacy", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "OnlineSpeechPrivacy"),
        ("Feature_DisableLocationFeatures", "RyTuneX.Views.PrivacyPage", "Privacy", "\uE7B3", "LocationFeatures"),

        // FeaturesPage - System Features
        ("Feature_DisableHomeGroup", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "HomeGroup"),
        ("Feature_DisablePrintService", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "PrintService"),
        ("Feature_DisableCompatibilityAssistant", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "CompatibilityAssistant"),
        ("Feature_DisableSearch", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "Search"),
        ("Feature_DisableErrorReporting", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "ErrorReporting"),
        ("Feature_DisableBiometrics", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "Biometrics"),
        ("Feature_DisableGameBar", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "GameBar"),
        ("Feature_DisableQuickAccessHistory", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "QuickAccessHistory"),
        ("Feature_DisableMyPeople", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "MyPeople"),
        ("Feature_DisableSensorServices", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "SensorServices"),
        ("Feature_DisableWindowsInk", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "WindowsInk"),
        ("Feature_DisableSpellingAndTypingFeatures", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "SpellingAndTypingFeatures"),
        ("Feature_DisableFaxService", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "FaxService"),
        ("Feature_DisableInsiderService", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "InsiderService"),
        ("Feature_DisableCloudClipboard", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "CloudClipboard"),
        ("Feature_DisableStickyKeys", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "StickyKeys"),
        ("Feature_DisableCastToDevice", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "CastToDevice"),
        ("Feature_DisableVBS", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "VBS"),
        ("Feature_DisableSMBv1", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "SMBv1"),
        ("Feature_DisableSMBv2", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "SMBv2"),
        ("Feature_DisableHibernation", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "Hibernation"),
        ("Feature_EnableEndTask", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "EndTask"),
        ("Feature_EnableVerboseLogon", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "VerboseLogon"),
        ("Feature_EnableClassicContextMenu", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "ClassicContextMenu"),
        ("Feature_EnableWindowsDarkMode", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "WindowsDarkMode"),
        ("Feature_DisableWindowsTransparency", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "WindowsTransparency"),
        
        // FeaturesPage - Windows 11 Exclusive
        ("Feature_MoveTaskbarToLeft", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "TaskbarToLeft"),
        ("Feature_DisableSnapAssist", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "SnapAssist"),
        ("Feature_DisableWidgets", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "Widgets"),
        ("Feature_DisableChat", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "Chat"),
        ("Feature_EnableFilesCompactMode", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "FilesCompactMode"),
        ("Feature_DisableStickers", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "Stickers"),
        ("Feature_DisableEdgeDiscoverBar", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "EdgeDiscoverBar"),
        ("Feature_DisableCoPilotAI", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "CoPilotAI"),
        ("Feature_DisableRecommendedSectionStartMenu", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "RecommendedSectionStartMenu"),
        ("Feature_DisableWindowsRecall", "RyTuneX.Views.FeaturesPage", "Features", "\uE74C", "WindowsRecall"),
    ];

    // Navigation pages with their resource keys for localization
    private static readonly List<(string ResourceKey, string PageTypeName, string Glyph)> NavigationPages =
    [
        ("Shell_Home", "RyTuneX.Views.HomePage", "\uE80F"),
        ("Shell_OptimizeSystem", "RyTuneX.Views.OptimizeSystemPage", "\uF259"),
        ("Shell_Repair", "RyTuneX.Views.RepairPage", "\uE90F"),
        ("Shell_Debloat", "RyTuneX.Views.DebloatSystemPage", "\uE74D"),
        ("Shell_Privacy", "RyTuneX.Views.PrivacyPage", "\uE7B3"),
        ("Shell_Features", "RyTuneX.Views.FeaturesPage", "\uE74C"),
        ("Shell_Network", "RyTuneX.Views.NetworkPage", "\uE968"),
        ("Shell_Security", "RyTuneX.Views.SecurityPage", "\uEA18"),
        ("Shell_SystemInfo", "RyTuneX.Views.SystemInfoPage", "\uE770"),
        ("Settings", "RyTuneX.Views.SettingsPage", "\uE713")
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
        foreach (var (resourceKey, pageTypeName, glyph) in NavigationPages)
        {
            // Try with .Content suffix first (Shell_Home.Content)
            var displayName = $"{resourceKey}.Content".TryGetLocalized();
            if (string.IsNullOrEmpty(displayName))
            {
                // Fallback to page name extraction
                displayName = pageTypeName.Split('.').Last().Replace("Page", "");
            }

            items.Add(new SearchableItem
            {
                DisplayName = displayName,
                Glyph = glyph,
                PageTypeName = pageTypeName,
                Category = "Navigation"
            });
        }

        // Add feature items from resources using the slash format for x:Uid
        foreach (var (resourceKeyPrefix, pageTypeName, category, glyph, toggleTag) in FeatureMap)
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
                PageTypeName = pageTypeName,
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
