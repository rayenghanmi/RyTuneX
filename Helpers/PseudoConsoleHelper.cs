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
    private struct COORD { public short X, Y; }

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
    private struct STARTUPINFOEX { public STARTUPINFO StartupInfo; public IntPtr lpAttributeList; }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION { public IntPtr hProcess, hThread; public int dwProcessId, dwThreadId; }

    // Runs a command using ConPTY to capture real-time output.
    public static async Task<int> RunAsync(string commandLine, Action<string> outputCallback, CancellationToken ct = default)
    {
        SafeFileHandle? inputRead = null, inputWrite = null, outputRead = null, outputWrite = null;
        var pty = IntPtr.Zero;
        var attrList = IntPtr.Zero;
        var proc = new PROCESS_INFORMATION();

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
                !UpdateProcThreadAttribute(attrList, 0, (IntPtr)PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE, pty, (IntPtr)IntPtr.Size, IntPtr.Zero, IntPtr.Zero))
            {
                throw new InvalidOperationException($"Attribute list setup failed: {Marshal.GetLastWin32Error()}");
            }

            var si = new STARTUPINFOEX { lpAttributeList = attrList };
            si.StartupInfo.cb = Marshal.SizeOf<STARTUPINFOEX>();

            if (!CreateProcessW(null, commandLine, IntPtr.Zero, IntPtr.Zero, false, EXTENDED_STARTUPINFO_PRESENT, IntPtr.Zero, null, ref si, out proc))
            {
                throw new InvalidOperationException($"CreateProcessW failed: {Marshal.GetLastWin32Error()}");
            }

            // Close pipe ends we don't need
            outputWrite.Dispose();
            outputWrite = null;
            inputRead.Dispose();
            inputRead = null;

            // Create a process wrapper to wait for exit
            using var process = System.Diagnostics.Process.GetProcessById(proc.dwProcessId);

            // Read output while process runs
            var readTask = ReadOutputAsync(outputRead, outputCallback, ct);

            await process.WaitForExitAsync(ct);

            // Close the PTY to signal EOF to reader
            if (pty != IntPtr.Zero)
            {
                ClosePseudoConsole(pty);
                pty = IntPtr.Zero;
            }

            // Wait for read to complete with timeout
            await Task.WhenAny(readTask, Task.Delay(1000, ct));

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
