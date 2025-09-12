// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.Core.CommandLine;

/// <summary>
/// Provides static methods for executing dotnet CLI commands related to scaffolding.
/// </summary>
internal static class DotnetCommands
{
    /// <summary>
    /// Adds a NuGet package to a project using the dotnet CLI.
    /// </summary>
    /// <param name="packageName">The name of the package to add.</param>
    /// <param name="logger">The logger for output.</param>
    /// <param name="projectFile">The project file to add the package to (optional).</param>
    /// <param name="packageVersion">The version of the package to add (optional).</param>
    /// <param name="tfm">The target framework (optional).</param>
    /// <param name="includePrerelease">Whether to include prerelease versions.</param>
    /// <returns>True if the package was added successfully; otherwise, false.</returns>
    public static bool AddPackage(string packageName, ILogger logger, string? projectFile = null, string? packageVersion = null, string? tfm = null, bool includePrerelease = false)
    {
        if (!string.IsNullOrEmpty(packageName))
        {
            var arguments = new List<string>();
            if (!string.IsNullOrEmpty(projectFile))
            {
                arguments.Add(projectFile);
            }

            arguments.AddRange(["package", packageName]);
            if (!string.IsNullOrEmpty(packageVersion))
            {
                arguments.AddRange(["-v", packageVersion]);
            }

            if (includePrerelease)
            {
                arguments.Add("--prerelease");
            }

            logger.LogInformation(string.Format("\nAdding package '{0}'...", packageName));
            var runner = DotnetCliRunner.CreateDotNet("add", arguments);

            // Buffer the output here because we'll only display it in the failure scenario
            var exitCode = runner.ExecuteAndCaptureOutput(out var stdOut, out var stdErr);

            if (exitCode != 0)
            {
                if (!string.IsNullOrWhiteSpace(stdOut))
                {
                    logger.LogInformation($"\n{stdOut}");
                }
                if (!string.IsNullOrWhiteSpace(stdErr))
                {
                    logger.LogInformation($"\n{stdErr}");
                }

                logger.LogInformation("Failed.");
            }
            else
            {
                logger.LogInformation("Done");
            }

            return exitCode == 0;
        }

        return false;
    }

    /// <summary>
    /// Finds project references for a given package using the dotnet CLI.
    /// </summary>
    /// <param name="packageName">The name of the package to search for.</param>
    /// <param name="logger">The logger for output.</param>
    /// <param name="projectFile">The project file to search in (optional).</param>
    /// <param name="packageVersion">The version of the package (optional).</param>
    /// <param name="tfm">The target framework (optional).</param>
    /// <param name="includePrerelease">Whether to include prerelease versions.</param>
    /// <returns>True if the command succeeded; otherwise, false.</returns>
    public static bool FindProjectReferences(string packageName, ILogger logger, string? projectFile = null, string? packageVersion = null, string? tfm = null, bool includePrerelease = false)
    {
        var arguments = new List<string>();
        if (!string.IsNullOrEmpty(projectFile))
        {
            arguments.AddRange(["--project", projectFile]);
        }

        logger.LogInformation(string.Format("\nFinding project references for package '{0}'...", packageName));
        var runner = DotnetCliRunner.CreateDotNet("reference list", arguments);

        // Buffer the output here because we'll only display it in the failure scenario
        var exitCode = runner.ExecuteAndCaptureOutput(out var stdOut, out var stdErr);

        if (exitCode != 0)
        {
            if (!string.IsNullOrWhiteSpace(stdOut))
            {
                logger.LogInformation($"\n{stdOut}");
            }
            if (!string.IsNullOrWhiteSpace(stdErr))
            {
                logger.LogInformation($"\n{stdErr}");
            }

            logger.LogInformation("Failed.");
        }
        else
        {
            logger.LogInformation("Done");
        }

        return exitCode == 0;
    }
}
