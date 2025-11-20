// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Provides helper methods for tool version and prerelease checks
using System.Reflection;

namespace Microsoft.DotNet.Tools.Scaffold.Helpers;

internal static class ToolHelper
{
    /// <summary>
    /// Keywords that indicate prerelease versions.
    /// </summary>
    internal static string[] PrereleaseKeywords = { "preview", "rc", "rtm", "alpha", "beta", "dev" };

    /// <summary>
    /// Gets the informational version of the currently executing assembly.
    /// </summary>
    /// <returns>The informational version string, or null if not found.</returns>
    internal static string? GetToolVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyAttr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        return assemblyAttr?.InformationalVersion;
    }

    /// <summary>
    /// Determines if the current tool version is a prerelease.
    /// </summary>
    /// <returns>True if the tool version is a prerelease; otherwise, false.</returns>
    internal static bool IsToolPrerelease()
    {
        var versionString = GetToolVersion();
        return !string.IsNullOrEmpty(versionString) && IsVersionPrerelease(versionString);
    }

    /// <summary>
    /// Determines if a version string contains prerelease keywords.
    /// </summary>
    /// <param name="versionString">The version string to check.</param>
    /// <returns>True if the version string contains prerelease keywords; otherwise, false.</returns>
    internal static bool IsVersionPrerelease(string versionString)
    {
        // Check if any prerelease keyword is in the version string (case-insensitive)
        return PrereleaseKeywords.Any(keyword => versionString.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}
