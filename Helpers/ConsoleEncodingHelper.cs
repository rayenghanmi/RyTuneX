using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace RyTuneX.Helpers;

internal static partial class ConsoleEncodingHelper
{
    private static readonly SemaphoreSlim SyncLock = new(1, 1);
    private static Encoding? _oemConsoleEncoding;

    private static readonly Lazy<string> CmdFullPath = new(() =>
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess ? "SysNative" : "System32",
            "cmd.exe");
    });

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint GetConsoleOutputCP();

    public static async Task<Encoding> GetOemConsoleEncodingAsync()
    {
        if (_oemConsoleEncoding != null)
        {
            return _oemConsoleEncoding;
        }

        await SyncLock.WaitAsync();
        try
        {
            if (_oemConsoleEncoding != null)
            {
                return _oemConsoleEncoding;
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var codePage = (int)GetConsoleOutputCP();
            if (codePage == 0)
            {
                codePage = await TryResolveCodePageFromChcpAsync();
            }

            if (codePage == 0)
            {
                codePage = GetFallbackOemCodePage();
            }

            _oemConsoleEncoding = Encoding.GetEncoding(codePage);
            return _oemConsoleEncoding;
        }
        finally
        {
            SyncLock.Release();
        }
    }

    private static async Task<int> TryResolveCodePageFromChcpAsync()
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = CmdFullPath.Value,
                    Arguments = "/C \"chcp\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Console.OutputEncoding
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            var match = CodePageRegex().Match(output);
            if (match.Success && int.TryParse(match.Value, out var codePage))
            {
                return codePage;
            }
        }
        catch
        {
            // fallback below
        }

        return 0;
    }

    private static int GetFallbackOemCodePage()
    {
        var ansiCodePage = Console.OutputEncoding.CodePage;

        return ansiCodePage switch
        {
            1251 => 866,
            1252 => 437,
            1250 => 852,
            1253 => 737,
            1254 => 857,
            1255 => 862,
            1256 => 708,
            1257 => 775,
            1258 => 869,
            932 => 932,
            936 => 936,
            949 => 949,
            950 => 950,
            _ => 437
        };
    }

    [GeneratedRegex(@"(\d+)")]
    private static partial Regex CodePageRegex();
}
