// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.DotNet.Scaffolding.Internal.CliHelpers;

internal class MsBuildCliRunner
{
    private const string MsbuildCommandName = "msbuild";

    /// <summary>
    /// Executes an MSBuild command and deserializes the JSON output into the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the JSON output into.</typeparam>
    /// <param name="args">The arguments to pass to the MSBuild command.</param>
    /// <param name="projectPath">The path to the project file.</param>
    /// <returns>The deserialized object of type T, or null if deserialization fails.</returns>
    public static T? RunMSBuildCommandAndDeserialize<T>(IEnumerable<string> args, string projectPath) where T : class
    {
        try
        {
            if (RunMsBuildCommand(args, projectPath) is string stdOut)
            {
                return JsonSerializer.Deserialize<T>(stdOut);
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Runs an MSBuild command with the specified arguments and project path, and returns the output as a string.
    /// </summary>
    /// <param name="args">The collection of command-line arguments to pass to MSBuild. Each string represents a single argument.</param>
    /// <param name="projectPath">The full path to the project file to build. Cannot be null or empty.</param>
    /// <returns>A string containing the output from the MSBuild command, or null if the command fails or produces no output.</returns>
    public static string? RunMSBuildCommandAndGetOutput(IEnumerable<string> args, string projectPath)
    {
        return RunMsBuildCommand(args, projectPath);
    }

    private static string? RunMsBuildCommand(IEnumerable<string> args, string projectPath)
    {
        DotnetCliRunner runner = DotnetCliRunner.CreateDotNet(MsbuildCommandName, args.Append(projectPath));
        int exitCode = runner.ExecuteAndCaptureOutput(out string? stdOut, out string? stdErr);

        return exitCode != 0 || string.IsNullOrEmpty(stdOut) || !string.IsNullOrEmpty(stdErr) ? null : stdOut;
    }
}
