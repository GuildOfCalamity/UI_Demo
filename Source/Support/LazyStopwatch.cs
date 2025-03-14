using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace UI_Demo;

public static class LazyStopwatch
{
    static readonly Lazy<Stopwatch> _stopwatch = new(() => { return Stopwatch.StartNew(); });
    public static Stopwatch Instance => _stopwatch.Value;
    public static TimeSpan Elapsed => Instance.Elapsed;
    public static string ElapsedReadable => ToHumanFriendly(Instance.Elapsed);
    public static long ElapsedMilliseconds => Instance.ElapsedMilliseconds;
    public static void Stop() => Instance.Stop();
    public static void Restart() => Instance.Restart();
    public static void Reset() => Instance.Reset();
    public static bool IsRunning => Instance.IsRunning;
    internal static string ToHumanFriendly(TimeSpan timeSpan)
    {
        if (timeSpan == TimeSpan.Zero)
            return "0 seconds";

        bool isNegative = false;
        List<string> parts = new();

        // Check for negative TimeSpan.
        if (timeSpan < TimeSpan.Zero)
        {
            isNegative = true;
            timeSpan = timeSpan.Negate(); // Make it positive for the calculations.
        }

        if (timeSpan.Days > 0)
            parts.Add($"{timeSpan.Days} day{(timeSpan.Days > 1 ? "s" : "")}");
        if (timeSpan.Hours > 0)
            parts.Add($"{timeSpan.Hours} hour{(timeSpan.Hours > 1 ? "s" : "")}");
        if (timeSpan.Minutes > 0)
            parts.Add($"{timeSpan.Minutes} minute{(timeSpan.Minutes > 1 ? "s" : "")}");
        if (timeSpan.Seconds > 0)
            parts.Add($"{timeSpan.Seconds} second{(timeSpan.Seconds > 1 ? "s" : "")}");

        // If no large amounts so far, try milliseconds.
        if (parts.Count == 0 && timeSpan.Milliseconds > 0)
            parts.Add($"{timeSpan.Milliseconds} millisecond{(timeSpan.Milliseconds > 1 ? "s" : "")}");

        // If no milliseconds, use ticks (nanoseconds).
        if (parts.Count == 0 && timeSpan.Ticks > 0)
        {
            // A tick is equal to 100 nanoseconds. While this maps well into units of time
            // such as hours and days, any periods longer than that aren't representable in
            // a succinct fashion, e.g. a month can be between 28 and 31 days, while a year
            // can contain 365 or 366 days. A decade can have between 1 and 3 leap-years,
            // depending on when you map the TimeSpan into the calendar. This is why TimeSpan
            // does not provide a "Years" property or a "Months" property.
            parts.Add($"{(timeSpan.Ticks * TimeSpan.TicksPerMicrosecond)} microsecond{((timeSpan.Ticks * TimeSpan.TicksPerMicrosecond) > 1 ? "s" : "")}");
        }

        // Join the sections with commas and "and" for the last one.
        if (parts.Count == 1)
            return isNegative ? $"Negative {parts[0]}" : parts[0];
        else if (parts.Count == 2)
            return isNegative ? $"Negative {string.Join(" and ", parts)}" : string.Join(" and ", parts);
        else
        {
            string lastPart = parts[parts.Count - 1];
            parts.RemoveAt(parts.Count - 1);
            return isNegative ? $"Negative " + string.Join(", ", parts) + " and " + lastPart : string.Join(", ", parts) + " and " + lastPart;
        }
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
