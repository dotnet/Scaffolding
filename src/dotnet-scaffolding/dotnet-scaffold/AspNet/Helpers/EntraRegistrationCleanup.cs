// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

internal static class EntraRegistrationCleanup
{
    internal const string CreatedClientIdKey = "EntraCreatedClientId";

    internal static void TryCleanup(ScaffolderContext context, string? tenantId, string? username, ILogger logger)
        => TryCleanup(context, tenantId, username, logger, args =>
        {
            var runner = DotnetCliRunner.CreateDotNet("msidentity", args);
            var exitCode = runner.ExecuteAndCaptureOutput(out _, out var error);
            return (exitCode, error);
        });

    internal static void TryCleanup(ScaffolderContext context, string? tenantId, string? username, ILogger logger, Func<string[], (int ExitCode, string? Error)> execute)
    {
        if (!context.Properties.TryGetValue(CreatedClientIdKey, out var value) || value is not string clientId)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(username))
        {
            logger.LogError("Cannot safely remove application {ClientId} without the selected tenant and username. Remove it manually before retrying.", clientId);
            return;
        }

        try
        {
            logger.LogInformation("Removing application {ClientId} created during this run from tenant {TenantId}.", clientId, tenantId);
            var (exitCode, error) = execute(
                ["--unregister-app", "--client-id", clientId, "--tenant-id", tenantId,
                 "--username", username]);
            if (exitCode != 0)
            {
                logger.LogError("Could not remove application {ClientId} created during this run. Remove it from tenant {TenantId} before retrying. Cleanup exited with code {ExitCode}. MSIdentity error: {Error}",
                    clientId, tenantId, exitCode,
                    string.IsNullOrWhiteSpace(error) ? "No standard-error details were returned." : error);
                return;
            }

            context.Properties.Remove(CreatedClientIdKey);
            context.Properties.Remove("ClientId");
            context.Properties.Remove("ClientSecret");
            logger.LogInformation("Removed application {ClientId} created during the failed registration.", clientId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not remove application {ClientId} created during this run. Remove it from tenant {TenantId} before retrying.",
                clientId, tenantId);
        }
    }
}
