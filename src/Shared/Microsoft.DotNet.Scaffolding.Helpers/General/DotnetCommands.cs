// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Helpers.Services;

namespace Microsoft.DotNet.Scaffolding.Helpers.General;

public static class DotnetCommands
{
    public static void AddPackage(string packageName, ILogger logger, string? projectFile = null, string? packageVersion = null, string? tfm = null)
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

            arguments.Add("--prerelease");
            logger.LogMessage(string.Format("\nAdding package '{0}'", packageName));

            var runner = DotnetCliRunner.CreateDotNet("add", arguments);
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
                logger.LogMessage("Success!");
            }
        }
    }
}
