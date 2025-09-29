// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Spectre.Console;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

internal class AzCliHelper
{
    /// <summary>
    /// Gets Azure usernames, tenant IDs, and application IDs using the Azure CLI.
    /// </summary>
    /// <param name="usernames">the user IDs</param>
    /// <param name="tenants">the tenant IDs</param>
    /// <param name="appIds">the app IDs</param>
    /// <returns>if successful, return true</returns>
    public static bool GetAzureInformation(out List<string> usernames, out List<string> tenants, out List<string> appIds)
    {
        // Create a runner to execute the 'az account list' command with json output format
        var runner = AzCliRunner.Create();

        if (EnsureUserIsLoggedIn(runner, out string? output) && !string.IsNullOrEmpty(output))
        {
            if (GetAzureUsernamesAndTenatIds(runner, output, out usernames, out tenants))
            {
                if (GetAzureAppIds(runner, out appIds))
                {
                    return true;
                }
            }
        }
        usernames = [];
        tenants = [];
        appIds = [];

        return false;
    }

    /// <summary>
    /// Ensures the user is logged into Azure CLI. If not logged in, it will prompt for login.
    /// </summary>
    /// <param name="runner">the az cli runner</param>
    /// <param name="output">the CLI output if available</param>
    /// <returns>if successful, return true</returns>
    private static bool EnsureUserIsLoggedIn(AzCliRunner runner, out string? output)
    {
        try
        {
            int exitCode = runner.RunAzCli("account list --output json", out var stdOut, out var stdErr);

            if (stdOut is not null)
            {
                var result = StringUtil.ConvertStringToArray(stdOut);
                if (result.Length is 0)
                {
                    exitCode = runner.RunAzCli("login", out stdOut, out stdErr);
                }
            }
            output = stdOut;
            return exitCode == 0 && string.IsNullOrEmpty(stdErr);
        }
        catch (Exception ex)
        {
            output = null;
            AnsiConsole.WriteLine($"Error checking Azure login status: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets Azure usernames and tenant IDs from the JSON output of 'az account list'.
    /// </summary>
    /// <param name="runner">the az cli runner</param>
    /// <param name="output">the output from the account list</param>
    /// <param name="usernames">the usernames if available</param>
    /// <param name="tenants">the tenant ids if available</param>
    /// <returns>if successful, return true</returns>
    private static bool GetAzureUsernamesAndTenatIds(AzCliRunner runner, string output, out List<string> usernames, out List<string> tenants)
    {
        usernames = [];
        tenants = [];

        try
        {
            // Parse the JSON output
            using JsonDocument doc = JsonDocument.Parse(output);
            JsonElement root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {

                foreach (JsonElement account in root.EnumerateArray())
                {
                    if (account.TryGetProperty("user", out JsonElement user) &&
                        user.TryGetProperty("name", out JsonElement name))
                    {
                        string? username = name.GetString();
                        if (!string.IsNullOrEmpty(username))
                        {
                            usernames.Add(username);
                        }
                    }

                    // Extract tenant ID from the JSON array
                    if (account.TryGetProperty("tenantId", out JsonElement tenant))
                    {
                        string? id = tenant.GetString();
                        if (!string.IsNullOrEmpty(id))
                        {
                            tenants.Add(id);
                        }
                    }
                }

            }
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteLine($"Error parsing Azure accounts JSON: {ex.Message}");
            usernames = [];
            tenants = [];
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets Azure application IDs using the Azure CLI.
    /// </summary>
    /// <param name="runner">the az cli runner</param>
    /// <param name="appIds"> the appIds</param>
    /// <returns>if successful, returns true</returns>
    private static bool GetAzureAppIds(AzCliRunner runner, out List<string> appIds)
    {
        try
        {

            appIds = [];
            var exitCode = runner.RunAzCli("ad app list --output json", out string? stdOut, out string? stdErr);

            if (exitCode == 0 && !string.IsNullOrEmpty(stdOut))
            {
                // Parse the JSON output
                using JsonDocument doc = JsonDocument.Parse(stdOut);
                JsonElement root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Array)
                {

                    foreach (JsonElement app in root.EnumerateArray())
                    {
                        if (app.TryGetProperty("appId", out JsonElement appId))
                        {
                            string? id = appId.GetString();
                            string? displayName = app.TryGetProperty("displayName", out JsonElement name) ?
                                                 name.GetString() : "Unknown App";

                            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(displayName))
                            {
                                // Format as "DisplayName (AppId)" for better user experience
                                appIds.Add($"{displayName} {id}");
                            }
                        }
                    }
                    return true;
                }
            }

            if (!string.IsNullOrEmpty(stdErr))
            {
                AnsiConsole.WriteLine($"Error executing 'az ad app list': {stdErr}");
            }
        }
        catch (Exception ex)
        {
            appIds = [];
            // Handle any exceptions, like az CLI not being installed
            AnsiConsole.WriteLine($"Error getting Azure apps: {ex.Message}");
        }
        return false;
    }
}
