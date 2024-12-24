using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;

internal class LogHelper
{
    private static readonly SemaphoreSlim LogSemaphore = new(1, 1);

    public static async Task ShowErrorMessageAndLog(Exception ex, XamlRoot xamlRoot)
    {
        var errorMessage = $"Caught Error: {ex.Message}\nStack Trace: {ex.StackTrace}";

        await Log($"Error: {errorMessage}");

        await InitializeErrorMessage(errorMessage, xamlRoot);
    }

    private static async Task LogToFile(string message, string fileName)
    {
        await LogSemaphore.WaitAsync();
        try
        {
            var tempFolder = ApplicationData.Current.TemporaryFolder;

            var logFile = await tempFolder.CreateFileAsync($"{fileName}_{DateTime.Now:yyyy-MM-dd}.txt", CreationCollisionOption.OpenIfExists);

            await FileIO.AppendTextAsync(logFile, $"{DateTime.Now:T}: {message}\n");
        }
        catch (Exception logException)
        {
            Console.WriteLine($"Error logging to file: {logException.Message}");
        }
        finally
        {
            LogSemaphore.Release();
        }
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

    public static Task Log(string message) => LogToFile(message, "Logs");
    public static Task LogError(string message) => LogToFile($"Error: {message}", "Logs");
}