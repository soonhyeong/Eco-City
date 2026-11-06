using System;
using EcoCity.Validation;

internal static class ConsoleRunner
{
    private static int Main()
    {
        string[] failures = CoreChecks.RunAll();
        foreach (string failure in failures) Console.Error.WriteLine(failure);
        Console.WriteLine("Core checks: " + CoreChecks.CheckCount + "; failures: " + failures.Length);
        return failures.Length == 0 ? 0 : 1;
    }
}
