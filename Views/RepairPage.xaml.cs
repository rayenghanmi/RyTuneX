using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using RyTuneX.Helpers;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Windows.Storage;

namespace RyTuneX.Views;

public sealed partial class RepairPage : Page
{
    private static readonly (string Name, string FriendlyName)[] Components =
    {
        ("DISM", "Windows image"),
        ("SFC", "System files"),
        ("CHKDSK", "System drive"),
    };

    private enum RepairState
    {
        Idle,
        Checking,
        Healthy,
        IssuesFound,
        Repairing,
        RepairCompleted
    }

    private enum RowStatus
    {
        Pending,
        Checking,
        Healthy,
        NeedsAttention,
        Scheduled,
        Unknown
    }

    private readonly Dictionary<string, StringBuilder> _scanResults = new()
    {
        { "DISM", new StringBuilder() },
        { "SFC", new StringBuilder() },
        { "CHKDSK", new StringBuilder() }
    };

    // Tracks the last known health per component: true = healthy, false = needs attention, null = unknown/scheduled
    private readonly Dictionary<string, bool?> _componentHealth = new()
    {
        { "DISM", null },
        { "SFC", null },
        { "CHKDSK", null }
    };

    private RepairState _currentState = RepairState.Idle;
    private Process? _runningProcess;
    private CancellationTokenSource? _cancellationTokenSource;
    private Guid? _cancellationRegistrationId;
    private int _currentProcessId;
    private string? _pendingScrollTarget;
    private int _sfcPrefaceLinesSkipped;

    public RepairPage()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        InitializeComponent();
        LogHelper.Log("Initializing RepairPage");
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        Loaded += RepairPage_Loaded;
        Unloaded += RepairPage_Unloaded;
        SetState(RepairState.Idle);
        LoadLastCheckedTime();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string optionTag && !string.IsNullOrEmpty(optionTag))
        {
            _pendingScrollTarget = optionTag;
        }
    }

    private async void RepairPage_Loaded(object sender, RoutedEventArgs e)
    {
        LoadLastCheckedTime();
        if (!string.IsNullOrEmpty(_pendingScrollTarget))
        {
            await ScrollToElementHelper.ScrollToElementAsync(this, _pendingScrollTarget);
            _pendingScrollTarget = null;
        }
    }

    private void LoadLastCheckedTime()
    {
        try
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue("RepairPage_LastCheckedTime", out var obj) &&
                obj is string timeStr &&
                DateTime.TryParse(timeStr, out var lastChecked))
            {
                LastCheckedText.Text = "RepairPage_LastCheckedFormat".GetLocalized().Replace("{time}", lastChecked.ToString("g"));
            }
            else
            {
                LastCheckedText.Text = "RepairPage_LastCheckedNever.Text".GetLocalized();
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogWarning($"Failed to load last checked time: {ex.Message}");
        }
    }

    private void RepairPage_Unloaded(object sender, RoutedEventArgs e)
    {
    }

    // Main workflow

    private async void OnMainActionButtonClick(object sender, RoutedEventArgs e)
    {
        switch (_currentState)
        {
            case RepairState.Idle:
            case RepairState.Healthy:
            case RepairState.RepairCompleted:
                _ = LogHelper.Log("Starting system health check");
                await RunWorkflowAsync(isRepair: false);
                break;

            case RepairState.IssuesFound:
                _ = LogHelper.Log("Starting system repair");
                await RunWorkflowAsync(isRepair: true);
                break;
        }
    }

    private async void OnStopButtonClick(object sender, RoutedEventArgs e)
    {
        StopButton.IsEnabled = false;
        _ = LogHelper.Log("User requested to stop current repair/scan operation");
        await StopCurrentOperationAsync();
    }

    private async Task StopCurrentOperationAsync()
    {
        _cancellationTokenSource?.Cancel();

        var processId = _currentProcessId;
        if (processId > 0)
        {
            await PseudoConsoleHelper.KillProcessTreeAsync(processId);
        }

        if (_runningProcess != null)
        {
            try
            {
                if (!_runningProcess.HasExited)
                {
                    await PseudoConsoleHelper.KillProcessTreeAsync(_runningProcess.Id);
                }
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogWarning($"Error stopping running process: {ex.Message}");
            }
        }
    }

    private async Task RunWorkflowAsync(bool isRepair)
    {
        var namesToRun = isRepair
            ? _componentHealth.Where(kv => kv.Value == false).Select(kv => kv.Key).ToList()
            : Components.Select(c => c.Name).ToList();

        // Safety net: if "repair" is invoked with nothing flagged unhealthy, fall back to a full check.
        if (isRepair && namesToRun.Count == 0)
        {
            isRepair = false;
            namesToRun = Components.Select(c => c.Name).ToList();
        }

        SetState(isRepair ? RepairState.Repairing : RepairState.Checking);

        ProgressGrid.Visibility = Visibility.Visible;
        StatusTextBlock.Visibility = Visibility.Visible;
        ActionPanel.Visibility = Visibility.Collapsed;
        StopButton.Visibility = Visibility.Visible;
        StopButton.IsEnabled = true;
        ProgressBar.Value = 0;
        PercentageTextBlock.Text = string.Empty;
        _currentProcessId = 0;

        _cancellationTokenSource?.Dispose();
        if (_cancellationRegistrationId.HasValue)
        {
            OperationCancellationManager.Unregister(_cancellationRegistrationId.Value);
            _cancellationRegistrationId = null;
        }
        _cancellationTokenSource = new CancellationTokenSource();
        _cancellationRegistrationId = OperationCancellationManager.Register(_cancellationTokenSource);
        var ct = _cancellationTokenSource.Token;

        var commandArgs = new Dictionary<string, (string Args, string ScheduleTemplate)>
        {
            ["DISM"] = (isRepair ? "/Online /Cleanup-Image /RestoreHealth" : "/Online /Cleanup-Image /ScanHealth", string.Empty),
            ["SFC"] = (isRepair ? "/scannow" : "/verifyonly", string.Empty),
            // schedule template uses placeholder {DriveRoot}, replaced at runtime
            ["CHKDSK"] = (isRepair ? "/f" : string.Empty, "echo Y|chkdsk {DriveRoot} /f"),
        };

        var current = 0;
        var total = namesToRun.Count;
        var wasCancelled = false;
        var hasError = false;
        var ranNames = new List<string>();
        var chkdskScheduled = false;

        try
        {
            foreach (var name in namesToRun)
            {
                if (ct.IsCancellationRequested)
                {
                    wasCancelled = true;
                    break;
                }

                var friendlyName = Components.First(c => c.Name == name).FriendlyName;
                var (args, scheduleTemplate) = commandArgs[name];

                current++;
                ranNames.Add(name);
                StatusTextBlock.Text = isRepair
                    ? "RepairPage_StatusRepairing".GetLocalized()
                        .Replace("{friendlyName}", friendlyName.ToLowerInvariant())
                        .Replace("{current}", current.ToString())
                        .Replace("{total}", total.ToString())
                    : "RepairPage_StatusChecking".GetLocalized()
                        .Replace("{friendlyName}", friendlyName.ToLowerInvariant())
                        .Replace("{current}", current.ToString())
                        .Replace("{total}", total.ToString());
                ProgressBar.Value = 0;
                PercentageTextBlock.Text = string.Empty;
                UpdateRowUi(name, RowStatus.Pending, string.Empty);

                if (name == "CHKDSK" && isRepair)
                {
                    var driveRoot = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows))?.TrimEnd('\\') ?? "C:";

                    App.ShowNotification("Repair".GetLocalized(), "ScheduledLater".GetLocalized(), InfoBarSeverity.Success, 5000);
                    _scanResults[name].Clear();
                    _scanResults[name].AppendLine("ScheduledLater".GetLocalized());

                    if (!string.IsNullOrEmpty(scheduleTemplate))
                    {
                        var scheduleCmd = scheduleTemplate.Replace("{DriveRoot}", driveRoot);
                        await OptimizationOptions.StartInCmd(scheduleCmd);
                    }

                    _componentHealth[name] = null;
                    chkdskScheduled = true;
                    UpdateRowUi(name, RowStatus.Scheduled, string.Empty);
                    continue;
                }

                try
                {
                    await RunCommandAsync(name, args, ct);

                    var rawOutput = _scanResults[name].ToString();
                    var health = DetermineHealth(name, rawOutput);
                    _componentHealth[name] = isRepair ? (health ?? true) : health;

                    ProcessCommandResult(name);

                    UpdateRowUi(name, _componentHealth[name] switch
                    {
                        true => RowStatus.Healthy,
                        false => RowStatus.NeedsAttention,
                        null => RowStatus.Unknown
                    }, string.Empty);
                }
                catch (OperationCanceledException)
                {
                    wasCancelled = true;
                    break;
                }
                catch (Exception ex)
                {
                    _ = LogHelper.Log($"Error running {name}: {ex.Message}");
                    _scanResults[name].AppendLine($"Error: {ex.Message}");
                    hasError = true;
                    UpdateRowUi(name, RowStatus.Unknown, string.Empty);

                    if (ct.IsCancellationRequested)
                    {
                        wasCancelled = true;
                        break;
                    }
                }
            }
        }
        finally
        {
            _currentProcessId = 0;

            var capturedRanNames = ranNames;
            var capturedWasCancelled = wasCancelled;
            var capturedHasError = hasError;
            var capturedIsRepair = isRepair;
            var capturedChkdskScheduled = chkdskScheduled;
            var anyIssues = _componentHealth.Values.Any(v => v == false);

            DispatcherQueue?.TryEnqueue(() =>
            {
                try
                {
                    ProgressGrid.Visibility = Visibility.Collapsed;
                    StatusTextBlock.Visibility = Visibility.Collapsed;
                    StopButton.Visibility = Visibility.Collapsed;
                    StopButton.IsEnabled = true;
                    ActionPanel.Visibility = Visibility.Visible;
                    PercentageTextBlock.Text = string.Empty;

                    PopulateTechnicalDetailsOnUiThread(capturedRanNames);

                    if (capturedWasCancelled)
                    {
                        App.ShowNotification("Repair".GetLocalized(), "OperationStopped".GetLocalized(), InfoBarSeverity.Error, 5000);
                        SetState(RepairState.Idle);
                    }
                    else
                    {
                        if (capturedHasError)
                        {
                            App.ShowNotification("Repair".GetLocalized(), "UnexpectedError".GetLocalized(), InfoBarSeverity.Error, 5000);
                        }
                        else
                        {
                            App.ShowNotification(
                                "Repair".GetLocalized(),
                                (capturedIsRepair ? "OperationCompleted" : "CheckCompleted").GetLocalized(),
                                InfoBarSeverity.Success,
                                5000);
                        }

                        var now = DateTime.Now;
                        try
                        {
                            ApplicationData.Current.LocalSettings.Values["RepairPage_LastCheckedTime"] = now.ToString("o");
                        }
                        catch (Exception ex)
                        {
                            _ = LogHelper.LogWarning($"Failed to save last checked timestamp: {ex.Message}");
                        }

                        LastCheckedText.Text = "RepairPage_LastCheckedFormat".GetLocalized().Replace("{time}", now.ToString("g"));
                        ResultsCard.Visibility = Visibility.Visible;

                        if (capturedIsRepair)
                        {
                            SetState(anyIssues ? RepairState.IssuesFound : RepairState.RepairCompleted,
                                note: capturedChkdskScheduled ? "RepairPage_ChkdskScheduledNote".GetLocalized() : null);

                            // Notify optimization completion to trigger review popup
                            ReviewPromptHelper.NotifyOptimizationCompleted(XamlRoot);
                        }
                        else
                        {
                            SetState(anyIssues ? RepairState.IssuesFound : RepairState.Healthy);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _ = LogHelper.LogException(ex, "Crash in post-scan UI update");
                }
            });

            try
            {
                if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                }
            }
            catch { }

            if (_cancellationRegistrationId.HasValue)
            {
                OperationCancellationManager.Unregister(_cancellationRegistrationId.Value);
                _cancellationRegistrationId = null;
            }

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void SetState(RepairState state, string? note = null)
    {
        _currentState = state;

        var successBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorSuccessBrush"];
        var cautionBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
        var neutralBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];

        switch (state)
        {
            case RepairState.Idle:
                MainStatusIcon.Glyph = "\uE90F"; // Repair
                MainStatusIcon.Foreground = neutralBrush;
                MainTitleText.Text = "RepairPage_StateIdle_Title".GetLocalized();
                MainSubtitleText.Text = "RepairPage_StateIdle_Subtitle".GetLocalized();
                MainActionButton.Content = "RepairPage_StateIdle_Button".GetLocalized();
                break;

            case RepairState.Checking:
                MainStatusIcon.Glyph = "\uE90F";
                MainStatusIcon.Foreground = neutralBrush;
                MainTitleText.Text = "RepairPage_StateChecking_Title".GetLocalized();
                MainSubtitleText.Text = "RepairPage_StateChecking_Subtitle".GetLocalized();
                break;

            case RepairState.Healthy:
                MainStatusIcon.Glyph = "\uE930"; // Completed
                MainStatusIcon.Foreground = successBrush;
                MainTitleText.Text = "RepairPage_StateHealthy_Title".GetLocalized();
                MainSubtitleText.Text = "RepairPage_StateHealthy_Subtitle".GetLocalized();
                MainActionButton.Content = "RepairPage_StateHealthy_Button".GetLocalized();
                break;

            case RepairState.IssuesFound:
                MainStatusIcon.Glyph = "\uE7BA"; // Important/Warning
                MainStatusIcon.Foreground = cautionBrush;
                MainTitleText.Text = "RepairPage_StateIssuesFound_Title".GetLocalized();
                MainSubtitleText.Text = note ?? "RepairPage_StateIssuesFound_Subtitle".GetLocalized();
                MainActionButton.Content = "RepairPage_StateIssuesFound_Button".GetLocalized();
                break;

            case RepairState.Repairing:
                MainStatusIcon.Glyph = "\uE90F";
                MainStatusIcon.Foreground = neutralBrush;
                MainTitleText.Text = "RepairPage_StateRepairing_Title".GetLocalized();
                MainSubtitleText.Text = "RepairPage_StateRepairing_Subtitle".GetLocalized();
                break;

            case RepairState.RepairCompleted:
                MainStatusIcon.Glyph = "\uE930";
                MainStatusIcon.Foreground = successBrush;
                MainTitleText.Text = "RepairPage_StateRepairCompleted_Title".GetLocalized();
                MainSubtitleText.Text = note ?? "RepairPage_StateRepairCompleted_Subtitle".GetLocalized();
                MainActionButton.Content = "RepairPage_StateHealthy_Button".GetLocalized();
                break;
        }
    }

    private void UpdateRowUi(string name, RowStatus status, string _)
    {
        var (icon, text) = name switch
        {
            "DISM" => (ImageStatusIcon, ImageStatusText),
            "SFC" => (FilesStatusIcon, FilesStatusText),
            "CHKDSK" => (DriveStatusIcon, DriveStatusText),
            _ => (null, null)
        };

        if (icon is null || text is null)
        {
            return;
        }

        void ApplyStatus()
        {
            var successBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorSuccessBrush"];
            var cautionBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
            var neutralBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];

            switch (status)
            {
                case RowStatus.Healthy:
                    icon.Glyph = "\uE73E";
                    icon.Foreground = successBrush;
                    text.Text = "RepairPage_RowStatus_Healthy".GetLocalized();
                    break;
                case RowStatus.NeedsAttention:
                    icon.Glyph = "\uE7BA";
                    icon.Foreground = cautionBrush;
                    text.Text = "RepairPage_RowStatus_NeedsAttention".GetLocalized();
                    break;
                case RowStatus.Scheduled:
                    icon.Glyph = "\uE823";
                    icon.Foreground = neutralBrush;
                    text.Text = "RepairPage_RowStatus_Scheduled".GetLocalized();
                    break;
                case RowStatus.Unknown:
                    icon.Glyph = "\uE946";
                    icon.Foreground = neutralBrush;
                    text.Text = "RepairPage_RowStatus_Unknown".GetLocalized();
                    break;
                default:
                    icon.Glyph = "\uE9CE";
                    icon.Foreground = neutralBrush;
                    text.Text = "RepairPage_RowStatus_Checking".GetLocalized();
                    break;
            }
        }

        // If already on the UI thread, apply directly otherwise enqueue
        if (DispatcherQueue?.HasThreadAccess == true)
        {
            ApplyStatus();
        }
        else
        {
            DispatcherQueue?.TryEnqueue(ApplyStatus);
        }
    }

    private void PopulateTechnicalDetailsOnUiThread(List<string> ranNames)
    {
        TechnicalDetailsPanel.Children.Clear();

        var toolLabel = new Dictionary<string, string>
        {
            ["DISM"] = "DISM — " + "RepairPage_WindowsImage.Text".GetLocalized(),
            ["SFC"] = "SFC — " + "RepairPage_SystemFiles.Text".GetLocalized(),
            ["CHKDSK"] = "CHKDSK — " + "RepairPage_SystemDrive.Text".GetLocalized()
        };

        foreach (var name in ranNames)
        {
            TechnicalDetailsPanel.Children.Add(new TextBlock
            {
                Text = toolLabel.TryGetValue(name, out var label) ? label : name,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 8, 0, 4)
            });
            TechnicalDetailsPanel.Children.Add(new TextBlock
            {
                Text = _scanResults[name].ToString().Trim(),
                TextWrapping = TextWrapping.Wrap,
                Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
                IsTextSelectionEnabled = true
            });
        }

        TechnicalDetailsExpander.Visibility = ranNames.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static bool? DetermineHealth(string name, string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        var text = output.ToLowerInvariant();

        return name switch
        {
            "DISM" =>
                text.Contains("no component store corruption") || text.Contains("operation completed successfully")
                    ? true
                    : text.Contains("repairable") || text.Contains("corruption detected") || text.Contains("restore health")
                        ? false
                        : (bool?)null,

            "SFC" =>
                text.Contains("did not find any integrity violations")
                    ? true
                    : text.Contains("found corrupt files")
                        ? false
                        : (bool?)null,

            "CHKDSK" =>
                text.Contains("found no problems") || text.Contains("no further action is required")
                    ? true
                    : text.Contains("errors found") || text.Contains("found problems")
                        ? false
                        : (bool?)null,

            _ => null
        };
    }


    private async Task RunCommandAsync(string name, string args, CancellationToken ct)
    {
        _scanResults[name].Clear();

        if (name == "SFC")
        {
            _sfcPrefaceLinesSkipped = 0;
        }

        var toolExecutable = name switch
        {
            "DISM" => "dism.exe",
            "SFC" => "sfc.exe",
            "CHKDSK" => "chkdsk.exe",
            _ => name + ".exe"
        };

        var fileName = GetSystemToolPath(toolExecutable);

        try
        {
            var stdinInput = name == "CHKDSK" ? "N\n" : null;

            await PseudoConsoleHelper.RunAsync(
                $"\"{fileName}\" {args}",
                line =>
                {
                    LogHelper.Log($"Output: {line}");
                    HandleOutputLine(name, line);
                },
                ct,
                processId => _currentProcessId = processId,
                stdinInput);
        }
        catch (OperationCanceledException)
        {
            _ = LogHelper.Log($"Operation cancelled for {name}");
            throw;
        }
        catch (Exception ex)
        {
            _ = LogHelper.Log($"ConPTY failed for {name}, falling back to standard: {ex.Message}");
            await RunCommandStandardAsync(name, fileName, args, ct);
        }
    }

    private async Task RunCommandStandardAsync(string name, string fileName, string args, CancellationToken ct)
    {
        var outputEncoding = name.Equals("SFC", StringComparison.OrdinalIgnoreCase)
            ? Encoding.Unicode
            : await ConsoleEncodingHelper.GetOemConsoleEncodingAsync();

        var processStartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = outputEncoding,
            StandardErrorEncoding = outputEncoding
        };

        _runningProcess = new Process { StartInfo = processStartInfo, EnableRaisingEvents = true };

        try
        {
            _runningProcess.Start();
            _currentProcessId = _runningProcess.Id;

            var outputTask = ReadStreamAsync(_runningProcess.StandardOutput, name, isError: false, ct);
            var errorTask = ReadStreamAsync(_runningProcess.StandardError, name, isError: true, ct);

            try
            {
                await Task.WhenAll(_runningProcess.WaitForExitAsync(ct), outputTask, errorTask);
            }
            catch (OperationCanceledException)
            {
                await PseudoConsoleHelper.KillProcessTreeAsync(_runningProcess.Id);
                throw;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _ = LogHelper.Log($"Failed to start {name}: {ex.Message}");
            _scanResults[name].AppendLine(ex.Message);
            throw;
        }
        finally
        {
            _runningProcess = null;
            _currentProcessId = 0;
        }
    }

    private async Task ReadStreamAsync(StreamReader reader, string name, bool isError, CancellationToken ct)
    {
        var buffer = new char[256];
        var lineBuilder = new StringBuilder();

        while (!ct.IsCancellationRequested)
        {
            int read;
            try
            {
                read = await reader.ReadAsync(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogWarning($"Error reading stream for {name}: {ex.Message}");
                break;
            }

            if (read == 0)
            {
                FlushLine(lineBuilder, name, isError);
                break;
            }

            for (var i = 0; i < read; i++)
            {
                var ch = buffer[i];
                if (ch == '\r' || ch == '\n')
                {
                    FlushLine(lineBuilder, name, isError);
                }
                else
                {
                    lineBuilder.Append(ch);
                }
            }
        }
    }

    private void FlushLine(StringBuilder lineBuilder, string name, bool isError)
    {
        if (lineBuilder.Length == 0)
        {
            return;
        }

        var line = lineBuilder.ToString();
        lineBuilder.Clear();

        if (isError)
        {
            LogHelper.Log($"Error: {line}");
            _scanResults[name].AppendLine(line);
            return;
        }

        LogHelper.Log($"Output: {line}");
        HandleOutputLine(name, line);
    }

    private void HandleOutputLine(string name, string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        UpdateProgress(name, line);

        if (name == "DISM")
        {
            line = Regex.Replace(line, @"\[\s*[= ]*\s*\d+(?:[\.,]\d+)?%\s*[= ]*\]\s*", string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }
        }

        var isProgress = name switch
        {
            "DISM" => Regex.IsMatch(line, @"^\s*\[\s*[= ]*\s*(\d+(\.\d+)?)%\s*[= ]*\]\s*$"),
            "SFC" => Regex.IsMatch(line, @"^\s*[^\d\r\n]*?(\d{1,3}(?:[\.,]\d+)?)\s*%\s*[^\d\r\n]*$"),
            "CHKDSK" => Regex.IsMatch(line, @"^\s*[^\d\r\n]*?(\d{1,3}(?:[\.,]\d+)?)\s*%\s*[^\d\r\n]*$"),
            _ => false
        };

        if (isProgress)
        {
            return;
        }

        if (name == "SFC" && _sfcPrefaceLinesSkipped < 2)
        {
            _sfcPrefaceLinesSkipped++;
            return;
        }

        _scanResults[name].AppendLine(line);
    }

    private void UpdateProgress(string commandName, string data)
    {
        if (string.IsNullOrEmpty(data))
        {
            return;
        }

        var percentage = 0;

        try
        {
            if (commandName == "DISM")
            {
                var match = Regex.Match(data, @"\[\s*[= ]*\s*(\d+(\.\d+)?)%\s*[= ]*\]");
                if (match.Success)
                {
                    percentage = (int)Math.Round(double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
                }
            }
            else if (commandName == "SFC")
            {
                var match = Regex.Match(data, @"(\d+)%", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    percentage = int.Parse(match.Groups[1].Value);
                }
            }
            else if (commandName == "CHKDSK")
            {
                var match = Regex.Match(data, @"(\d+(?:[\.,]\d+)?)\s*%", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var percentageText = match.Groups[1].Value.Replace(',', '.');
                    percentage = (int)Math.Round(double.Parse(percentageText, CultureInfo.InvariantCulture));
                }
            }

            if (percentage > 0 && percentage <= 100)
            {
                DispatcherQueue?.TryEnqueue(() =>
                {
                    ProgressBar.Value = percentage;
                    PercentageTextBlock.Text = $"{percentage}%";
                });
            }
        }
        catch (Exception ex)
        {
            LogHelper.Log($"Error updating progress: {ex.Message}");
        }
    }

    private static string GetSystemToolPath(string toolExecutable)
    {
        var winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (string.IsNullOrEmpty(winDir))
        {
            return toolExecutable;
        }

        if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
        {
            var sysNativePath = Path.Combine(winDir, "SysNative", toolExecutable);
            if (File.Exists(sysNativePath))
            {
                return sysNativePath;
            }
        }

        var system32Path = Path.Combine(winDir, "System32", toolExecutable);
        if (File.Exists(system32Path))
        {
            return system32Path;
        }

        return Path.Combine(winDir, toolExecutable);
    }

    private void ProcessCommandResult(string commandName)
    {
        var rawOutput = _scanResults[commandName].ToString();
        var lines = rawOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0) return;

        _scanResults[commandName].Clear();

        if (commandName == "DISM")
        {
            var dismLines = rawOutput
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(l => !string.IsNullOrWhiteSpace(l) && !l.Contains(@"[==========================") && !l.EndsWith(@"%]"))
                .TakeLast(5)
                .ToList();

            if (dismLines.Count > 0)
            {
                foreach (var line in dismLines)
                {
                    _scanResults[commandName].AppendLine(line);
                }
            }
            else
            {
                var lastLines = lines.Skip(Math.Max(0, lines.Length - 5)).ToList();
                foreach (var line in lastLines)
                {
                    _scanResults[commandName].AppendLine(line);
                }
            }
        }
        else if (commandName == "CHKDSK")
        {
            var lastLines = lines.Skip(Math.Max(0, lines.Length - 10)).ToList();
            foreach (var line in lastLines)
            {
                _scanResults[commandName].AppendLine(line);
            }
        }
        else if (commandName == "SFC")
        {
            var lastLines = lines.Skip(Math.Max(0, lines.Length - 5)).ToList();
            foreach (var line in lastLines)
            {
                _scanResults[commandName].AppendLine(line);
            }
            try
            {
                var cbsLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Logs", "CBS", "CBS.log");
                if (File.Exists(cbsLogPath))
                {
                    var cbsLines = File.ReadLines(cbsLogPath)
                                       .Where(l => l.Contains("[SR] Repair", StringComparison.OrdinalIgnoreCase))
                                       .ToList();

                    if (cbsLines.Count > 0)
                    {
                        var headerStr = "RepairedFiles".GetLocalized();
                        if (string.IsNullOrEmpty(headerStr) || headerStr == "RepairedFiles")
                        {
                            headerStr = "Repaired files (from CBS.log):";
                        }

                        _scanResults[commandName].AppendLine();
                        _scanResults[commandName].AppendLine(headerStr);

                        foreach (var line in cbsLines)
                        {
                            _scanResults[commandName].AppendLine(line);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Could not parse CBS.log: {ex.Message}");
            }
        }
    }

    private async void BatteryHealthButton_Click(object sender, RoutedEventArgs e)
    {
        var reportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "batteryreport.html");

        _ = LogHelper.Log($"Generating battery report to {reportPath}");
        var command = $"%SystemRoot%\\System32\\powercfg.exe /batteryreport /output \"{reportPath}\"";

        var result = await OptimizationOptions.StartInCmd(command);

        if (result == 0 && File.Exists(reportPath))
        {
            _ = LogHelper.Log("Battery report generated successfully");
            App.ShowNotification("BatteryStatus".GetLocalized(), "ReportSaved".GetLocalized(), InfoBarSeverity.Success, 5000);
            return;
        }
        _ = LogHelper.LogError($"Battery report generation failed with exit code {result}");
        App.ShowNotification("BatteryStatus".GetLocalized(), "UnexpectedError".GetLocalized(), InfoBarSeverity.Error, 5000);
    }

    private async void MemoryHealthButton_Click(object sender, RoutedEventArgs e)
    {
        _ = LogHelper.Log("Opening memory diagnostic dialog");
        var memDialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            RequestedTheme = ActualTheme,
            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
            BorderBrush = (SolidColorBrush)Application.Current.Resources["AccentAAFillColorDefaultBrush"],
            PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"],
            SecondaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"],
            Title = "MemoryDiagnosticDialogTitle".GetLocalized(),
            Content = "MemoryDiagnosticDialogText".GetLocalized(),
            PrimaryButtonText = "RestartNow".GetLocalized(),
            SecondaryButtonText = "ScheduleLater".GetLocalized(),
            CloseButtonText = "Cancel".GetLocalized()
        };
        memDialog.PrimaryButtonClick += async (sender, args) =>
        {
            await OptimizationOptions.StartInCmd("bcdedit /bootsequence {memdiag} && shutdown /r /t 0");
        };
        memDialog.SecondaryButtonClick += async (sender, args) =>
        {
            App.ShowNotification("MemoryDiagnosticDialogTitle".GetLocalized(), "ScheduledLater".GetLocalized(), InfoBarSeverity.Success, 5000);
            MemCheckButton.IsEnabled = false;
            await OptimizationOptions.StartInCmd("bcdedit /bootsequence {memdiag}");
        };
        await memDialog.ShowAsync();
    }

    private async void EventViewerSettingsCard_Click(object sender, RoutedEventArgs e)
    {
        _ = LogHelper.Log("Opening Event Viewer");
        await OptimizationOptions.StartInCmd("eventvwr.msc");
    }

    private async void DiskOptimizationsButton_Click(object sender, RoutedEventArgs e)
    {
        _ = LogHelper.Log("Opening Disk Optimization utility");
        await OptimizationOptions.StartInCmd("%SystemRoot%\\System32\\dfrgui.exe");
    }
}