using System.Diagnostics;
using System.Text;
using Windows.Storage;

internal class LogHelper
{
    private static readonly SemaphoreSlim LogSemaphore = new(1, 1);

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
    public static async Task LogError(string message)
    {
        await LogToFile($"[ERROR] {message}", "Logs");
    }
}