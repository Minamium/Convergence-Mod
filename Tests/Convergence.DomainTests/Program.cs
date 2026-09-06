using System;
using System.Linq;
using System.Reflection;

namespace Convergence.DomainTests;

[AttributeUsage(AttributeTargets.Method)]
internal sealed class DomainTestAttribute(string name) : Attribute
{
    internal string Name { get; } = name;
}

internal static partial class Program
{
    private static int Main(string[] args)
    {
        bool list = args.Length == 1 && args[0] == "--list";
        string? filter = args.Length == 2 && args[0] == "--filter" ? args[1] : null;
        if (args.Length != 0 && !list && filter is null)
        {
            Console.Error.WriteLine("Usage: domain tests [--list | --filter <name substring>]");
            return 2;
        }
        var tests = typeof(Program).Assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Select(method => (Method: method, Attribute: method.GetCustomAttribute<DomainTestAttribute>()))
            .Where(entry => entry.Attribute is not null)
            .Select(entry => (Name: entry.Attribute!.Name, Body: entry.Method.CreateDelegate<Action>()))
            .OrderBy(test => test.Name, StringComparer.Ordinal)
            .ToArray();
        if (tests.Length == 0 || tests.Select(test => test.Name).Distinct(StringComparer.Ordinal).Count() != tests.Length)
            throw new InvalidOperationException("Test discovery is empty or has duplicate names.");
        if (filter is not null) tests = tests.Where(test => test.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (tests.Length == 0)
        {
            Console.Error.WriteLine("No matching tests; no verification was performed.");
            return 2;
        }

        int failures = 0;
        foreach (var test in tests)
        {
            if (list) { Console.WriteLine(test.Name); continue; }
            try { test.Body(); Console.WriteLine($"PASS {test.Name}"); }
            catch (Exception exception)
            {
                failures++;
                Console.Error.WriteLine($"FAIL {test.Name}: {exception.Message}");
            }
        }
        if (!list) Console.WriteLine($"Executed {tests.Length} deterministic domain tests; failures: {failures}.");
        return failures == 0 ? 0 : 1;
    }
}
