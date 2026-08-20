// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.Helpers;

/// <summary>
/// Helpers for detecting, installing, and reporting on the 'Microsoft.dotnet-msidentity' tool
/// that Entra ID scaffolding depends on.
/// </summary>
internal static class MsIdentityToolHelper
{
    /// <summary>
    /// The NuGet package / dotnet tool id for the msidentity CLI.
    /// </summary>
    internal const string MsIdentityToolName = "Microsoft.dotnet-msidentity";

    /// <summary>
    /// The command name used to invoke the tool via 'dotnet &lt;command&gt;'.
    /// </summary>
    internal const string MsIdentityCommandName = "msidentity";

    /// <summary>
    /// A clear, actionable message explaining that the msidentity tool is missing and how to install it.
    /// </summary>
    internal static string NotInstalledMessage =>
        $"The '{MsIdentityToolName}' tool is required for Entra ID scaffolding but is not installed or could not be found. " +
        $"Install it by running 'dotnet tool install --global {MsIdentityToolName}' " +
        "(append '--prerelease' when using a prerelease build), then run the scaffolder again.";

    /// <summary>
    /// Determines whether the msidentity tool is installed globally or locally.
    /// </summary>
    internal static bool IsMsIdentityInstalled(ILogger logger)
    {
        try
        {
            // Check if the tool is installed globally
            DotnetCliRunner globalRunner = DotnetCliRunner.CreateDotNet("tool", ["list", "-g"]);
            int globalExitCode = globalRunner.ExecuteAndCaptureOutput(out string? globalStdOut, out string? _);
            if (globalExitCode == 0 && !string.IsNullOrEmpty(globalStdOut) && globalStdOut.Contains(MsIdentityToolName, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation($"{MsIdentityToolName} is already installed globally.");
                return true;
            }

            // Check if the tool is installed locally
            DotnetCliRunner localRunner = DotnetCliRunner.CreateDotNet("tool", ["list"]);
            int localExitCode = localRunner.ExecuteAndCaptureOutput(out string? localStdOut, out string? _);
            if (localExitCode == 0 && !string.IsNullOrEmpty(localStdOut) && localStdOut.Contains(MsIdentityToolName, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation($"{MsIdentityToolName} is already installed locally.");
                return true;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Exception while checking whether {MsIdentityToolName} is installed: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// Ensures the msidentity tool is installed, attempting a global install if it is missing.
    /// Emits an actionable error message when the tool cannot be installed.
    /// </summary>
    internal static bool EnsureMsIdentityIsInstalled(ILogger logger, IDictionary<string, string>? environmentVariables = null)
    {
        try
        {
            if (IsMsIdentityInstalled(logger))
            {
                return true;
            }

            logger.LogInformation($"{MsIdentityToolName} was not found. Attempting to install it...");

            // Determine if prerelease is needed
            bool isPreRelease = ToolHelper.IsToolPrerelease();

            // install the tool globally
            List<string> args = ["install", "--global", MsIdentityToolName];
            if (isPreRelease)
            {
                args.Add("--prerelease");
            }

            DotnetCliRunner runner = DotnetCliRunner.CreateDotNet("tool", [.. args], environmentVariables);
            int exitCode = runner.ExecuteAndCaptureOutput(out string? _, out string? stdErr);
            if (exitCode == 0)
            {
                logger.LogInformation($"Successfully installed {MsIdentityToolName}.");
                return true;
            }

            logger.LogError($"Failed to install {MsIdentityToolName}: {stdErr}");
            logger.LogError(NotInstalledMessage);
        }
        catch (Exception ex)
        {
            logger.LogError($"Exception while installing {MsIdentityToolName}: {ex.Message}");
            logger.LogError(NotInstalledMessage);
        }

        return false;
    }
}
