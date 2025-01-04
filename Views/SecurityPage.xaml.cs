using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Management;
using Microsoft.Win32;
using NetFwTypeLib;
using RyTuneX.Helpers;

namespace RyTuneX.Views;

public sealed partial class SecurityPage : Page
{
    public SecurityPage()
    {
        InitializeComponent();
        CheckSecurityStatus();
    }

    private void CheckSecurityStatus()
    {
        var virusThreatProtection = IsAntivirusEnabled();
        var firewallProtection = IsFirewallEnabled();
        var windowsUpdate = IsWindowsUpdateEnabled();
        var smartscreen = IsSmartScreenEnabled();
        var realTimeProtection = IsRealTimeProtectionEnabled();
        var uac = IsUACEnabled();

        VirusThreatProtectionStatus.Text = virusThreatProtection ? "Enabled".GetLocalized() : "Disabled".GetLocalized();
        FirewallStatus.Text = firewallProtection ? "Enabled".GetLocalized() : "Disabled".GetLocalized();
        WindowsUpdateStatus.Text = windowsUpdate ? "Enabled".GetLocalized() : "Disabled".GetLocalized();
        SmartScreenStatus.Text = smartscreen ? "Enabled".GetLocalized() : "Disabled".GetLocalized();
        RealTimeProtectionStatus.Text = realTimeProtection ? "Enabled".GetLocalized() : "Disabled".GetLocalized();
        UACStatus.Text = uac ? "Enabled".GetLocalized() : "Disabled".GetLocalized();
        
        UpdateSecurityImage(virusThreatProtection, firewallProtection, windowsUpdate, smartscreen, uac, realTimeProtection);
    }

    [Flags]
    enum ProductState
    {
        Off = 0x0000,
        On = 0x1000,
        Snoozed = 0x2000,
        Expired = 0x3000
    }

    private bool IsAntivirusEnabled()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT * FROM AntiVirusProduct");
            foreach (var obj in searcher.Get())
            {
                if (obj["productState"] != null && int.TryParse(obj["productState"].ToString(), out var state))
                {
                    var productState = (ProductState)(state & 0xF000);
                    return productState == ProductState.On;
                }
            }
        }
        catch { }
        return false;
    }

    private bool IsFirewallEnabled()
    {
        try
        {
            var type = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            if (type != null && Activator.CreateInstance(type) is INetFwPolicy2 firewallPolicy)
            {
                return firewallPolicy.FirewallEnabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC];
            }
        }
        catch { }
        return false;
    }

    private bool IsWindowsUpdateEnabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate");
            return key == null || key.GetValue("DisableWindowsUpdateAccess") == null;
        }
        catch { }
        return false;
    }

    private bool IsSmartScreenEnabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer");
            var value = key?.GetValue("SmartScreenEnabled") as string;
            return value != "Off";
        }
        catch { }
        return false;
    }

    private bool IsRealTimeProtectionEnabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Real-Time Protection");
            var value = key?.GetValue("DisableRealtimeMonitoring");
            return value == null || (int)value == 0;
        }
        catch { }
        return false;
    }
    
    private bool IsUACEnabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
            var value = key?.GetValue("EnableLUA");
            return value is int enabled && enabled == 1;
        }
        catch { }
        return false;
    }

    private void UpdateSecurityImage(params bool[] featureStates)
    {
        var disabledCount = featureStates.Count(status => !status);

        var imageUri = disabledCount switch
        {
            0 => "ms-appx:///Assets/secure.png",
            <= 2 => "ms-appx:///Assets/warning.png",
            _ => "ms-appx:///Assets/unsecure.png"
        };

        SecurityStatusImage.Source = new BitmapImage(new Uri(imageUri));
    }
}