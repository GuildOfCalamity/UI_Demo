using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UI_Demo;

public static class LazyStopwatch
{
    static readonly Lazy<Stopwatch> _stopwatch = new(() => { return Stopwatch.StartNew(); });
    public static Stopwatch Instance => _stopwatch.Value;
    public static TimeSpan Elapsed => Instance.Elapsed;
    public static string ElapsedReadable => ToReadableTimeFormat(Instance.Elapsed);
    public static long ElapsedMilliseconds => Instance.ElapsedMilliseconds;
    public static void Stop() => Instance.Stop();
    public static void Restart() => Instance.Restart();
    public static void Reset() => Instance.Reset();
    public static bool IsRunning => Instance.IsRunning;
    internal static string ToReadableTimeFormat(TimeSpan span, int significantDigits = 2)
    {
        var format = $"G{significantDigits}";
        return span.TotalMilliseconds < 1000 ? span.TotalMilliseconds.ToString(format) + " milliseconds"
                : (span.TotalSeconds < 60 ? span.TotalSeconds.ToString(format) + " seconds"
                : (span.TotalMinutes < 60 ? span.TotalMinutes.ToString(format) + " minutes"
                : (span.TotalHours < 24 ? span.TotalHours.ToString(format) + " hours"
                : span.TotalDays.ToString(format) + " days")));
    }
}

public class LazyStopwatchTest
{
    public static void Run()
    {
        _ = Task.Run(() =>
        {
            _ = LazyStopwatch.Instance;
            Debug.WriteLine($"[DEBUG] Stopwatch is running: {LazyStopwatch.IsRunning}");
            Thread.Sleep(1000);
            // The first access of the stopwatch seems incorrect, the second access is correct.
            Debug.WriteLine($"[DEBUG] Elapsed timespan (after 1000ms): {LazyStopwatch.Elapsed}");
            LazyStopwatch.Restart();
            Thread.Sleep(1000);
            Debug.WriteLine($"[DEBUG] Elapsed readable (after 1000ms): {LazyStopwatch.ElapsedReadable}");
            LazyStopwatch.Stop();
        });
    }
}
