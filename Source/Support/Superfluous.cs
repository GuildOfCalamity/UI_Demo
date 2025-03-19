using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace UI_Demo;

public class Miscellaneous
{
    public static void PEHeaderTesting()
    {
        var mt = MachineHelper.GetDllMachineType(Path.Combine(Directory.GetCurrentDirectory(), $"{AppDomain.CurrentDomain.FriendlyName}.dll"));
        Debug.WriteLine($"[INFO] MachineType is {mt}");
        var peh = new PeHeader(Path.Combine(Directory.GetCurrentDirectory(), $"{AppDomain.CurrentDomain.FriendlyName}.dll"));
        if (!peh.Is32BitHeader)
        {
            var header = peh.OptionalHeader64;
            Debug.WriteLine($"[INFO] FileAlignment..............: {header.FileAlignment}");
            Debug.WriteLine($"[INFO] MajorOperatingSystemVersion: {header.MajorOperatingSystemVersion}");
        }
        Debug.WriteLine($"[INFO] TimeStamp..................: {peh.TimeStamp}");
        Debug.WriteLine($"[INFO] TimeStampUnix..............: {peh.TimeStampUnix}");
        
    }

    public static void TimeZoneHoursCompare()
    {
        var now = DateTime.Now;
        var utcNow = DateTime.UtcNow;

        var hoursDiff = TimeZoneInfo.Local.GetUtcOffset(now).TotalHours;

        Debug.WriteLine($"DateTime.Now.Hour ⇒ {now.Hour}");
        Debug.WriteLine($"DateTime.Now.UtcHour ⇒ {utcNow.Hour}");

        var dt = DateTime.Now.AddDays(1);
        Debug.WriteLine($" GetUtcOffset ⇒ {TimeZone.CurrentTimeZone.GetUtcOffset(dt)}"); // obsolete
        Debug.WriteLine($" GetUtcOffset ⇒ {TimeZoneInfo.Local.GetUtcOffset(dt)}");
    }

    public static void RegExGenerated()
    {
        var regex1 = RegexHelpers.WhitespaceAtLeastOnce().Replace($"–= Superfluous Testing Zone =–", "_");
        Debug.WriteLine($"{regex1}");

        var regex2 = RegexHelpers.SpaceSplit().Split($"–= Superfluous Testing Zone =–");
        foreach (var split in regex2)
        { 
            Debug.WriteLine($"{split}"); 
        }
        
        var regex3 = RegexHelpers.DriveLetter().Match("D:");
        if (regex3.Success) 
        { 
            Debug.WriteLine($"DriveLetter() is true"); 
        }
    }

    public static void SpanTesting()
    {
        // Allocate a span on the stack. 
        Span<int> snums = stackalloc int[8192];

        // Ramps quickly and then plateaus.
        for (int i = 0; i < 8192; i++)
            snums[i] = (int)Math.Floor(Math.Log(i+1, Extensions.RootTwo));

        snums.Split(snums.Length / 2, out var left, out var right);

        Debug.WriteLine($"[DEBUG] Span.LastValue={snums[snums.Length - 1]}");
        Debug.WriteLine($"[DEBUG] Span.FindLast={snums.FindLastIndexOf(17)}");
        Debug.WriteLine($"[DEBUG] Span.Median={snums.Median()}");
    }

    /// <summary>
    /// <see cref="TimeSpan"/> was updated in .NET 7.0 to include
    ///     public const long TicksPerMicrosecond = 10L;
    ///     public const double TicksPerNanosecond = 0.01D; // 100 nanoseconds per tick
    /// </summary>
    public static void TicksTesting()
    {
        Debug.WriteLine($"   TicksPerMicroSecond ⇒ {TimeSpan.TicksPerMicrosecond} ");
        Debug.WriteLine($"   TicksPerMilliSecond ⇒ {TimeSpan.TicksPerMillisecond} ");
        Debug.WriteLine($"        TicksPerSecond ⇒ {TimeSpan.TicksPerSecond} ");
        Debug.WriteLine($"        TicksPerMinute ⇒ {TimeSpan.TicksPerMinute} ");
        Debug.WriteLine($"          TicksPerHour ⇒ {TimeSpan.TicksPerHour} ");
        Debug.WriteLine($"           TicksPerDay ⇒ {TimeSpan.TicksPerDay} ");
        Debug.WriteLine($" One tick equals {TimeSpan.NanosecondsPerTick} nanoseconds");

        // TimeOnly is smaller than DateTime and only includes time functions with no date integration.
        Debug.WriteLine($"       TimeOnly.MaxValue ⇒ {TimeOnly.MaxValue}");       // 11:59 PM
        Debug.WriteLine($" TimeOnly.MaxValue.Ticks ⇒ {TimeOnly.MaxValue.Ticks}"); // 863999999999
        Debug.WriteLine($"       TimeOnly.MinValue ⇒ {TimeOnly.MinValue}");       // 12:00 AM
        Debug.WriteLine($" TimeOnly.MinValue.Ticks ⇒ {TimeOnly.MinValue.Ticks}"); // 0

        TimeOnly morning = new TimeOnly(10, 30);
        Debug.WriteLine($"The time is: {morning}"); // Output: The time is: 10:30:00
        TimeOnly afternoonTime = morning.AddHours(2);
        Debug.WriteLine($"Two hours later: {afternoonTime}"); // Output: Two hours later: 12:30:00
        bool isBetween = morning.IsBetween(new TimeOnly(9, 0, 0), new TimeOnly(12, 0, 0));
        Debug.WriteLine($"The time {(isBetween ? "is" : "is not")} between 9:00 AM and 12:00 PM"); // Output: Is the time between 9:00 AM and 12:00 PM? True
        Debug.WriteLine($"Hour: {morning.Hour}, Minute: {morning.Minute}"); // Output: Hour: 10, Minute: 30
        TimeOnly parsedTime1 = TimeOnly.Parse("14:15");
        Debug.WriteLine($"Parsed time: {parsedTime1}"); // Output: Parsed time: 2:15 PM
        TimeOnly parsedTime2 = TimeOnly.Parse("1:15 AM");
        Debug.WriteLine($"Parsed time: {parsedTime2}"); // Output: Parsed time: 1:15 AM

        var dtmilli = DateTime.Parse("0001-01-01 00:00:00.1000000").Millisecond;  // 100 milliseconds
        var dtmicro = DateTime.Parse("0001-01-01 00:00:00.0009990").Microsecond;  // 999 microseconds
        var dtnano = DateTime.Parse("0001-01-01 00:00:00.0000009").Nanosecond;    // 900 nanoseconds

        var tsmilli = TimeSpan.Parse("00:00:00.1000000").Milliseconds; // 100 milliseconds
        var tsmicro = TimeSpan.Parse("00:00:00.0009990").Microseconds; // 999 microseconds
        var tsnano = TimeSpan.Parse("00:00:00.0000009").Nanoseconds;   // 900 nanoseconds

        var ttt = Extensions.TimeToTicks(10, 30, 0);

        // Fixed rounding issues. https://github.com/dotnet/runtime/issues/66815
        var seconds = 0.9999999;
        var tsfs = TimeSpan.FromSeconds(seconds);
        var dtas = DateTime.MinValue.AddSeconds(seconds).Ticks;
        var dtoa = DateTimeOffset.MinValue.Add(tsfs).Ticks;

        var tsIsBetween = DateTime.Now.TimeOfDay.IsBetween(TimeSpan.Parse("23:00:00"), TimeSpan.Parse("02:00:00"));
        var dtIsBetween = DateTime.Now.IsBetween(DateTime.Now.AddHours(-1), DateTime.Now.AddHours(1));

        var now = Extensions.IsNowBetween("10:00:00", "12:00:00");
    }

}

/// <summary>
///   <b>Predicate<T> (Delegate)</b>
///   Represents the method that defines a set of criteria and 
///   determines whether the specified object meets those criteria.
///   https://learn.microsoft.com/en-us/dotnet/api/system.predicate-1?view=net-9.0
/// </summary>
public class PredicateTesting
{
    public static void BasicTest()
    {
        List<int> numbers = new() { 1, -2, 3, 4, 5, 6, 7, -8, 9, 10 };

        Predicate<int> isEven = (n) => n % 2 == 0; // Predicate to check even numbers
        List<int> evenNumbers = numbers.FindAll(isEven);
        Debug.WriteLine("[INFO] Even numbers: " + string.Join(", ", evenNumbers));

        Predicate<int> isNegative = n => n < 0;
        numbers.RemoveAll(isNegative);
        Debug.WriteLine("[INFO] Positive numbers: " + string.Join(", ", numbers));

        List<string> names = new() { "Steve", "Alice", "Cornholio", "Andrew" };
        Predicate<string> startsWithA = name => name.StartsWith("A");
        string? result = names.Find(startsWithA);
        Debug.WriteLine("[INFO] Result: " + result ?? "No match found");

        //Predicate<Action> predicate = action => action.Method.Name == "TestMethod";
    }

    public static void ActionTest()
    {
        List<Action> actions = new()
        {
            () => { Thread.Sleep(24);  Debug.WriteLine(" - Very Quick Task"); },
            () => { Thread.Sleep(200);  Debug.WriteLine(" - Quick Task"); },
            () => { Thread.Sleep(1000); Debug.WriteLine(" - Medium Task"); },
            () => { Thread.Sleep(2500); Debug.WriteLine(" - Slow Task"); },
        };

        Predicate<Action> isFastAction = action =>
        {
            var vsw = ValueStopwatch.StartNew();
            action?.Invoke();
            return vsw.GetElapsedTime().TotalMilliseconds <= 300;
        };

        List<Action> fastActions = actions.Where(action => isFastAction(action)).ToList();

        //Debug.WriteLine("[INFO] Only execute actions less than 300ms:");
        //foreach (var action in fastActions) { action(); }
    }

    public static void FuncTest()
    {
        List<Func<int>> functions = new()
        {
            () => 10,
            () => -5,
            () => 23,
            () => -1
        };

        Predicate<Func<int>> isPositive = func => func() > 0;

        List<Func<int>> validFunctions = functions.FindAll(isPositive);

        Debug.WriteLine("[INFO] Valid Functions Output:");
        foreach (var func in validFunctions)
        {
            Debug.WriteLine($" - {func()}");
        }
    }
}
