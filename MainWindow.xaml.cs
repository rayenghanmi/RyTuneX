using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using RyTuneX.Helpers;
using Windows.UI.ViewManagement;

namespace RyTuneX;

public sealed partial class MainWindow : WindowEx
{
    private readonly DispatcherQueue dispatcherQueue;
    private readonly UISettings settings;
    private static int _exitRequested = 0;

    public MainWindow()
    {
        InitializeComponent();
        LogHelper.Log("Initializing MainWindow");
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
        Content = null;
        Title = "RyTuneX";

        dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        settings = new UISettings();
        settings.ColorValuesChanged += Settings_ColorValuesChanged;

        // Set the appropriate backdrop
        SetBackdrop();

        this.AppWindow.Closing += MainWindow_Closing;
    }

    private async void MainWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs e)
    {
        try
        {
            // If an exit was already requested programmatically, allow the close to proceed.
            var exitRequested = Interlocked.CompareExchange(ref _exitRequested, 0, 0) == 1;
            if (!exitRequested && (OptimizationOptions.HasPendingToggleOperations || OperationCancellationManager.Count > 0))
            {
                // Prevent immediate close
                e.Cancel = true;

                var dialog = new ContentDialog
                {
                    Title = "Operations in progress",
                    Content = "There are background optimization operations running. Do you want to wait for them to finish or exit now?",
                    PrimaryButtonText = "Wait",
                    CloseButtonText = "Cancel",
                    SecondaryButtonText = "Exit",
                    PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"],
                    Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"]
                };

                // Determine a valid XamlRoot
                var xr = (this.Content as FrameworkElement)?.XamlRoot ?? App.MainWindow.Content?.XamlRoot;
                if (xr != null)
                    dialog.XamlRoot = xr;

                // Ensure dialog is shown on UI thread associated with XamlRoot
                var tcs = new TaskCompletionSource<ContentDialogResult>();
                dispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        var r = await dialog.ShowAsync();
                        tcs.SetResult(r);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });

                var result = await tcs.Task.ConfigureAwait(false);
                _ = LogHelper.Log($"Close dialog result: {result}");
                if (result == ContentDialogResult.Primary)
                {
                    // Run a background waiter that will close the dialog and exit when done
                    _ = Task.Run(async () =>
                    {
                        var done = await OperationCancellationManager.WaitForPendingOperationsAsync(null);
                        if (done)
                        {
                            // Mark exit requested and close on UI thread
                            Interlocked.Exchange(ref _exitRequested, 1);
                            dispatcherQueue.TryEnqueue(() =>
                            {
                                try { dialog.Hide(); } catch (Exception ex) { _ = LogHelper.LogWarning($"Error hiding dialog on exit: {ex.Message}"); }
                                try { App.MainWindow.Close(); } catch (Exception ex) { _ = LogHelper.LogWarning($"Error closing window on exit: {ex.Message}"); }
                            });
                        }
                    });
                }
                else if (result == ContentDialogResult.Secondary)
                {
                    // Cancel registered operations then force exit
                    OperationCancellationManager.CancelAll();
                    // Mark exit requested immediately
                    Interlocked.Exchange(ref _exitRequested, 1);
                    dispatcherQueue.TryEnqueue(() =>
                    {
                        try { dialog.Hide(); } catch (Exception ex) { _ = LogHelper.LogWarning($"Error hiding dialog on force exit: {ex.Message}"); }
                        try { App.MainWindow.Close(); } catch (Exception ex) { _ = LogHelper.LogWarning($"Error closing window on force exit: {ex.Message}"); }
                    });
                }
                else
                {
                    // Do nothing and keep app open
                }
            }
            else if (Interlocked.CompareExchange(ref _exitRequested, 0, 0) == 1)
            {
                // Exit was requested programmatically, allow close to proceed
            }
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogException(ex, "MainWindow_Closing");
        }
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
