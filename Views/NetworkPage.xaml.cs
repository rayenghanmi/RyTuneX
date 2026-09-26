using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Win32;
using RyTuneX.Helpers;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace RyTuneX.Views;

public sealed partial class NetworkPage : Page
{
    public string[] DNSOptions { get; } =
    [
        "Automatic".GetLocalized(),
        "Cloudflare",
        "OpenDNS",
        "Quad9",
        "Google",
        "AlternateDNS",
        "Adguard",
        "CleanBrowsing",
        "CleanBrowsing " + "AdultFilter".GetLocalized(),
        "Comodo Secure DNS",
        "Verisign Public DNS"
    ];

    private string selectedInterfaceName = string.Empty;
    private string? _pendingScrollTarget;
    private bool _isInitializing = false;

    public NetworkPage()
    {
        InitializeComponent();
        LogHelper.Log("Initializing NetworkPage");
        this.NavigationCacheMode = NavigationCacheMode.Required;
        Loaded += NetworkPage_Loaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string optionTag && !string.IsNullOrEmpty(optionTag))
            _pendingScrollTarget = optionTag;
    }

    private async void NetworkPage_Loaded(object sender, RoutedEventArgs e)
    {
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await Task.Yield();
        try
        {
            _isInitializing = true;
            await PopulateNetworkInterfacesAsync();
            DisplayNetworkInfo();
            cmbDNSOptions.SelectedIndex = 0;
            await InitializeNetworkOptimizationTogglesAsync();
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"NetworkPage init error: {ex.Message}");
        }
        finally
        {
            _isInitializing = false;
        }

        if (!string.IsNullOrEmpty(_pendingScrollTarget))
        {
            await ScrollToElementHelper.ScrollToElementAsync(this, _pendingScrollTarget);
            _pendingScrollTarget = null;
        }
    }

    private async Task PopulateNetworkInterfacesAsync()
    {
        try
        {
            _ = LogHelper.Log("Populating Network Interfaces");

            var networkInterfaces = await Task.Run(() =>
                NetworkInterface.GetAllNetworkInterfaces().Where(
                    a => a.OperationalStatus == OperationalStatus.Up &&
                    (a.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || a.NetworkInterfaceType == NetworkInterfaceType.Ethernet) &&
                    a.GetIPProperties().GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork))
                .Select(n => n.Name).ToList()
            );

            cmbNetworkInterfaces.ItemsSource = networkInterfaces;
            if (networkInterfaces.Count > 0)
            {
                cmbNetworkInterfaces.SelectedIndex = 0;
                selectedInterfaceName = networkInterfaces[0];
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error populating network interfaces: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
    }

    private async void DisplayNetworkInfo()
    {
        try
        {
            _ = LogHelper.Log("Displaying Network Info");

            var selectedInterface = selectedInterfaceName;
            var nic = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(n => n.Name == selectedInterface);

            if (nic != null)
            {
                var dnsAddresses = nic.GetIPProperties().DnsAddresses;
                var ipv4Addresses = dnsAddresses.Where(d => d.AddressFamily == AddressFamily.InterNetwork).ToList();
                var ipv6Addresses = dnsAddresses.Where(d => d.AddressFamily == AddressFamily.InterNetworkV6).ToList();

                if (ipv4Addresses.Count > 0)
                {
                    txtIPv4DNSPrimary.Text = ipv4Addresses[0].ToString();
                    txtIPv4DNSSecondary.Text = ipv4Addresses.Count > 1 ? ipv4Addresses[1].ToString() : "NotSet".GetLocalized();
                }
                else
                {
                    txtIPv4DNSPrimary.Text = "NoIPv4".GetLocalized();
                    txtIPv4DNSSecondary.Text = "NoIPv4".GetLocalized();
                }

                if (ipv6Addresses.Count > 0)
                {
                    txtIPv6DNSPrimary.Text = ipv6Addresses[0].ToString();
                    txtIPv6DNSSecondary.Text = ipv6Addresses.Count > 1 ? ipv6Addresses[1].ToString() : "NotSet".GetLocalized();
                }
                else
                {
                    txtIPv6DNSPrimary.Text = "NoIPv6".GetLocalized();
                    txtIPv6DNSSecondary.Text = "NoIPv6".GetLocalized();
                }
            }
            else
            {
                txtIPv4DNSPrimary.Text = "NoNic".GetLocalized();
                txtIPv4DNSSecondary.Text = "NoNic".GetLocalized();
                txtIPv6DNSPrimary.Text = "NoNic".GetLocalized();
                txtIPv6DNSSecondary.Text = "NoNic".GetLocalized();
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error displaying network info: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
    }

    private async void ApplyDNS_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(selectedInterfaceName))
        {
            App.ShowNotification("NoInterfaceTitle".GetLocalized(), "NoInterfaceMessage".GetLocalized(), Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning, 4000);
            return;
        }

        var applyButton = sender as Button;
        if (applyButton != null) applyButton.IsEnabled = false;

        try
        {
            var selectedIndex = cmbDNSOptions.SelectedIndex;
            _ = LogHelper.Log($"Applying DNS index {selectedIndex} on interface: {selectedInterfaceName}");
            var (dnsv4, dnsv6) = GetDNSAddressesByIndex(selectedIndex);
            await SetDNS(selectedInterfaceName, dnsv4, dnsv6);
            DisplayNetworkInfo();
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error applying DNS settings: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
        finally
        {
            if (applyButton != null) applyButton.IsEnabled = true;
        }
    }

    private static (string[] dnsv4, string[] dnsv6) GetDNSAddressesByIndex(int index)
    {
        return index switch
        {
            1 /* Cloudflare */          => (new[] { "1.1.1.1", "1.0.0.1" }, new[] { "2606:4700:4700::1111", "2606:4700:4700::1001" }),
            2 /* OpenDNS */             => (new[] { "208.67.222.222", "208.67.220.220" }, new[] { "2620:0:ccc::2", "2620:0:ccd::2" }),
            3 /* Quad9 */               => (new[] { "9.9.9.9", "149.112.112.112" }, new[] { "2620:fe::fe", string.Empty }),
            4 /* Google */              => (new[] { "8.8.8.8", "8.8.4.4" }, new[] { "2001:4860:4860::8888", "2001:4860:4860::8844" }),
            5 /* AlternateDNS */        => (new[] { "76.76.19.19", "76.223.122.150" }, new[] { "2602:fcbc::ad", "2602:fcbc:2::ad" }),
            6 /* Adguard */             => (new[] { "94.140.14.14", "94.140.15.15" }, new[] { "2a10:50c0::ad1:ff", "2a10:50c0::ad2:ff" }),
            7 /* CleanBrowsing */       => (new[] { "185.228.168.168", "185.228.168.169" }, new[] { "2a0d:2a00:1::", "2a0d:2a00:2::" }),
            8 /* CleanBrowsing adult */ => (new[] { "185.228.168.10", "185.228.168.11" }, new[] { "2a0d:2a00:1::1", "2a0d:2a00:2::1" }),
            9 /* Comodo */              => (new[] { "8.26.56.26", "8.20.247.20" }, new[] { "2a00:d8a0:1:200::c056", "2a00:d8a0:1:200::c060" }),
            10 /* Verisign */           => (new[] { "64.6.64.6", "64.6.65.6" }, new[] { "2620:113::130", "2620:113::131" }),
            _ /* 0=Automatic or unknown */ => (Array.Empty<string>(), Array.Empty<string>())
        };
    }

    private async Task SetDNS(string nic, string[] dnsv4, string[] dnsv6)
    {
        try
        {
            _ = LogHelper.Log($"Setting DNS for {nic}");
            var commands = new List<string>();

            if (dnsv4.Length > 0 && !string.IsNullOrEmpty(dnsv4[0]))
                commands.Add($"netsh interface ipv4 set dnsservers \"{nic}\" static {dnsv4[0]} primary");
            if (dnsv4.Length > 1 && !string.IsNullOrEmpty(dnsv4[1]))
                commands.Add($"netsh interface ipv4 add dnsservers \"{nic}\" {dnsv4[1]} index=2");
            if (dnsv6.Length > 0 && !string.IsNullOrEmpty(dnsv6[0]))
                commands.Add($"netsh interface ipv6 set dnsservers \"{nic}\" static {dnsv6[0]} primary");
            if (dnsv6.Length > 1 && !string.IsNullOrEmpty(dnsv6[1]))
                commands.Add($"netsh interface ipv6 add dnsservers \"{nic}\" {dnsv6[1]} index=2");

            foreach (var cmd in commands)
                await OptimizationOptions.StartInCmd(cmd);
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error setting DNS for {nic}: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
    }

    private async void ResetDefaultDNS_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(selectedInterfaceName))
        {
            App.ShowNotification("NoInterfaceTitle".GetLocalized(), "NoInterfaceMessage".GetLocalized(), Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning, 4000);
            return;
        }

        var resetButton = sender as Button;
        if (resetButton != null) resetButton.IsEnabled = false;

        try
        {
            cmbDNSOptions.SelectedIndex = 0;
            await ResetDefaultDNS(selectedInterfaceName);
            DisplayNetworkInfo();
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error resetting DNS to default: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
        finally
        {
            if (resetButton != null) resetButton.IsEnabled = true;
        }
    }

    private async Task ResetDefaultDNS(string nic)
    {
        try
        {
            _ = LogHelper.Log($"Resetting DNS to default for {nic}");
            await OptimizationOptions.StartInCmd($"netsh interface ipv4 set dnsservers \"{nic}\" dhcp");
            await OptimizationOptions.StartInCmd($"netsh interface ipv6 set dnsservers \"{nic}\" dhcp");
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error resetting DNS to default for {nic}: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
    }

    private async void cmbNetworkInterfaces_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            selectedInterfaceName = cmbNetworkInterfaces.SelectedItem?.ToString() ?? "";
            _ = LogHelper.Log($"Network interface changed to: {selectedInterfaceName}");
            DisplayNetworkInfo();
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error changing network interface selection: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
    }

    private async void FlushDNS_Click(object sender, RoutedEventArgs e)
    {
        FlushDNSButton.IsEnabled = false;
        FlushDNSProgressRing.Visibility = Visibility.Visible;
        try
        {
            _ = LogHelper.Log("Flushing DNS cache");
            await Task.Run(async () => await OptimizationOptions.StartInCmd("ipconfig /flushdns"));
            _ = LogHelper.Log("DNS cache flushed successfully");
            App.ShowNotification("Network_SuccessTitle".GetLocalized(), "Network_FlushDNSSuccess".GetLocalized(), Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success, 4000);
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error flushing DNS: {ex.Message}");
            App.ShowNotification("Network_SuccessTitle".GetLocalized(), ex.Message, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error, 5000);
        }
        finally
        {
            FlushDNSProgressRing.Visibility = Visibility.Collapsed;
            FlushDNSButton.IsEnabled = true;
        }
    }

    private async void ReleaseRenewIP_Click(object sender, RoutedEventArgs e)
    {
        ReleaseRenewButton.IsEnabled = false;
        ReleaseRenewProgressRing.Visibility = Visibility.Visible;
        try
        {
            _ = LogHelper.Log("Releasing and renewing IP address");
            await Task.Run(async () =>
            {
                await OptimizationOptions.StartInCmd("ipconfig /release");
                await OptimizationOptions.StartInCmd("ipconfig /renew");
            });
            _ = LogHelper.Log("IP released and renewed successfully");
            App.ShowNotification("Network_SuccessTitle".GetLocalized(), "Network_ReleaseRenewSuccess".GetLocalized(), Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success, 4000);
            DisplayNetworkInfo();
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error releasing/renewing IP: {ex.Message}");
            App.ShowNotification("Network_SuccessTitle".GetLocalized(), ex.Message, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error, 5000);
        }
        finally
        {
            ReleaseRenewProgressRing.Visibility = Visibility.Collapsed;
            ReleaseRenewButton.IsEnabled = true;
        }
    }

    private async void ResetTCPIP_Click(object sender, RoutedEventArgs e)
    {
        ResetTCPIPButton.IsEnabled = false;
        ResetTCPIPProgressRing.Visibility = Visibility.Visible;
        try
        {
            _ = LogHelper.Log("Resetting TCP/IP stack and Winsock");
            await Task.Run(async () =>
            {
                await OptimizationOptions.StartInCmd("netsh int ip reset");
                await OptimizationOptions.StartInCmd("netsh winsock reset");
            });
            _ = LogHelper.Log("TCP/IP and Winsock reset completed");
            App.ShowNotification("Network_ResetTCPIPWarningTitle".GetLocalized(), "Network_ResetTCPIPWarningMessage".GetLocalized(), Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning, 6000);
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error resetting TCP/IP: {ex.Message}");
            App.ShowNotification("Network_ResetTCPIPWarningTitle".GetLocalized(), ex.Message, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error, 5000);
        }
        finally
        {
            ResetTCPIPProgressRing.Visibility = Visibility.Collapsed;
            ResetTCPIPButton.IsEnabled = true;
        }
    }

    private async void OpenNetworkSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:network"));
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error opening network settings: {ex.Message}");
        }
    }

    private void OpenAdapterSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("ncpa.cpl") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error opening adapter settings: {ex.Message}");
        }
    }

    private async Task InitializeNetworkOptimizationTogglesAsync()
    {
        try
        {
            _ = LogHelper.Log("Initializing network optimization toggles");

            // Network Throttling
            bool throttlingDisabled = await Task.Run(() =>
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(
                        @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile");
                    if (key == null) return false;
                    var val = key.GetValue("NetworkThrottlingIndex");
                    if (val is int intVal) return (uint)intVal == 0xFFFFFFFF;
                    if (val is long longVal) return (ulong)longVal == 0xFFFFFFFF;
                    return false;
                }
                catch { return false; }
            });
            NetworkThrottlingToggle.IsOn = throttlingDisabled;

            // Nagle: check TcpAckFrequency on any interface subkey
            bool nagleDisabled = await Task.Run(() =>
            {
                try
                {
                    using var interfacesKey = Registry.LocalMachine.OpenSubKey(
                        @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces");
                    if (interfacesKey == null) return false;
                    foreach (var subKeyName in interfacesKey.GetSubKeyNames())
                    {
                        using var subKey = interfacesKey.OpenSubKey(subKeyName);
                        if (subKey == null) continue;
                        var val = subKey.GetValue("TcpAckFrequency");
                        if (val is int v && v == 1) return true;
                    }
                    return false;
                }
                catch { return false; }
            });
            NagleToggle.IsOn = nagleDisabled;

            // RSS, ECN, CTCP: parse netsh output
            var netshOutput = await Task.Run(async () =>
                await OptimizationOptions.RunPowerShell("netsh int tcp show global"));

            RSSToggle.IsOn = ParseNetshBoolValue(netshOutput, "Receive-Side Scaling State", "enabled");
            ECNToggle.IsOn = ParseNetshBoolValue(netshOutput, "ECN Capability", "enabled");
            CongestionToggle.IsOn = ParseNetshStringValue(netshOutput, "Congestion Control Provider", "ctcp");

            // IPv6: check DisabledComponents registry
            bool ipv6Disabled = await Task.Run(() =>
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(
                        @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters");
                    if (key == null) return false;
                    var val = key.GetValue("DisabledComponents");
                    if (val is int intVal) return (uint)intVal >= 0xFF;
                    return false;
                }
                catch { return false; }
            });
            IPv6Toggle.IsOn = ipv6Disabled;

            // P2P Delivery Optimization: check DODownloadMode
            bool p2pDisabled = await Task.Run(() =>
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(
                        @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization");
                    if (key == null) return false;
                    var val = key.GetValue("DODownloadMode");
                    return val is int v && v == 0;
                }
                catch { return false; }
            });
            P2PDeliveryToggle.IsOn = p2pDisabled;

            // Hotspot 2.0
            bool hotspotDisabled = await Task.Run(() =>
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(
                        @"SOFTWARE\Microsoft\WlanSvc\AnqpCache");
                    if (key == null) return false;
                    var val = key.GetValue("OsuRegistrationStatus");
                    return val is int v && v == 0;
                }
                catch { return false; }
            });
            Hotspot20Toggle.IsOn = hotspotDisabled;

            // NetBIOS over TCP/IP
            bool netbiosDisabled = await Task.Run(() =>
            {
                try
                {
                    using var interfacesKey = Registry.LocalMachine.OpenSubKey(
                        @"SYSTEM\CurrentControlSet\Services\NetBT\Parameters\Interfaces");
                    if (interfacesKey == null) return false;
                    foreach (var subKeyName in interfacesKey.GetSubKeyNames())
                    {
                        using var subKey = interfacesKey.OpenSubKey(subKeyName);
                        if (subKey == null) continue;
                        var val = subKey.GetValue("NetbiosOptions");
                        if (val is int v && v == 2) return true;
                    }
                    return false;
                }
                catch { return false; }
            });
            NetBIOSToggle.IsOn = netbiosDisabled;
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error initializing optimization toggles: {ex.Message}");
        }
    }

    private static bool ParseNetshBoolValue(string output, string label, string expectedValue)
    {
        foreach (var line in output.Split('\n'))
        {
            if (line.Contains(label, StringComparison.OrdinalIgnoreCase))
            {
                var parts = line.Split(':');
                if (parts.Length >= 2)
                    return parts[1].Trim().StartsWith(expectedValue, StringComparison.OrdinalIgnoreCase);
            }
        }
        return false;
    }

    private static bool ParseNetshStringValue(string output, string label, string expectedValue)
        => ParseNetshBoolValue(output, label, expectedValue);

    private async void NetworkThrottlingToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        var toggle = (ToggleSwitch)sender;
        toggle.IsEnabled = false;
        try
        {
            bool disable = toggle.IsOn;
            _ = LogHelper.Log($"Network throttling: {(disable ? "disabling" : "enabling")}");
            await Task.Run(() =>
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", writable: true);
                if (key == null) return;
                // 0xFFFFFFFF = disabled, 10 = default
                key.SetValue("NetworkThrottlingIndex",
                    disable ? unchecked((int)0xFFFFFFFF) : 10,
                    RegistryValueKind.DWord);
            });
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error toggling network throttling: {ex.Message}");
        }
        finally
        {
            toggle.IsEnabled = true;
        }
    }

    private async void NagleToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        var toggle = (ToggleSwitch)sender;
        toggle.IsEnabled = false;
        try
        {
            bool disable = toggle.IsOn;
            _ = LogHelper.Log($"Nagle algorithm: {(disable ? "disabling" : "enabling")}");
            await Task.Run(() =>
            {
                using var interfacesKey = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces", writable: true);
                if (interfacesKey == null) return;
                foreach (var subKeyName in interfacesKey.GetSubKeyNames())
                {
                    using var subKey = interfacesKey.OpenSubKey(subKeyName, writable: true);
                    if (subKey == null) continue;
                    if (disable)
                    {
                        // TcpAckFrequency=1 and TCPNoDelay=1 disables Nagle
                        subKey.SetValue("TcpAckFrequency", 1, RegistryValueKind.DWord);
                        subKey.SetValue("TCPNoDelay", 1, RegistryValueKind.DWord);
                    }
                    else
                    {
                        // Remove values to restore default
                        try { subKey.DeleteValue("TcpAckFrequency", throwOnMissingValue: false); } catch { }
                        try { subKey.DeleteValue("TCPNoDelay", throwOnMissingValue: false); } catch { }
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error toggling Nagle algorithm: {ex.Message}");
        }
        finally
        {
            toggle.IsEnabled = true;
        }
    }

    private async void RSSToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        var toggle = (ToggleSwitch)sender;
        toggle.IsEnabled = false;
        try
        {
            bool enable = toggle.IsOn;
            _ = LogHelper.Log($"RSS: {(enable ? "enabling" : "disabling")}");
            await Task.Run(async () =>
                await OptimizationOptions.StartInCmd(
                    $"netsh int tcp set global rss={(enable ? "enabled" : "disabled")}"));
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error toggling RSS: {ex.Message}");
        }
        finally
        {
            toggle.IsEnabled = true;
        }
    }

    private async void ECNToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        var toggle = (ToggleSwitch)sender;
        toggle.IsEnabled = false;
        try
        {
            bool enable = toggle.IsOn;
            _ = LogHelper.Log($"ECN: {(enable ? "enabling" : "disabling")}");
            await Task.Run(async () =>
                await OptimizationOptions.StartInCmd(
                    $"netsh int tcp set global ecncapability={(enable ? "enabled" : "disabled")}"));
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error toggling ECN: {ex.Message}");
        }
        finally
        {
            toggle.IsEnabled = true;
        }
    }

    private async void CongestionToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        var toggle = (ToggleSwitch)sender;
        toggle.IsEnabled = false;
        try
        {
            bool enable = toggle.IsOn;
            _ = LogHelper.Log($"CTCP: {(enable ? "enabling" : "disabling")}");
            await Task.Run(async () =>
                await OptimizationOptions.StartInCmd(
                    $"netsh int tcp set global congestionprovider={(enable ? "ctcp" : "default")}"));
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error toggling CTCP: {ex.Message}");
        }
        finally
        {
            toggle.IsEnabled = true;
        }
    }

    private async void IPv6Toggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        var toggle = (ToggleSwitch)sender;
        toggle.IsEnabled = false;
        try
        {
            bool disable = toggle.IsOn;
            _ = LogHelper.Log($"IPv6: {(disable ? "disabling" : "enabling")}");
            await Task.Run(() =>
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters", writable: true)
                    ?? Registry.LocalMachine.CreateSubKey(
                        @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters");
                // 0xFF = all IPv6 interfaces and loopback disabled, 0 = enabled
                key?.SetValue("DisabledComponents", disable ? 0xFF : 0, RegistryValueKind.DWord);
            });

            if (disable)
            {
                App.ShowNotification("Network_ResetTCPIPWarningTitle".GetLocalized(), "Network_DisableIPv6.Description".GetLocalized(), Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning, 6000);
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error toggling IPv6: {ex.Message}");
        }
        finally
        {
            toggle.IsEnabled = true;
        }
    }

    private async void P2PDeliveryToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        var toggle = (ToggleSwitch)sender;
        toggle.IsEnabled = false;
        try
        {
            bool disable = toggle.IsOn;
            _ = LogHelper.Log($"P2P Delivery: {(disable ? "disabling" : "enabling")}");
            await Task.Run(async () =>
            {
                if (disable)
                    await OptimizationOptions.StartInCmd(
                        "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DeliveryOptimization\" /v DODownloadMode /t REG_DWORD /d 0 /f");
                else
                    await OptimizationOptions.StartInCmd(
                        "reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DeliveryOptimization\" /v DODownloadMode /f");
            });
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error toggling P2P Delivery: {ex.Message}");
        }
        finally
        {
            toggle.IsEnabled = true;
        }
    }

    private async void Hotspot20Toggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        var toggle = (ToggleSwitch)sender;
        toggle.IsEnabled = false;
        try
        {
            bool disable = toggle.IsOn;
            _ = LogHelper.Log($"Hotspot 2.0: {(disable ? "disabling" : "enabling")}");
            await Task.Run(async () =>
            {
                if (disable)
                {
                    await OptimizationOptions.StartInCmd(
                        "reg add \"HKLM\\SOFTWARE\\Microsoft\\WlanSvc\\AnqpCache\" /v OsuRegistrationStatus /t REG_DWORD /d 0 /f");
                    await OptimizationOptions.StartInCmd(
                        "reg add \"HKLM\\SOFTWARE\\Microsoft\\WcmSvc\\wifinetworkmanager\\config\" /v AutoConnectAllowedOEM /t REG_DWORD /d 0 /f");
                }
                else
                {
                    await OptimizationOptions.StartInCmd(
                        "reg delete \"HKLM\\SOFTWARE\\Microsoft\\WlanSvc\\AnqpCache\" /v OsuRegistrationStatus /f");
                    await OptimizationOptions.StartInCmd(
                        "reg delete \"HKLM\\SOFTWARE\\Microsoft\\WcmSvc\\wifinetworkmanager\\config\" /v AutoConnectAllowedOEM /f");
                }
            });
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error toggling Hotspot 2.0: {ex.Message}");
        }
        finally
        {
            toggle.IsEnabled = true;
        }
    }

    private async void NetBIOSToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        var toggle = (ToggleSwitch)sender;
        toggle.IsEnabled = false;
        try
        {
            bool disable = toggle.IsOn;
            _ = LogHelper.Log($"NetBIOS: {(disable ? "disabling" : "enabling")}");
            await Task.Run(() =>
            {
                using var interfacesKey = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Services\NetBT\Parameters\Interfaces", writable: true);
                if (interfacesKey == null) return;
                foreach (var subKeyName in interfacesKey.GetSubKeyNames())
                {
                    using var subKey = interfacesKey.OpenSubKey(subKeyName, writable: true);
                    if (subKey == null) continue;
                    // 0 = default (use DHCP), 1 = enabled, 2 = disabled
                    subKey.SetValue("NetbiosOptions", disable ? 2 : 0, RegistryValueKind.DWord);
                }
            });
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error toggling NetBIOS: {ex.Message}");
        }
        finally
        {
            toggle.IsEnabled = true;
        }
    }

}
