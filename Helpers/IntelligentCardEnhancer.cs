using CommunityToolkit.WinUI.Controls;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using RyTuneX.Controls;
using RyTuneX.Models;
using RyTuneX.Services;

namespace RyTuneX.Helpers;

public static class IntelligentCardEnhancer
{
    private static readonly object Sentinel = new();
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ToggleSwitch, object> HookedToggles = new();
    private static readonly HashSet<ToggleSwitch> SuppressedToggles = new();

    public static bool IsSuppressed(ToggleSwitch toggle)
    {
        lock (SuppressedToggles)
        {
            return SuppressedToggles.Contains(toggle);
        }
    }

    public static void SetToggleIsOnSilently(ToggleSwitch toggle, bool isOn)
    {
        if (toggle.IsOn == isOn) return;

        lock (SuppressedToggles)
        {
            SuppressedToggles.Add(toggle);
        }

        try
        {
            toggle.IsOn = isOn;
        }
        finally
        {
            lock (SuppressedToggles)
            {
                SuppressedToggles.Remove(toggle);
            }
        }
    }

    // Enhance page controls
    public static void EnhancePage(Page page)
    {
        if (page == null) return;

        try
        {
            var toggleSwitches = GetAllToggleSwitches(page);
            _ = LogHelper.Log($"[IntelligentCardEnhancer] Enhancing page '{page.GetType().Name}' with {toggleSwitches.Count} toggles.");

            foreach (var toggle in toggleSwitches)
            {
                var tagName = (toggle.Tag as string) ?? toggle.Name;
                if (!string.IsNullOrEmpty(tagName))
                {
                    try
                    {
                        EnhanceToggleControl(page, toggle, tagName);
                    }
                    catch (Exception itemEx)
                    {
                        _ = LogHelper.LogError($"[IntelligentCardEnhancer] Error enhancing toggle '{tagName}': {itemEx.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"[IntelligentCardEnhancer] Error enhancing page {page.GetType().Name}: {ex.Message}");
        }
    }

    private static void EnhanceToggleControl(Page page, ToggleSwitch toggle, string tag)
    {
        // Handle toggle state change
        if (HookedToggles.TryAdd(toggle, Sentinel))
        {
            toggle.Toggled += async (s, e) =>
            {
                if (IsSuppressed(toggle))
                {
                    return;
                }

                try
                {
                    // Save pre-apply backup
                    if (toggle.IsOn)
                    {
                        var itemModel = IntelligentOptimizationEngine.GetItemByTag(tag);
                        var details = itemModel?.TechnicalDetails ?? GetTechnicalDetailsForTag(tag);
                        ItemRollbackService.SavePreApplyBackup(tag, false, details);
                    }

                    // Refresh this card's controls
                    EnhanceToggleControl(page, toggle, tag);

                    // Refresh score header on page if present
                    var scoreHeader = FindVisualChildren<IntelligentScoreHeaderControl>(page).FirstOrDefault();
                    if (scoreHeader != null)
                    {
                        await scoreHeader.RefreshScoreAsync();
                    }
                }
                catch (Exception ex)
                {
                    _ = LogHelper.LogError($"[IntelligentCardEnhancer] Error handling toggle event for {tag}: {ex.Message}");
                }
            };
        }

        // Query backup info and catalog metadata
        var (hasBackup, preState, backupDt, details) = ItemRollbackService.GetBackupInfo(tag);
        var settingsCard = FindParent<SettingsCard>(toggle);
        var catalogItem = IntelligentOptimizationEngine.GetItemByTag(tag)
            ?? OptimizeFunctionInspector.CreateItemModel(tag, page, toggle, settingsCard);

        if (settingsCard != null)
        {
            EnhanceSettingsCard(page, settingsCard, toggle, tag, hasBackup, backupDt, details, catalogItem);
        }
        else
        {
            EnhanceStandaloneToggle(page, toggle, tag, hasBackup, backupDt, details, catalogItem);
        }
    }

    private static void EnhanceSettingsCard(Page page, SettingsCard settingsCard, ToggleSwitch toggle, string tag, bool hasBackup, DateTime? backupDt, string? details, OptimizationItemModel? catalogItem)
    {
        StackPanel? actionPanel;

        if (settingsCard.Content is StackPanel existingStack && existingStack.Name == "IntelligentCardStack")
        {
            actionPanel = existingStack;
        }
        else
        {
            var originalContent = settingsCard.Content;

            actionPanel = new StackPanel
            {
                Name = "IntelligentCardStack",
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                VerticalAlignment = VerticalAlignment.Center
            };

            settingsCard.Content = null;

            if (originalContent is UIElement originalElem)
            {
                actionPanel.Children.Add(originalElem);
            }

            settingsCard.Content = actionPanel;
        }

        var existingBar = actionPanel.Children.OfType<Border>().FirstOrDefault(b => (string?)b.Tag == "IntelligentBar");
        if (existingBar != null)
        {
            actionPanel.Children.Remove(existingBar);
        }

        var intelligentBar = CreateIntelligentBar(page, toggle, tag, hasBackup, backupDt, details, catalogItem);
        actionPanel.Children.Insert(0, intelligentBar);
    }

    private static void EnhanceStandaloneToggle(Page page, ToggleSwitch toggle, string tag, bool hasBackup, DateTime? backupDt, string? details, OptimizationItemModel? catalogItem)
    {
        var parentPanel = VisualTreeHelper.GetParent(toggle) as Panel;
        if (parentPanel == null) return;

        if (parentPanel is StackPanel sp)
        {
            var existingBar = sp.Children.OfType<Border>().FirstOrDefault(b => (string?)b.Tag == "IntelligentBar");
            if (existingBar != null) sp.Children.Remove(existingBar);

            var intelligentBar = CreateIntelligentBar(page, toggle, tag, hasBackup, backupDt, details, catalogItem);
            int idx = sp.Children.IndexOf(toggle);
            sp.Children.Insert(Math.Max(0, idx), intelligentBar);
        }
    }

    private static Border CreateIntelligentBar(Page page, ToggleSwitch toggle, string tag, bool hasBackup, DateTime? backupDt, string? details, OptimizationItemModel? catalogItem)
    {
        var risk = catalogItem?.Risk ?? RiskLevel.Safe;
        var scoreWeight = catalogItem?.ScoreWeight ?? 5;
        var isOptimal = toggle.IsOn;

        var intelligentBar = new Border
        {
            Tag = "IntelligentBar",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };

        var barStack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Risk level badge
        var (riskLabel, riskBg, riskFg) = GetRiskBadgeInfo(risk, catalogItem?.IsRecommended ?? false, isOptimal);
        var riskBadge = new Border
        {
            Background = riskBg,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 2, 6, 2),
            VerticalAlignment = VerticalAlignment.Center
        };
        riskBadge.Child = new TextBlock
        {
            Text = riskLabel,
            FontSize = 10,
            Foreground = riskFg,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        if (catalogItem?.IsRecommended == true && !isOptimal)
        {
            ToolTipService.SetToolTip(riskBadge, $"{riskLabel} • {"Intelligent_Badge_Recommended".GetLocalized()}");
        }
        else
        {
            ToolTipService.SetToolTip(riskBadge, riskLabel);
        }
        barStack.Children.Add(riskBadge);

        // Score weight chip
        if (!isOptimal && risk != RiskLevel.Cosmetic)
        {
            var scoreChip = new Border
            {
                Background = GetResourceBrush("SubtleFillColorSecondaryBrush", new SolidColorBrush(Windows.UI.Color.FromArgb(30, 128, 128, 128))),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(5, 2, 5, 2),
                VerticalAlignment = VerticalAlignment.Center
            };
            scoreChip.Child = new TextBlock
            {
                Text = string.Format("Intelligent_PointsGain".GetLocalized(), scoreWeight),
                FontSize = 10,
                Foreground = GetResourceBrush("TextFillColorSecondaryBrush", new SolidColorBrush(Colors.Gray)),
                FontWeight = Microsoft.UI.Text.FontWeights.Medium
            };
            barStack.Children.Add(scoreChip);
        }

        // Rollback button
        var rollbackBtn = new Button
        {
            IsEnabled = hasBackup,
            Style = GetResourceStyle("DefaultButtonStyle"),
            Padding = new Thickness(6, 3, 6, 3),
            VerticalAlignment = VerticalAlignment.Center
        };

        var (_, preState, _, _) = ItemRollbackService.GetBackupInfo(tag);
        string preStateStr = preState ? "Intelligent_State_On".GetLocalized() : "Intelligent_State_Off".GetLocalized();
        ToolTipService.SetToolTip(rollbackBtn, hasBackup
            ? string.Format("Intelligent_Flyout_PreApplyBackup".GetLocalized(), $"{backupDt:g}", preStateStr)
            : "Intelligent_Flyout_NoBackup".GetLocalized());

        var rollbackStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        rollbackStack.Children.Add(new FontIcon { Glyph = "\uE7A7", FontSize = 11 });
        rollbackStack.Children.Add(new TextBlock { Text = "Intelligent_Btn_Rollback".GetLocalized(), FontSize = 11 });
        rollbackBtn.Content = rollbackStack;

        rollbackBtn.Click += async (s, e) =>
        {
            rollbackBtn.IsEnabled = false;
            _ = LogHelper.Log($"[IntelligentCardEnhancer] User clicked per-item rollback for tag '{tag}'");

            // Capture state before rollback
            var (_, targetState, _, _) = ItemRollbackService.GetBackupInfo(tag);

            bool success = await ItemRollbackService.RollbackItemAsync(tag);
            if (success)
            {
                SetToggleIsOnSilently(toggle, targetState);

                var scoreHeader = FindVisualChildren<IntelligentScoreHeaderControl>(page).FirstOrDefault();
                if (scoreHeader != null)
                {
                    await scoreHeader.RefreshScoreAsync();
                }

                EnhanceToggleControl(page, toggle, tag);
            }
            else
            {
                rollbackBtn.IsEnabled = true;
            }
        };

        barStack.Children.Add(rollbackBtn);

        // Details flyout button
        var detailsBtn = new Button
        {
            Style = GetResourceStyle("DefaultButtonStyle"),
            Padding = new Thickness(6, 3, 6, 3),
            VerticalAlignment = VerticalAlignment.Center
        };

        var detailsStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 3 };
        detailsStack.Children.Add(new FontIcon { Glyph = "\uE946", FontSize = 11 });
        detailsBtn.Content = detailsStack;
        ToolTipService.SetToolTip(detailsBtn, "Intelligent_Flyout_TechnicalDetails".GetLocalized());

        var flyout = new Flyout();
        var flyoutStack = new StackPanel { Width = 340, Spacing = 8 };

        var titleBlock = new TextBlock
        {
            Text = catalogItem?.Title ?? tag,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap
        };
        flyoutStack.Children.Add(titleBlock);

        var riskTextBlock = new TextBlock
        {
            Text = string.Format("Intelligent_Flyout_CategoryRisk".GetLocalized(), catalogItem?.CategoryDisplay ?? "Intelligent_General_Optimization".GetLocalized(), risk),
            FontSize = 11,
            Foreground = GetResourceBrush("TextFillColorSecondaryBrush", new SolidColorBrush(Colors.Gray))
        };
        flyoutStack.Children.Add(riskTextBlock);

        var desc = !string.IsNullOrEmpty(catalogItem?.Description)
            ? catalogItem.Description
            : OptimizeFunctionInspector.GetFunctionInfo(tag).ActionSummary;

        if (!string.IsNullOrEmpty(desc))
        {
            var descBlock = new TextBlock
            {
                Text = desc,
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap
            };
            flyoutStack.Children.Add(descBlock);
        }

        var impact = !string.IsNullOrEmpty(catalogItem?.ImpactDescription)
            ? catalogItem.ImpactDescription
            : OptimizeFunctionInspector.GetFunctionInfo(tag).ActionSummary;

        if (!string.IsNullOrEmpty(impact) && impact != desc)
        {
            var impactBorder = new Border
            {
                Background = GetResourceBrush("SubtleFillColorSecondaryBrush", new SolidColorBrush(Windows.UI.Color.FromArgb(20, 128, 128, 128))),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4, 8, 4)
            };
            impactBorder.Child = new TextBlock
            {
                Text = string.Format("Intelligent_Flyout_Impact".GetLocalized(), impact),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap
            };
            flyoutStack.Children.Add(impactBorder);
        }

        var currentTechDetails = catalogItem?.TechnicalDetails ?? GetTechnicalDetailsForTag(tag);
        var techDetails = !string.IsNullOrEmpty(currentTechDetails) ? currentTechDetails : details;

        if (!string.IsNullOrEmpty(techDetails))
        {
            var techHeader = new TextBlock
            {
                Text = "Intelligent_Flyout_TechnicalDetails".TryGetLocalized() ?? "Technical Details",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 11,
                Margin = new Thickness(0, 4, 0, 0)
            };
            flyoutStack.Children.Add(techHeader);

            var techBorder = new Border
            {
                Background = GetResourceBrush("SubtleFillColorSecondaryBrush", new SolidColorBrush(Windows.UI.Color.FromArgb(20, 128, 128, 128))),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 6, 8, 6)
            };

            var techBlock = new TextBlock
            {
                Text = techDetails,
                FontSize = 10,
                FontFamily = new FontFamily("Consolas"),
                TextWrapping = TextWrapping.Wrap,
                Foreground = GetResourceBrush("TextFillColorSecondaryBrush", new SolidColorBrush(Colors.Gray))
            };
            techBorder.Child = techBlock;
            flyoutStack.Children.Add(techBorder);
        }

        var currentToggleStateStr = toggle.IsOn ? "Intelligent_State_On".GetLocalized() : "Intelligent_State_Off".GetLocalized();
        var backupInfoBlock = new TextBlock
        {
            Text = string.Format("Intelligent_Flyout_Status".GetLocalized(), isOptimal ? "Intelligent_Badge_Optimal".GetLocalized() : currentToggleStateStr),
            FontSize = 10,
            Foreground = isOptimal ? new SolidColorBrush(Colors.MediumSeaGreen) : GetResourceBrush("TextFillColorSecondaryBrush", new SolidColorBrush(Colors.Gray))
        };
        flyoutStack.Children.Add(backupInfoBlock);

        if (hasBackup && backupDt.HasValue)
        {
            var originalStateStr = preState ? "Intelligent_State_On".GetLocalized() : "Intelligent_State_Off".GetLocalized();
            var rollbackText = new TextBlock
            {
                Text = string.Format("Intelligent_Flyout_RollbackPoint".GetLocalized(), $"{backupDt.Value:g}", originalStateStr),
                FontSize = 10,
                Foreground = new SolidColorBrush(Colors.SteelBlue)
            };
            flyoutStack.Children.Add(rollbackText);
        }

        var scrollViewer = new ScrollViewer
        {
            Content = flyoutStack,
            MaxHeight = 440,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        flyout.Content = scrollViewer;
        detailsBtn.Flyout = flyout;
        barStack.Children.Add(detailsBtn);

        intelligentBar.Child = barStack;
        return intelligentBar;
    }

    private static (string Label, Brush Background, Brush Foreground) GetRiskBadgeInfo(RiskLevel risk, bool isRecommended, bool isOptimal)
    {
        var white = new SolidColorBrush(Colors.White);

        if (isOptimal)
        {
            return ("Intelligent_Badge_Optimal".GetLocalized(), new SolidColorBrush(Windows.UI.Color.FromArgb(255, 34, 139, 34)), white);
        }

        return risk switch
        {
            RiskLevel.Cosmetic => ("Intelligent_Badge_Cosmetic".TryGetLocalized() ?? "Cosmetic", new SolidColorBrush(Windows.UI.Color.FromArgb(255, 96, 125, 139)), white),
            RiskLevel.Safe => ("Intelligent_Badge_Safe".GetLocalized(), new SolidColorBrush(Windows.UI.Color.FromArgb(255, 46, 117, 182)), white),
            RiskLevel.Moderate => ("Intelligent_Badge_Moderate".GetLocalized(), new SolidColorBrush(Windows.UI.Color.FromArgb(255, 216, 119, 0)), white),
            RiskLevel.Advanced => ("Intelligent_Badge_Advanced".GetLocalized(), new SolidColorBrush(Windows.UI.Color.FromArgb(255, 112, 48, 160)), white),
            RiskLevel.Caution => ("Intelligent_Badge_Caution".GetLocalized(), new SolidColorBrush(Windows.UI.Color.FromArgb(255, 180, 40, 40)), white),
            _ => ("Intelligent_Badge_Safe".GetLocalized(), new SolidColorBrush(Windows.UI.Color.FromArgb(255, 46, 117, 182)), white)
        };
    }

    private static Brush GetResourceBrush(string key, Brush fallback)
    {
        try
        {
            if (Application.Current.Resources.TryGetValue(key, out var res) && res != null)
            {
                if (res is Brush brush) return brush;
                if (res is Windows.UI.Color color) return new SolidColorBrush(color);
            }
        }
        catch { }
        return fallback;
    }

    private static Style? GetResourceStyle(string key)
    {
        try
        {
            if (Application.Current.Resources.TryGetValue(key, out var res) && res is Style style)
            {
                return style;
            }
        }
        catch { }
        return null;
    }

    public static string GetTechnicalDetailsForTag(string tag)
    {
        return OptimizeFunctionInspector.GetTechnicalDetailsForTag(tag);
    }

    public static List<ToggleSwitch> GetAllToggleSwitches(DependencyObject root)
    {
        var list = new List<ToggleSwitch>();
        var visited = new HashSet<DependencyObject>();
        TraverseElementTree(root, list, visited);
        return list.Distinct().ToList();
    }

    private static void TraverseElementTree(DependencyObject element, List<ToggleSwitch> list, HashSet<DependencyObject> visited)
    {
        if (element == null || !visited.Add(element)) return;

        if (element is ToggleSwitch toggle)
        {
            list.Add(toggle);
        }

        try
        {
            if (element is Page page && page.Content != null)
            {
                TraverseElementTree(page.Content, list, visited);
            }
            else if (element is ScrollViewer sv && sv.Content is UIElement svContent)
            {
                TraverseElementTree(svContent, list, visited);
            }
            else if (element is Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    TraverseElementTree(child, list, visited);
                }
            }
            else if (element is ContentControl cc && cc.Content is UIElement ccContent)
            {
                TraverseElementTree(ccContent, list, visited);
            }
            else if (element is Border border && border.Child != null)
            {
                TraverseElementTree(border.Child, list, visited);
            }
            else if (element is UserControl uc && uc.Content != null)
            {
                TraverseElementTree(uc.Content, list, visited);
            }
        }
        catch { }

        try
        {
            int count = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);
                TraverseElementTree(child, list, visited);
            }
        }
        catch { }
    }

    public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
    {
        if (depObj != null)
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T typedChild)
                {
                    yield return typedChild;
                }
                if (child is SettingsCard settingsCard)
                {
                    foreach (var childOfSettingsCard in FindVisualChildren<T>(settingsCard))
                    {
                        yield return childOfSettingsCard;
                    }
                }
                else
                {
                    foreach (var childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }

    public static T? FindParent<T>(DependencyObject element) where T : DependencyObject
    {
        DependencyObject current = element;
        while (current != null)
        {
            if (current is T parent) return parent;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
