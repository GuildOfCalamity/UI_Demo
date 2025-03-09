using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace UI_Demo;

public class LazyCache<T>
{
    Lazy<T> _cache;
    readonly Func<T> _valueFactory;
    readonly object _lock = new(); // Thread-safety

    public LazyCache(Func<T> valueFactory)
    {
        _valueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));
        _cache = new Lazy<T>(_valueFactory, isThreadSafe: true);
    }

    /// <summary>
    /// Returns the cached value (or creates on first access).
    /// </summary>
    public T Value => _cache.Value;

    /// <summary>
    /// Refreshes the cache with a new instance.
    /// </summary>
    public void Refresh()
    {
        lock (_lock)
        {
            _cache = new Lazy<T>(_valueFactory, isThreadSafe: true);
        }
    }

    /// <summary>
    /// Clears the cache (next access triggers recreation).
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _cache = new Lazy<T>(() => throw new InvalidOperationException("Cache cleared. Access requires Refresh()."), isThreadSafe: true);
        }
    }

    /// <summary>
    /// Checks if the value is already created.
    /// </summary>
    public bool IsValueCreated => _cache.IsValueCreated;
}

public class LazyCacheTest
{
    public static async Task RunAsync()
    {
        var cache = new LazyCache<string>(() =>
        {
            Debug.WriteLine($"[INFO] Expensive operation running (tid={Thread.CurrentThread.ManagedThreadId})");
            System.Threading.Thread.Sleep(1000);
            return $"Data generated at {DateTime.Now.ToString("hh:mm:ss.fff tt")}";
        });

        Debug.WriteLine("[INFO] Cache created — no expensive operation yet.");

        // Parallel requests for the cached value (only one thread initializes it)
        await Task.WhenAll(
            Task.Run(() => Debug.WriteLine($"[INFO] Thread 1 Value: {cache.Value} (tid={Task.CurrentId})")),
            Task.Run(() => Debug.WriteLine($"[INFO] Thread 2 Value: {cache.Value} (tid={Task.CurrentId})")),
            Task.Run(() => Debug.WriteLine($"[INFO] Thread 3 Value: {cache.Value} (tid={Task.CurrentId})")),
            Task.Run(() => Debug.WriteLine($"[INFO] Thread 4 Value: {cache.Value} (tid={Task.CurrentId})"))
        );

        // Access again — no recreation
        Debug.WriteLine($"[INFO] Cached Value (again): {cache.Value}");

        // Refreshing the cache to force regeneration
        cache.Refresh();
        Debug.WriteLine($"[INFO] After Refresh: {cache.Value}");

        // Clearing the cache invalidates it
        cache.Clear();
        try { Debug.WriteLine($"[INFO] After Clear: {cache.Value}"); }
        catch (Exception ex) { Debug.WriteLine($"[WARNING] Exception caught: {ex.Message}"); }
    }
}
