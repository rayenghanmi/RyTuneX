using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media;
using RyTuneX.Helpers;
using Windows.UI.ViewManagement;

namespace RyTuneX;

public sealed partial class MainWindow : WindowEx
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue;
    private readonly UISettings settings;

    public MainWindow()
    {
        InitializeComponent();
        LogHelper.Log("Initializing MainWindow");
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
        Content = null;
        Title = "RyTuneX";

        // Theme change code picked from https://github.com/microsoft/WinUI-Gallery/pull/1239
        dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        settings = new UISettings();
        settings.ColorValuesChanged += Settings_ColorValuesChanged; // cannot use FrameworkElement.ActualThemeChanged event

        // Set the appropriate backdrop
        SetBackdrop();
    }

    private void SetBackdrop()
    {
        if (MicaController.IsSupported())
        {
            SystemBackdrop = new MicaBackdrop();
        }
        else
        {
            SystemBackdrop = new DesktopAcrylicBackdrop();
        }
    }

    // this handles updating the caption button colors correctly when windows system theme is changed
    // while the app is open
    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        // This calls comes off-thread, hence we will need to dispatch it to current app's thread
        dispatcherQueue.TryEnqueue(() =>
        {
            TitleBarHelper.ApplySystemThemeToCaptionButtons();
        });
    }
}
