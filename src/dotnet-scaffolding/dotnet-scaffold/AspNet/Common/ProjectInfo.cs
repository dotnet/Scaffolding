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
    private static string? GetLowestTargetFrameworkFromCli(string projectPath)
    {
        //Should I only care about net 8 and above???? 

        var runner = DotnetCliRunner.CreateDotNet("msbuild", new[] { "-getProperty:TargetFramework;TargetFrameworks", projectPath });
        int exitCode = runner.ExecuteAndCaptureOutput(out var stdOut, out var stdErr);

        if (exitCode != 0 || string.IsNullOrEmpty(stdOut))
        {
            return null;
        }

        // Parse the JSON output
        try
        {
            var msbuildOutput = JsonSerializer.Deserialize<MsBuildPropertiesOutput>(stdOut);
            if (msbuildOutput?.Properties == null)
            {
                return null;
            }

            // If single TargetFramework is set, return it
            if (!string.IsNullOrEmpty(msbuildOutput.Properties.TargetFramework))
            {
                return msbuildOutput.Properties.TargetFramework;
            }

            // If multiple TargetFrameworks are set, find the lowest version
            if (!string.IsNullOrEmpty(msbuildOutput.Properties.TargetFrameworks))
            {
                var frameworks = msbuildOutput.Properties.TargetFrameworks
                    .Split(';')
                    .Where(x => x.StartsWith("net") || x.StartsWith("netstandard"))
                    .OrderBy(ParseFrameworkVersion)
                    .ToList();

                return frameworks.FirstOrDefault();
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }

        static Version ParseFrameworkVersion(string tfm)
        {
            // Remove "net" or "netstandard" prefix to parse the version
            string versionPart = tfm.StartsWith("netstandard") ?
                tfm.Replace("netstandard", "") :
                tfm.Replace("net", "");

            // Parse to Version; assume "0.0" for invalid formats
            return Version.TryParse(versionPart, out var version) ? version : new Version(0, 0);
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
