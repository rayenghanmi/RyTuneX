using System.Management;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RyTuneX.Helpers;


namespace RyTuneX.Views;

public sealed partial class SystemInfoPage : Page
{
    public SystemInfoPage()
    {
        InitializeComponent();
        LogHelper.Log("Initializing SystemInfoPage");
        UpdateSystemInfoAsync();
    }
    private async void UpdateSystemInfoAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                // This code will run on a background thread
                LogHelper.Log("Updating SystemInfo");
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
                LogHelper.LogError($"Error updating system information: {ex}");
            }
        });
    }

    private static string GetCpuInformation()
    {
        try
        {
            LogHelper.Log("Getting CPU Info");
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            var collection = searcher.Get();
            searcher.Dispose();
            var cpuInfoLines = collection.Cast<ManagementObject>().Select(cpu =>
            "Name".GetLocalized() + $": {cpu["Name"]}\n" +
            "Manufacturer".GetLocalized() + $": {cpu["Manufacturer"]}\n" +
            "Architecture".GetLocalized() + $": {cpu["Architecture"]}\n" +
            "Cores".GetLocalized() + $": {cpu["NumberOfCores"]}\n" +
            "LogicalProcessors".GetLocalized() + $": {cpu["NumberOfLogicalProcessors"]}\n" +
            "MaxSpeed".GetLocalized() + $": {cpu["MaxClockSpeed"]} MHz\n" +
            "SocketDesignation".GetLocalized() + $": {cpu["SocketDesignation"]}\n" +
            "L2Cache".GetLocalized() + $": {cpu["L2CacheSize"]} KB\n" +
            "L3Cache".GetLocalized() + $": {cpu["L3CacheSize"]} KB");

            return string.Join(Environment.NewLine, cpuInfoLines);
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"Error getting CPU info: {ex}");
            return string.Empty;
        }
    }

    private static string GetGpuInformation()
    {
        try
        {
            LogHelper.Log("Getting GPU Info");
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            var collection = searcher.Get();
            searcher.Dispose();
            var gpuNumber = 0;
            var gpuInfoLines = collection.Cast<ManagementObject>().Select(gpu =>
            {
                var gpuInfo = "GPU".GetLocalized() + $" {gpuNumber}:\n" +
                              "   " + "Name".GetLocalized() + $": {gpu["Caption"]}\n" +
                              "   " + "AdapterRAM".GetLocalized() + $": {gpu["AdapterRAM"]} bytes\n" +
                              "   " + "DriverVersion".GetLocalized() + $": {gpu["DriverVersion"]}\n" +
                              "   " + "VideoArchitecture".GetLocalized() + $": {gpu["VideoArchitecture"]}";

                gpuNumber++;
                return gpuInfo;
            });

            return string.Join(Environment.NewLine, gpuInfoLines);
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"Error getting GPU info: {ex}");
            return string.Empty;
        }
    }

    private static string GetRamInformation()
    {
        try
        {
            LogHelper.Log("Getting RAM Info");
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
            var collection = searcher.Get();
            searcher.Dispose();

            var ramInfoLines = collection.Cast<ManagementObject>().Select((ram, i) =>
                "RAMModule".GetLocalized() + $" {i + 1}:\n" +
                "   " + "DeviceLocator".GetLocalized() + $": {ram["DeviceLocator"]}\n" +
                "   " + "Capacity".GetLocalized() + $": {((ulong)ram["Capacity"]) / (1024 * 1024)} MB\n" +
                "   " + "Speed".GetLocalized() + $": {ram["Speed"]} MHz\n" +
                "   " + "Manufacturer".GetLocalized() + $": {ram["Manufacturer"]}");

            return string.Join(Environment.NewLine, ramInfoLines);
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"Error getting RAM info: {ex}");
            return string.Empty;
        }
    }

    private static string GetDiskInformation()
    {
        try
        {
            LogHelper.Log("Getting Disks Info");
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
            var collection = searcher.Get();
            searcher.Dispose();
            var diskNumber = 0;
            var diskInfoLines = collection.Cast<ManagementObject>().Select(disk =>
            {
                var diskInfo = "Disk".GetLocalized() + $" {diskNumber}:\n" +
                               "   " + "Caption".GetLocalized() + $": {disk["Caption"]}\n" +
                               "   " + "Size".GetLocalized() + $": {disk["Size"]} bytes\n" +
                               "   " + "InterfaceType".GetLocalized() + $": {disk["InterfaceType"]}\n" +
                               "   " + "Manufacturer".GetLocalized() + $": {disk["Manufacturer"]}\n" +
                               "   " + "Model".GetLocalized() + $": {disk["Model"]}";

                diskNumber++;
                return diskInfo;
            });
            return string.Join(Environment.NewLine, diskInfoLines);
        }

        catch (Exception ex)
        {
            LogHelper.LogError($"Error getting Disk info: {ex}");
            return string.Empty; ;
        }
    }

    private static string GetOsInformation()
    {
        try
        {
            LogHelper.Log("Getting OS Info");
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            var collection = searcher.Get();
            searcher.Dispose();

            var osInfoLines = collection.Cast<ManagementObject>().Select(os =>
                "OSName".GetLocalized() + $": {os["Caption"]}\n" +
                "Version".GetLocalized() + $": {os["Version"]}\n" +
                "BuildNumber".GetLocalized() + $": {os["BuildNumber"]}\n" +
                "Architecture".GetLocalized() + $": {os["OSArchitecture"]}\n" +
                "InstallDate".GetLocalized() + $": {os["InstallDate"]}\n" +
                "RegisteredUser".GetLocalized() + $": {os["RegisteredUser"]}\n" +
                "WindowsDirectory".GetLocalized() + $": {os["WindowsDirectory"]}\n" +
                "SystemDirectory".GetLocalized() + $": {os["SystemDirectory"]}\n" +
                "LastBoot".GetLocalized() + $": {os["LastBootUpTime"]}");

            return string.Join(Environment.NewLine, osInfoLines);
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"Error getting OS info: {ex}");
            return string.Empty;
        }
    }

    // Working on it

    /*private static async Task ImportDrivers(string path)
    {
        await OptimizationOptions.StartInCmd($"pnputil.exe /add-driver '{path}'\\*.inf /subdirs /install");
    }
    public static async Task<int> ProcessSubdirectories(string parentDirectory)
    {
        try
        {
            string[] subdirectories = Directory.GetDirectories(parentDirectory);

            foreach (string subdir in subdirectories)
            {
                await OptimizationOptions.StartInCmd($"pnputil.exe /add-driver '{subdir}'\\*.inf /install");
                await ProcessSubdirectories(subdir);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        return 0;
    }
    private async void ImportButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var folderPath = FolderPathText.Text;
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        ImportingStatusText.Text = "Importing drivers...";
        ImportingStatusPb.ShowError = false;
        ImportingStatus.Visibility = Visibility.Visible;
        try
        {
            //var exitCode = await OptimizationOptions.StartInCmd($"pnputil.exe /add-driver '{folderPath}'\\*.inf /subdirs /install");
            var exitCode = await ProcessSubdirectories(folderPath);
            if (exitCode == 0)
            {
                ImportingStatusText.Text = "Done";
                ImportingStatusPb.Visibility = Visibility.Collapsed;
            }
            else
            {
                ImportingStatusText.Text = "There was an error while importing drivers";
                ImportingStatusPb.ShowError = true;
            }
        }
        catch
        {
            ImportingStatusText.Text = "There was an error while importing drivers";
            ImportingStatusPb.ShowError = true;
        }

    }

    private void ImportSelectPathButton_Click(object sender, RoutedEventArgs e)
    {

        var selectedFolderPath = ShowDialog("C:\\", "Select a Folder...");
        if (!string.IsNullOrEmpty(selectedFolderPath))
        {
            ImportFolderPathText.Text = selectedFolderPath;
            ImportButton.Visibility = Visibility.Visible;
        }
        else
        {
            ImportButton.Visibility = Visibility.Collapsed;
            ImportFolderPathText.Text = "Select a folder";
        }
    }*/

    private async void ExtractButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var folderPath = FolderPathText.Text + $"\\RyTuneX_Drivers_{DateTime.Now:yyyy-MM-dd}";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        ExtractingStatusText.Text = "ExtractingDrivers".GetLocalized() + "...";
        ExtractingStatusPb.ShowError = false;
        ExtractingStatus.Visibility = Visibility.Visible;
        try
        {
            var exitCode = await OptimizationOptions.StartInCmd($"powershell Export-WindowsDriver -Online -Destination '{folderPath}'");
            if (exitCode == 0)
            {
                ExtractingStatusText.Text = "Done".GetLocalized();
                ExtractingStatusPb.Visibility = Visibility.Collapsed;
            }
            else
            {
                ExtractingStatusText.Text = "ErrDriversExtract".GetLocalized();
                ExtractingStatusPb.ShowError = true;
            }
        }
        catch
        {
            ExtractingStatusText.Text = "ErrDriversExtract".GetLocalized();
            ExtractingStatusPb.ShowError = true;
        }

    }

    private void SelectPathButton_Click(object sender, RoutedEventArgs e)
    {

        var selectedFolderPath = ShowDialog("C:\\", "SelecFold".GetLocalized() + "...");
        if (!string.IsNullOrEmpty(selectedFolderPath))
        {
            FolderPathText.Text = selectedFolderPath;
            ExtractButton.Visibility = Visibility.Visible;
        }
        else
        {
            ExtractButton.Visibility = Visibility.Collapsed;
            FolderPathText.Text = "SelecFold".GetLocalized();
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHBrowseForFolder(ref BROWSEINFO lpbi);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool SHGetPathFromIDList(IntPtr pidl, IntPtr pszPath);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct BROWSEINFO
    {
        public IntPtr hwndOwner;
        public IntPtr pidlRoot;
        public string pszDisplayName;
        public string lpszTitle;
        public uint ulFlags;
        public IntPtr lpfn;
        public IntPtr lParam;
        public int iImage;
    }

    public static string ShowDialog(string startingDirectory, string dialogTitle)
    {
        var bi = new BROWSEINFO
        {
            hwndOwner = IntPtr.Zero,
            pidlRoot = IntPtr.Zero,
            pszDisplayName = new string('\0', 260),
            lpszTitle = dialogTitle,
            ulFlags = 0x00000040 | 0x00000001 // BIF_NEWDIALOGSTYLE | BIF_RETURNONLYFSDIRS
        };

        var pidl = SHBrowseForFolder(ref bi);
        if (pidl != IntPtr.Zero)
        {
            var pathPtr = Marshal.AllocCoTaskMem(260 * sizeof(char));
            if (SHGetPathFromIDList(pidl, pathPtr))
            {
                var path = Marshal.PtrToStringUni(pathPtr);
                Marshal.FreeCoTaskMem(pidl);
                Marshal.FreeCoTaskMem(pathPtr);
                return path;
            }
            Marshal.FreeCoTaskMem(pidl);
            Marshal.FreeCoTaskMem(pathPtr);
        }
        return string.Empty;
    }
}
