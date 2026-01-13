using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using RyTuneX.Helpers;

namespace RyTuneX.Views;

public sealed partial class RepairPage : Page
{
    private readonly Dictionary<string, StringBuilder> _scanResults = new()
    {
        { "DISM", new StringBuilder() },
        { "SFC", new StringBuilder() },
        { "CHKDSK", new StringBuilder() }
    };
    private Process? _runningProcess;
    public int selectedCount = 0;
    private string? _pendingScrollTarget;

    public RepairPage()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        Loaded += RepairPage_Loaded;
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
        if (!string.IsNullOrEmpty(_pendingScrollTarget))
        {
            await ScrollToElementHelper.ScrollToElementAsync(this, _pendingScrollTarget);
            _pendingScrollTarget = null;
        }
    }

    private async void OnScanButtonClick(object sender, RoutedEventArgs e)
    {
        if (selectedCount == 0) { return; }
        await RunCommandsAsync(isRepair: false);
    }

    private async void OnRepairButtonClick(object sender, RoutedEventArgs e)
    {
        if (selectedCount == 0) { return; }
        await RunCommandsAsync(isRepair: true);
    }

    private void OnStopButtonClick(object sender, RoutedEventArgs e)
    {
        if (_runningProcess != null && !_runningProcess.HasExited)
        {
            _runningProcess.Kill(); // Stop the running process
            StatusTextBlock.Text = "OperationStopped".GetLocalized();
            ProgressBar.Value = 0;
            StopButton.Visibility = Visibility.Collapsed;
            ScanRepairPanel.Visibility = Visibility.Visible;
            PercentageTextBlock.Text = string.Empty;
        }
    }

    private async Task RunCommandsAsync(bool isRepair)
    {
        // Hide ScanRepair Panel while operation is in progress
        ScanRepairPanel.Visibility = Visibility.Collapsed;
        StopButton.Visibility = Visibility.Visible;
        ProgressBar.Value = 0;

        var commands = new[]
        {
            (DismCheckBox, "DISM", isRepair ? "/Online /Cleanup-Image /RestoreHealth" : "/Online /Cleanup-Image /ScanHealth"),
            (SfcCheckBox, "SFC", isRepair ? "/scannow" : "/verifyonly"),
            (ChkdskCheckBox, "CHKDSK", isRepair ? "/f" : "")
        };

        var current = 0;
        var selectedNames = new List<string>();
        foreach (var (checkBox, name, args) in commands)
        {
            if (checkBox.IsChecked == true)
            {
                current++;
                selectedNames.Add(name);
                StatusTextBlock.Text = $"{current} / {selectedCount}: {name} {(isRepair ? "repair" : "scan")} in progress...";
                ProgressBar.Value = 0;
                await RunCommandAsync(name, args);
            }
        }

        StatusTextBlock.Text = "OperationCompleted".GetLocalized();
        ScanRepairPanel.Visibility = Visibility.Visible;
        StopButton.Visibility = Visibility.Collapsed;
        PercentageTextBlock.Text = string.Empty;
        ProgressBar.Value = 0;

        // Show results dialog
        await ShowScanResultsDialogAsync(selectedNames);
    }

    private async Task RunCommandAsync(string name, string args)
    {
        _scanResults[name].Clear();

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
            await PseudoConsoleHelper.RunAsync(
                $"\"{fileName}\" {args}",
                line =>
                {
                    LogHelper.Log($"Output: {line}");
                    HandleOutputLine(name, line);
                });
        }
        catch (Exception ex)
        {
            await LogHelper.Log($"ConPTY failed for {name}, falling back to standard: {ex.Message}");
            await RunCommandStandardAsync(name, fileName, args);
        }
    }

    private async Task RunCommandStandardAsync(string name, string fileName, string args)
    {
        var outputEncoding = name.Equals("SFC", StringComparison.OrdinalIgnoreCase)
            ? Encoding.Unicode
            : Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);

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

            var outputTask = ReadStreamAsync(_runningProcess.StandardOutput, name, isError: false);
            var errorTask = ReadStreamAsync(_runningProcess.StandardError, name, isError: true);

            await Task.WhenAll(_runningProcess.WaitForExitAsync(), outputTask, errorTask);
        }
        catch (Exception ex)
        {
            await LogHelper.Log($"Failed to start {name}: {ex.Message}");
            _scanResults[name].AppendLine(ex.Message);
            DispatcherQueue.TryEnqueue(() => StatusTextBlock.Text = $"Failed to start {name}");
        }
    }

    private async Task ReadStreamAsync(StreamReader reader, string name, bool isError)
    {
        var buffer = new char[256];
        var lineBuilder = new StringBuilder();

        while (true)
        {
            var read = await reader.ReadAsync(buffer, 0, buffer.Length);
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

        var isProgress = name switch
        {
            "DISM" => Regex.IsMatch(line, @"\[\s*[= ]*\s*(\d+(\.\d+)?)%\s*[= ]*\]"),
            "SFC" => Regex.IsMatch(line, @"^\s*(\d+)\s*%\s*$"),
            "CHKDSK" => Regex.IsMatch(line, @"Total:\s*(\d+)%", RegexOptions.IgnoreCase),
            _ => false
        };

        if (isProgress)
        {
            return;
        }

        if (name == "SFC" || name == "DISM")
        {
            // Keep only the last meaningful non-progress line (the result)
            _scanResults[name].Clear();
            _scanResults[name].AppendLine(line);
        }
        else
        {
            _scanResults[name].AppendLine(line);
        }
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
                else{}
            }
            else if (commandName == "CHKDSK")
            {
                var match = Regex.Match(data, @"Total:\s*(\d+)%", RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    percentage = int.Parse(match.Groups[1].Value);
                }
                else{}
            }

            if (percentage > 0 && percentage <= 100)
            {
                DispatcherQueue.TryEnqueue(() =>
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
        var winDir = Environment.GetEnvironmentVariable("windir");
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

    private async Task ShowScanResultsDialogAsync(List<string> selectedNames)
    {
        var stackPanel = new StackPanel { Spacing = 8 };

        foreach (var name in selectedNames)
        {
            stackPanel.Children.Add(new TextBlock
            {
                Text = $"{name}:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 8, 0, 4)
            });
            stackPanel.Children.Add(new TextBlock
            {
                Text = _scanResults[name].ToString().Trim(),
                TextWrapping = TextWrapping.Wrap,
                IsTextSelectionEnabled = true
            });
        }

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
            BorderBrush = (SolidColorBrush)Application.Current.Resources["AccentAAFillColorDefaultBrush"],
            Title = "ScanResults".GetLocalized(),
            Content = new ScrollViewer
            {
                Content = stackPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                MaxHeight = 350
            },
            CloseButtonText = "Close".GetLocalized()
        };

        await dialog.ShowAsync();
    }

    private async void BatteryHealthButton_Click(object sender, RoutedEventArgs e)
    {
        var reportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "batteryreport.html");

        var command = $"%SystemRoot%\\System32\\powercfg.exe /batteryreport /output \"{reportPath}\"";

        var result = await OptimizationOptions.StartInCmd(command);

        if (result == 0 && File.Exists(reportPath))
        {
            App.ShowNotification("BatteryStatus".GetLocalized(), "ReportSaved".GetLocalized(), InfoBarSeverity.Success, 5000);
            return;
        }
        App.ShowNotification("BatteryStatus".GetLocalized(), "UnexpectedError".GetLocalized(), InfoBarSeverity.Error, 5000);
    }

    private async void MemoryHealthButton_Click(object sender, RoutedEventArgs e)
    {
        var memDialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
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
        await OptimizationOptions.StartInCmd("eventvwr.msc");
    }
    private async void DiskOptimizationsButton_Click(object sender, RoutedEventArgs e)
    {
        await OptimizationOptions.StartInCmd("%SystemRoot%\\System32\\dfrgui.exe");

    }

    private void CheckBox_Changed(object sender, RoutedEventArgs e)
    {
        selectedCount = 0;
        foreach (var checkbox in CheckBoxes.Children)
        {
            if (checkbox is CheckBox checkBox && checkBox.IsChecked == true)
            {
                selectedCount++;
            }
        }
    }
}
