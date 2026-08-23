using System;
using System.Collections.Generic;

namespace Convergence.Content.Encounters.ThirdSeverance;

internal static class ThirdSeverancePlanCollections
{
    public static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> source, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(source, parameterName);

        var copy = new T[source.Count];
        for (int index = 0; index < source.Count; index++)
        {
            copy[index] = source[index];
        }

        return Array.AsReadOnly(copy);
    }
}
