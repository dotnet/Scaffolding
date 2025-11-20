// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Provides helper methods for parsing ILookup collections for CLI scenarios.
namespace Microsoft.DotNet.Tools.Scaffold.Helpers;

internal static class CliHelpers
{
    /// <summary>
    /// Converts an <see cref="ILookup{TKey, TElement}"/> to a dictionary of string keys and list of string values, omitting null elements.
    /// </summary>
    /// <param name="lookup">The lookup to parse.</param>
    /// <returns>A dictionary mapping each key to a list of non-null string values.</returns>
    public static IDictionary<string, List<string>> ParseILookup(ILookup<string, string?> lookup)
    {
        ArgumentNullException.ThrowIfNull(lookup);
        var parsedDict = new Dictionary<string, List<string>>();
        foreach (var group in lookup)
        {
            var valList = new List<string>();
            var key = group.Key;
            foreach (var element in group)
            {
                if (element != null)
                {
                    valList.Add(element);
                }
            }

            parsedDict.Add(key, valList);
        }

        return parsedDict;
    }
}
