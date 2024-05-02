using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.UI.Xaml.Controls;
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

    public NetworkPage()
    {
        InitializeComponent();
        PopulateNetworkInterfaces();
        DisplayNetworkInfo();
        cmbDNSOptions.SelectedIndex = 0;
    }

    private void PopulateNetworkInterfaces()
    {
        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces().Where(
            a => a.OperationalStatus == OperationalStatus.Up &&
            (a.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || a.NetworkInterfaceType == NetworkInterfaceType.Ethernet) &&
            a.GetIPProperties().GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork));

        var interfaceNames = networkInterfaces.Select(n => n.Name).ToList();

        cmbNetworkInterfaces.ItemsSource = interfaceNames;
        if (interfaceNames.Count > 0)
        {
            cmbNetworkInterfaces.SelectedIndex = 0;
            selectedInterfaceName = interfaceNames[0];
        }
    }

    private void DisplayNetworkInfo()
    {
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

    private async void ApplyDNS_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var selectedDNS = cmbDNSOptions.SelectedItem?.ToString();

        if (!string.IsNullOrEmpty(selectedDNS))
        {
            var dnsv4 = Array.Empty<string>();
            var dnsv6 = Array.Empty<string>();

            switch (selectedDNS)
            {
                case "Google":
                    dnsv4 = ["8.8.8.8", "8.8.4.4"];
                    dnsv6 = ["2001:4860:4860::8888", "2001:4860:4860::8844"];
                    break;
                case "OpenDNS":
                    dnsv4 = ["208.67.222.222", "208.67.220.220"];
                    dnsv6 = ["2620:0:ccc::2", "2620:0:ccd::2"];
                    break;
                case "Cloudflare":
                    dnsv4 = ["1.1.1.1", "1.0.0.1"];
                    dnsv6 = ["2606:4700:4700::1111", "2606:4700:4700::1001"];
                    break;
                case "Quad9":
                    dnsv4 = ["9.9.9.9", "149.112.112.112"];
                    dnsv6 = ["2620:fe::fe", string.Empty];
                    break;
                case "CleanBrowsing":
                    dnsv4 = ["185.228.168.168", "185.228.168.169"];
                    dnsv6 = ["2a0d:2a00:1::", "2a0d:2a00:2::"];
                    break;
                case "CleanBrowsing (adult filter)":
                    dnsv4 = ["185.228.168.10", "185.228.168.11"];
                    dnsv6 = ["2a0d:2a00:1::1", "2a0d:2a00:2::1"];
                    break;
                case "AlternateDNS":
                    dnsv4 = ["76.76.19.19", "76.223.122.150"];
                    dnsv6 = ["2602:fcbc::ad", "2602:fcbc:2::ad"];
                    break;
                case "Adguard":
                    dnsv4 = ["94.140.14.14", "94.140.15.15"];
                    dnsv6 = ["2a10:50c0::ad1:ff", "2a10:50c0::ad2:ff"];
                    break;
                case "Comodo Secure DNS":
                    dnsv4 = ["8.26.56.26", "8.20.247.20"];
                    dnsv6 = ["2a00:d8a0:1:200::c056", "2a00:d8a0:1:200::c060"];
                    break;
                case "Verisign Public DNS":
                    dnsv4 = ["64.6.64.6", "64.6.65.6"];
                    dnsv6 = ["2620:113::130", "2620:113::131"];
                    break;
                default:
                    break;
            }

            await SetDNS(selectedInterfaceName, dnsv4, dnsv6);

            DisplayNetworkInfo();
        }
    }

    private async Task SetDNS(string nic, string[] dnsv4, string[] dnsv6)
    {
        await Task.Run(async () =>
        {
            var cmdv4Alternate = string.Empty;
            var cmdv6Alternate = string.Empty;

            var cmdv4Primary = $"netsh interface ipv4 set dnsservers {nic} static {dnsv4[0]} primary";
            if (dnsv4.Length == 2)
            {
                cmdv4Alternate = $"netsh interface ipv4 add dnsservers {nic} {dnsv4[1]} index=2";
            }

            var cmdv6Primary = $"netsh interface ipv6 set dnsservers {nic} static {dnsv6[0]} primary";
            if (dnsv6.Length == 2)
            {
                cmdv6Alternate = $"netsh interface ipv6 add dnsservers {nic} {dnsv6[1]} index=2";
            }

            await OptimizationOptions.StartInCmd(cmdv4Primary);
            if (!string.IsNullOrEmpty(cmdv4Alternate))
            {
                await OptimizationOptions.StartInCmd(cmdv4Alternate);
            }

            await OptimizationOptions.StartInCmd(cmdv6Primary);
            if (!string.IsNullOrEmpty(cmdv6Alternate))
            {
                await OptimizationOptions.StartInCmd(cmdv6Alternate);
            }
        });
    }

    private async void ResetDefaultDNS_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        cmbDNSOptions.SelectedIndex = 0;
        await ResetDefaultDNS(selectedInterfaceName);
        DisplayNetworkInfo();
    }

    private async Task ResetDefaultDNS(string nic)
    {
        var cmdv4 = $"netsh interface ipv4 set dnsservers \"{nic}\" dhcp";
        var cmdv6 = $"netsh interface ipv6 set dnsservers \"{nic}\" dhcp";

        await OptimizationOptions.StartInCmd(cmdv4);
        await OptimizationOptions.StartInCmd(cmdv6);
    }

    private void cmbNetworkInterfaces_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        selectedInterfaceName = cmbNetworkInterfaces.SelectedItem?.ToString() ?? "";
        DisplayNetworkInfo();
    }
}