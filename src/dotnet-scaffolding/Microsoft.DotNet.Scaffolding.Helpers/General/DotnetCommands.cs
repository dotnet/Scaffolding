// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Helpers.Services;

namespace Microsoft.DotNet.Scaffolding.Helpers.General;

internal static class DotnetCommands
{
    public static void AddPackage(string packageName, ILogger logger, string? projectFile = null, string? packageVersion = null, string? tfm = null, bool includePrerelease = false)
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
            logger.LogMessage(string.Format("\nAdding package '{0}'...", packageName));

            var runner = DotnetCliRunner.CreateDotNet("add", arguments);

            // Buffer the output here because we'll only display it in the failure scenario
            var exitCode = runner.ExecuteAndCaptureOutput(out var stdOut, out var stdErr);

            if (exitCode != 0)
            {
                if (!string.IsNullOrWhiteSpace(stdOut))
                {
                    logger.LogMessage($"\n{stdOut}");
                }
                if (!string.IsNullOrWhiteSpace(stdErr))
                {
                    logger.LogMessage($"\n{stdErr}");
                }

                logger.LogMessage("Failed!");
            }
            else
            {
                logger.LogMessage("Done");
            }
        }
    }
}
