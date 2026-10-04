using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace RyTuneX.Helpers;

public sealed class StressEngine : IDisposable
{

    private Thread? _gpuThread;
    private CancellationTokenSource? _gpuCts;
    private volatile bool _gpuRunning;
    private long _totalFrames;
    private double _dispatchRate;  // dispatches per second
    private string _gpuRenderer = "Unknown";
    private string _gpuVendor = "Unknown";

    // ~83.886 GFLOP per dispatch: 2048 groups × 256 threads × 4000 iters × ~40 FP ops
    private const double GFlopsPerDispatch = (2048.0 * 256 * 4000 * 40) / 1e9;

    public bool IsGpuRunning => _gpuRunning;
    // Compute dispatches per second (raw engine rate).
    public double DispatchRate => _dispatchRate;
    // Estimated GPU compute throughput in GFLOPS based on shader workload per dispatch.
    public double CurrentGFlops => _dispatchRate * GFlopsPerDispatch;
    public long TotalDispatches => _totalFrames;
    public string GpuRenderer => _gpuRenderer;
    public string GpuVendor => _gpuVendor;

    public event Action? GpuStopped;

    private CancellationTokenSource? _cpuCts;
    private long _cpuIterations;
    private readonly List<Thread> _cpuThreads = new();

    public bool IsCpuRunning => _cpuCts != null;
    public long CpuIterations => Interlocked.Read(ref _cpuIterations);

    public event Action? CpuStopped;

    private CancellationTokenSource? _ramCts;
    private byte[][]? _ramBlocks;

    public bool IsRamRunning => _ramCts != null;

    public event Action? RamStopped;
    public event Action<long>? RamAllocated;   // allocated MB
    public event Action<double>? RamBandwidth; // MB/s

    private CancellationTokenSource? _diskCts;

    public bool IsDiskRunning => _diskCts != null;

    public event Action? DiskStopped;
    public event Action<double, double, long>? DiskMetrics; // writeMBps, readMBps, totalIops

    private readonly List<PerformanceCounter> _gpuCounters = new();
    private DateTime _lastGpuCounterScan = DateTime.MinValue;
    private IntPtr _nvmlDevice = IntPtr.Zero;
    private bool _nvmlInitialized;
    private bool _nvmlAttempted;
    private string _vramDisplay = "--";

    public string VramDisplay => _vramDisplay;

    private ulong _prevIdleTime;
    private ulong _prevKernelTime;
    private ulong _prevUserTime;
    private bool _cpuMetricsInitialized;

    public void StartGpu()
    {
        if (_gpuRunning) return;

        _gpuCts = new CancellationTokenSource();
        _gpuRunning = true;
        _totalFrames = 0;
        _dispatchRate = 0;

        var ct = _gpuCts.Token;
        _gpuThread = new Thread(() => GpuRenderLoop(ct))
        {
            IsBackground = true,
            Name = "RyTuneX_GpuStressThread"
        };
        _gpuThread.Start();
    }

    public void StopGpu()
    {
        if (!_gpuRunning && _gpuThread == null) return;

        _gpuRunning = false;
        _gpuCts?.Cancel();

        try { _gpuThread?.Join(500); } catch { }

        _gpuThread = null;
        _gpuCts?.Dispose();
        _gpuCts = null;

        GpuStopped?.Invoke();
    }

    private void GpuRenderLoop(CancellationToken ct)
    {
        try
        {
            // Enumerate DXGI adapters and select the discrete/high-performance GPU
            using var factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();
            IDXGIAdapter1? bestAdapter = null;
            ulong maxVram = 0;

            for (uint i = 0; factory.EnumAdapters1(i, out var adapter).Success; i++)
            {
                ulong vram = (ulong)adapter.Description.DedicatedVideoMemory;
                if (vram > maxVram || bestAdapter == null)
                {
                    bestAdapter?.Dispose();
                    bestAdapter = adapter;
                    maxVram = vram;
                }
                else
                {
                    adapter.Dispose();
                }
            }

            if (bestAdapter == null)
            {
                LogHelper.LogError("[StressEngine] No DXGI adapter found.");
                return;
            }

            _gpuRenderer = bestAdapter.Description.Description;
            _gpuVendor = bestAdapter.Description.VendorId switch
            {
                0x10DE => "NVIDIA",
                0x1002 => "AMD",
                0x8086 => "Intel",
                _ => "Unknown"
            };

            LogHelper.Log($"[StressEngine] Selected GPU for stress test: {_gpuRenderer} ({maxVram / (1024 * 1024)} MB VRAM)");

            var creationFlags = DeviceCreationFlags.None;
            var featureLevels = new[] { FeatureLevel.Level_11_1, FeatureLevel.Level_11_0 };

            var hr = D3D11.D3D11CreateDevice(
                (IDXGIAdapter)bestAdapter,
                DriverType.Unknown,
                creationFlags,
                featureLevels,
                out ID3D11Device device,
                out ID3D11DeviceContext context);

            if (!hr.Success || device == null || context == null)
            {
                LogHelper.LogError($"[StressEngine] D3D11CreateDevice failed: {hr}");
                bestAdapter.Dispose();
                return;
            }

            // HLSL compute shader — heavy math loop, saturates GPU at ~100% with < 15ms queue latency
            const string hlslCode = @"
            RWStructuredBuffer<float4> Output : register(u0);

            [numthreads(256, 1, 1)]
            void CSMain(uint3 id : SV_DispatchThreadID)
            {
                float4 val = float4(id.x * 0.001, 1.0, 2.0, 3.0);
                [loop]
                for (uint i = 0; i < 4000; i++)
                {
                    val = sin(val * 1.0001) * cos(val * 0.9999) + sqrt(abs(val) + 1.0);
                    val += frac(val * 1.6180339887);
                }
                Output[id.x] = val;
            }";

            var shaderBytecode = Compiler.Compile(hlslCode, "CSMain", "ComputeShader.hlsl", "cs_5_0");
            using var computeShader = device.CreateComputeShader(shaderBytecode.Span);

            const int threadGroupCount = 2048; // 2048 * 256 = 524,288 threads
            const int totalThreads = threadGroupCount * 256;

            using var buffer = device.CreateBuffer(new BufferDescription
            {
                ByteWidth = totalThreads * 16,
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.UnorderedAccess,
                StructureByteStride = 16,
                MiscFlags = ResourceOptionFlags.BufferStructured
            });

            using var uav = device.CreateUnorderedAccessView(buffer);

            context.CSSetShader(computeShader);
            context.CSSetUnorderedAccessView(0, uav);

            // Double-buffered D3D11 queries: limits queue depth to 2 dispatches so Stop() exits within < 20ms
            const int InFlightCount = 2;
            var queries = new ID3D11Query[InFlightCount];
            for (int i = 0; i < InFlightCount; i++)
            {
                queries[i] = device.CreateQuery(new QueryDescription(QueryType.Event, QueryFlags.None));
            }

            // Prime the pipeline with dispatches
            for (int i = 0; i < InFlightCount; i++)
            {
                context.Dispatch(threadGroupCount, 1, 1);
                context.End(queries[i]);
            }
            context.Flush();

            int slot = 0;
            var rateTimer = Stopwatch.StartNew();
            long dispatchCountSinceLastRate = 0;

            // Continuous hardware execution loop
            while (!ct.IsCancellationRequested && _gpuRunning)
            {
                // Wait for the query in the current slot to complete execution on the GPU
                int spinCount = 0;
                while (!context.GetData(queries[slot], out int _))
                {
                    if (!_gpuRunning || ct.IsCancellationRequested)
                        break;

                    spinCount++;
                    if (spinCount < 60)
                    {
                        Thread.SpinWait(20);
                    }
                    else
                    {
                        Thread.Yield();
                    }
                }

                if (!_gpuRunning || ct.IsCancellationRequested)
                    break;

                _totalFrames++;
                dispatchCountSinceLastRate++;

                if (rateTimer.ElapsedMilliseconds >= 300)
                {
                    _dispatchRate = dispatchCountSinceLastRate / rateTimer.Elapsed.TotalSeconds;
                    dispatchCountSinceLastRate = 0;
                    rateTimer.Restart();
                }

                // Dispatch the next batch into the now-free slot
                context.Dispatch(threadGroupCount, 1, 1);
                context.End(queries[slot]);
                context.Flush();

                slot = (slot + 1) % InFlightCount;
            }

            context.ClearState();
            context.Flush();

            for (int i = 0; i < InFlightCount; i++)
            {
                queries[i].Dispose();
            }

            context.Dispose();
            device.Dispose();
            bestAdapter.Dispose();

            LogHelper.Log("[StressEngine] D3D11 HLSL compute stress test cleanly finished.");
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"[StressEngine] GPU compute loop exception: {ex.Message}");
        }
        finally
        {
            _gpuRunning = false;
            GpuStopped?.Invoke();
        }
    }

    // --- CPU Stress ---

    public void StartCpu(IReadOnlyList<int> cpuIndices)
    {
        if (_cpuCts != null) return;

        _cpuCts = new CancellationTokenSource();
        Interlocked.Exchange(ref _cpuIterations, 0);
        var ct = _cpuCts.Token;

        _cpuThreads.Clear();
        foreach (var cpuIndex in cpuIndices)
        {
            var idx = cpuIndex; // capture loop variable
            var t = new Thread(() => CpuWorker(idx, ct))
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal,
                Name = $"RyTuneX_CpuStress_CPU{idx}"
            };
            _cpuThreads.Add(t);
            t.Start();
        }

        LogHelper.Log($"[StressEngine] CPU stress started on cores: {string.Join(", ", cpuIndices)}");
    }

    public void StopCpu()
    {
        _cpuCts?.Cancel();
        _cpuCts?.Dispose();
        _cpuCts = null;

        // Give each stress thread a moment to exit gracefully
        foreach (var t in _cpuThreads)
        {
            try { t.Join(300); } catch { }
        }
        _cpuThreads.Clear();

        CpuStopped?.Invoke();
        LogHelper.Log("[StressEngine] CPU stress test stopped");
    }

    private void CpuWorker(int cpuIndex, CancellationToken ct)
    {
        // Pin thread to exact logical processor so chosen cores hit 100% instead of spreading load
        if (cpuIndex < 64)
        {
            try
            {
                var mask = new IntPtr(1L << cpuIndex);
                SetThreadAffinityMask(GetCurrentThread(), mask);
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogWarning($"[StressEngine] Affinity set failed for CPU {cpuIndex}: {ex.Message}");
            }
        }

        // Mixed integer + floating-point workload
        double accumulator = 1.0;
        long localIterations = 0;

        while (!ct.IsCancellationRequested)
        {
            // Integer work: prime sieve-like computation
            for (var i = 2; i < 10000 && !ct.IsCancellationRequested; i++)
            {
                var isPrime = true;
                for (var j = 2; j * j <= i; j++)
                {
                    if (i % j == 0) { isPrime = false; break; }
                }
                if (isPrime) accumulator += i;
            }

            // Floating-point work: trigonometric chain
            for (var i = 0; i < 5000 && !ct.IsCancellationRequested; i++)
            {
                accumulator = Math.Sin(accumulator) * Math.Cos(accumulator) + Math.Sqrt(Math.Abs(accumulator) + 1.0);
            }

            localIterations++;
            if (localIterations % 10 == 0)
            {
                Interlocked.Add(ref _cpuIterations, 10);
            }
        }

        // Flush remaining
        Interlocked.Add(ref _cpuIterations, localIterations % 10);
    }

    // --- RAM Stress ---

    public void StartRam(double targetPercent)
    {
        if (_ramCts != null) return;

        _ramCts = new CancellationTokenSource();
        var ct = _ramCts.Token;

        _ = Task.Run(() => RamWorker(targetPercent, ct), ct);

        LogHelper.Log($"[StressEngine] RAM stress test started at {targetPercent * 100}%");
    }

    public void StopRam()
    {
        _ramCts?.Cancel();
        _ramCts?.Dispose();
        _ramCts = null;

        RamStopped?.Invoke();
        LogHelper.Log("[StressEngine] RAM stress test stopped");
    }

    private void RamWorker(double targetPercent, CancellationToken ct)
    {
        try
        {
            var memStatus = new MEMORYSTATUSEX();
            if (!GlobalMemoryStatusEx(ref memStatus)) return;

            var totalBytes = (long)memStatus.ullTotalPhys;
            var availBytes = (long)memStatus.ullAvailPhys;
            // Only allocate from currently-available memory to avoid OOM
            var targetAllocBytes = (long)(totalBytes * targetPercent) - (totalBytes - availBytes);
            if (targetAllocBytes <= 0) targetAllocBytes = availBytes / 4;

            // Cap at 90% of available to leave headroom for OS
            targetAllocBytes = Math.Min(targetAllocBytes, (long)(availBytes * 0.90));

            const int blockSize = 64 * 1024 * 1024; // 64 MB blocks
            var blockCount = (int)Math.Max(1, targetAllocBytes / blockSize);
            _ramBlocks = new byte[blockCount][];

            var allocatedMb = 0L;

            // Allocate blocks
            for (var i = 0; i < blockCount && !ct.IsCancellationRequested; i++)
            {
                try
                {
                    _ramBlocks[i] = new byte[blockSize];
                    // Touch all pages to force physical commit
                    Random.Shared.NextBytes(_ramBlocks[i]);
                    allocatedMb += blockSize / (1024 * 1024);
                    RamAllocated?.Invoke(allocatedMb);
                }
                catch (OutOfMemoryException)
                {
                    break;
                }
            }

            var sw = Stopwatch.StartNew();
            long totalBytesAccessed = 0;

            // Continuous read/write pattern to stress memory subsystem
            while (!ct.IsCancellationRequested)
            {
                for (var b = 0; b < _ramBlocks.Length && !ct.IsCancellationRequested; b++)
                {
                    if (_ramBlocks[b] == null) continue;

                    // Sequential write pattern
                    var block = _ramBlocks[b];
                    for (var i = 0; i < block.Length && !ct.IsCancellationRequested; i += 4096)
                    {
                        block[i] = (byte)(i ^ b);
                    }

                    // Sequential read verification
                    byte checksum = 0;
                    for (var i = 0; i < block.Length && !ct.IsCancellationRequested; i += 4096)
                    {
                        checksum ^= block[i];
                    }

                    totalBytesAccessed += block.Length * 2L;

                    // Update bandwidth
                    var elapsedSec = sw.Elapsed.TotalSeconds;
                    if (elapsedSec > 0.5)
                    {
                        var bandwidthMBps = totalBytesAccessed / (1024.0 * 1024.0) / elapsedSec;
                        RamBandwidth?.Invoke(bandwidthMBps);
                        totalBytesAccessed = 0;
                        sw.Restart();
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _ = LogHelper.LogWarning($"[StressEngine] RAM stress error: {ex.Message}");
        }
        finally
        {
            // Free all blocks
            if (_ramBlocks != null)
            {
                for (var i = 0; i < _ramBlocks.Length; i++)
                    _ramBlocks[i] = null!;
                _ramBlocks = null;
            }

            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
        }
    }

    // --- Disk Stress ---

    public void StartDisk()
    {
        if (_diskCts != null) return;

        _diskCts = new CancellationTokenSource();
        var ct = _diskCts.Token;

        _ = Task.Run(() => DiskWorker(ct), ct);

        LogHelper.Log("[StressEngine] Disk stress test started");
    }

    public void StopDisk()
    {
        _diskCts?.Cancel();
        _diskCts?.Dispose();
        _diskCts = null;

        DiskStopped?.Invoke();
        LogHelper.Log("[StressEngine] Disk stress test stopped");
    }

    private async Task DiskWorker(CancellationToken ct)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "RyTuneX_StressTest");
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "stress_test.tmp");

        try
        {
            const int bufferSize = 1024 * 1024; // 1 MB buffer
            var buffer = new byte[bufferSize];
            Random.Shared.NextBytes(buffer);

            var sw = Stopwatch.StartNew();
            long totalIops = 0;

            while (!ct.IsCancellationRequested)
            {
                // Write phase - 256 MB sequential write
                sw.Restart();
                long bytesWritten = 0;
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None,
                    bufferSize, FileOptions.WriteThrough | FileOptions.SequentialScan))
                {
                    for (var i = 0; i < 256 && !ct.IsCancellationRequested; i++)
                    {
                        await fs.WriteAsync(buffer, ct).ConfigureAwait(false);
                        bytesWritten += bufferSize;
                        totalIops++;
                    }
                    await fs.FlushAsync(ct).ConfigureAwait(false);
                }
                var writeElapsed = sw.Elapsed.TotalSeconds;
                var writeMBps = writeElapsed > 0 ? bytesWritten / (1024.0 * 1024.0) / writeElapsed : 0;

                if (ct.IsCancellationRequested) break;

                // Read phase - read the entire file back
                sw.Restart();
                long bytesRead = 0;
                var readBuffer = new byte[bufferSize];
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None,
                    bufferSize, FileOptions.SequentialScan))
                {
                    int read;
                    while ((read = await fs.ReadAsync(readBuffer, ct).ConfigureAwait(false)) > 0 && !ct.IsCancellationRequested)
                    {
                        bytesRead += read;
                        totalIops++;
                    }
                }
                var readElapsed = sw.Elapsed.TotalSeconds;
                var readMBps = readElapsed > 0 ? bytesRead / (1024.0 * 1024.0) / readElapsed : 0;

                DiskMetrics?.Invoke(writeMBps, readMBps, totalIops);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _ = LogHelper.LogWarning($"[StressEngine] Disk stress error: {ex.Message}");
        }
        finally
        {
            // Cleanup
            try { if (File.Exists(filePath)) File.Delete(filePath); } catch { }
            try { if (Directory.Exists(tempDir) && !Directory.EnumerateFileSystemEntries(tempDir).Any()) Directory.Delete(tempDir); } catch { }
        }
    }

    // --- GPU Monitoring (NVML + Performance Counters) ---

    // Detects GPU name and VRAM via WMI and raises the onResult callback on the calling thread.
    public void DetectGpuInfo(Action<string, ulong> onResult)
    {
        _ = Task.Run(() =>
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Caption, AdapterRAM FROM Win32_VideoController");
                foreach (var obj in searcher.Get())
                {
                    var name = obj["Caption"]?.ToString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        var ramObj = obj["AdapterRAM"];
                        ulong vramBytes = 0;
                        if (ramObj != null)
                        {
                            ulong.TryParse(ramObj.ToString(), out vramBytes);
                        }

                        if (vramBytes > 0)
                        {
                            var vramMb = vramBytes / (1024 * 1024);
                            _vramDisplay = $"{vramMb:N0} MB VRAM";
                        }

                        onResult(name, vramBytes);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogWarning($"[StressEngine] GPU info detection error: {ex.Message}");
            }
        });
    }

    public void TryInitNvml()
    {
        if (_nvmlAttempted) return;
        _nvmlAttempted = true;
        try
        {
            var nvmlPath = Path.Combine(Environment.SystemDirectory, "nvml.dll");
            if (File.Exists(nvmlPath))
            {
                if (nvmlInit_v2() == 0)
                {
                    if (nvmlDeviceGetHandleByIndex_v2(0, out _nvmlDevice) == 0)
                    {
                        _nvmlInitialized = true;
                        LogHelper.Log("[StressEngine] Hardware NVML telemetry successfully connected.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogHelper.LogWarning($"[StressEngine] NVML init notice: {ex.Message}");
        }
    }

    public void ShutdownNvml()
    {
        if (_nvmlInitialized)
        {
            try { nvmlShutdown(); } catch { }
            _nvmlInitialized = false;
            _nvmlDevice = IntPtr.Zero;
        }
    }

    public float GetGpuUsage(out uint gpuTemp)
    {
        gpuTemp = 0;
        if (!_nvmlAttempted) TryInitNvml();

        if (_nvmlInitialized && _nvmlDevice != IntPtr.Zero)
        {
            try
            {
                if (nvmlDeviceGetUtilizationRates(_nvmlDevice, out var rates) == 0)
                {
                    _ = nvmlDeviceGetTemperature(_nvmlDevice, 0, out gpuTemp);
                    return rates.gpu;
                }
            }
            catch { }
        }

        return GetGpuUsagePerformanceCounters();
    }

    private float GetGpuUsagePerformanceCounters()
    {
        try
        {
            int currentPid = Environment.ProcessId;

            // Rescan instances every 15 seconds or when empty
            if (_gpuCounters.Count == 0 || (DateTime.UtcNow - _lastGpuCounterScan).TotalSeconds > 15)
            {
                var category = new PerformanceCounterCategory("GPU Engine");
                var instances = category.GetInstanceNames();
                var newCounters = new List<PerformanceCounter>();

                foreach (var instance in instances)
                {
                    // Prioritize our own process's GPU engines or system 3D engines
                    if ((instance.Contains($"pid_{currentPid}_", StringComparison.OrdinalIgnoreCase) ||
                         instance.Contains("engtype_3D", StringComparison.OrdinalIgnoreCase)) &&
                        !instance.Contains("engtype_Copy", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", instance, true);
                            counter.NextValue(); // Baseline reading
                            newCounters.Add(counter);
                        }
                        catch { }
                    }
                }

                if (newCounters.Count > 0)
                {
                    DisposeGpuCounters();
                    _gpuCounters.AddRange(newCounters);
                }
                _lastGpuCounterScan = DateTime.UtcNow;
            }

            float totalUsage = 0f;
            for (int i = _gpuCounters.Count - 1; i >= 0; i--)
            {
                try
                {
                    float val = _gpuCounters[i].NextValue();
                    if (val > 0)
                    {
                        totalUsage += val;
                    }
                }
                catch
                {
                    try { _gpuCounters[i].Dispose(); } catch { }
                    _gpuCounters.RemoveAt(i);
                }
            }

            return Math.Clamp(totalUsage, 0f, 100f);
        }
        catch
        {
            return 0f;
        }
    }

    public void DisposeGpuCounters()
    {
        foreach (var c in _gpuCounters) { try { c.Dispose(); } catch { } }
        _gpuCounters.Clear();
    }

    // --- System Metrics (P/Invoke) ---

    public int GetCpuUsage()
    {
        if (!GetSystemTimes(out var idleTime, out var kernelTime, out var userTime)) return 0;

        var idle = FileTimeToUInt64(idleTime);
        var kernel = FileTimeToUInt64(kernelTime);
        var user = FileTimeToUInt64(userTime);

        if (!_cpuMetricsInitialized)
        {
            _prevIdleTime = idle; _prevKernelTime = kernel; _prevUserTime = user;
            _cpuMetricsInitialized = true;
            return 0;
        }

        var idleDiff = idle - _prevIdleTime;
        var kernelDiff = kernel - _prevKernelTime;
        var userDiff = user - _prevUserTime;
        var total = kernelDiff + userDiff;
        var usage = total > 0 ? (total - idleDiff) * 100.0 / total : 0.0;

        _prevIdleTime = idle; _prevKernelTime = kernel; _prevUserTime = user;
        return (int)Math.Clamp(usage, 0, 100);
    }

    public static int GetRamUsage(out double usedMb, out double totalMb)
    {
        var memStatus = new MEMORYSTATUSEX();
        if (GlobalMemoryStatusEx(ref memStatus))
        {
            totalMb = memStatus.ullTotalPhys / (1024.0 * 1024.0);
            usedMb = totalMb - memStatus.ullAvailPhys / (1024.0 * 1024.0);
            return (int)memStatus.dwMemoryLoad;
        }
        usedMb = 0; totalMb = 0;
        return 0;
    }

    private static ulong FileTimeToUInt64(FILETIME ft) => ((ulong)ft.dwHighDateTime << 32) | ft.dwLowDateTime;

    // --- IDisposable ---

    public void Dispose()
    {
        StopGpu();
        StopCpu();
        StopRam();
        StopDisk();
        DisposeGpuCounters();
        ShutdownNvml();
    }

    // --- P/Invoke declarations ---

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

    // Returns a pseudo-handle for the calling thread, no need to close it.
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentThread();

    // Pins the thread to the cores indicated by the bitmask (bit N = core N).
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr SetThreadAffinityMask(IntPtr hThread, IntPtr dwThreadAffinityMask);

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME { public uint dwLowDateTime; public uint dwHighDateTime; }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength; public uint dwMemoryLoad; public ulong ullTotalPhys; public ulong ullAvailPhys;
        public ulong ullTotalPageFile; public ulong ullAvailPageFile; public ulong ullTotalVirtual;
        public ulong ullAvailVirtual; public ulong ullAvailExtendedVirtual;
        public MEMORYSTATUSEX() { dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)); dwMemoryLoad = 0; ullTotalPhys = 0; ullAvailPhys = 0; ullTotalPageFile = 0; ullAvailPageFile = 0; ullTotalVirtual = 0; ullAvailVirtual = 0; ullAvailExtendedVirtual = 0; }
    }

    [DllImport("nvml.dll", EntryPoint = "nvmlInit_v2")]
    private static extern int nvmlInit_v2();

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetHandleByIndex_v2")]
    private static extern int nvmlDeviceGetHandleByIndex_v2(uint index, out IntPtr device);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetUtilizationRates")]
    private static extern int nvmlDeviceGetUtilizationRates(IntPtr device, out NVML_UTILIZATION rates);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetTemperature")]
    private static extern int nvmlDeviceGetTemperature(IntPtr device, int sensorType, out uint temp);

    [DllImport("nvml.dll", EntryPoint = "nvmlShutdown")]
    private static extern int nvmlShutdown();

    [StructLayout(LayoutKind.Sequential)]
    private struct NVML_UTILIZATION
    {
        public uint gpu;
        public uint memory;
    }
}
