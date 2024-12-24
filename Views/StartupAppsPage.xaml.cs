using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.Diagnostics;
using System.Linq;
using System;
using Microsoft.UI.Xaml;

namespace RyTuneX.Views;

public sealed partial class StartupAppsPage : Page
{
    public ObservableCollection<StartupApp> AppList
    {
        get; set;
    }

    public StartupAppsPage()
    {
        InitializeComponent();
        AppList = new ObservableCollection<StartupApp>();
        LoadStartupApps();
    }

    private void LoadStartupApps()
    {
        try
        {
            var startupApps = GetStartupApps();

            AppList.Clear();
            foreach (var app in startupApps)
            {
                AppList.Add(app);
            }

            appTreeView.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading startup apps: {ex.Message}");
        }
    }

    private void AppTreeView_DragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs args)
    {
        args.Cancel = true;
    }

    private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        var toggleSwitch = (ToggleSwitch)sender;
        var startupApp = (StartupApp)toggleSwitch.DataContext;

        try
        {
            string registryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

            using (var key = Registry.LocalMachine.OpenSubKey(registryPath, true))
            {
                if (key != null)
                {
                    // Getting the current value of the app's registry entry
                    var currentValue = key.GetValue(startupApp.Key) as byte[];
                    if (currentValue != null && currentValue.Length > 0)
                    {
                        // Create a new byte array to store the value
                        byte[] newValue = (byte[])currentValue.Clone();
                        // Get the first byte of the value
                        byte currentFirstByte = currentValue[0];
                        // Toggle the first byte based on the current value
                        if (toggleSwitch.IsOn)
                        {
                            if (currentFirstByte == 0x03)
                            {
                                newValue[0] = 0x02;
                            }
                            else if (currentFirstByte == 0x05)
                            {
                                newValue[0] = 0x04;
                            }
                            else if (currentFirstByte == 0x07)
                            {
                                newValue[0] = 0x06;
                            }
                        }
                        else
                        {
                            if (currentFirstByte == 0x02)
                            {
                                newValue[0] = 0x03;
                            }
                            else if (currentFirstByte == 0x04)
                            {
                                newValue[0] = 0x05;
                            }
                            else if (currentFirstByte == 0x06)
                            {
                                newValue[0] = 0x07;
                            }
                        }
                        // Set the new value to the registry entry
                        key.SetValue(startupApp.Key, newValue);
                        Debug.WriteLine($"{(toggleSwitch.IsOn ? "Enabled" : "Disabled")} {startupApp.Key} at startup.");
                    }
                }
            }
            // Update the Toggle switch status of the app
            startupApp.IsEnabled = toggleSwitch.IsOn;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error toggling startup for {startupApp.Key}: {ex.Message}");
        }
    }


    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        // Get the StartupApp object from the button's DataContext
        var button = (Button)sender;
        var startupApp = (StartupApp)button.DataContext;

        try
        {
            string registryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
            // Delete the registry entry of the app
            using (var key = Registry.LocalMachine.OpenSubKey(registryPath, true))
            {
                if (key != null)
                {
                    if (key.GetValue(startupApp.Key) != null)
                    {
                        // Delete the registry entry of the app
                        key.DeleteValue(startupApp.Key);
                        Debug.WriteLine($"Deleted {startupApp.Key} from startup.");
                    }
                    else
                    {
                        Debug.WriteLine($"{startupApp.Key} does not exist in the registry.");
                    }
                }
            }

            // Remove the app from the AppList
            AppList.Remove(startupApp);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error deleting {startupApp.Key} from startup: {ex.Message}");
        }
    }


    public List<StartupApp> GetStartupApps()
    {
        List<StartupApp> startupApps = new List<StartupApp>();

        // Registry paths for startup apps
        string[] registryPaths = new string[]
        {
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run",
        @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run32",
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder"
        };

        foreach (var registryPath in registryPaths)
        {
            try
            {
                // Access the 64-bit registry explicitly
                using (var localMachineKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var currentUserKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
                {
                    using (var keyLocalMachine = localMachineKey.OpenSubKey(registryPath))
                    using (var keyCurrentUser = currentUserKey.OpenSubKey(registryPath))
                    {
                        if (keyLocalMachine != null)
                        {
                            foreach (var appName in keyLocalMachine.GetValueNames())
                            {
                                var appValue = keyLocalMachine.GetValue(appName) as byte[];
                                if (appValue != null && appValue.Length > 0)
                                {
                                    var firstByte = appValue[0];

                                    // Determine the state of the startup app based on the first byte
                                    bool isEnabled = false;
                                    if (firstByte == 0x02 || firstByte == 0x04 || firstByte == 0x06)
                                    {
                                        isEnabled = true;
                                    }

                                    startupApps.Add(new StartupApp
                                    {
                                        Key = appName,
                                        Value = BitConverter.ToString(appValue), // Store the full binary value for reference
                                        IsEnabled = isEnabled
                                    });
                                }
                            }
                        }

                        if (keyCurrentUser != null)
                        {
                            foreach (var appName in keyCurrentUser.GetValueNames())
                            {
                                var appValue = keyCurrentUser.GetValue(appName) as byte[];
                                if (appValue != null && appValue.Length > 0)
                                {
                                    var firstByte = appValue[0];

                                    // Determine the state of the startup app based on the first byte
                                    bool isEnabled = false;
                                    if (firstByte == 0x02 || firstByte == 0x04 || firstByte == 0x06)
                                    {
                                        isEnabled = true;
                                    }

                                    startupApps.Add(new StartupApp
                                    {
                                        Key = appName,
                                        Value = BitConverter.ToString(appValue), // Store the full binary value for reference
                                        IsEnabled = isEnabled
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"Access denied to registry key '{registryPath}': {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading registry key '{registryPath}': {ex.Message}");
            }
        }

        return startupApps.Distinct().ToList(); // Remove duplicates if any
    }



}


public class StartupApp
{
    public string Key
    {
        get; set;
    }
    public string Value
    {
        get; set;
    }
    public bool IsEnabled
    {
        get; set;
    }
}
