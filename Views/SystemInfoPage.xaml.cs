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

                var cpuInformation = GetCpuInformation();
                var gpuInformation = GetGpuInformation();
                var ramInformation = GetRamInformation();
                var diskInformation = GetDiskInformation();

                // Update UI on the UI thread
                DispatcherQueue.TryEnqueue(() =>
                {
                    cpu.Text = cpuInformation;
                    gpu.Text = gpuInformation;
                    ram.Text = ramInformation;
                    disk.Text = diskInformation;

                    // Show text blocks
                    cpu.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    gpu.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    ram.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    disk.Visibility = Microsoft.UI.Xaml.Visibility.Visible;

                    // Hide progress Bars
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
            var cpuInfoLines = collection.Cast<ManagementObject>()
                .Select(cpu => $"Name: {cpu["Name"]}\nCores: {cpu["NumberOfCores"]}\nMax Speed: {cpu["MaxClockSpeed"]} MHz");
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
            var gpuInfoLines = collection.Cast<ManagementObject>()
                .Select((gpu) => $"Name: {gpu["Caption"]}\nVideo RAM: {gpu["AdapterRAM"]} bytes");
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
            var totalRam = collection.Cast<ManagementObject>()
                .Sum(ram => Convert.ToInt64(ram["Capacity"]));
            Debug.WriteLine("Getting RAM info");
            return $"Total RAM: {totalRam} bytes";
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
            var diskInfoLines = collection.Cast<ManagementObject>()
                .Select((disk, i) => $"Disk {i}: {disk["Caption"]}\nSize: {disk["Size"]} bytes");
            Debug.WriteLine("Getting Disk info");
            return string.Join(Environment.NewLine, diskInfoLines);
        }

        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting Disk info: {ex}");
            return string.Empty; ;
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
