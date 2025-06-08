using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
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
    private int _sfcNonProgressLineCount = 0;
    private Process? _runningProcess;
    public int selectedCount = 0;
    public RepairPage()
    {
        InitializeComponent();
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
                StatusTextBlock.Text = $"{current} of {selectedCount}: {name} {(isRepair ? "repair" : "scan")} in progress...";
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

        var processStartInfo = new ProcessStartInfo
        {
            FileName = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                ? Path.Combine(Environment.GetEnvironmentVariable("windir"), @"SysNative\cmd.exe")
                : Path.Combine(Environment.GetEnvironmentVariable("windir"), @"System32\cmd.exe"),
            Arguments = $"/C {name} {args}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = name.Equals("SFC", StringComparison.OrdinalIgnoreCase)
                            ? Encoding.Unicode // UTF-16LE for SFC
                            : Encoding.UTF8    // UTF-8 for other commands
        };

        _runningProcess = new Process { StartInfo = processStartInfo, EnableRaisingEvents = true };

        _runningProcess.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                LogHelper.Log($"Output: {e.Data}");
                UpdateProgress(name, e.Data);

                var isProgress = false;
                if (name == "DISM")
                {
                    isProgress = Regex.IsMatch(e.Data, @"\[\s*[= ]*\s*(\d+(\.\d+)?)%\s*[= ]*\]");
                }
                else if (name == "SFC")
                {
                    isProgress = Regex.IsMatch(e.Data, @"(\d+)%", RegexOptions.IgnoreCase);
                }
                else if (name == "CHKDSK")
                {
                    isProgress = Regex.IsMatch(e.Data, @"Total:\s*(\d+)%", RegexOptions.IgnoreCase);
                }

                if (!isProgress)
                {
                    if (name == "SFC")
                    {
                        if (_sfcNonProgressLineCount < 2)
                        {
                            _sfcNonProgressLineCount++;
                        }
                        else
                        {
                            _scanResults[name].AppendLine(e.Data);
                        }
                    }
                    else
                    {
                        _scanResults[name].AppendLine(e.Data);
                    }
                }
            }
        };

        _runningProcess.Start();
        _runningProcess.BeginOutputReadLine();
        _runningProcess.BeginErrorReadLine();

        await _runningProcess.WaitForExitAsync();
    }

    private void UpdateProgress(string commandName, string data)
    {
        if (string.IsNullOrEmpty(data)) return;

        var percentage = 0;

        try
        {
            if (commandName == "DISM")
            {
                // Match the progress from DISM (e.g., [====    ] 50% complete)
                var match = Regex.Match(data, @"\[\s*[= ]*\s*(\d+(\.\d+)?)%\s*[= ]*\]");
                if (match.Success)
                {
                    percentage = (int)Math.Round(double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
                }
            }
            else if (commandName == "SFC")
            {

                // Match the number immediately before the '%' sign
                var match = Regex.Match(data, @"(\d+)%", RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    percentage = int.Parse(match.Groups[1].Value);
                }
                else
                {
                    LogHelper.Log($"No match found for SFC progress: {data}");
                }
            }
            else if (commandName == "CHKDSK")
            {
                // Match the 'Total' progress specifically
                var match = Regex.Match(data, @"Total:\s*(\d+)%", RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    percentage = int.Parse(match.Groups[1].Value);
                    LogHelper.Log($"CHKDSK Total Progress: {percentage}%");
                }
                else
                {
                    LogHelper.Log($"No match found for CHKDSK progress: {data}");
                }
            }

            // Ensure the progress bar only updates if the percentage is between 0 and 100
            if (percentage > 0 && percentage <= 100)
            {
                // Ensure updates happen on the UI thread
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

    private async Task ShowScanResultsDialogAsync(List<string> selectedNames)
    {
        var stackPanel = new StackPanel();

        foreach (var name in selectedNames)
        {
            stackPanel.Children.Add(new TextBlock
            {
                Text = $"{name}:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 16, 0, 8)
            });
            stackPanel.Children.Add(new TextBlock
            {
                Text = _scanResults[name].ToString(),
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 100,
                MaxHeight = 200,
                Margin = new Thickness(0, 0, 0, 8)
            });
        }

        var scrollViewer = new ScrollViewer
        {
            Content = stackPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = 400
        };

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
            BorderBrush = (SolidColorBrush)Application.Current.Resources["AccentAAFillColorDefaultBrush"],
            Title = "Scan Results",
            Content = scrollViewer,
            CloseButtonText = "Close".GetLocalized()
        };

        await dialog.ShowAsync();
    }

    private async void BatteryHealthButton_Click(object sender, RoutedEventArgs e)
    {
        await OptimizationOptions.StartInCmd($"powercfg /batteryreport /output \"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\\batteryreport.html\"");
        App.ShowNotification("BatteryStatus".GetLocalized(), "ReportSaved".GetLocalized(), InfoBarSeverity.Informational, 5000);
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
            App.ShowNotification("MemoryDiagnosticDialogTitle".GetLocalized(), "ScheduledLater".GetLocalized(), InfoBarSeverity.Informational, 5000);
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
        await OptimizationOptions.StartInCmd("dfrgui.exe");
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
