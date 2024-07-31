// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Reflection;

namespace Microsoft.DotNet.Scaffolding.CodeModification.Helpers;

internal static class ProjectModelHelper
{
    internal static Dictionary<string, string> ShortTfmDictionary = new Dictionary<string, string>()
    {
        { ".NETCoreApp,Version=v3.1", "netcoreapp3.1" },
        { ".NETCoreApp,Version=v5.0", "net5.0" },
        { ".NETCoreApp,Version=v6.0", "net6.0" },
        { ".NETCoreApp,Version=v2.1", "netcoreapp2.1" },
        { ".NETCoreApp,Version=v7.0", "net7.0" },
        { ".NETCoreApp,Version=v8.0", "net8.0" },
        { ".NETCoreApp,Version=v9.0", "net9.0" },
    };

    internal static bool IsTfmPreRelease(string tfm)
    {
        return tfm.Equals("net9.0", StringComparison.OrdinalIgnoreCase);
    }

    internal static string GetManifestResource(Assembly assembly, string shortResourceName)
    {
        string jsonText = string.Empty;
        var resourceNames = assembly.GetManifestResourceNames();
        var resourceName = resourceNames?.FirstOrDefault(x => x.EndsWith(shortResourceName));
        if (assembly != null && !string.IsNullOrEmpty(resourceName))
        {
            Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                return string.Empty;
            }

            using (StreamReader reader = new StreamReader(stream))
            {
                jsonText = reader.ReadToEnd();
            }
        }

        return jsonText;
    }
}
