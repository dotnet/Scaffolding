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
            var runner = DotnetCliRunner.CreateDotNet(MsbuildCommandName, args.Append(projectPath));
            int exitCode = runner.ExecuteAndCaptureOutput(out var stdOut, out var stdErr);

            if (exitCode != 0 || string.IsNullOrEmpty(stdOut))
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(stdOut);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}