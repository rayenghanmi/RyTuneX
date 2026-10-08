using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml.Controls;
using RyTuneX.Models;
using RyTuneX.Views;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace RyTuneX.Helpers;

public class OptimizeFunctionInfo
{
    public required string Tag { get; set; }
    public required string MethodName { get; set; }
    public string RevertMethodName { get; set; } = string.Empty;
    public List<string> RawCommands { get; set; } = new();
    public List<string> FormattedActions { get; set; } = new();
    public List<string> Comments { get; set; } = new();
    public RiskLevel Risk { get; set; } = RiskLevel.Safe;
    public int ScoreWeight { get; set; } = 6;
    public bool IsRecommended { get; set; }
    public string RecommendationReason { get; set; } = string.Empty;
    public string TechnicalDetails { get; set; } = string.Empty;
    public string ActionSummary { get; set; } = string.Empty;
}

public static class OptimizeFunctionInspector
{
    private static readonly object InitLock = new();
    private static bool _isInitialized;
    private static string _helperSourceCode = string.Empty;
    private static string _optionsSourceCode = string.Empty;

    private static readonly ConcurrentDictionary<string, string> TagToMethodMap = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, string> TagToRevertMethodMap = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, OptimizeFunctionInfo> FunctionInfoCache = new(StringComparer.OrdinalIgnoreCase);

    // Tag category mappings
    private static readonly ConcurrentDictionary<string, OptimizationCategory> TagCategoryMap = new(StringComparer.OrdinalIgnoreCase);

    public static void EnsureInitialized()
    {
        if (_isInitialized) return;

        lock (InitLock)
        {
            if (_isInitialized) return;

            LoadSourceCodes();
            BuildTagToMethodMap();
            BuildCategoryMap();
            _isInitialized = true;
        }
    }

    private static void LoadSourceCodes()
    {
        _helperSourceCode = ReadSourceFileOrResource("Helpers/OptimizeSystemHelper.cs", "RyTuneX.Helpers.OptimizeSystemHelper.cs");
        _optionsSourceCode = ReadSourceFileOrResource("Helpers/OptimizationOptions.cs", "RyTuneX.Helpers.OptimizationOptions.cs");
    }

    private static string ReadSourceFileOrResource(string relativeFilePath, string resourceName)
    {
        try
        {
            // Try local disk path
            var baseDir = AppContext.BaseDirectory;
            var candidates = new[]
            {
                Path.Combine(baseDir, relativeFilePath),
                Path.Combine(baseDir, "..", "..", "..", "..", "..", relativeFilePath),
                Path.Combine(baseDir, "..", "..", "..", "..", relativeFilePath),
                Path.Combine(baseDir, "..", "..", "..", relativeFilePath),
                Path.Combine(Environment.CurrentDirectory, relativeFilePath)
            };

            foreach (var candidate in candidates)
            {
                var full = Path.GetFullPath(candidate);
                if (File.Exists(full))
                {
                    return File.ReadAllText(full, Encoding.UTF8);
                }
            }
        }
        catch { }

        // Fall back to embedded resource
        try
        {
            var assembly = typeof(OptimizeSystemHelper).Assembly;
            var resNames = assembly.GetManifestResourceNames();
            var targetFileName = Path.GetFileName(relativeFilePath);
            var matchedRes = resNames.FirstOrDefault(r =>
                r.Equals(resourceName, StringComparison.OrdinalIgnoreCase) ||
                r.EndsWith(targetFileName, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(matchedRes))
            {
                using var stream = assembly.GetManifestResourceStream(matchedRes);
                if (stream != null)
                {
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    return reader.ReadToEnd();
                }
            }
        }
        catch { }

        return string.Empty;
    }

    private static void BuildTagToMethodMap()
    {
        if (string.IsNullOrEmpty(_optionsSourceCode)) return;

        // Parse switch cases
        var caseMatches = Regex.Matches(_optionsSourceCode, @"case\s+""([^""]+)""\s*:([\s\S]*?)(?:break\s*;|(?=case\s+""))");
        foreach (Match m in caseMatches)
        {
            var tag = m.Groups[1].Value.Trim();
            var block = m.Groups[2].Value;

            var applyMatch = Regex.Match(block, @"if\s*\(\s*isOn\s*\)[\s\S]*?OptimizeSystemHelper\.([A-Za-z0-9_]+)\(");
            if (applyMatch.Success)
            {
                TagToMethodMap[tag] = applyMatch.Groups[1].Value.Trim();
            }

            var revertMatch = Regex.Match(block, @"else[\s\S]*?OptimizeSystemHelper\.([A-Za-z0-9_]+)\(");
            if (revertMatch.Success)
            {
                TagToRevertMethodMap[tag] = revertMatch.Groups[1].Value.Trim();
            }
        }
    }

    private static void BuildCategoryMap()
    {
        // Build category mappings
        string[] perfTags =
        [
            "UsbPowerSaving", "PowerThrottling", "MenuShowDelay", "MouseHoverTime", "KeyboardLatency",
            "MouseAcceleration", "AutoComplete", "WindowShake", "GlobalTimerResolution", "HPET",
            "DynamicTick", "SysMain", "Prefetch", "LargeSystemCache", "NDU", "PageFileEncryption",
            "GamingMode", "FullscreenOptimizations", "GpuDriverTweaks", "SystemProfile", "OptimizeNTFS",
            "PrioritizeForegroundApplications", "LowDiskSpaceChecks", "FileExtensionsAndHiddenFiles",
            "LinkResolve", "BackgroundApps", "CrashDump", "RemoteAssistance", "RemoteRegistry",
            "ServiceHostSplitting", "TaskTimeouts", "ServiceTimeouts", "CopyMoveContextMenu", "WPBT",
            "LegacyBootMenu", "SystemRestore", "Cortana", "StoreUpdates", "SmartScreen", "Drivers"
        ];

        string[] privTags =
        [
            "AdvertisingID", "BluetoothAdvertising", "NewsAndInterests", "SpotlightFeatures", "TailoredExperiences",
            "CloudOptimizedContent", "FeedbackNotifications", "TelemetryServices", "EdgeTelemetry",
            "VisualStudioTelemetry", "NvidiaTelemetry", "ChromeTelemetry", "FirefoxTelemetry", "DiagnosticsToast",
            "HandwritingDataSharing", "TextInputDataCollection", "InputPersonalization", "OnlineSpeechPrivacy",
            "SafeSearchMode", "ClipboardSync", "MessageSync", "SettingSync", "Cdp", "LocationFeatures",
            "Biometrics", "FindMyDevice", "VoiceActivation", "AutomaticRestartSignOn", "SMBv1", "SMBv2",
            "WiFiSense", "WebSearchResults", "AppLaunchTracking", "TimelineHistory"
        ];

        string[] featTags =
        [
            "WindowsTransparency", "WindowsDarkMode", "VerboseLogon", "HomeGroup", "PrintService",
            "CompatibilityAssistant", "Search", "FaxService", "InsiderService", "Hibernation", "StickyKeys",
            "WindowsInk", "SpellingAndTypingFeatures", "CloudClipboard", "QuickAccessHistory", "MyPeople",
            "CastToDevice", "ErrorReporting", "GameBar", "LockScreenNotifications", "ToastNotifications",
            "NotificationCenter", "SuggestedActions", "ClassicContextMenu", "RecommendedSectionStartMenu",
            "TaskbarToLeft", "SnapAssist", "Widgets", "Chat", "FilesCompactMode", "EndTask", "Stickers",
            "CoPilotAI", "WindowsRecall", "WindowsAI", "EdgeDiscoverBar", "VBS", "TakeOwnership",
            "OpenCmdHere", "CopyFilePath"
        ];

        foreach (var t in perfTags) TagCategoryMap[t] = OptimizationCategory.Performance;
        foreach (var t in privTags) TagCategoryMap[t] = OptimizationCategory.PrivacyAndTelemetry;
        foreach (var t in featTags) TagCategoryMap[t] = OptimizationCategory.FeaturesAndUsability;
    }

    public static string ResolveMethodNameForTag(string tag)
    {
        EnsureInitialized();

        if (TagToMethodMap.TryGetValue(tag, out var mapped) && !string.IsNullOrEmpty(mapped))
        {
            return mapped;
        }

        // Method name candidates
        string[] candidates =
        [
            $"Disable{tag}",
            $"Enable{tag}",
            $"Optimize{tag}",
            $"Adjust{tag}",
            $"Decrease{tag}",
            $"Show{tag}",
            $"Add{tag}",
            $"Apply{tag}",
            $"Align{tag}",
            $"Exclude{tag}",
            tag
        ];

        foreach (var cand in candidates)
        {
            if (_helperSourceCode.Contains($"Task {cand}("))
            {
                TagToMethodMap[tag] = cand;
                return cand;
            }
        }

        return $"Disable{tag}";
    }

    public static string ResolveRevertMethodNameForTag(string tag)
    {
        EnsureInitialized();

        if (TagToRevertMethodMap.TryGetValue(tag, out var mapped) && !string.IsNullOrEmpty(mapped))
        {
            return mapped;
        }

        string[] candidates =
        [
            $"Enable{tag}",
            $"Disable{tag}",
            $"Revert{tag}",
            $"Restore{tag}",
            $"Remove{tag}"
        ];

        foreach (var cand in candidates)
        {
            if (_helperSourceCode.Contains($"Task {cand}("))
            {
                TagToRevertMethodMap[tag] = cand;
                return cand;
            }
        }

        return string.Empty;
    }

    public static OptimizeFunctionInfo GetFunctionInfo(string tag)
    {
        EnsureInitialized();

        if (FunctionInfoCache.TryGetValue(tag, out var cached))
        {
            return cached;
        }

        var methodName = ResolveMethodNameForTag(tag);
        var info = ParseMethodInfo(tag, methodName);
        FunctionInfoCache[tag] = info;
        return info;
    }

    private static OptimizeFunctionInfo ParseMethodInfo(string tag, string methodName)
    {
        var revertMethodName = ResolveRevertMethodNameForTag(tag);
        var info = new OptimizeFunctionInfo
        {
            Tag = tag,
            MethodName = methodName,
            RevertMethodName = revertMethodName
        };

        if (string.IsNullOrEmpty(_helperSourceCode))
        {
            info.TechnicalDetails = $"Function: OptimizeSystemHelper.{methodName}()\nState Key: HKLM\\SOFTWARE\\RyTuneX\\Optimizations\\{tag}";
            info.Risk = EvaluateRiskFromCode(tag, methodName, string.Empty, new(), new());
            (info.ScoreWeight, info.IsRecommended, info.RecommendationReason) = EvaluateScoreAndRecommendation(tag, info.Risk, info.Comments, info.FormattedActions);
            return info;
        }

        var methodIdx = _helperSourceCode.IndexOf($"Task {methodName}(");
        if (methodIdx < 0)
        {
            // Fallback evaluation
            info.TechnicalDetails = $"Function: OptimizeSystemHelper.{methodName}()\nState Key: HKLM\\SOFTWARE\\RyTuneX\\Optimizations\\{tag}";
            info.Risk = EvaluateRiskFromCode(tag, methodName, string.Empty, new(), new());
            (info.ScoreWeight, info.IsRecommended, info.RecommendationReason) = EvaluateScoreAndRecommendation(tag, info.Risk, info.Comments, info.FormattedActions);
            return info;
        }

        // Extract preceding comments
        var linesBefore = _helperSourceCode.Substring(0, methodIdx).Split('\n');
        for (int i = linesBefore.Length - 1; i >= 0; i--)
        {
            var line = linesBefore[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            if (line.StartsWith("//"))
            {
                var comment = line.TrimStart('/', ' ').Trim();
                if (!string.IsNullOrEmpty(comment) &&
                    !comment.StartsWith("TODO", StringComparison.OrdinalIgnoreCase) &&
                    !comment.StartsWith("NOTE", StringComparison.OrdinalIgnoreCase))
                {
                    info.Comments.Insert(0, comment);
                }
            }
            else
            {
                break;
            }
        }

        // Find method body
        var braceStart = _helperSourceCode.IndexOf('{', methodIdx);
        if (braceStart < 0) return info;

        var depth = 1;
        var curr = braceStart + 1;
        while (depth > 0 && curr < _helperSourceCode.Length)
        {
            var ch = _helperSourceCode[curr];
            if (ch == '{') depth++;
            else if (ch == '}') depth--;
            curr++;
        }

        var methodBody = _helperSourceCode.Substring(braceStart + 1, curr - braceStart - 2);

        // Extract string literals
        var strMatches = Regex.Matches(methodBody, @"(?<!//.*)""((?:[^""\\]|\\.)*)""");
        foreach (Match sm in strMatches)
        {
            var raw = sm.Groups[1].Value.Replace("\\\"", "\"").Trim();
            if (raw.Length > 3 && !raw.Contains("taskkill") && !raw.Contains("explorer.exe") && !raw.Contains("powershell -NoProfile"))
            {
                info.RawCommands.Add(raw);
            }
        }

        // Extract comments
        var commentMatches = Regex.Matches(methodBody, @"//\s*([^\r\n]+)");
        foreach (Match cm in commentMatches)
        {
            var commentText = cm.Groups[1].Value.Trim();
            if (!string.IsNullOrWhiteSpace(commentText) &&
                !commentText.StartsWith("TODO", StringComparison.OrdinalIgnoreCase) &&
                !commentText.StartsWith("NOTE", StringComparison.OrdinalIgnoreCase) &&
                !commentText.Contains("ConfigureAwait") &&
                !commentText.Contains("reg add") &&
                !commentText.Contains("sc config") &&
                !info.Comments.Contains(commentText))
            {
                info.Comments.Add(commentText);
            }
        }

        // Format technical actions
        info.FormattedActions = ParseFormattedActions(methodBody, info.RawCommands);

        // Determine risk
        info.Risk = EvaluateRiskFromCode(tag, methodName, methodBody, info.RawCommands, info.Comments);

        // Evaluate score and recommendation
        (info.ScoreWeight, info.IsRecommended, info.RecommendationReason) = EvaluateScoreAndRecommendation(tag, info.Risk, info.Comments, info.FormattedActions);

        // Build action summary
        if (info.Comments.Count > 0)
        {
            info.ActionSummary = string.Join(". ", info.Comments);
        }
        else if (info.FormattedActions.Count > 0)
        {
            info.ActionSummary = string.Format("OptimizeInspector_ConfiguresCount".GetLocalized(), info.FormattedActions.Count, tag);
        }
        else
        {
            info.ActionSummary = string.Format("OptimizeInspector_OptimizesDefault".GetLocalized(), tag);
        }

        // Build technical details
        var sb = new StringBuilder();
        sb.AppendLine($"Function: OptimizeSystemHelper.{methodName}()");
        if (!string.IsNullOrEmpty(info.RevertMethodName))
        {
            sb.AppendLine($"Revert: OptimizeSystemHelper.{info.RevertMethodName}()");
        }

        if (info.Comments.Count > 0)
        {
            sb.AppendLine($"Code Summary: {string.Join(". ", info.Comments.Take(3))}");
        }
        else if (!string.IsNullOrEmpty(info.ActionSummary))
        {
            sb.AppendLine($"Code Summary: {info.ActionSummary}");
        }

        var recStatus = info.IsRecommended ? "Intelligent_Badge_Recommended".GetLocalized() : "Intelligent_Status_Optional".GetLocalized();
        sb.AppendLine(string.Format("OptimizeInspector_SafetyLine".GetLocalized(), info.Risk, info.ScoreWeight, recStatus));
        if (!string.IsNullOrWhiteSpace(info.RecommendationReason))
        {
            sb.AppendLine(string.Format("OptimizeInspector_SafetyNote".GetLocalized(), info.RecommendationReason));
        }

        sb.AppendLine($"State Key: HKLM\\SOFTWARE\\RyTuneX\\Optimizations\\{tag}");

        if (info.FormattedActions.Count > 0)
        {
            sb.AppendLine("OptimizeInspector_ActionsRegistry".GetLocalized());
            foreach (var act in info.FormattedActions)
            {
                sb.AppendLine($"  • {act}");
            }
        }
        else if (info.RawCommands.Count > 0)
        {
            sb.AppendLine("OptimizeInspector_Commands".GetLocalized());
            foreach (var cmd in info.RawCommands.Take(6))
            {
                sb.AppendLine($"  • {cmd}");
            }
        }

        info.TechnicalDetails = sb.ToString().TrimEnd();

        return info;
    }

    private static List<string> ParseFormattedActions(string body, List<string> rawCommands)
    {
        var actions = new List<string>();

        // Parse service helper calls
        var svcHelperMatches = Regex.Matches(body, @"SetServiceStatusAsync\s*\(\s*""([^""]+)""\s*,\s*(\d+)\s*\)");
        foreach (Match sm in svcHelperMatches)
        {
            var svc = sm.Groups[1].Value;
            var val = sm.Groups[2].Value == "4" ? "Disabled" : sm.Groups[2].Value == "2" ? "Automatic" : sm.Groups[2].Value == "3" ? "Manual" : sm.Groups[2].Value;
            actions.Add($"Service {svc} -> Start = {val}");
        }

        // Parse component helper calls
        if (body.Contains("MSPower_DeviceEnable"))
        {
            actions.Add("WMI MSPower_DeviceEnable: USB\\ROOT -> Enable = false");
        }
        if (body.Contains("ToggleCopilotJsonPolicy"))
        {
            actions.Add("IntegratedServicesRegionPolicySet.json: CoPilot defaultState = disabled");
        }
        if (body.Contains("RemoveAISystemComponents"))
        {
            actions.Add("Remove AI CBS Packages & Machine Learning DLLs");
        }
        if (body.Contains("ToggleSettingsAIVisibility"))
        {
            actions.Add("Settings AI Visibility: Hide System AI pages");
        }

        foreach (var cmd in rawCommands)
        {
            // Parse default registry value
            var regAddDefault = Regex.Match(cmd, @"reg add\s+""([^""]+)""\s+/ve\s+/[Tt]\s+(?:REG_[A-Z0-9_]+)\s+/[Dd]\s+""?([^""]*?)""?(?:\s+/f|\s*$)", RegexOptions.IgnoreCase);
            if (regAddDefault.Success)
            {
                actions.Add($"{regAddDefault.Groups[1].Value} -> (Default) = {regAddDefault.Groups[2].Value}");
                continue;
            }

            // Parse registry value
            var regAdd = Regex.Match(cmd, @"reg add\s+""([^""]+)""\s+/[Vv]\s+""?([^""]+?)""?\s+/[Tt]\s+(?:REG_[A-Z0-9_]+)\s+/[Dd]\s+""?([^""]*?)""?(?:\s+/f|\s*$)", RegexOptions.IgnoreCase);
            if (regAdd.Success)
            {
                actions.Add($"{regAdd.Groups[1].Value} -> {regAdd.Groups[2].Value.Trim()} = {regAdd.Groups[3].Value.Trim()}");
                continue;
            }

            // Parse registry value deletion
            var regDelVal = Regex.Match(cmd, @"reg delete\s+""([^""]+)""\s+/[Vv]\s+""?([^""]+?)""?(?:\s+/f|\s*$)", RegexOptions.IgnoreCase);
            if (regDelVal.Success)
            {
                actions.Add($"Delete {regDelVal.Groups[1].Value} -> {regDelVal.Groups[2].Value.Trim()}");
                continue;
            }

            // Parse registry key deletion
            var regDelKey = Regex.Match(cmd, @"reg delete\s+""([^""]+)""\s+/f", RegexOptions.IgnoreCase);
            if (regDelKey.Success)
            {
                actions.Add($"Delete Key {regDelKey.Groups[1].Value}");
                continue;
            }

            // Parse service configuration
            var sc = Regex.Match(cmd, @"sc\s+(config|stop|start)\s+([^\s]+)(?:\s+start=\s*([^\s]+))?", RegexOptions.IgnoreCase);
            if (sc.Success)
            {
                var op = sc.Groups[1].Value.ToLowerInvariant();
                var svc = sc.Groups[2].Value;
                var st = sc.Groups[3].Value;
                if (op == "config" && !string.IsNullOrEmpty(st))
                {
                    actions.Add($"Service {svc} -> Start = {st}");
                }
                else
                {
                    actions.Add($"Service {svc} -> {op}");
                }
                continue;
            }

            // Parse scheduled tasks
            var task = Regex.Match(cmd, @"schtasks\s+/change\s+/tn\s+""?([^""\s]+)""?\s+/(disable|enable)", RegexOptions.IgnoreCase);
            if (task.Success)
            {
                var mode = task.Groups[2].Value.Equals("disable", StringComparison.OrdinalIgnoreCase) ? "Disable" : "Enable";
                actions.Add($"{mode} Task: {task.Groups[1].Value}");
                continue;
            }

            // Parse boot configuration
            var bcd = Regex.Match(cmd, @"bcdedit\s+/set\s+([^\s]+)\s+([^\s]+)", RegexOptions.IgnoreCase);
            if (bcd.Success)
            {
                actions.Add($"BCDEdit: {bcd.Groups[1].Value} = {bcd.Groups[2].Value}");
                continue;
            }

            // Parse file system configuration
            var fs = Regex.Match(cmd, @"fsutil\s+behavior\s+set\s+([^\s]+)\s+([^\s]+)", RegexOptions.IgnoreCase);
            if (fs.Success)
            {
                actions.Add($"FSUtil: {fs.Groups[1].Value} = {fs.Groups[2].Value}");
                continue;
            }

            // Parse DISM feature configuration
            var dism = Regex.Match(cmd, @"dism\s+/Online\s+/(Disable|Enable)-Feature\s+/FeatureName:([^\s]+)", RegexOptions.IgnoreCase);
            if (dism.Success)
            {
                actions.Add($"DISM: {dism.Groups[1].Value}-Feature '{dism.Groups[2].Value}'");
                continue;
            }

            // Parse power configuration
            var powercfg = Regex.Match(cmd, @"powercfg\s+(.*)", RegexOptions.IgnoreCase);
            if (powercfg.Success)
            {
                var pArgs = powercfg.Groups[1].Value.Trim();
                if (pArgs.Contains("2a737441-1930-4402-8d77-b2bebba308a3"))
                {
                    actions.Add("PowerCfg: USB Selective Suspend = Disabled");
                }
                else
                {
                    actions.Add($"PowerCfg: {pArgs}");
                }
                continue;
            }

            // Parse network configuration
            if (cmd.StartsWith("netsh", StringComparison.OrdinalIgnoreCase))
            {
                actions.Add($"Netsh: {cmd}");
                continue;
            }

            // Fallback commands
            if (cmd.StartsWith("reg ", StringComparison.OrdinalIgnoreCase) ||
                cmd.StartsWith("bcdedit", StringComparison.OrdinalIgnoreCase) ||
                cmd.StartsWith("fsutil", StringComparison.OrdinalIgnoreCase) ||
                cmd.StartsWith("sc ", StringComparison.OrdinalIgnoreCase))
            {
                actions.Add(cmd);
            }
        }

        return actions.Distinct().ToList();
    }

    private static bool IsCosmeticFromCode(string tag, string methodName, string body, List<string> rawCommands, List<string> comments)
    {
        var normalizedBody = body.Replace(@"\\", @"\");
        var normalizedCommands = rawCommands.Select(c => c.Replace(@"\\", @"\"));
        var text = $"{tag} {methodName} {normalizedBody} {string.Join(" ", comments)} {string.Join(" ", normalizedCommands)}".ToLowerInvariant();

        // Personalization and themes
        if (text.Contains(@"themes\personalize") ||
            text.Contains("themes/personalize") ||
            text.Contains("personalize") ||
            text.Contains("appsuselighttheme") ||
            text.Contains("systemuseslighttheme") ||
            text.Contains("enabletransparency") ||
            text.Contains("darkmode") ||
            text.Contains("lightmode") ||
            text.Contains("transparency") ||
            text.Contains("stickers"))
        {
            return true;
        }

        // Visual layout and shell
        if (text.Contains("taskbaral") ||
            text.Contains("usecompactmode") ||
            text.Contains("taskbartoleft") ||
            text.Contains("compactmode") ||
            text.Contains("snapassist") ||
            text.Contains("enablesnapassist") ||
            text.Contains("enablesnapbar") ||
            text.Contains("windowarrangementactive") ||
            text.Contains("snapsizing") ||
            text.Contains("verbosestatus") ||
            text.Contains("verboselogon"))
        {
            return true;
        }

        // Context menu shortcuts
        if (text.Contains(@"\shell\runas") ||
            text.Contains(@"\shell\cmd") ||
            text.Contains(@"\shell\copypath") ||
            text.Contains("shell/runas") ||
            text.Contains("shell/cmd") ||
            text.Contains("shell/copypath") ||
            text.Contains("play to menu") ||
            text.Contains("takeownership") ||
            text.Contains("opencmdhere") ||
            text.Contains("copyfilepath") ||
            text.Contains("casttodevice"))
        {
            return true;
        }

        // Accessibility and typing
        if (text.Contains(@"accessibility\stickykeys") ||
            text.Contains("accessibility/stickykeys") ||
            text.Contains(@"tablettip") ||
            text.Contains("enableautocorrection") ||
            text.Contains("enablespellchecking") ||
            text.Contains("stickykeys") ||
            text.Contains("spelling") ||
            text.Contains("typing") ||
            text.Contains("windowsink") ||
            text.Contains("enabledoubletapspace"))
        {
            return true;
        }

        // Notifications and history
        if (text.Contains("notoastapplicationnotification") ||
            text.Contains("toastnotification") ||
            text.Contains("lockscreennotifications") ||
            text.Contains("showrecent") ||
            text.Contains("showfrequent") ||
            text.Contains("quickaccesshistory"))
        {
            return true;
        }

        // Explicit preference comments
        if (comments.Any(c =>
            c.Contains("cosmetic", StringComparison.OrdinalIgnoreCase) ||
            c.Contains("visual preference", StringComparison.OrdinalIgnoreCase) ||
            c.Contains("user interface preference", StringComparison.OrdinalIgnoreCase) ||
            c.Contains("personal preference", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    private static RiskLevel EvaluateRiskFromCode(
        string tag, string methodName, string body, List<string>? rawCommands = null, List<string>? comments = null)
    {
        var lower = (tag + " " + methodName + " " + body).ToLowerInvariant();

        // Caution risk
        if (tag.Equals("VBS", StringComparison.OrdinalIgnoreCase) ||
            tag.Equals("SmartScreen", StringComparison.OrdinalIgnoreCase) ||
            tag.Equals("SystemRestore", StringComparison.OrdinalIgnoreCase) ||
            tag.Equals("PrintService", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("smartscreen") ||
            lower.Contains("virtualization-based security") ||
            (tag.Equals("PrintService", StringComparison.OrdinalIgnoreCase) && lower.Contains("spooler")))
        {
            return RiskLevel.Caution;
        }

        // Moderate risk
        if (tag.Equals("PowerThrottling", StringComparison.OrdinalIgnoreCase) ||
            tag.Equals("HPET", StringComparison.OrdinalIgnoreCase) ||
            tag.Equals("DynamicTick", StringComparison.OrdinalIgnoreCase) ||
            tag.Equals("SysMain", StringComparison.OrdinalIgnoreCase) ||
            tag.Equals("Prefetch", StringComparison.OrdinalIgnoreCase) ||
            tag.Equals("LargeSystemCache", StringComparison.OrdinalIgnoreCase) ||
            tag.Equals("NDU", StringComparison.OrdinalIgnoreCase) ||
            tag.Equals("PageFileEncryption", StringComparison.OrdinalIgnoreCase) ||
            tag.Equals("GpuDriverTweaks", StringComparison.OrdinalIgnoreCase) ||
            tag.Equals("LegacyBootMenu", StringComparison.OrdinalIgnoreCase) ||
            tag.Equals("SettingSync", StringComparison.OrdinalIgnoreCase) ||
            tag.Equals("FindMyDevice", StringComparison.OrdinalIgnoreCase) ||
            tag.Equals("Hibernation", StringComparison.OrdinalIgnoreCase) ||
            tag.Equals("Search", StringComparison.OrdinalIgnoreCase) ||
            tag.Equals("Drivers", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("useplatformclock") ||
            lower.Contains("disabledynamictick") ||
            lower.Contains("largesystemcache") ||
            lower.Contains("enableprefetcher") ||
            lower.Contains("powerthrottlingoff") ||
            lower.Contains("hiberfil") ||
            lower.Contains("wsearch") ||
            lower.Contains("4d36e968"))
        {
            return RiskLevel.Moderate;
        }

        // Cosmetic risk
        if (IsCosmeticFromCode(tag, methodName, body, rawCommands ?? new(), comments ?? new()))
        {
            return RiskLevel.Cosmetic;
        }

        // Safe risk
        return RiskLevel.Safe;
    }

    private static (int Weight, bool Recommended, string Reason) EvaluateScoreAndRecommendation(
        string tag, RiskLevel risk, List<string> comments, List<string> actions)
    {
        // Caution items
        if (risk == RiskLevel.Caution)
        {
            return (5, false, "OptimizeInspector_ReasonCaution".GetLocalized());
        }

        // Cosmetic items
        if (risk == RiskLevel.Cosmetic)
        {
            return (5, false, "OptimizeInspector_ReasonCosmetic".TryGetLocalized()
                ?? "Optional (Cosmetic): Visual layout or subjective user preference.");
        }

        // Moderate items
        if (risk == RiskLevel.Moderate)
        {
            bool isRecommendedModerate = tag switch
            {
                "PowerThrottling" or "GpuDriverTweaks" or "SysMain" or "Hibernation" or
                "UsbPowerSaving" or "CompatibilityAssistant" or "GameBar" or "SMBv1" => true,
                _ => false
            };

            var weight = tag switch
            {
                "GpuDriverTweaks" or "PowerThrottling" or "SysMain" or "Hibernation" => 9,
                "UsbPowerSaving" or "GameBar" or "LargeSystemCache" or "HPET" => 8,
                _ => 6
            };

            var reason = isRecommendedModerate
                ? (comments.Count > 0
                    ? $"{"Intelligent_Badge_Recommended".GetLocalized()} ({"Intelligent_Badge_Moderate".GetLocalized()}): {comments[0]}"
                    : "OptimizeInspector_ReasonModerateRec".GetLocalized())
                : "OptimizeInspector_ReasonModerate".GetLocalized();

            return (weight, isRecommendedModerate, reason);
        }

        // Safe items
        var safeWeight = tag switch
        {
            "TelemetryServices" or "WindowsAI" or "WindowsRecall" or "SystemProfile" => 10,
            "BackgroundApps" or "CoPilotAI" or "GamingMode" or "PrioritizeForegroundApplications" => 9,
            "AdvertisingID" or "TextInputDataCollection" or "ClassicContextMenu" or "EndTask" => 8,
            "MenuShowDelay" or "KeyboardLatency" or "MouseHoverTime" or "MouseAcceleration" or "LowDiskSpaceChecks" => 7,
            _ => 6
        };

        var safeReason = comments.Count > 0
            ? $"{"Intelligent_Badge_Recommended".GetLocalized()}: {comments[0]}"
            : "OptimizeInspector_ReasonSafeRec".GetLocalized();

        return (safeWeight, true, safeReason);
    }

    public static OptimizationItemModel CreateItemModel(
        string tag,
        Page? contextPage = null,
        ToggleSwitch? toggle = null,
        SettingsCard? card = null)
    {
        EnsureInitialized();

        var info = GetFunctionInfo(tag);

        // Determine category
        var category = OptimizationCategory.Performance;
        if (contextPage is OptimizeSystemPage)
        {
            category = OptimizationCategory.Performance;
        }
        else if (contextPage is PrivacyPage)
        {
            category = OptimizationCategory.PrivacyAndTelemetry;
        }
        else if (contextPage is FeaturesPage)
        {
            category = OptimizationCategory.FeaturesAndUsability;
        }
        else if (TagCategoryMap.TryGetValue(tag, out var mappedCat))
        {
            category = mappedCat;
        }

        // Resolve title
        string title = tag;
        if (card != null && card.Header is string cardHeader && !string.IsNullOrWhiteSpace(cardHeader))
        {
            title = cardHeader;
        }
        else
        {
            var localizedTitle = $"Feature_{tag}.Header".TryGetLocalized()
                ?? $"Feature_{tag}/Header".TryGetLocalized()
                ?? $"{tag}Title".TryGetLocalized();
            if (!string.IsNullOrEmpty(localizedTitle))
            {
                title = localizedTitle;
            }
        }

        // Resolve description
        string description = info.ActionSummary;
        if (card != null && card.Description is string cardDesc && !string.IsNullOrWhiteSpace(cardDesc))
        {
            description = cardDesc;
        }
        else
        {
            var localizedDesc = $"Feature_{tag}.Description".TryGetLocalized()
                ?? $"Feature_{tag}/Description".TryGetLocalized();
            if (!string.IsNullOrEmpty(localizedDesc))
            {
                description = localizedDesc;
            }
        }

        // Resolve impact description
        var impact = $"Intelligent_Impact_{tag}".TryGetLocalized()
            ?? $"Feature_{tag}.Impact".TryGetLocalized()
            ?? info.ActionSummary;

        var model = new OptimizationItemModel
        {
            Tag = tag,
            Title = title,
            Description = description,
            ImpactDescription = impact,
            Category = category,
            ScoreWeight = info.ScoreWeight,
            Risk = info.Risk,
            TechnicalDetails = info.TechnicalDetails,
            IsRecommended = info.IsRecommended,
            RecommendationReason = info.RecommendationReason,
            IsApplied = toggle?.IsOn ?? false
        };

        return model;
    }

    public static string GetTechnicalDetailsForTag(string tag)
    {
        return GetFunctionInfo(tag).TechnicalDetails;
    }

    public static RiskLevel GetRiskForTag(string tag)
    {
        return GetFunctionInfo(tag).Risk;
    }

    public static bool IsCosmeticTag(string tag)
    {
        return GetFunctionInfo(tag).Risk == RiskLevel.Cosmetic;
    }

    public static List<string> GetAllKnownTags()
    {
        EnsureInitialized();
        return TagCategoryMap.Keys.ToList();
    }
}
