// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Tools.Scaffold;

internal static class CliHelpers
{
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
