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

        for (int i = 0; i < 8192; i++)
        {
            //snums[i] = i + i + i;
            snums[i] = (int)Math.Floor(Math.Log(i, Extensions.RootTwo));
        }

        snums.Split(snums.Length / 2, out var left, out var right);

        Debug.WriteLine($"[DEBUG] Span.FindLast={snums.FindLastIndexOf(17)}");
        Debug.WriteLine($"[DEBUG] Span.Median={snums.Median()}");
    }

    public static void TicksTesting()
    {
        Debug.WriteLine($"   TicksPerMicroSecond ⇒ {TimeSpan.TicksPerMicrosecond} ");
        Debug.WriteLine($"   TicksPerMilliSecond ⇒ {TimeSpan.TicksPerMillisecond} ");
        Debug.WriteLine($"        TicksPerSecond ⇒ {TimeSpan.TicksPerSecond} ");
        Debug.WriteLine($"        TicksPerMinute ⇒ {TimeSpan.TicksPerMinute} ");
        Debug.WriteLine($"          TicksPerHour ⇒ {TimeSpan.TicksPerHour} ");
        Debug.WriteLine($"           TicksPerDay ⇒ {TimeSpan.TicksPerDay} ");
        Debug.WriteLine($" One tick equals {TimeSpan.NanosecondsPerTick} nanoseconds");
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
