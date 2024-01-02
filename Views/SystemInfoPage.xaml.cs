using System.Diagnostics;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using RyTuneX.ViewModels;

namespace RyTuneX.Views;

public sealed partial class SystemInfoPage : Page
{
    public SystemInfoViewModel ViewModel
    {
        get;
    }

    public SystemInfoPage()
    {
        ViewModel = App.GetService<SystemInfoViewModel>();
        InitializeComponent();
        UpdateSystemInfoAsync();
    }
    private async void UpdateSystemInfoAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                // This code will run on a background thread

                var osInformation = GetOsInformation();
                var cpuInformation = GetCpuInformation();
                var gpuInformation = GetGpuInformation();
                var ramInformation = GetRamInformation();
                var diskInformation = GetDiskInformation();

                // Update UI on the UI thread
                DispatcherQueue.TryEnqueue(() =>
                {
                    os.Text = osInformation;
                    cpu.Text = cpuInformation;
                    gpu.Text = gpuInformation;
                    ram.Text = ramInformation;
                    disk.Text = diskInformation;

                    // Show text blocks
                    os.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    cpu.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    gpu.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    ram.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    disk.Visibility = Microsoft.UI.Xaml.Visibility.Visible;

                    // Hide progress Bars
                    osProgressRing.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                    cpuProgressRing.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                    gpuProgressRing.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                    ramProgressRing.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                    diskProgressRing.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating system information: {ex}");
            }
        });
    }

    private static string GetCpuInformation()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            var collection = searcher.Get();
            searcher.Dispose();
            var cpuInfoLines = collection.Cast<ManagementObject>().Select(cpu =>
            $"Name: {cpu["Name"]}\n" +
            $"Manufacturer: {cpu["Manufacturer"]}\n" +
            $"Architecture: {cpu["Architecture"]}\n" +
            $"Cores: {cpu["NumberOfCores"]}\n" +
            $"Logical Processors: {cpu["NumberOfLogicalProcessors"]}\n" +
            $"Max Speed: {cpu["MaxClockSpeed"]} MHz\n" +
            $"Socket Designation: {cpu["SocketDesignation"]}\n" +
            $"L2 Cache Size: {cpu["L2CacheSize"]} KB\n" +
            $"L3 Cache Size: {cpu["L3CacheSize"]} KB");
            Debug.WriteLine("Getting CPU info");

            return string.Join(Environment.NewLine, cpuInfoLines);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting CPU info: {ex}");
            return string.Empty;
        }
    }

    private static string GetGpuInformation()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            var collection = searcher.Get();
            searcher.Dispose();
            var gpuNumber = 0;
            var gpuInfoLines = collection.Cast<ManagementObject>().Select(gpu =>
            {
                var gpuInfo = $"GPU {gpuNumber}:\n" +
                              $"  Name: {gpu["Caption"]}\n" +
                              $"  Adapter RAM: {gpu["AdapterRAM"]} bytes\n" +
                              $"  Driver Version: {gpu["DriverVersion"]}\n" +
                              $"  Video Architecture: {gpu["VideoArchitecture"]}";

                gpuNumber++;
                return gpuInfo;
            });
            Debug.WriteLine("Getting GPU info");
            return string.Join(Environment.NewLine, gpuInfoLines);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting GPU info: {ex}");
            return string.Empty;
        }
    }

    private static string GetRamInformation()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
            var collection = searcher.Get();
            searcher.Dispose();

            var ramInfoLines = collection.Cast<ManagementObject>().Select((ram, i) =>
                $"RAM Module {i + 1}:\n" +
                $"  Device Locator: {ram["DeviceLocator"]}\n" +
                $"  Capacity: {((ulong)ram["Capacity"]) / (1024 * 1024)} MB\n" +
                $"  Speed: {ram["Speed"]} MHz\n" +
                $"  Manufacturer: {ram["Manufacturer"]}");

            Debug.WriteLine("Getting RAM info");
            return string.Join(Environment.NewLine, ramInfoLines);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting RAM info: {ex}");
            return string.Empty;
        }
    }

    private static string GetDiskInformation()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
            var collection = searcher.Get();
            searcher.Dispose();
            var diskNumber = 0;
            var diskInfoLines = collection.Cast<ManagementObject>().Select(disk =>
            {
                var diskInfo = $"Disk {diskNumber}:\n" +
                               $"  Caption: {disk["Caption"]}\n" +
                               $"  Size: {disk["Size"]} bytes\n" +
                               $"  Interface Type: {disk["InterfaceType"]}\n" +
                               $"  Manufacturer: {disk["Manufacturer"]}\n" +
                               $"  Model: {disk["Model"]}";

                diskNumber++;
                return diskInfo;
            });
            Debug.WriteLine("Getting Disk info");
            return string.Join(Environment.NewLine, diskInfoLines);
        }

        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting Disk info: {ex}");
            return string.Empty; ;
        }
    }

    private static string GetOsInformation()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            var collection = searcher.Get();
            searcher.Dispose();

            var osInfoLines = collection.Cast<ManagementObject>().Select(os =>
                $"OS Name: {os["Caption"]}\n" +
                $"Version: {os["Version"]}\n" +
                $"Build Number: {os["BuildNumber"]}\n" +
                $"Architecture: {os["OSArchitecture"]}\n" +
                $"Install Date: {os["InstallDate"]}\n" +
                $"Registered User: {os["RegisteredUser"]}\n" +
                $"Product Key: {os["SerialNumber"]}\n" +
                $"Windows Directory: {os["WindowsDirectory"]}\n" +
                $"System Directory: {os["SystemDirectory"]}\n" +
                $"Last Boot Up Time: {os["LastBootUpTime"]}");

            Debug.WriteLine("Getting OS info");
            return string.Join(Environment.NewLine, osInfoLines);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting OS info: {ex}");
            return string.Empty;
        }
    }

    private void ReloadInfo(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            Debug.WriteLine("Reloading SystemInfo Page");
            Frame.Navigate(Frame.CurrentSourcePageType);
            Frame.BackStack.RemoveAt(Frame.BackStack.Count - 1);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reloading SystemInfo Page: {ex}");
        }
    }
}
