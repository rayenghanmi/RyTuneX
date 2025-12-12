using System.Diagnostics;
using System.Management;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Win32;
using NetFwTypeLib;
using RyTuneX.Helpers;

namespace RyTuneX.Views;

public sealed partial class SecurityPage : Page
{
    private DispatcherTimer? _refreshTimer;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isCheckInProgress;
    private string? _pendingScrollTarget;

    public SecurityPage()
    {
        InitializeComponent();

        // Initialize cancellation token
        _cancellationTokenSource = new CancellationTokenSource();

        // Start initial check asynchronously
        _ = CheckSecurityStatusAsync(_cancellationTokenSource.Token);

        // Auto-refresh every 30 seconds
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _refreshTimer.Tick += async (s, e) => await CheckSecurityStatusAsync(_cancellationTokenSource.Token);
        _refreshTimer.Start();

        // Clean up on unload
        Unloaded += (s, e) =>
        {
            _refreshTimer?.Stop();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        };

        Loaded += SecurityPage_Loaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string optionTag && !string.IsNullOrEmpty(optionTag))
        {
            _pendingScrollTarget = optionTag;
        }
    }

    private async void SecurityPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_pendingScrollTarget))
        {
            await ScrollToElementHelper.ScrollToElementAsync(this, _pendingScrollTarget);
            _pendingScrollTarget = null;
        }
    }

    private async Task CheckSecurityStatusAsync(CancellationToken cancellationToken = default)
    {
        // Prevent multiple simultaneous checks
        if (_isCheckInProgress)
            return;

        _isCheckInProgress = true;

        try
        {
            // Run all checks in parallel on background thread
            var checksTask = Task.Run(async () =>
            {
                var antivirusInfo = await GetAntivirusInfoAsync(cancellationToken).ConfigureAwait(false);
                var firewallProtection = await IsFirewallEnabledAsync(cancellationToken).ConfigureAwait(false);
                var windowsUpdate = await IsWindowsUpdateEnabledAsync(cancellationToken).ConfigureAwait(false);
                var smartscreen = await IsSmartScreenEnabledAsync(cancellationToken).ConfigureAwait(false);
                var realTimeProtection = await IsRealTimeProtectionEnabledAsync(cancellationToken).ConfigureAwait(false);
                var uac = await IsUACEnabledAsync(cancellationToken).ConfigureAwait(false);
                var tamperProtection = await IsTamperProtectionEnabledAsync(cancellationToken).ConfigureAwait(false);
                var controlledFolderAccess = await IsControlledFolderAccessEnabledAsync(cancellationToken).ConfigureAwait(false);
                var bitLockerEnabled = await IsBitLockerEnabledAsync(cancellationToken).ConfigureAwait(false);
                var defenderServiceEnabled = await IsDefenderServiceEnabledAsync(cancellationToken).ConfigureAwait(false);

                return (antivirusInfo, firewallProtection, windowsUpdate, smartscreen, realTimeProtection,
                        uac, tamperProtection, controlledFolderAccess, bitLockerEnabled, defenderServiceEnabled);
            }, cancellationToken);

            var results = await checksTask.ConfigureAwait(true);

            // Update UI on UI thread
            DispatcherQueue.TryEnqueue(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                UpdateStatusCard(VirusThreatProtectionStatus, VirusThreatProtectionLink, results.antivirusInfo.IsEnabled);
                UpdateStatusCard(FirewallStatus, FirewallLink, results.firewallProtection);
                UpdateStatusCard(WindowsUpdateStatus, WindowsUpdateLink, results.windowsUpdate);
                UpdateStatusCard(SmartScreenStatus, SmartScreenLink, results.smartscreen);
                UpdateStatusCard(RealTimeProtectionStatus, RealTimeProtectionLink, results.realTimeProtection);
                UpdateStatusCard(UACStatus, UACLink, results.uac);
                UpdateStatusCard(TamperProtectionStatus, TamperProtectionLink, results.tamperProtection);
                UpdateStatusCard(ControlledFolderAccessStatus, ControlledFolderAccessLink, results.controlledFolderAccess);
                UpdateStatusCard(BitLockerStatus, BitLockerLink, results.bitLockerEnabled);
                UpdateStatusCard(DefenderServiceStatus, DefenderServiceLink, results.defenderServiceEnabled);

                AntivirusProductName.Text = results.antivirusInfo.ProductName ?? "None".GetLocalized();

                // Show signature update date if available
                if (results.antivirusInfo.SignatureUpdated.HasValue)
                {
                    SignatureUpdateText.Text = $"{"SecurityPage_LastUpdated".GetLocalized()}: {results.antivirusInfo.SignatureUpdated.Value:g}";
                    SignatureUpdateText.Visibility = Visibility.Visible;
                }
                else
                {
                    SignatureUpdateText.Visibility = Visibility.Collapsed;
                }

                UpdateSecurityImage(results.antivirusInfo.IsEnabled, results.firewallProtection, results.windowsUpdate,
                    results.smartscreen, results.uac, results.realTimeProtection, results.tamperProtection, results.defenderServiceEnabled);

                LastRefreshedText.Text = $"{"SecurityPage_LastRefreshed".GetLocalized()}: {DateTime.Now:T}";
            });
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error checking security status: {ex.Message}");
        }
        finally
        {
            _isCheckInProgress = false;
        }
    }

    private void UpdateStatusCard(TextBlock statusText, HyperlinkButton link, bool isEnabled)
    {
        statusText.Text = isEnabled ? "Enabled".GetLocalized() : "Disabled".GetLocalized();
        link.Visibility = isEnabled ? Visibility.Collapsed : Visibility.Visible;
    }

    [Flags]
    enum ProductState
    {
        Off = 0x0000,
        On = 0x1000,
        Snoozed = 0x2000,
        Expired = 0x3000
    }

    private class AntivirusInfo
    {
        public string? ProductName
        {
            get; set;
        }
        public bool IsEnabled
        {
            get; set;
        }
        public DateTime? SignatureUpdated
        {
            get; set;
        }
    }

    private async Task<AntivirusInfo> GetAntivirusInfoAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var result = new AntivirusInfo { ProductName = "Windows Defender", IsEnabled = false };

            try
            {
                // Try to get antivirus products from SecurityCenter2
                using var searcher = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT * FROM AntiVirusProduct");
                var products = searcher.Get();

                if (products.Count > 0)
                {
                    foreach (var obj in products)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        var productName = obj["displayName"]?.ToString();

                        if (obj["productState"] != null && int.TryParse(obj["productState"].ToString(), out var state))
                        {
                            var productState = (ProductState)(state & 0xF000);
                            var isEnabled = productState == ProductState.On;

                            // Prefer enabled products
                            if (isEnabled || result.ProductName == "Windows Defender")
                            {
                                result.ProductName = productName ?? "Unknown Antivirus";
                                result.IsEnabled = isEnabled;
                            }
                        }
                    }
                }

                // Try to get Windows Defender signature update date
                try
                {
                    using var defenderSearcher = new ManagementObjectSearcher(@"root\Microsoft\Windows\Defender",
                        "SELECT * FROM MSFT_MpComputerStatus");
                    foreach (var obj in defenderSearcher.Get())
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        if (obj["AntivirusSignatureLastUpdated"] != null)
                        {
                            result.SignatureUpdated = ManagementDateTimeConverter.ToDateTime(obj["AntivirusSignatureLastUpdated"].ToString());
                        }
                        break;
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Error getting antivirus info: {ex.Message}");
            }

            return result;
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> IsFirewallEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var type = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
                if (type != null && Activator.CreateInstance(type) is INetFwPolicy2 firewallPolicy)
                {
                    // Check all profiles: Domain, Private, and Public
                    return firewallPolicy.FirewallEnabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN] ||
                           firewallPolicy.FirewallEnabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE] ||
                           firewallPolicy.FirewallEnabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC];
                }
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Error checking firewall: {ex.Message}");
            }
            return false;
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> IsWindowsUpdateEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Check multiple registry keys for Windows Update status
                using var key1 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU");
                if (key1?.GetValue("NoAutoUpdate") is int noAutoUpdate && noAutoUpdate == 1)
                    return false;

                using var key2 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate");
                if (key2?.GetValue("DisableWindowsUpdateAccess") is int disabled && disabled == 1)
                    return false;

                // Check service status
                using var key3 = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\wuauserv");
                if (key3?.GetValue("Start") is int start && start == 4)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Error checking Windows Update: {ex.Message}");
            }
            return false;
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> IsSmartScreenEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Check if SmartScreen is explicitly disabled in policies
                using var policyKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System");
                if (policyKey?.GetValue("EnableSmartScreen") is int policyValue && policyValue == 0)
                {
                    return false;
                }

                // Check the main SmartScreen setting
                using var explorerKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer");
                var smartScreenValue = explorerKey?.GetValue("SmartScreenEnabled") as string;

                // If explicitly set to "Off", it's disabled
                if (smartScreenValue == "Off")
                    return false;

                // Check user-level setting
                using var userExplorerKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer");
                var userSmartScreenValue = userExplorerKey?.GetValue("SmartScreenEnabled") as string;

                if (userSmartScreenValue == "Off")
                    return false;

                // If not explicitly disabled, consider it enabled
                return true;
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Error checking SmartScreen: {ex.Message}");
            }
            return true; // Default to enabled if we can't determine
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> IsRealTimeProtectionEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Real-Time Protection");
                var value = key?.GetValue("DisableRealtimeMonitoring");

                // If the value doesn't exist or is 0, real-time protection is enabled
                if (value == null)
                    return true;

                return (int)value == 0;
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Error checking Real-Time Protection: {ex.Message}");
            }
            return false;
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> IsUACEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
                var value = key?.GetValue("EnableLUA");
                return value is int enabled && enabled == 1;
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Error checking UAC: {ex.Message}");
            }
            return false;
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> IsTamperProtectionEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Features");
                var value = key?.GetValue("TamperProtection");

                if (value == null)
                {
                    return true; // Default to enabled if not found
                }

                return (int)value == 5; // 5 means enabled
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Error checking Tamper Protection: {ex.Message}");
            }
            return false;
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> IsControlledFolderAccessEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(async () =>
        {
            try
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-NoProfile -NonInteractive -Command \"(Get-MpPreference).EnableControlledFolderAccess\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(psi);
                    if (process != null)
                    {
                        var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

                        _ = LogHelper.Log($"Controlled Folder Access PowerShell output: '{output}'");

                        // Parse the output
                        // 0 = Disabled, 1 = Enabled, 2 = Audit mode
                        if (int.TryParse(output.Trim(), out var status))
                        {
                            _ = LogHelper.Log($"Controlled Folder Access status: {status}");
                            return status != 0; // Enabled if 1 or 2
                        }
                    }
                }
                catch (Exception psEx)
                {
                    _ = LogHelper.LogError($"PowerShell check for Controlled Folder Access failed: {psEx.Message}");
                }

                // Fallback to registry check
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Windows Defender Exploit Guard\Controlled Folder Access");
                if (key != null)
                {
                    var value = key.GetValue("EnableControlledFolderAccess");

                    if (value != null)
                    {
                        var status = (int)value;
                        _ = LogHelper.Log($"Controlled Folder Access Registry status: {status}");
                        // 0 = Disabled, 1 = Enabled, 2 = Audit mode (consider as enabled)
                        return status != 0;
                    }
                }

                _ = LogHelper.Log("Controlled Folder Access: No value found, assuming disabled");
                return false;
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Error checking Controlled Folder Access: {ex.Message}");
            }
            return false;
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> IsBitLockerEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var drives = DriveInfo.GetDrives();
                foreach (var drive in drives)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    if (drive.DriveType == DriveType.Fixed)
                    {
                        // Try WMI first (more reliable)
                        try
                        {
                            using var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_EncryptableVolume WHERE DriveLetter = '{drive.Name.TrimEnd('\\', ':')}'");
                            foreach (var volume in searcher.Get())
                            {
                                var protectionStatus = volume["ProtectionStatus"];
                                if (protectionStatus != null && (uint)protectionStatus == 1)
                                {
                                    return true; // At least one drive is encrypted
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Error checking BitLocker: {ex.Message}");
            }
            return false;
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> IsDefenderServiceEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Check service startup type (not just running state)
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\WinDefend");
                if (key?.GetValue("Start") is int startType)
                {
                    // 2 = Automatic, 3 = Manual, 4 = Disabled
                    return startType != 4;
                }
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Error checking Defender Service: {ex.Message}");
            }
            return false;
        }, cancellationToken).ConfigureAwait(false);
    }

    private void UpdateSecurityImage(params bool[] featureStates)
    {
        var disabledCount = featureStates.Count(status => !status);

        var imageUri = disabledCount switch
        {
            0 => "ms-appx:///Assets/secure.png",     // All features enabled - Green shield
            <= 2 => "ms-appx:///Assets/warning.png",  // 1-2 features disabled - Warning
            _ => "ms-appx:///Assets/unsecure.png"     // 3+ features disabled - Red shield
        };

        SecurityStatusImage.Source = new BitmapImage(new Uri(imageUri));
        
        // Hide loading ring and show the image
        SecurityStatusLoadingRing.IsActive = false;
        SecurityStatusLoadingRing.Visibility = Visibility.Collapsed;
        SecurityStatusImage.Visibility = Visibility.Visible;
        LastRefreshedText.Visibility = Visibility.Visible;
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await CheckSecurityStatusAsync(_cancellationTokenSource?.Token ?? default);
    }

    private void OpenWindowsSecurity_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "windowsdefender://",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error opening Windows Security: {ex.Message}");
        }
    }

    private async void RunQuickScan_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            QuickScanButton.IsEnabled = false;
            QuickScanProgressRing.Visibility = Visibility.Visible;
            QuickScanIcon.Visibility = Visibility.Collapsed;

            await Task.Run(() =>
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"Start-MpScan -ScanType QuickScan\"",
                        UseShellExecute = true,
                        CreateNoWindow = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };
                process.Start();
                process.WaitForExit();
            }).ConfigureAwait(true);

            App.ShowNotification("SecurityPage_QuickScanTitle".GetLocalized(), "SecurityPage_QuickScanCompleted".GetLocalized(), Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success, 5000);

            await Task.Delay(1000);
            await CheckSecurityStatusAsync(_cancellationTokenSource?.Token ?? default);
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error running quick scan: {ex.Message}");
            App.ShowNotification("SecurityPage_QuickScanTitle".GetLocalized(), "SecurityPage_QuickScanFailed".GetLocalized(), Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error, 5000);
        }
        finally
        {
            QuickScanButton.IsEnabled = true;
            QuickScanProgressRing.Visibility = Visibility.Collapsed;
            QuickScanIcon.Visibility = Visibility.Visible;
        }
    }

    private async void UpdateDefenderSignatures_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await Task.Run(() =>
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"Update-MpSignature\"",
                        UseShellExecute = true,
                        CreateNoWindow = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };
                process.Start();
                process.WaitForExit();
            }).ConfigureAwait(true);

            App.ShowNotification("SecurityPage_UpdateDefinitionsTitle".GetLocalized(), "SecurityPage_DefinitionsUpdated".GetLocalized(), Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success, 5000);

            await Task.Delay(2000);
            await CheckSecurityStatusAsync(_cancellationTokenSource?.Token ?? default);
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error updating signatures: {ex.Message}");
            App.ShowNotification("SecurityPage_UpdateDefinitionsTitle".GetLocalized(), "SecurityPage_DefinitionsUpdateFailed".GetLocalized(), Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error, 5000);
        }
    }

    // Hyperlink click handlers for each security feature
    private void VirusThreatProtectionLink_Click(object sender, RoutedEventArgs e)
    {
        OpenWindowsSecurityPage("windowsdefender://threatsettings/");
    }

    private void FirewallLink_Click(object sender, RoutedEventArgs e)
    {
        OpenWindowsSecurityPage("windowsdefender://network/");
    }

    private void WindowsUpdateLink_Click(object sender, RoutedEventArgs e)
    {
        OpenWindowsSecurityPage("ms-settings:windowsupdate");
    }

    private void SmartScreenLink_Click(object sender, RoutedEventArgs e)
    {
        // Open Reputation-based protection settings where SmartScreen toggles are
        OpenWindowsSecurityPage("windowsdefender://smartscreenpua/");
    }

    private void RealTimeProtectionLink_Click(object sender, RoutedEventArgs e)
    {
        OpenWindowsSecurityPage("windowsdefender://threatsettings/");
    }

    private void UACLink_Click(object sender, RoutedEventArgs e)
    {
        OpenWindowsSecurityPage("ms-settings:useraccounts");
    }

    private void TamperProtectionLink_Click(object sender, RoutedEventArgs e)
    {
        OpenWindowsSecurityPage("windowsdefender://threatsettings/");
    }

    private void ControlledFolderAccessLink_Click(object sender, RoutedEventArgs e)
    {
        OpenWindowsSecurityPage("windowsdefender://ransomwareprotection/");
    }

    private void BitLockerLink_Click(object sender, RoutedEventArgs e)
    {
        // Try device encryption settings first, then fall back to control panel
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:deviceencryption",
                UseShellExecute = true
            });
        }
        catch
        {
            // Fallback to control panel BitLocker
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "control.exe",
                    Arguments = "/name Microsoft.BitLockerDriveEncryption",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Error opening BitLocker settings: {ex.Message}");
            }
        }
    }

    private void DefenderServiceLink_Click(object sender, RoutedEventArgs e)
    {
        OpenWindowsSecurityPage("windowsdefender://threatsettings/");
    }

    private void OpenWindowsSecurityPage(string uri)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error opening Windows Security page '{uri}': {ex.Message}");
            // Fallback to main Windows Security
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "windowsdefender://",
                    UseShellExecute = true
                });
            }
            catch (Exception fallbackEx)
            {
                _ = LogHelper.LogError($"Error opening fallback Windows Security: {fallbackEx.Message}");
            }
        }
    }
}