using System.Collections.Concurrent;
using System.Diagnostics;

namespace RyTuneX.Helpers;

// Central manager to register CancellationTokenSource instances so they can be cancelled
internal static class OperationCancellationManager
{
    private static readonly ConcurrentDictionary<Guid, CancellationTokenSource> _ctsMap = new();
    private static int _explicitOperationCount = 0;

    public static Guid Register(CancellationTokenSource cts)
    {
        if (cts == null) throw new ArgumentNullException(nameof(cts));
        var id = Guid.NewGuid();
        _ctsMap[id] = cts;
        return id;
    }

    public static void Unregister(Guid id)
    {
        if (_ctsMap.TryRemove(id, out var _)) { }
    }

    public static void CancelAll()
    {
        _ = LogHelper.Log($"Cancelling all {_ctsMap.Count} registered operations");
        foreach (var kvp in _ctsMap)
        {
            try
            {
                kvp.Value.Cancel();
            }
            catch { }
        }
    }

    public static int Count => _ctsMap.Count;

    // Explicitly mark that a critical operation is in progress
    public static void EnterOperation()
    {
        Interlocked.Increment(ref _explicitOperationCount);
    }

    public static void ExitOperation()
    {
        Interlocked.Decrement(ref _explicitOperationCount);
        if (_explicitOperationCount < 0) Interlocked.Exchange(ref _explicitOperationCount, 0);
    }

    public static bool HasExplicitOperations => _explicitOperationCount > 0;
    // Returns true if no pending operations after waiting and returns false on timeout.
    public static async Task<bool> WaitForPendingOperationsAsync(TimeSpan? timeout = null)
    {
        // If timeout is null, wait indefinitely until there are no registered CTS and the toggle queue is empty.
        var sw = Stopwatch.StartNew();
        var hasTimeout = timeout.HasValue;
        var to = timeout ?? TimeSpan.Zero;

        while (_ctsMap.Count > 0 || OptimizationOptions.HasPendingToggleOperations)
        {
            if (hasTimeout && sw.Elapsed >= to)
                break;

            await Task.Delay(250).ConfigureAwait(false);
        }

        return _ctsMap.Count == 0 && !OptimizationOptions.HasPendingToggleOperations;
    }
}
