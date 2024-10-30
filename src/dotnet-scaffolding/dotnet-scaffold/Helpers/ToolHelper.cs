// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;

namespace Microsoft.DotNet.Tools.Scaffold.Helpers;

internal static class ToolHelper
{
    // Define keywords for prerelease versions
    internal static string[] PrereleaseKeywords = { "preview", "rc", "rtm", "alpha", "beta", "dev" };
    internal static string? GetToolVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyAttr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        return assemblyAttr?.InformationalVersion;
    }

    internal static bool IsToolPrerelease()
    {
        var versionString = GetToolVersion();
        return !string.IsNullOrEmpty(versionString) && IsVersionPrerelease(versionString);
    }

    internal static bool IsVersionPrerelease(string versionString)
    {
        // Check if any prerelease keyword is in the version string (case-insensitive)
        return PrereleaseKeywords.Any(keyword => versionString.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}
