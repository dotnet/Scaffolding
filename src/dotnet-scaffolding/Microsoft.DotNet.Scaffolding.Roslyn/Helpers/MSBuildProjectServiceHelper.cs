// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Scaffolding.Roslyn.Helpers;

internal static class MSBuildProjectServiceHelper
{
    /// <summary>
    /// Method to parse version from a TFM string.
    /// Given a short tfm like "net5.0" or "netstandard2.0", it will return a Version object with just major and minor.
    /// eg, 5.0 or 2.0
    /// </summary>
    internal static Version ParseFrameworkVersion(string tfm)
    {
        // Remove "net" or "netstandard" prefix to parse the version
        string versionPart = tfm.StartsWith("netstandard") ?
            tfm.Replace("netstandard", "") :
            tfm.Replace("net", "");

        // Parse to Version; assume "0.0" for invalid formats
        return Version.TryParse(versionPart, out var version) ? version : new Version(0, 0);
    }

    /// <summary>
    /// Get the lowest target framework version from the project.
    /// </summary>
    internal static string? GetLowestTargetFramework(Build.Evaluation.Project project)
    {
        if (project is null)
        {
            return null;
        }

        // Check for TargetFramework (single TFM) and TargetFrameworks (multiple TFMs)
        var targetFramework = project.GetProperty("TargetFramework")?.EvaluatedValue;
        var targetFrameworks = project.GetProperty("TargetFrameworks")?.EvaluatedValue;
        if (!string.IsNullOrEmpty(targetFramework))
        {
            // Single TFM
            return targetFramework;
        }
        else if (!string.IsNullOrEmpty(targetFrameworks))
        {
            // Multiple TFMs: Split and find the lowest version
            var frameworks = targetFrameworks
                .Split(';')
                .Where(x => x.StartsWith("net") || x.StartsWith("netstandard")) // Filter out non-TFM values
                .OrderBy(ParseFrameworkVersion) // Sort by version to get the lowest
                .ToList();

            // Return the lowest version TFM as a short string
            return frameworks.FirstOrDefault();
        }

        // No TFM found
        return null;
    }

    internal static IEnumerable<string> GetProjectCapabilities(Build.Evaluation.Project project)
    {
        if (project is not null)
        {
            return project.GetItems("ProjectCapability").Select(i => i.EvaluatedInclude);
        }

        return [];
    }

}
