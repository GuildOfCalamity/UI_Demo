using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace UI_Demo;

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

        Predicate<Action> predicate = action => action.Method.Name == "TestMethod";
    }

    public static void ActionTest()
    {
        List<Action> actions = new()
    {
        () => { Thread.Sleep(200);  Debug.WriteLine(" - Quick Task"); },
        () => { Thread.Sleep(2500); Debug.WriteLine(" - Slow Task"); },
        () => { Thread.Sleep(1000); Debug.WriteLine(" - Medium Task"); }
    };

        Predicate<Action> isFastAction = action =>
        {
            Stopwatch sw = Stopwatch.StartNew();
            action?.Invoke();
            sw.Stop();
            return sw.ElapsedMilliseconds <= 300;
        };

        List<Action> fastActions = actions.Where(action => isFastAction(action)).ToList();

        Debug.WriteLine("[INFO] Executing Fast Actions:");
        foreach (var action in fastActions)
        {
            action();
        }
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
