using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Storage;

internal class LogHelper
{
    private static readonly SemaphoreSlim LogSemaphore = new(1, 1);

    private static readonly string LogDirectory = Path.Combine(
        ApplicationData.Current.LocalCacheFolder.Path,
        "Logs");

    private static readonly string LogFilePath = Path.Combine(LogDirectory, "Log.txt");

    private static async Task LogToFile(string message)
    {
        await LogSemaphore.WaitAsync();
        try
        {
            Directory.CreateDirectory(LogDirectory);
            await File.AppendAllTextAsync(LogFilePath,
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: {message}{Environment.NewLine}",
                new UTF8Encoding(false));
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

    // Writes a critical log entry synchronously.
    public static void LogCriticalSync(string message, [CallerMemberName] string caller = "", [CallerFilePath] string file = "")
    {
        var callerInfo = FormatCaller(caller, file);
        var entry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: [CRITICAL] [{callerInfo}] {message}{Environment.NewLine}";
        try
        {
            Directory.CreateDirectory(LogDirectory);
            File.AppendAllText(LogFilePath, entry);
        }
        catch
        {
            Debug.WriteLine(entry);
        }
    }

    public static Task Log(string message, [CallerMemberName] string caller = "", [CallerFilePath] string file = "")
    {
        var callerInfo = FormatCaller(caller, file);
        return LogToFile($"[INFO] [{callerInfo}] {message}");
    }

    public static Task LogWarning(string message, [CallerMemberName] string caller = "", [CallerFilePath] string file = "")
    {
        var callerInfo = FormatCaller(caller, file);
        return LogToFile($"[WARN] [{callerInfo}] {message}");
    }

    public static Task LogError(string message, [CallerMemberName] string caller = "", [CallerFilePath] string file = "")
    {
        var callerInfo = FormatCaller(caller, file);
        return LogToFile($"[ERROR] [{callerInfo}] {message}");
    }

    public static Task LogException(Exception ex, string context = "", [CallerMemberName] string caller = "", [CallerFilePath] string file = "")
    {
        var callerInfo = FormatCaller(caller, file);
        var prefix = string.IsNullOrEmpty(context) ? string.Empty : $"{context} | ";
        return LogToFile($"[ERROR] [{callerInfo}] {prefix}{ex}");
    }

    // Returns the full path to the log file.
    public static string GetLogFilePath() => LogFilePath;

    private static string FormatCaller(string caller, string file)
    {
        var fileName = Path.GetFileNameWithoutExtension(file);
        return $"{fileName}.{caller}";
    }
}