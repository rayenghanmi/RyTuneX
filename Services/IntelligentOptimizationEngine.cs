using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using RyTuneX.Helpers;
using RyTuneX.Models;
using RyTuneX.Views;

namespace RyTuneX.Services;

public static class IntelligentOptimizationEngine
{
    // Build master catalog
    public static List<OptimizationItemModel> GetCatalog()
    {
        var tags = OptimizeFunctionInspector.GetAllKnownTags();
        return tags.Select(tag => OptimizeFunctionInspector.CreateItemModel(tag)).ToList();
    }

    // Get item model by tag
    public static OptimizationItemModel? GetItemByTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return null;
        return OptimizeFunctionInspector.CreateItemModel(tag);
    }

    // Extract optimization items for page
    public static List<OptimizationItemModel> GetPageCatalog(Page page)
    {
        if (page == null) return new();

        var toggles = IntelligentCardEnhancer.GetAllToggleSwitches(page);
        var items = new List<OptimizationItemModel>();
        var seenTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var toggle in toggles)
        {
            var tag = (toggle.Tag as string) ?? toggle.Name;
            if (string.IsNullOrWhiteSpace(tag) ||
                tag is "Default" or "Security" or "Manually" or "Disabled" ||
                !seenTags.Add(tag))
            {
                continue;
            }

            var card = IntelligentCardEnhancer.FindParent<CommunityToolkit.WinUI.Controls.SettingsCard>(toggle);
            var model = OptimizeFunctionInspector.CreateItemModel(tag, page, toggle, card);
            items.Add(model);
        }

        return items;
    }

    // Get Recommended preset items
    public static List<OptimizationItemModel> GetRecommendedPresetItems(Page page)
    {
        return GetPageCatalog(page).Where(i => i.IsRecommended).ToList();
    }

    // Get Safe preset items
    public static List<OptimizationItemModel> GetSafePresetItems(Page page)
    {
        return GetPageCatalog(page).Where(i => i.Risk == RiskLevel.Safe).ToList();
    }

    // Get Cosmetic preset items
    public static List<OptimizationItemModel> GetCosmeticPresetItems(Page page)
    {
        return GetPageCatalog(page).Where(i => i.Risk == RiskLevel.Cosmetic).ToList();
    }

    // Get Moderate preset items
    public static List<OptimizationItemModel> GetModeratePresetItems(Page page)
    {
        return GetPageCatalog(page).Where(i => i.Risk == RiskLevel.Safe || i.Risk == RiskLevel.Moderate).ToList();
    }

    // Scan page items
    public static async Task<List<OptimizationItemModel>> ScanPageAsync(Page page, IProgress<string>? progress = null)
    {
        _ = LogHelper.Log($"[IntelligentEngine] === DYNAMIC SCAN FOR PAGE '{page.GetType().Name}' ===");
        var pageItems = GetPageCatalog(page);
        return await ScanItemsAsync(pageItems, progress).ConfigureAwait(false);
    }

    // Scan system optimizations
    public static async Task<List<OptimizationItemModel>> ScanAsync(IProgress<string>? progress = null)
    {
        var catalog = GetCatalog();
        return await ScanItemsAsync(catalog, progress).ConfigureAwait(false);
    }

    public static async Task<List<OptimizationItemModel>> ScanAsync(Page? page, IProgress<string>? progress = null)
    {
        if (page != null && page is not HomePage)
        {
            return await ScanPageAsync(page, progress).ConfigureAwait(false);
        }
        return await ScanAsync(progress).ConfigureAwait(false);
    }

    private static async Task<List<OptimizationItemModel>> ScanItemsAsync(List<OptimizationItemModel> items, IProgress<string>? progress = null)
    {
        _ = LogHelper.Log($"[IntelligentEngine] === STEP 1: SCAN SYSTEM START ({items.Count} items) ===");
        progress?.Report("Scanning system hardware profile and registry state...");

        var rollbackTags = ItemRollbackService.GetAvailableRollbackTags();

        await Task.Run(() =>
        {
            foreach (var item in items)
            {
                progress?.Report($"Scanning {item.CategoryDisplay}: {item.Title}...");

                // Detect live state
                var detectedState = SystemStateDetector.DetectState(item.Tag);

                if (detectedState.HasValue)
                {
                    item.IsApplied = detectedState.Value;
                    // Sync saved state
                    try
                    {
                        var regView = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                            ? RegistryView.Registry64
                            : RegistryView.Default;
                        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, regView);
                        using var syncKey = baseKey.CreateSubKey(@"SOFTWARE\RyTuneX\Optimizations");
                        syncKey?.SetValue(item.Tag, detectedState.Value ? 1 : 0, RegistryValueKind.DWord);
                    }
                    catch { }
                }
                else
                {
                    // Fall back to saved registry state
                    var regState = GetSavedRegistryState(item.Tag);
                    item.IsApplied = regState == 1;
                }

                // Check rollback backup
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

        _ = LogHelper.Log($"[IntelligentEngine] === STEP 1: SCAN FINISHED. Scanned {items.Count} items ===");
        return items;
    }

    // Calculate optimization scores
    public static IntelligentScoreModel Analyse(List<OptimizationItemModel> items)
    {
        _ = LogHelper.Log("[IntelligentEngine] === STEP 2: ANALYSE START ===");

        var scoreableItems = items.Where(IsScoreable).ToList();
        int totalWeight = scoreableItems.Sum(i => i.ScoreWeight);
        int appliedWeight = scoreableItems.Where(i => i.IsApplied).Sum(i => i.ScoreWeight);

        int overallScore = totalWeight > 0 ? (int)Math.Round((double)appliedWeight / totalWeight * 100) : 100;

        int perfScore = CalculateCategoryScore(items, OptimizationCategory.Performance);
        int privScore = CalculateCategoryScore(items, OptimizationCategory.PrivacyAndTelemetry);
        int featScore = CalculateCategoryScore(items, OptimizationCategory.FeaturesAndUsability);

        var unappliedRec = scoreableItems.Where(i => !i.IsApplied && (i.Risk == RiskLevel.Safe || i.IsRecommended)).ToList();
        int potentialGainPoints = unappliedRec.Sum(i => i.ScoreWeight);
        int potentialScoreGain = totalWeight > 0 ? (int)Math.Round((double)potentialGainPoints / totalWeight * 100) : 0;

        var result = new IntelligentScoreModel
        {
            OverallScore = overallScore,
            PerformanceScore = perfScore,
            PrivacyScore = privScore,
            FeaturesScore = featScore,
            PotentialScoreGain = potentialScoreGain,
            TotalItemsCount = items.Count,
            OptimalItemsCount = items.Count(i => i.IsApplied),
            RecommendedItemsCount = items.Count(i => i.IsRecommended && !i.IsApplied),
            RollbackableItemsCount = items.Count(i => i.RollbackAvailable),
            SystemSummary = GetHardwareProfileSummary()
        };

        _ = LogHelper.Log($"[IntelligentEngine] Analysis complete: Score={overallScore}%, Perf={perfScore}%, Priv={privScore}%, Feat={featScore}%");
        return result;
    }

    // Generate recommendations
    public static void Recommend(List<OptimizationItemModel> items)
    {
        _ = LogHelper.Log("[IntelligentEngine] === STEP 3: RECOMMEND START ===");

        var totalRamGb = (long)(MemoryHelper.GetTotalPhysicalMemory() / (1024.0 * 1024.0 * 1024.0));
        bool isLaptop = IsLikelyLaptop();
        bool isOnSsd = IsSystemDriveOnSsd();

        foreach (var item in items)
        {
            // Caution items
            if (item.Risk == RiskLevel.Caution)
            {
                item.IsRecommended = false;
                item.RecommendationReason = "Caution: System component or security feature that may be required by specific software.";
                item.IsSelectedForApply = false;
                continue;
            }

            // Cosmetic items
            if (item.Risk == RiskLevel.Cosmetic)
            {
                item.IsRecommended = false;
                item.RecommendationReason = !string.IsNullOrEmpty(item.ImpactDescription)
                    ? $"Optional (Cosmetic): {item.ImpactDescription}"
                    : "Optional (Cosmetic): Visual layout, appearance, or personal workflow preference.";
            }
            // Moderate items
            else if (item.Risk == RiskLevel.Moderate)
            {
                if (item.Tag.Equals("PowerThrottling", StringComparison.OrdinalIgnoreCase))
                {
                    item.IsRecommended = true;
                    item.RecommendationReason = "Recommended: Prevents CPU frequency downclocking on active performance threads.";
                }
                else if (item.Tag.Equals("GpuDriverTweaks", StringComparison.OrdinalIgnoreCase))
                {
                    item.IsRecommended = true;
                    item.RecommendationReason = "Recommended: Prioritizes graphics driver thread responsiveness and GPU scheduling.";
                }
                else if (item.Tag.Equals("UsbPowerSaving", StringComparison.OrdinalIgnoreCase))
                {
                    item.IsRecommended = true;
                    item.RecommendationReason = "Recommended: Prevents USB power cutoff and peripheral input latency.";
                }
                else if (item.Tag.Equals("SysMain", StringComparison.OrdinalIgnoreCase))
                {
                    item.IsRecommended = isOnSsd;
                    item.RecommendationReason = isOnSsd
                        ? "Recommended: Solid-state drive detected. Disabling SuperFetch/SysMain stops unnecessary disk thrashing."
                        : "Moderate: SuperFetch pre-loads apps on mechanical HDDs; optional for SSD upgrades.";
                }
                else if (item.Tag.Equals("Hibernation", StringComparison.OrdinalIgnoreCase))
                {
                    item.IsRecommended = !isLaptop;
                    item.RecommendationReason = !isLaptop
                        ? "Recommended: Desktop PC detected. Disabling hibernation frees gigabytes of storage and saves SSD write cycles."
                        : "Not recommended for laptops: Battery preservation requires hibernation support.";
                }
                else if (item.Tag.Equals("GameBar", StringComparison.OrdinalIgnoreCase))
                {
                    item.IsRecommended = true;
                    item.RecommendationReason = "Recommended: Eliminates background Xbox DVR recording overhead.";
                }
                else if (item.Tag.Equals("CompatibilityAssistant", StringComparison.OrdinalIgnoreCase))
                {
                    item.IsRecommended = true;
                    item.RecommendationReason = "Recommended: Prevents compatibility assistant hooks on application installers.";
                }
                else if (item.Tag.Equals("SMBv1", StringComparison.OrdinalIgnoreCase))
                {
                    item.IsRecommended = true;
                    item.RecommendationReason = "Recommended: Disables legacy, vulnerable SMBv1 protocol per Microsoft security guidance.";
                }
                else if (totalRamGb >= 16 && item.Tag.Equals("ServiceHostSplitting", StringComparison.OrdinalIgnoreCase))
                {
                    item.IsRecommended = true;
                    item.RecommendationReason = $"Recommended: {totalRamGb}GB RAM detected. Clean svchost grouping optimizes process overhead.";
                }
                else
                {
                    item.IsRecommended = false;
                    item.RecommendationReason = "Moderate: Advanced power-user optimization. Included in Advanced / Moderate preset.";
                }
            }
            else
            {
                // Safe items
                item.IsRecommended = true;
                item.RecommendationReason = !string.IsNullOrEmpty(item.ImpactDescription)
                    ? $"Recommended: {item.ImpactDescription}"
                    : "Recommended: Safe, high-impact tweak to boost performance and privacy.";
            }

            // Sync selection status
            if (item.IsApplied)
            {
                item.IsSelectedForApply = false;
                item.RecommendationReason = $"Already Optimal: This setting is currently active and optimized. ({item.RecommendationReason})";
            }
            else
            {
                item.IsSelectedForApply = item.IsRecommended;
            }
        }

        _ = LogHelper.Log($"[IntelligentEngine] Recommendation finished: {items.Count(i => i.IsRecommended)} recommended items.");
    }

    public static (int SelectedCount, int ExpectedScoreGain, List<OptimizationItemModel> SelectedItems) Preview(List<OptimizationItemModel> items)
    {
        _ = LogHelper.Log("[IntelligentEngine] === STEP 4: PREVIEW ===");

        var selected = items.Where(i => i.IsSelectedForApply).ToList();
        var scoreable = items.Where(IsScoreable).ToList();
        int totalWeight = scoreable.Sum(i => i.ScoreWeight);
        int gainPoints = selected.Where(IsScoreable).Sum(i => i.ScoreWeight);
        int gainPct = totalWeight > 0 ? (int)Math.Round((double)gainPoints / totalWeight * 100) : 0;

        _ = LogHelper.Log($"[IntelligentEngine] Preview: {selected.Count} selected, Expected gain: +{gainPct}%");
        return (selected.Count, gainPct, selected);
    }

    // Apply optimizations
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
                // Capture pre-apply backup
                bool preApplyState = item.IsApplied;
                ItemRollbackService.SavePreApplyBackup(item.Tag, preApplyState, item.TechnicalDetails);

                // Execute toggle action
                await OptimizationOptions.ExecuteToggleDirectAsync(item.Tag, true).ConfigureAwait(false);

                // Save state to registry
                try
                {
                    var regView = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                        ? RegistryView.Registry64
                        : RegistryView.Default;
                    using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, regView);
                    using var key = baseKey.CreateSubKey(@"SOFTWARE\RyTuneX\Optimizations");
                    key?.SetValue(item.Tag, 1, RegistryValueKind.DWord);
                }
                catch (Exception regEx)
                {
                    _ = LogHelper.LogError($"[IntelligentEngine] Failed to save registry state for {item.Tag}: {regEx.Message}");
                }

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

    // Verify optimizations
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

    // Rollback single optimization
    public static async Task<bool> RollbackItemAsync(OptimizationItemModel item)
    {
        _ = LogHelper.Log($"[IntelligentEngine] === STEP 7: PER-ITEM ROLLBACK for '{item.Tag}' ===");

        var (hasBackup, preApplyState, _, _) = ItemRollbackService.GetBackupInfo(item.Tag);
        if (!hasBackup)
        {
            _ = LogHelper.LogWarning($"[IntelligentEngine] No rollback backup found for '{item.Tag}'");
            return false;
        }

        bool success = await ItemRollbackService.RollbackItemAsync(item.Tag).ConfigureAwait(false);
        if (success)
        {
            item.IsApplied = preApplyState;
            item.RollbackAvailable = false;
            item.BackupDate = null;
            item.PreApplyValue = null;
            item.VerificationStatus = VerificationStatus.NotVerified;

            _ = LogHelper.Log($"[IntelligentEngine] Rollback succeeded for '{item.Tag}' -> IsApplied={item.IsApplied}");
        }

        return success;
    }

    public static bool IsScoreable(OptimizationItemModel item) =>
        item.Risk == RiskLevel.Safe || item.Risk == RiskLevel.Moderate;

    private static int CalculateCategoryScore(List<OptimizationItemModel> items, OptimizationCategory cat)
    {
        var catItems = items.Where(i => i.Category == cat && IsScoreable(i)).ToList();
        if (catItems.Count == 0) return 100;

        int appliedWeight = catItems.Where(i => i.IsApplied).Sum(i => i.ScoreWeight);
        int totalWeight = catItems.Sum(i => i.ScoreWeight);

        return totalWeight > 0 ? (int)Math.Round((double)appliedWeight / totalWeight * 100) : 100;
    }

    private static int GetSavedRegistryState(string tagName)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                    ? RegistryView.Registry64
                    : RegistryView.Default);
            using var key = baseKey.OpenSubKey(@"SOFTWARE\RyTuneX\Optimizations");

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
            var driveType = IsSystemDriveOnSsd() ? "SSD" : "HDD";
            return $"{cores}-Core CPU • {ramGb} GB RAM • {driveType} • Windows {(Environment.OSVersion.Version.Build >= 22000 ? "11" : "10")}";
        }
        catch
        {
            return "Windows PC";
        }
    }

    private static bool HasNvidiaGpu()
    {
        try
        {
            var regView = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                ? RegistryView.Registry64
                : RegistryView.Default;
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, regView);
            using var svcKey = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\NvTelemetryContainer");
            return svcKey != null;
        }
        catch { return false; }
    }

    private static bool IsLikelyLaptop()
    {
        try
        {
            var regView = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                ? RegistryView.Registry64
                : RegistryView.Default;
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, regView);
            using var batteryKey = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\CmBatt");
            if (batteryKey != null) return true;
            using var batteryKey2 = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Battery");
            return batteryKey2 != null;
        }
        catch { return false; }
    }

    private static bool IsSystemDriveOnSsd()
    {
        try
        {
            var sysRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var driveLetter = Path.GetPathRoot(sysRoot)?.TrimEnd('\\');
            if (string.IsNullOrEmpty(driveLetter)) return false;

            var regView = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                ? RegistryView.Registry64
                : RegistryView.Default;
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, regView);
            using var nvmeKey = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\stornvme");
            if (nvmeKey != null) return true;
            using var iaStorKey = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\iaStorA");
            if (iaStorKey != null) return true;
            return Environment.OSVersion.Version.Build >= 22000;
        }
        catch { return false; }
    }
}
