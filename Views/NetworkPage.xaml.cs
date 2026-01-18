using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using RyTuneX.Helpers;

namespace RyTuneX.Views;

public sealed partial class NetworkPage : Page
{
    public string[] DNSOptions
    {
        get;
    } =
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

    public NetworkPage()
    {
        InitializeComponent();
        LogHelper.Log("Initializing NetworkPage");
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        PopulateNetworkInterfaces();
        DisplayNetworkInfo();
        cmbDNSOptions.SelectedIndex = 0;
        Loaded += NetworkPage_Loaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string optionTag && !string.IsNullOrEmpty(optionTag))
        {
            _pendingScrollTarget = optionTag;
        }
    }

    private async void NetworkPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_pendingScrollTarget))
        {
            await ScrollToElementHelper.ScrollToElementAsync(this, _pendingScrollTarget);
            _pendingScrollTarget = null;
        }
    }

    private async void PopulateNetworkInterfaces()
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

                    if (ipv4Addresses.Count > 1)
                    {
                        txtIPv4DNSSecondary.Text = ipv4Addresses[1].ToString();
                    }
                    else
                    {
                        txtIPv4DNSSecondary.Text = "NotSet".GetLocalized();
                    }
                }
                else
                {
                    txtIPv4DNSPrimary.Text = "NoIPv4".GetLocalized();
                    txtIPv4DNSSecondary.Text = "NoIPv4".GetLocalized();
                }

                if (ipv6Addresses.Count > 0)
                {
                    txtIPv6DNSPrimary.Text = ipv6Addresses[0].ToString();

                    if (ipv6Addresses.Count > 1)
                    {
                        txtIPv6DNSSecondary.Text = ipv6Addresses[1].ToString();
                    }
                    else
                    {
                        txtIPv6DNSSecondary.Text = "NotSet".GetLocalized();
                    }
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
    private async void ApplyDNS_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            var selectedDNS = cmbDNSOptions.SelectedItem?.ToString();

            if (!string.IsNullOrEmpty(selectedDNS))
            {
                var (dnsv4, dnsv6) = GetDNSAddresses(selectedDNS);

                await SetDNS(selectedInterfaceName, dnsv4, dnsv6);
                DisplayNetworkInfo();
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error applying DNS settings: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
    }
    private (string[] dnsv4, string[] dnsv6) GetDNSAddresses(string selectedDNS)
    {
        return selectedDNS switch
        {
            "Google" => (new[] { "8.8.8.8", "8.8.4.4" }, new[] { "2001:4860:4860::8888", "2001:4860:4860::8844" }),
            "OpenDNS" => (new[] { "208.67.222.222", "208.67.220.220" }, new[] { "2620:0:ccc::2", "2620:0:ccd::2" }),
            "Cloudflare" => (new[] { "1.1.1.1", "1.0.0.1" }, new[] { "2606:4700:4700::1111", "2606:4700:4700::1001" }),
            "Quad9" => (new[] { "9.9.9.9", "149.112.112.112" }, new[] { "2620:fe::fe", string.Empty }),
            "CleanBrowsing" => (new[] { "185.228.168.168", "185.228.168.169" }, new[] { "2a0d:2a00:1::", "2a0d:2a00:2::" }),
            "CleanBrowsing (adult filter)" => (new[] { "185.228.168.10", "185.228.168.11" }, new[] { "2a0d:2a00:1::1", "2a0d:2a00:2::1" }),
            "AlternateDNS" => (new[] { "76.76.19.19", "76.223.122.150" }, new[] { "2602:fcbc::ad", "2602:fcbc:2::ad" }),
            "Adguard" => (new[] { "94.140.14.14", "94.140.15.15" }, new[] { "2a10:50c0::ad1:ff", "2a10:50c0::ad2:ff" }),
            "Comodo Secure DNS" => (new[] { "8.26.56.26", "8.20.247.20" }, new[] { "2a00:d8a0:1:200::c056", "2a00:d8a0:1:200::c060" }),
            "Verisign Public DNS" => (new[] { "64.6.64.6", "64.6.65.6" }, new[] { "2620:113::130", "2620:113::131" }),
            _ => (Array.Empty<string>(), Array.Empty<string>())
        };
    }
    private async Task SetDNS(string nic, string[] dnsv4, string[] dnsv6)
    {
        try
        {
            _ = LogHelper.Log($"Setting DNS for {nic}");

            var commands = new List<string>
            {
                $"netsh interface ipv4 set dnsservers {nic} static {dnsv4[0]} primary",
                dnsv4.Length == 2 ? $"netsh interface ipv4 add dnsservers {nic} {dnsv4[1]} index=2" : null,
                $"netsh interface ipv6 set dnsservers {nic} static {dnsv6[0]} primary",
                dnsv6.Length == 2 ? $"netsh interface ipv6 add dnsservers {nic} {dnsv6[1]} index=2" : null
            }.Where(cmd => !string.IsNullOrEmpty(cmd));

            foreach (var cmd in commands)
            {
                await OptimizationOptions.StartInCmd(cmd);
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error setting DNS for {nic}: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
    }
    private async void ResetDefaultDNS_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
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
    }
    private async Task ResetDefaultDNS(string nic)
    {
        try
        {
            _ = LogHelper.Log($"Resetting DNS to default for {nic}");

            var cmdv4 = $"netsh interface ipv4 set dnsservers \"{nic}\" dhcp";
            var cmdv6 = $"netsh interface ipv6 set dnsservers \"{nic}\" dhcp";

            await OptimizationOptions.StartInCmd(cmdv4);
            await OptimizationOptions.StartInCmd(cmdv6);
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
            DisplayNetworkInfo();
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error changing network interface selection: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
    }
}