using System.Diagnostics;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;

internal class LogHelper
{
    private static readonly SemaphoreSlim LogSemaphore = new(1, 1);

    public static async Task ShowErrorMessageAndLog(Exception ex, XamlRoot xamlRoot)
    {
        var errorMessage = $"{ex.Message}\nStack Trace: {ex.StackTrace}";

        await LogError(errorMessage);

        await InitializeErrorMessage(errorMessage, xamlRoot);
    }

    private static async Task InitializeErrorMessage(string errorMessage, XamlRoot xamlRoot)
    {
        await LogSemaphore.WaitAsync();
        try
        {
            var errorDialog = new ContentDialog
            {
                Title = "Error",
                Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
                BorderBrush = (SolidColorBrush)Application.Current.Resources["AccentAAFillColorDefaultBrush"],
                Content = errorMessage,
                CloseButtonText = "Close",
                PrimaryButtonText = "Open Logs File",
                XamlRoot = xamlRoot
            };

            errorDialog.PrimaryButtonClick += async (sender, args) =>
            {
                var tempFolder = ApplicationData.Current.TemporaryFolder;
                var logFile = await tempFolder.GetFileAsync($"ErrorLogs_{DateTime.Now:yyyy-MM-dd}.txt");
                if (logFile != null)
                {
                    var options = new Windows.System.LauncherOptions
                    {
                        DisplayApplicationPicker = false
                    };
                    await Windows.System.Launcher.LaunchFileAsync(logFile, options);
                }
            };
            await errorDialog.ShowAsync();
        }
        finally
        {
            LogSemaphore.Release();
        }
    }

    private static async Task LogToFile(string message, string fileName)
    {
        await LogSemaphore.WaitAsync();
        try
        {
            var tempFolder = ApplicationData.Current.TemporaryFolder;
            var logFile = await tempFolder.CreateFileAsync($"{fileName}_{DateTime.Now:yyyy-MM-dd}.txt", CreationCollisionOption.OpenIfExists);

            // Ensure UTF-8 encoding when appending to the log file
            using (var stream = await logFile.OpenStreamForWriteAsync())
            {
                stream.Seek(0, SeekOrigin.End); // Append to the file
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) // Disable BOM
                {
                    await writer.WriteLineAsync($"{DateTime.Now:T}: {message}");
                }
            }
        }
        catch (Exception logException)
        {
            Debug.WriteLine($"Error logging to file: {logException.Message}");
        }
        finally
        {
            LogSemaphore.Release();
        }
    }

    public static Task Log(string message) => LogToFile($"[DEBUG] {message}", "Logs");
    public static Task LogError(string message) => LogToFile($"[ERROR] {message}", "Logs");
}