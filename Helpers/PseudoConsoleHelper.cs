using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32.SafeHandles;

namespace RyTuneX.Helpers;

// Provides Windows Pseudo Console (ConPTY) support for capturing real-time output
internal static partial class PseudoConsoleHelper
{
    private const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
    private const int PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = 0x00020016;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int CreatePseudoConsole(COORD size, SafeFileHandle hInput, SafeFileHandle hOutput, uint dwFlags, out IntPtr phPC);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern void ClosePseudoConsole(IntPtr hPC);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool InitializeProcThreadAttributeList(IntPtr lpAttributeList, int dwAttributeCount, int dwFlags, ref IntPtr lpSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool UpdateProcThreadAttribute(IntPtr lpAttributeList, uint dwFlags, IntPtr attribute, IntPtr lpValue, IntPtr cbSize, IntPtr lpPreviousValue, IntPtr lpReturnSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern void DeleteProcThreadAttributeList(IntPtr lpAttributeList);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CreatePipe(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe, IntPtr lpPipeAttributes, int nSize);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcessW(string? lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string? lpCurrentDirectory, ref STARTUPINFOEX lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

    [StructLayout(LayoutKind.Sequential)]
    private struct COORD
    {
        public short X, Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFO
    {
        public int cb;
        public IntPtr lpReserved, lpDesktop, lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFOEX
    {
        public STARTUPINFO StartupInfo; public IntPtr lpAttributeList;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess, hThread; public int dwProcessId, dwThreadId;
    }

    // Runs a command using ConPTY to capture real-time output.
    public static async Task<int> RunAsync(string commandLine, Action<string> outputCallback, CancellationToken ct = default, Action<int>? processIdCallback = null)
    {
        SafeFileHandle? inputRead = null, inputWrite = null, outputRead = null, outputWrite = null;
        var pty = IntPtr.Zero;
        var attrList = IntPtr.Zero;
        var proc = new PROCESS_INFORMATION();
        int processId;

        try
        {
            if (!CreatePipe(out inputRead, out inputWrite, IntPtr.Zero, 0) ||
                !CreatePipe(out outputRead, out outputWrite, IntPtr.Zero, 0))
            {
                throw new InvalidOperationException($"CreatePipe failed: {Marshal.GetLastWin32Error()}");
            }

            var hr = CreatePseudoConsole(new COORD { X = 120, Y = 30 }, inputRead, outputWrite, 0, out pty);
            if (hr != 0)
            {
                throw new InvalidOperationException($"CreatePseudoConsole failed: {hr}");
            }

            var size = IntPtr.Zero;
            InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref size);
            attrList = Marshal.AllocHGlobal(size);

            if (!InitializeProcThreadAttributeList(attrList, 1, 0, ref size) ||
                !UpdateProcThreadAttribute(attrList, 0, PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE, pty, nint.Size, IntPtr.Zero, IntPtr.Zero))
            {
                throw new InvalidOperationException($"Attribute list setup failed: {Marshal.GetLastWin32Error()}");
            }

            var si = new STARTUPINFOEX { lpAttributeList = attrList };
            si.StartupInfo.cb = Marshal.SizeOf<STARTUPINFOEX>();

            if (!CreateProcessW(null, commandLine, IntPtr.Zero, IntPtr.Zero, false, EXTENDED_STARTUPINFO_PRESENT, IntPtr.Zero, null, ref si, out proc))
            {
                throw new InvalidOperationException($"CreateProcessW failed: {Marshal.GetLastWin32Error()}");
            }

            // Store the process ID for termination
            processId = proc.dwProcessId;

            // Notify caller of the process ID
            processIdCallback?.Invoke(processId);

            // Close pipe ends we don't need
            outputWrite.Dispose();
            outputWrite = null;
            inputRead.Dispose();
            inputRead = null;

            // Create a process wrapper to wait for exit
            using var process = Process.GetProcessById(processId);

            // Read output while process runs
            var readTask = ReadOutputAsync(outputRead, outputCallback, ct);

            try
            {
                await process.WaitForExitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                // Process was cancelled, terminate it using taskkill
                await KillProcessTreeAsync(processId);
                throw;
            }

            // Close the PTY to signal EOF to reader
            if (pty != IntPtr.Zero)
            {
                ClosePseudoConsole(pty);
                pty = IntPtr.Zero;
            }

            // Wait for read to complete with timeout (don't pass ct here as it may be cancelled)
            await Task.WhenAny(readTask, Task.Delay(1000));

            GetExitCodeProcess(proc.hProcess, out var exitCode);
            return (int)exitCode;
        }
        finally
        {
            if (proc.hProcess != IntPtr.Zero) CloseHandle(proc.hProcess);
            if (proc.hThread != IntPtr.Zero) CloseHandle(proc.hThread);
            if (attrList != IntPtr.Zero) { DeleteProcThreadAttributeList(attrList); Marshal.FreeHGlobal(attrList); }
            if (pty != IntPtr.Zero) ClosePseudoConsole(pty);
            inputRead?.Dispose();
            inputWrite?.Dispose();
            outputRead?.Dispose();
            outputWrite?.Dispose();
        }
    }

    // Kills a process and all its child processes using taskkill, and stops related Windows services.
    // For DISM/SFC operations, this also stops the TrustedInstaller and msiserver services.
    internal static async Task KillProcessTreeAsync(int processId)
    {
        if (processId <= 0)
        {
            return;
        }

        // Phase 1: Kill the main process and its direct children first
        await RunCommandAsync("taskkill", $"/T /F /PID {processId}");

        // Phase 2: Run service stops and process kills in parallel
        var tasks = new List<Task>
        {
            // Stop TrustedInstaller service this may stop TrustedInstaller.exe and TiWorker.exe
            RunCommandAsync("sc", "stop TrustedInstaller"),
            
            // Stop Windows Installer service that is sometimes used by DISM
            RunCommandAsync("sc", "stop msiserver"),
            
            // Kill any remaining dism.exe processes
            RunCommandAsync("taskkill", "/F /IM dism.exe"),
            
            // Kill any remaining DismHost.exe processes
            RunCommandAsync("taskkill", "/F /IM DismHost.exe"),
            
            // Kill any remaining sfc.exe processes  
            RunCommandAsync("taskkill", "/F /IM sfc.exe"),
            
            // Kill any remaining chkdsk.exe processes
            RunCommandAsync("taskkill", "/F /IM chkdsk.exe"),
            
            // Kill TiWorker.exe
            RunCommandAsync("taskkill", "/F /IM TiWorker.exe"),
            
            // Kill wusa.exe
            RunCommandAsync("taskkill", "/F /IM wusa.exe")
        };

        // Wait for all tasks with a timeout
        try
        {
            await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (TimeoutException)
        {
            // Some tasks took too long, but there's not much to do
        }
        catch
        {
            // Ignore other errors during cleanup
        }

        // Phase 3: Give services a moment to stop, then force kill any remaining processes
        await Task.Delay(500);

        // Final attempt to kill any stubborn processes
        var finalKillTasks = new List<Task>
        {
            RunCommandAsync("taskkill", "/F /IM dism.exe"),
            RunCommandAsync("taskkill", "/F /IM DismHost.exe"),
            RunCommandAsync("taskkill", "/F /IM sfc.exe"),
            RunCommandAsync("taskkill", "/F /IM TiWorker.exe")
        };

        try
        {
            await Task.WhenAll(finalKillTasks).WaitAsync(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // Ignore errors
        }
    }

    // Runs a command and waits for it to complete with a timeout.
    private static async Task RunCommandAsync(string fileName, string arguments)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();

            // Wait asynchronously with a short timeout
            using var cts = new CancellationTokenSource(2000);
            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(); } catch { }
            }
        }
        catch
        {
            // Ignore errors
        }
    }

    private static async Task ReadOutputAsync(SafeFileHandle handle, Action<string> callback, CancellationToken ct)
    {
        using var stream = new FileStream(handle, FileAccess.Read, 4096, false);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var buffer = new char[256];
        var line = new StringBuilder();

        while (!ct.IsCancellationRequested)
        {
            int read;
            try { read = await reader.ReadAsync(buffer, 0, buffer.Length); }
            catch { break; }
            if (read == 0) break;

            for (var i = 0; i < read; i++)
            {
                if (buffer[i] is '\r' or '\n')
                {
                    if (line.Length > 0)
                    {
                        var cleaned = StripAnsiSequences(line.ToString());
                        if (!string.IsNullOrWhiteSpace(cleaned))
                        {
                            callback(cleaned);
                        }
                        line.Clear();
                    }
                }
                else
                {
                    line.Append(buffer[i]);
                }
            }
        }

        if (line.Length > 0)
        {
            var cleaned = StripAnsiSequences(line.ToString());
            if (!string.IsNullOrWhiteSpace(cleaned))
            {
                callback(cleaned);
            }
        }
    }

    // Strips all ANSI/VT100 escape sequences from the input string.
    private static string StripAnsiSequences(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // Standard CSI sequences: ESC [ ... letter (e.g., ESC[0m, ESC[2J)
        var result = CsiSequenceRegex().Replace(input, string.Empty);

        // Private mode sequences without ESC: [?9001h, [?1004h, [?25l, [?25h, etc.
        result = PrivateModeRegex().Replace(result, string.Empty);

        // OSC sequences: ]0;... (window title) - terminated by BEL (\x07) or ST (ESC \)
        result = OscSequenceRegex().Replace(result, string.Empty);

        // ESC followed by single character (e.g., ESC M, ESC 7, ESC 8)
        result = EscSingleCharRegex().Replace(result, string.Empty);

        // Any remaining ESC sequences
        result = EscAnyRegex().Replace(result, string.Empty);

        return result;
    }

    // Standard CSI: ESC [ (params) letter
    [GeneratedRegex(@"\x1B\[[0-9;]*[A-Za-z]")]
    private static partial Regex CsiSequenceRegex();

    // Private mode: [?...h or [?...l (with or without ESC)
    [GeneratedRegex(@"(\x1B)?\[\?[\d;]*[a-zA-Z]")]
    private static partial Regex PrivateModeRegex();

    // OSC: ]0;... or ESC ]0;... (window title sequences)
    [GeneratedRegex(@"(\x1B)?\]0;[^\x07\x1B]*(\x07|\x1B\\)?")]
    private static partial Regex OscSequenceRegex();

    // ESC + single character
    [GeneratedRegex(@"\x1B[^[\]0-9]")]
    private static partial Regex EscSingleCharRegex();

    // Any remaining ESC
    [GeneratedRegex(@"\x1B")]
    private static partial Regex EscAnyRegex();
}
