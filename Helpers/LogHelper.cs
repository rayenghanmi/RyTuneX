using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;

internal class LogHelper
{
    public static async void ShowErrorMessageAndLog(Exception ex, XamlRoot xamlRoot)
    {
        var errorMessage = $"Caught Error: {ex.Message}";

        await LogError(errorMessage);

        InitiliseErrorMessage(errorMessage, xamlRoot);
    }

    private static async Task LogToFile(string message, string fileName)
    {
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
    }

    private static async void InitiliseErrorMessage(string errorMessage, XamlRoot xamlRoot)
    {
        ContentDialog errorDialog = new ContentDialog
        {
            Title = "Error",
            Content = errorMessage,
            CloseButtonText = "Close",
            PrimaryButtonText = "Open Logs File"
        };
        errorDialog.XamlRoot = xamlRoot;

        errorDialog.PrimaryButtonClick += async (sender, args) =>
        {
            StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
            StorageFile logFile = await tempFolder.GetFileAsync($"ErrorLogs_{DateTime.Now:yyyy-MM-dd}.txt");
            if (logFile != null)
            {
                var options = new Windows.System.LauncherOptions();
                options.DisplayApplicationPicker = false;
                await Windows.System.Launcher.LaunchFileAsync(logFile, options);
            }
        };
        await errorDialog.ShowAsync();
    }
    public static Task Log(string message) => LogToFile(message, "Logs");
    public static Task LogError(string message) => LogToFile(message, "ErrorLogs");
}