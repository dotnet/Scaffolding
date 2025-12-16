// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text.Json;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

/// <summary>
/// Represents information about a project, including its path, code service, target framework, and capabilities.
/// </summary>
internal class ProjectInfo
{
    public ProjectInfo(string? projectPath)
    {
        ProjectPath = projectPath;
        LowestTargetFramework = projectPath is not null ? GetLowestTargetFrameworkFromCli(projectPath) : null;
    }

    /// <summary>
    /// Gets or sets the path to the project file.
    /// </summary>
    public string? ProjectPath { get; }
    /// <summary>
    /// Gets or sets the code service for the project.
    /// </summary>
    public CodeService? CodeService { get; set; }
    /// <summary>
    /// Gets or sets the list of code change options for the project.
    /// </summary>
    public IList<string>? CodeChangeOptions { get; set; }
    /// <summary>
    /// Gets or sets the lowest target framework for the project (if multiple are found).
    /// </summary>
    public string? LowestTargetFramework { get; }
    /// <summary>
    /// Gets or sets the list of project capabilities.
    /// </summary>
    public IList<string>? Capabilities { get; set; }

    /// <summary>
    /// Gets the lowest target framework from the project using dotnet msbuild CLI command.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <returns>The lowest target framework moniker (TFM) as a string, or null if not found.</returns>
    internal static string? GetLowestTargetFrameworkFromCli(string projectPath)
    {
        try
        {
            MsBuildPropertiesOutput? msbuildOutput = MsBuildCliRunner.RunMSBuildCommandAndDeserialize<MsBuildPropertiesOutput>(["-getProperty:TargetFramework;TargetFrameworks"], projectPath);
            if (msbuildOutput?.Properties is null)
            {
                return null;
            }

            // If a single TargetFramework is set, return it
            if (!string.IsNullOrEmpty(msbuildOutput.Properties.TargetFramework))
            {
                return msbuildOutput.Properties.TargetFramework;
            }

            // If multiple TargetFrameworks are set, find the lowest version
            if (!string.IsNullOrEmpty(msbuildOutput.Properties.TargetFrameworks))
            {
                string[] frameworks = msbuildOutput.Properties.TargetFrameworks.Split(';');
                string? lowestFramework = GetLowestFromFrameworks(frameworks, projectPath);

                return lowestFramework;
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }

        static string? GetLowestFromFrameworks(string[] frameworks, string projectPath)
        {
            List<(string tfm, Version version)> targetFrameworks = [];
            foreach (string tfm in frameworks)
            {
                try
                {
                    if (MsBuildCliRunner.RunMSBuildCommandAndGetOutput([$"-p:TargetFramework=\"{tfm}\"",
                        "-getProperty:TargetFrameworkVersion"],
                        projectPath) is string frameworkOutput)
                    {
                        string version = frameworkOutput.TrimStart('v');
                        if (Version.TryParse(version, out var tfmVersion))
                        {
                            targetFrameworks.Add((tfm, tfmVersion));
                        }
                    }
                }
                catch (JsonException)
                {
                    continue;
                }
            }

            if (targetFrameworks.Count == 0)
            {
                return null;
            }

            return targetFrameworks
                .OrderBy(f => f.version)
                .Select(f => f.tfm)
                .FirstOrDefault();
        }
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
}
