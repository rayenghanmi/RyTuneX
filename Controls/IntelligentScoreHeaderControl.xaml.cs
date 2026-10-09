using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using RyTuneX.Contracts.Services;
using RyTuneX.Helpers;
using RyTuneX.Models;
using RyTuneX.Services;
using RyTuneX.Views;

namespace RyTuneX.Controls;

public sealed partial class IntelligentScoreHeaderControl : UserControl
{
    public static readonly DependencyProperty TargetCategoryProperty =
        DependencyProperty.Register(
            nameof(TargetCategory),
            typeof(string),
            typeof(IntelligentScoreHeaderControl),
            new PropertyMetadata("All", OnTargetCategoryChanged));

    public string TargetCategory
    {
        get => (string)GetValue(TargetCategoryProperty);
        set => SetValue(TargetCategoryProperty, value);
    }

    private static void OnTargetCategoryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is IntelligentScoreHeaderControl control)
        {
            _ = control.RefreshScoreAsync();
        }
    }

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private bool _isRefreshPending;
    private List<OptimizationItemModel> _allScannedItems = new();
    private Page? _parentPage;
    private string _selectedMode = "Recommended"; // "Recommended", "AllSafe", "Advanced"

    public IntelligentScoreHeaderControl()
    {
        InitializeComponent();
        Loaded += IntelligentScoreHeaderControl_Loaded;
    }

    private void IntelligentScoreHeaderControl_Loaded(object sender, RoutedEventArgs e)
    {
        _parentPage = FindParentPage(this);
        if (PreviewDialog != null)
        {
            PreviewDialog.RequestedTheme = this.ActualTheme;
            if (PreviewDialog.XamlRoot == null && this.XamlRoot != null)
            {
                PreviewDialog.XamlRoot = this.XamlRoot;
            }
        }

        // Auto-detect page category if not explicitly set
        if (TargetCategory == "All" && _parentPage != null)
        {
            var pageName = _parentPage.GetType().Name;
            if (pageName.Contains("OptimizeSystem", StringComparison.OrdinalIgnoreCase))
            {
                TargetCategory = "Performance";
            }
            else if (pageName.Contains("Privacy", StringComparison.OrdinalIgnoreCase))
            {
                TargetCategory = "PrivacyAndTelemetry";
            }
            else if (pageName.Contains("Features", StringComparison.OrdinalIgnoreCase))
            {
                TargetCategory = "FeaturesAndUsability";
            }
        }

        LocalizeStaticControls();
        UpdateModeButtonsUI();
        _ = RefreshScoreAsync();
    }

    private void LocalizeStaticControls()
    {
        if (ScoreLabelText != null) ScoreLabelText.Text = "Intelligent_ScoreTitle".GetLocalized();
        if (HealthGradeText != null) HealthGradeText.Text = "Intelligent_HealthGrade_Scanning".GetLocalized();

        if (DomainTitleText != null && TargetCategory == "All") DomainTitleText.Text = "Intelligent_Domain_Optimizer".GetLocalized();
        if (SystemSummaryText != null) SystemSummaryText.Text = "Intelligent_DetectingHardware".GetLocalized();
        if (StatusMessageText != null && TargetCategory == "All") StatusMessageText.Text = "Intelligent_StatusMessageDefault".GetLocalized();
        if (DomainMetric3Text != null) DomainMetric3Text.Text = "Intelligent_Metric_NonIntrusive".GetLocalized();

        if (ModeRecText != null) ModeRecText.Text = "Intelligent_Mode_Rec".GetLocalized();
        if (ModeSafeText != null) ModeSafeText.Text = "Intelligent_Mode_Safe".GetLocalized();
        if (ModeAdvText != null) ModeAdvText.Text = "Intelligent_Mode_Adv".TryGetLocalized() ?? "Intelligent_Badge_Moderate".TryGetLocalized() ?? "Mod";

        if (ModeRecommendedBtn != null) ToolTipService.SetToolTip(ModeRecommendedBtn, "Intelligent_Mode_Rec_Tooltip".GetLocalized());
        if (ModeAllSafeBtn != null) ToolTipService.SetToolTip(ModeAllSafeBtn, "Intelligent_Mode_Safe_Tooltip".GetLocalized());
        if (ModeAdvancedBtn != null) ToolTipService.SetToolTip(ModeAdvancedBtn, "Intelligent_Mode_Adv_Tooltip".TryGetLocalized() ?? "Moderate: Includes power-user & moderate tweaks");

        if (MainApplyBtnText != null) MainApplyBtnText.Text = "Intelligent_Btn_Apply".GetLocalized();
        if (PreviewBtnText != null) PreviewBtnText.Text = "Intelligent_Btn_Preview".GetLocalized();
        if (RollbackBtnText != null) RollbackBtnText.Text = "Intelligent_Btn_Rollback".GetLocalized();

        if (TuneByDomainHeaderText != null) TuneByDomainHeaderText.Text = "Intelligent_TuneByDomain".GetLocalized();
        if (HomeNavPerfText != null) HomeNavPerfText.Text = "Intelligent_Domain_PerfTitle".GetLocalized();
        if (HomeNavPrivText != null) HomeNavPrivText.Text = "Intelligent_Domain_PrivTitle".GetLocalized();
        if (HomeNavFeatText != null) HomeNavFeatText.Text = "Intelligent_Domain_FeatTitle".GetLocalized();

        if (PreviewDialog != null)
        {
            PreviewDialog.Title = "Intelligent_Preview_DialogDefaultTitle".GetLocalized();
            PreviewDialog.PrimaryButtonText = "Intelligent_Preview_ApplyBtn".GetLocalized();
            PreviewDialog.CloseButtonText = "Intelligent_Preview_CancelBtn".GetLocalized();
        }
        if (PreviewDialogDescText != null) PreviewDialogDescText.Text = "Intelligent_Preview_Desc".GetLocalized();

        UpdateApplyButtonText();
    }

    public async Task RefreshScoreAsync()
    {
        if (!await _refreshLock.WaitAsync(0))
        {
            _isRefreshPending = true;
            return;
        }

        try
        {
            do
            {
                _isRefreshPending = false;
                try
                {
                    MainProgressBar.Visibility = Visibility.Visible;
                    MainProgressBar.IsIndeterminate = true;

                    if (_parentPage != null && _parentPage is not HomePage)
                    {
                        _allScannedItems = await IntelligentOptimizationEngine.ScanPageAsync(_parentPage);
                    }
                    else
                    {
                        _allScannedItems = await IntelligentOptimizationEngine.ScanAsync();
                    }

                    // Sync visual toggle states
                    if (_parentPage != null)
                    {
                        var pageToggles = IntelligentCardEnhancer.FindVisualChildren<ToggleSwitch>(_parentPage);
                        foreach (var t in pageToggles)
                        {
                            if (t.Tag is string tTag && !string.IsNullOrEmpty(tTag))
                            {
                                var item = _allScannedItems.FirstOrDefault(i => string.Equals(i.Tag, tTag, StringComparison.OrdinalIgnoreCase));
                                if (item != null)
                                {
                                    item.IsApplied = t.IsOn;
                                }
                            }
                        }
                    }

                    var score = IntelligentOptimizationEngine.Analyse(_allScannedItems);
                    IntelligentOptimizationEngine.Recommend(_allScannedItems);

                    UpdateUI(score);
                }
                finally
                {
                    MainProgressBar.IsIndeterminate = false;
                    MainProgressBar.Visibility = Visibility.Collapsed;
                }
            } while (_isRefreshPending);
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"[IntelligentScoreHeader] Error refreshing score: {ex.Message}");
            MainProgressBar.Visibility = Visibility.Collapsed;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private int CalculateItemsScore(List<OptimizationItemModel> items)
    {
        var scoreable = items.Where(i => i.Risk == RiskLevel.Safe || i.Risk == RiskLevel.Moderate).ToList();
        if (scoreable.Count == 0) return 100;

        int appliedWeight = scoreable.Where(i => i.IsApplied).Sum(i => i.ScoreWeight);
        int totalWeight = scoreable.Sum(i => i.ScoreWeight);

        return totalWeight > 0 ? (int)Math.Round((double)appliedWeight / totalWeight * 100) : 100;
    }

    private void UpdateUI(IntelligentScoreModel score)
    {
        var category = TargetCategory;
        SystemSummaryText.Text = score.SystemSummary;
        var pageTags = GetParentPageToggleTags();

        if (category == "Performance")
        {
            DomainIcon.Glyph = "\uE9D9";
            DomainTitleText.Text = "Intelligent_Domain_PerfTitle".GetLocalized();
            ScoreLabelText.Text = "Intelligent_ScoreTitle".GetLocalized();

            var catItems = pageTags.Count > 0
                ? _allScannedItems.Where(i => pageTags.Contains(i.Tag)).ToList()
                : _allScannedItems.Where(i => i.Category == OptimizationCategory.Performance).ToList();

            int pageScore = CalculateItemsScore(catItems);
            ScoreText.Text = $"{pageScore}%";
            ScoreRing.Value = pageScore;
            HealthGradeText.Text = GetGrade(pageScore);

            int active = catItems.Count(i => i.IsApplied);
            int total = catItems.Count;
            int recCount = catItems.Count(i => i.IsRecommended && !i.IsApplied);
            int rollCount = catItems.Count(i => i.RollbackAvailable);

            ActiveCountText.Text = string.Format("Intelligent_ActiveCount".GetLocalized(), active, total);
            StatusMessageText.Text = "Intelligent_Domain_PerfDesc".GetLocalized();

            CategoryMiniScoresGrid.Visibility = Visibility.Collapsed;
            DomainSubMetricsPanel.Visibility = Visibility.Visible;
            PageActionPanel.Visibility = Visibility.Visible;
            HomeNavigationPanel.Visibility = Visibility.Collapsed;

            DomainMetric1Text.Text = string.Format("Intelligent_RecommendedCount".GetLocalized(), recCount);
            DomainMetric2Text.Text = string.Format("Intelligent_RollbacksCount".GetLocalized(), rollCount);
            DomainMetric3Text.Text = "Intelligent_Metric_LowLatency".GetLocalized();

            CalculateGain(catItems);
        }
        else if (category == "PrivacyAndTelemetry")
        {
            DomainIcon.Glyph = "\uE7B3";
            DomainTitleText.Text = "Intelligent_Domain_PrivTitle".GetLocalized();
            ScoreLabelText.Text = "Intelligent_ScoreTitle".GetLocalized();

            var catItems = pageTags.Count > 0
                ? _allScannedItems.Where(i => pageTags.Contains(i.Tag)).ToList()
                : _allScannedItems.Where(i => i.Category == OptimizationCategory.PrivacyAndTelemetry).ToList();

            int pageScore = CalculateItemsScore(catItems);
            ScoreText.Text = $"{pageScore}%";
            ScoreRing.Value = pageScore;
            HealthGradeText.Text = GetGrade(pageScore);

            int active = catItems.Count(i => i.IsApplied);
            int total = catItems.Count;
            int recCount = catItems.Count(i => i.IsRecommended && !i.IsApplied);
            int rollCount = catItems.Count(i => i.RollbackAvailable);

            ActiveCountText.Text = string.Format("Intelligent_ActiveCount".GetLocalized(), active, total);
            StatusMessageText.Text = "Intelligent_Domain_PrivDesc".GetLocalized();

            CategoryMiniScoresGrid.Visibility = Visibility.Collapsed;
            DomainSubMetricsPanel.Visibility = Visibility.Visible;
            PageActionPanel.Visibility = Visibility.Visible;
            HomeNavigationPanel.Visibility = Visibility.Collapsed;

            DomainMetric1Text.Text = string.Format("Intelligent_RecommendedCount".GetLocalized(), recCount);
            DomainMetric2Text.Text = string.Format("Intelligent_RollbacksCount".GetLocalized(), rollCount);
            DomainMetric3Text.Text = "Intelligent_Metric_DataShield".GetLocalized();

            CalculateGain(catItems);
        }
        else if (category == "FeaturesAndUsability")
        {
            DomainIcon.Glyph = "\uE74C";
            DomainTitleText.Text = "Intelligent_Domain_FeatTitle".GetLocalized();
            ScoreLabelText.Text = "Intelligent_ScoreTitle".GetLocalized();

            var catItems = pageTags.Count > 0
                ? _allScannedItems.Where(i => pageTags.Contains(i.Tag)).ToList()
                : _allScannedItems.Where(i => i.Category == OptimizationCategory.FeaturesAndUsability).ToList();

            int pageScore = CalculateItemsScore(catItems);
            ScoreText.Text = $"{pageScore}%";
            ScoreRing.Value = pageScore;
            HealthGradeText.Text = GetGrade(pageScore);

            int active = catItems.Count(i => i.IsApplied);
            int total = catItems.Count;
            int recCount = catItems.Count(i => i.IsRecommended && !i.IsApplied);
            int rollCount = catItems.Count(i => i.RollbackAvailable);

            ActiveCountText.Text = string.Format("Intelligent_ActiveCount".GetLocalized(), active, total);
            StatusMessageText.Text = "Intelligent_Domain_FeatDesc".GetLocalized();

            CategoryMiniScoresGrid.Visibility = Visibility.Collapsed;
            DomainSubMetricsPanel.Visibility = Visibility.Visible;
            PageActionPanel.Visibility = Visibility.Visible;
            HomeNavigationPanel.Visibility = Visibility.Collapsed;

            DomainMetric1Text.Text = string.Format("Intelligent_RecommendedCount".GetLocalized(), recCount);
            DomainMetric2Text.Text = string.Format("Intelligent_RollbacksCount".GetLocalized(), rollCount);
            DomainMetric3Text.Text = "Intelligent_Metric_Productivity".GetLocalized();

            CalculateGain(catItems);
        }
        else
        {
            DomainIcon.Glyph = "\uE9F5";
            DomainTitleText.Text = "Intelligent_Domain_GlobalTitle".GetLocalized();
            ScoreLabelText.Text = "Intelligent_ScoreTitle".GetLocalized();
            ScoreText.Text = $"{score.OverallScore}%";
            ScoreRing.Value = score.OverallScore;
            HealthGradeText.Text = score.HealthGrade;

            int active = _allScannedItems.Count(i => i.IsApplied);
            int total = _allScannedItems.Count;

            ActiveCountText.Text = string.Format("Intelligent_OptimizationsActive".GetLocalized(), active, total);
            StatusMessageText.Text = "Intelligent_Domain_GlobalDesc".GetLocalized();

            CategoryMiniScoresGrid.Visibility = Visibility.Visible;
            DomainSubMetricsPanel.Visibility = Visibility.Collapsed;
            PageActionPanel.Visibility = Visibility.Collapsed;
            HomeNavigationPanel.Visibility = Visibility.Visible;

            if (score.PotentialScoreGain > 0)
            {
                PotentialGainBorder.Visibility = Visibility.Visible;
                PotentialGainText.Text = string.Format("Intelligent_PotentialGain".GetLocalized(), score.PotentialScoreGain);
            }
            else
            {
                PotentialGainBorder.Visibility = Visibility.Collapsed;
            }

            PerfScoreText.Text = $"{"Intelligent_Category_Performance".GetLocalized()}: {score.PerformanceScore}%";
            PerfScoreProgress.Value = score.PerformanceScore;

            PrivScoreText.Text = $"{"Intelligent_Category_Privacy".GetLocalized()}: {score.PrivacyScore}%";
            PrivScoreProgress.Value = score.PrivacyScore;

            FeatScoreText.Text = $"{"Intelligent_Category_Usability".GetLocalized()}: {score.FeaturesScore}%";
            FeatScoreProgress.Value = score.FeaturesScore;
        }

        UpdateApplyButtonText();
    }

    private void CalculateGain(List<OptimizationItemModel> catItems)
    {
        var scoreable = catItems.Where(i => i.Risk == RiskLevel.Safe || i.Risk == RiskLevel.Moderate).ToList();
        var unapplied = scoreable.Where(i => !i.IsApplied && (i.Risk == RiskLevel.Safe || i.IsRecommended));
        int gainPoints = unapplied.Sum(i => i.ScoreWeight);
        int totalWeight = scoreable.Sum(i => i.ScoreWeight);
        int gainPct = totalWeight > 0 ? (int)Math.Round((double)gainPoints / totalWeight * 100) : 0;

        if (gainPct > 0)
        {
            PotentialGainBorder.Visibility = Visibility.Visible;
            PotentialGainText.Text = string.Format("Intelligent_PotentialGain".GetLocalized(), gainPct);
        }
        else
        {
            PotentialGainBorder.Visibility = Visibility.Collapsed;
        }
    }

    private static string GetGrade(int score) => score switch
    {
        >= 90 => "Intelligent_HealthGrade_Excellent".GetLocalized(),
        >= 75 => "Intelligent_HealthGrade_Good".GetLocalized(),
        >= 60 => "Intelligent_HealthGrade_Fair".GetLocalized(),
        >= 40 => "Intelligent_HealthGrade_NeedsTuning".GetLocalized(),
        _ => "Intelligent_HealthGrade_Unoptimized".GetLocalized()
    };

    private void ModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton clickedBtn && clickedBtn.Tag is string mode)
        {
            _selectedMode = mode;
            UpdateModeButtonsUI();
            UpdateApplyButtonText();
        }
    }

    private void UpdateModeButtonsUI()
    {
        if (ModeRecommendedBtn != null) ModeRecommendedBtn.IsChecked = _selectedMode == "Recommended";
        if (ModeAllSafeBtn != null) ModeAllSafeBtn.IsChecked = _selectedMode is "AllSafe" or "Safe";
        if (ModeAdvancedBtn != null) ModeAdvancedBtn.IsChecked = _selectedMode is "Advanced" or "Moderate";
    }

    private void UpdateApplyButtonText()
    {
        if (MainApplyBtnText == null) return;

        var candidates = GetCandidateItemsForMode(_selectedMode);
        int count = candidates.Count;

        string modeLabel = _selectedMode switch
        {
            "AllSafe" or "Safe" => "Intelligent_Mode_Safe".GetLocalized(),
            "Advanced" or "Moderate" => "Intelligent_Badge_Moderate".TryGetLocalized() ?? "Intelligent_Mode_Adv".GetLocalized(),
            _ => "Intelligent_Mode_Rec".GetLocalized()
        };

        MainApplyBtnText.Text = count > 0
            ? string.Format("Intelligent_Apply_Format".GetLocalized(), modeLabel, count)
            : string.Format("Intelligent_Apply_Active".GetLocalized(), modeLabel);
    }

    private List<OptimizationItemModel> GetCandidateItemsForMode(string mode)
    {
        var domainFiltered = (_parentPage != null && _parentPage is not HomePage)
            ? _allScannedItems
            : _allScannedItems.Where(i => GetParentPageToggleTags().Count == 0 || GetParentPageToggleTags().Contains(i.Tag)).ToList();

        return mode switch
        {
            "Cosmetic" => domainFiltered.Where(i => !i.IsApplied && i.Risk == RiskLevel.Cosmetic).ToList(),
            "AllSafe" or "Safe" => domainFiltered.Where(i => !i.IsApplied && i.Risk == RiskLevel.Safe).ToList(),
            "Advanced" or "Moderate" => domainFiltered.Where(i => !i.IsApplied && (i.Risk == RiskLevel.Safe || i.Risk == RiskLevel.Moderate)).ToList(),
            _ => domainFiltered.Where(i => !i.IsApplied && i.IsRecommended).ToList()
        };
    }

    private async void ApplySelectedMode_Click(object sender, RoutedEventArgs e)
    {
        var itemsToApply = GetCandidateItemsForMode(_selectedMode);

        if (itemsToApply.Count == 0)
        {
            string modeName = _selectedMode switch
            {
                "AllSafe" or "Safe" => "Intelligent_Mode_Safe".GetLocalized(),
                "Advanced" or "Moderate" => "Intelligent_Badge_Moderate".TryGetLocalized() ?? "Intelligent_Mode_Adv".GetLocalized(),
                _ => "Intelligent_Mode_Rec".GetLocalized()
            };
            StatusMessageText.Text = string.Format("Intelligent_Status_AllActive".GetLocalized(), modeName);
            return;
        }

        await ExecuteApplyAsync(itemsToApply);
    }

    private async void Preview_Click(object sender, RoutedEventArgs e)
    {
        var domainFiltered = (_parentPage != null && _parentPage is not HomePage)
            ? _allScannedItems
            : _allScannedItems.Where(i => GetParentPageToggleTags().Count == 0 || GetParentPageToggleTags().Contains(i.Tag)).ToList();

        var allUnappliedOnPage = domainFiltered
            .Where(i => !i.IsApplied)
            .ToList();

        if (allUnappliedOnPage.Count == 0)
        {
            StatusMessageText.Text = "Intelligent_Preview_Empty".GetLocalized();
            return;
        }

        // Pre-select items based on currently toggled mode
        var modeCandidates = new HashSet<string>(GetCandidateItemsForMode(_selectedMode).Select(i => i.Tag), StringComparer.OrdinalIgnoreCase);
        foreach (var item in allUnappliedOnPage)
        {
            item.IsSelectedForApply = modeCandidates.Contains(item.Tag);
        }

        PreviewDialog.RequestedTheme = this.ActualTheme;
        if (PreviewDialog.XamlRoot == null && this.XamlRoot != null)
        {
            PreviewDialog.XamlRoot = this.XamlRoot;
        }

        string modeName = _selectedMode switch
        {
            "AllSafe" or "Safe" => "Intelligent_Mode_Safe".GetLocalized(),
            "Advanced" or "Moderate" => "Intelligent_Badge_Moderate".TryGetLocalized() ?? "Intelligent_Mode_Adv".GetLocalized(),
            _ => "Intelligent_Mode_Rec".GetLocalized()
        };
        PreviewDialog.Title = string.Format("Intelligent_Preview_Title".GetLocalized(), modeName);
        PreviewDialog.PrimaryButtonText = "Intelligent_Preview_ApplyBtn".GetLocalized();
        PreviewDialog.CloseButtonText = "Intelligent_Preview_CancelBtn".GetLocalized();

        PreviewListView.ItemsSource = allUnappliedOnPage;
        var res = await PreviewDialog.ShowAsync();
        if (res == ContentDialogResult.Primary)
        {
            var selectedToApply = allUnappliedOnPage.Where(i => i.IsSelectedForApply).ToList();
            if (selectedToApply.Count > 0)
            {
                await ExecuteApplyAsync(selectedToApply);
            }
        }
    }

    private async void RollbackPageItems_Click(object sender, RoutedEventArgs e)
    {
        var domainFiltered = (_parentPage != null && _parentPage is not HomePage)
            ? _allScannedItems
            : _allScannedItems.Where(i => GetParentPageToggleTags().Count == 0 || GetParentPageToggleTags().Contains(i.Tag)).ToList();

        var itemsToRollback = domainFiltered
            .Where(i => i.RollbackAvailable)
            .ToList();

        if (itemsToRollback.Count == 0)
        {
            StatusMessageText.Text = "Intelligent_Status_NoRollbacks".GetLocalized();
            return;
        }

        MainProgressBar.Visibility = Visibility.Visible;
        StatusMessageText.Text = string.Format("Intelligent_Status_RollingBack".GetLocalized(), itemsToRollback.Count);

        foreach (var item in itemsToRollback)
        {
            await IntelligentOptimizationEngine.RollbackItemAsync(item);
        }

        // Sync page toggle switches
        if (_parentPage != null)
        {
            var rolledMap = itemsToRollback.ToDictionary(i => i.Tag, i => i.IsApplied, StringComparer.OrdinalIgnoreCase);
            foreach (var toggle in IntelligentCardEnhancer.FindVisualChildren<ToggleSwitch>(_parentPage))
            {
                if (toggle.Tag is string tag && rolledMap.TryGetValue(tag, out var newState))
                {
                    IntelligentCardEnhancer.SetToggleIsOnSilently(toggle, newState);
                }
            }
        }

        await RefreshScoreAsync();
        if (_parentPage != null)
        {
            IntelligentCardEnhancer.EnhancePage(_parentPage);
        }

        StatusMessageText.Text = string.Format("Intelligent_Status_RollbackDone".GetLocalized(), itemsToRollback.Count);
    }

    private async Task ExecuteApplyAsync(List<OptimizationItemModel> items)
    {
        try
        {
            MainProgressBar.Visibility = Visibility.Visible;
            MainProgressBar.IsIndeterminate = false;

            var applyProgress = new Progress<(int Current, int Total, string Status)>(p =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    MainProgressBar.Value = ((double)p.Current / p.Total) * 100;
                    StatusMessageText.Text = string.Format("Intelligent_Status_Applying".GetLocalized(), p.Current, p.Total, p.Status);
                });
            });

            await IntelligentOptimizationEngine.ApplyAsync(items, applyProgress);

            StatusMessageText.Text = "Intelligent_Status_Verifying".GetLocalized();
            await IntelligentOptimizationEngine.VerifyAsync(items);

            // Sync page toggle switches
            if (_parentPage != null)
            {
                var appliedTags = new HashSet<string>(items.Select(i => i.Tag), StringComparer.OrdinalIgnoreCase);
                foreach (var toggle in IntelligentCardEnhancer.FindVisualChildren<ToggleSwitch>(_parentPage))
                {
                    if (toggle.Tag is string tag && appliedTags.Contains(tag))
                    {
                        IntelligentCardEnhancer.SetToggleIsOnSilently(toggle, true);
                    }
                }
            }

            await RefreshScoreAsync();

            if (_parentPage != null)
            {
                IntelligentCardEnhancer.EnhancePage(_parentPage);
            }

            StatusMessageText.Text = string.Format("Intelligent_Status_Success".GetLocalized(), items.Count);

            // Notify completion to trigger review popup
            ReviewPromptHelper.NotifyOptimizationCompleted(XamlRoot);
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"[IntelligentScoreHeader] Apply error: {ex.Message}");
            StatusMessageText.Text = $"Error: {ex.Message}";
        }
        finally
        {
            MainProgressBar.Visibility = Visibility.Collapsed;
        }
    }

    private void NavigateToPerformance_Click(object sender, RoutedEventArgs e) =>
        App.GetService<INavigationService>().NavigateTo(typeof(OptimizeSystemPage).FullName!);

    private void NavigateToPrivacy_Click(object sender, RoutedEventArgs e) =>
        App.GetService<INavigationService>().NavigateTo(typeof(PrivacyPage).FullName!);

    private void NavigateToFeatures_Click(object sender, RoutedEventArgs e) =>
        App.GetService<INavigationService>().NavigateTo(typeof(FeaturesPage).FullName!);

    private HashSet<string> GetParentPageToggleTags()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (_parentPage == null) return set;

        foreach (var toggle in IntelligentCardEnhancer.FindVisualChildren<ToggleSwitch>(_parentPage))
        {
            if (toggle.Tag is string tag && !string.IsNullOrEmpty(tag))
            {
                set.Add(tag);
            }
        }
        return set;
    }

    private static Page? FindParentPage(DependencyObject element)
    {
        DependencyObject current = element;
        while (current != null)
        {
            if (current is Page page) return page;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
