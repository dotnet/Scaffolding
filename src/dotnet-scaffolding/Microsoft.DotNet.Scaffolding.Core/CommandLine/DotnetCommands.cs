// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.Core.CommandLine;

internal static class DotnetCommands
{
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

                logger.LogInformation("Failed!");
            }
            else
            {
                logger.LogInformation("Done");
            }

            return exitCode == 0;
        }

        return false;
    }
}
