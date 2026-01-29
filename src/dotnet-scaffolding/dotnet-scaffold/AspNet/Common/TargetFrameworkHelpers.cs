// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

internal class TargetFrameworkHelpers
{   
    /// <summary>
    /// Gets the target framework enum for the specified project file. Returns null if no compatible target
    /// framework is found.
    /// </summary>
    /// <param name="projectPath">The full path to the project file to evaluate. Cannot be null or empty.</param>
    /// <returns>The target framework enum representing the lowest compatible framework, or null if no compatible
    /// framework is found.</returns>
    internal static TargetFramework? GetTargetFrameworkForProject(string projectPath)
    {
        string? lowestCompatibleTfm = GetLowestCompatibleTargetFramework(projectPath);
        if (lowestCompatibleTfm is not null)
        {
            return GetTargetFrameworkEnum(lowestCompatibleTfm);
        }
        return null;
    }

    /// <summary>
    /// Determines the lowest compatible target framework for the specified project file. Returns null if there are any incompatible target frameworks.
    /// </summary>
    /// <remarks>If the project specifies multiple target frameworks, the method evaluates each and returns
    /// the lowest version that is compatible. If none are compatible, or if the project does not specify any target
    /// frameworks, the method returns null.</remarks>
    /// <param name="projectPath">The full path to the project file to evaluate. Cannot be null or empty.</param>
    /// <returns>The target framework moniker (TFM) string representing the lowest compatible framework, or null if no compatible
    /// framework is found.</returns>
    private static string? GetLowestCompatibleTargetFramework(string projectPath)
    {
        MsBuildPropertiesOutput? msbuildOutput = MsBuildCliRunner.RunMSBuildCommandAndDeserialize<MsBuildPropertiesOutput>(["-getProperty:TargetFramework;TargetFrameworks"], projectPath);
        if (msbuildOutput?.Properties is null)
        {
            return null;
        }

        // If a single TargetFramework is set, validate its compatiblity and return it
        if (!string.IsNullOrEmpty(msbuildOutput.Properties.TargetFramework))
        {
            string tfm = msbuildOutput.Properties.TargetFramework;
            if (IsCompatibleFramework(tfm, projectPath, out _))
            {
                return tfm;
            }
            return null;
        }

        // If multiple TargetFrameworks are set, find the lowest compatible version, if there isn't any incompatible ones, return null
        if (!string.IsNullOrEmpty(msbuildOutput.Properties.TargetFrameworks))
        {
            string[] frameworks = msbuildOutput.Properties.TargetFrameworks.Split(';');
            List<(string tfm, Version version)>? compatibleFrameworks = GetCompatibleFrameworks(frameworks, projectPath);

            if (compatibleFrameworks is not null)
            {
                return GetLowestCompatibleFramework(compatibleFrameworks);
            }
        }

        return null;
    }

    /// <summary>
    /// Determines which target frameworks from the specified list are compatible with the given project and returns
    /// their identifiers and versions. If any framework is found to be incompatible, the method returns null.
    /// </summary>
    /// <param name="frameworks">An array of target framework monikers (TFMs) to check for compatibility with the project. Each element should be
    /// a valid TFM string.</param>
    /// <param name="projectPath">The file path to the project whose compatibility with the specified frameworks is to be evaluated. Must not be
    /// null or empty.</param>
    /// <returns>A list of tuples containing the TFM and its corresponding version for each compatible framework. Returns null if
    /// any framework is incompatible or if no compatible frameworks are found.</returns>
    private static List<(string tfm, Version version)>? GetCompatibleFrameworks(string[] frameworks, string projectPath)
    {
        List<(string tfm, Version version)> targetFrameworks = [];
        foreach (string tfm in frameworks)
        {
            if (IsCompatibleFramework(tfm, projectPath, out Version? frameworkVersion))
            {
                if (frameworkVersion is not null)
                {
                    targetFrameworks.Add((tfm, frameworkVersion));
                }
            }
            else
            {
                // If any framework is incompatible, return null
                return null;
            }
        }

        if (targetFrameworks.Count == 0)
        {
            return null;
        }

        return targetFrameworks;
    }

    /// <summary>
    /// Determines whether the specified target framework moniker (TFM) represents a .NET Core application with a
    /// version of 8.0 or higher.
    /// </summary>
    /// <remarks>This method checks the project's target framework by invoking MSBuild and analyzing the
    /// framework identifier and version. Only .NET Core applications with a version of 8.0 or higher are considered
    /// compatible.</remarks>
    /// <param name="tfm">The target framework moniker (TFM) to evaluate, such as "net8.0".</param>
    /// <param name="projectPath">The full path to the project file to use when evaluating the framework.</param>
    /// <param name="frameworkVersion">Outputs the version of the framework if compatible; otherwise, null.</param>
    /// <returns>true if the TFM corresponds to .NET Core (netcoreapp) version 8.0 or higher; otherwise, false.</returns>
    private static bool IsCompatibleFramework(string tfm, string projectPath, out Version? frameworkVersion)
    {
        frameworkVersion = null;
        MsBuildFrameworkOutput? frameworkOutput = MsBuildCliRunner.RunMSBuildCommandAndDeserialize<MsBuildFrameworkOutput>([$"-p:TargetFramework=\"{tfm}\"",
                "-getProperty:TargetFrameworkIdentifier;TargetFrameworkVersion"],
            projectPath);

        if (frameworkOutput?.Properties?.TargetFrameworkIdentifier is not null &&
                string.Equals(frameworkOutput.Properties.TargetFrameworkIdentifier, TargetFrameworkConstants.NetCoreApp, StringComparison.OrdinalIgnoreCase))
        {
            if (frameworkOutput.Properties.TargetFrameworkVersion is not null)
            {
                string version = frameworkOutput.Properties.TargetFrameworkVersion.TrimStart('v');
                if (Version.TryParse(version, out Version? tfmVersion))
                {
                    frameworkVersion = tfmVersion;
                    return tfmVersion.Major >= 8;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Determines the target framework moniker (TFM) with the lowest version from the provided list of frameworks.
    /// </summary>
    /// <param name="frameworks">A list of tuples, each containing a target framework moniker (TFM) and its associated version. Cannot be null.</param>
    /// <returns>The TFM string corresponding to the lowest version in the list, or null if the list is empty.</returns>
    private static string? GetLowestCompatibleFramework(List<(string tfm, Version version)> frameworks)
    {
        return frameworks
            .OrderBy(f => f.version)
            .Select(f => f.tfm)
            .FirstOrDefault();
    }

    /// <summary>
    /// Maps a target framework moniker (TFM) string to its corresponding TargetFramework enum value.
    /// </summary>
    /// <param name="tfm">The target framework moniker string to map, such as "net8.0".</param>
    /// <returns>The corresponding TargetFramework enum value if the mapping exists; otherwise, null.</returns>
    private static TargetFramework? GetTargetFrameworkEnum(string tfm)
    {
        if (TargetFrameworkConstants.TargetFrameworkMapping.TryGetValue(tfm, out TargetFramework framework))
        {
            return framework;
        }
        return null;
    }

    /// <summary>
    /// Represents the output from dotnet msbuild -getProperty command.
    /// </summary>
    private class MsBuildPropertiesOutput
    {
        public MsBuildProperties? Properties { get; set; }
    }

    /// <summary>
    /// Represents the properties returned from dotnet msbuild -getProperty command.
    /// </summary>
    private class MsBuildProperties
    {
        public string? TargetFramework { get; set; }
        public string? TargetFrameworks { get; set; }
    }

    /// <summary>
    /// Represents the output from dotnet msbuild -getProperty command for framework identifiers.
    /// </summary>
    private class MsBuildFrameworkOutput
    {
        public MsBuildFrameworkProperties? Properties { get; set; }
    }

    /// <summary>
    /// Represents the framework properties returned from dotnet msbuild -getProperty command.
    /// </summary>
    private class MsBuildFrameworkProperties
    {
        public string? TargetFrameworkIdentifier { get; set; }
        public string? TargetFrameworkVersion { get; set; }
    }
}
