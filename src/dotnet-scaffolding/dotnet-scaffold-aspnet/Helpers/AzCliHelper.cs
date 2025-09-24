// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

internal class AzCliHelper
{
    /// <summary>
    /// Gets Azure usernames, tenant IDs, and application IDs using the Azure CLI.
    /// </summary>
    /// <returns>if successful, return true</returns>
    public static async Task<(bool success, List<string> usernames, List<string> tenants, List<string> appIds)> GetAzureInformationAsync()
    {
        // Create a runner to execute the 'az account list' command with json output format
        var runner = AzCliRunner.Create();

        List<string> usernames = [];
        List<string> tenants = [];
        List<string> appIds = [];

        (bool exitCode, string? output) = await EnsureUserIsLoggedInAsync(runner);

        if (exitCode && !string.IsNullOrEmpty(output))
        {
            if (GetAzureUsernamesAndTenatIds(runner, output, out usernames, out tenants))
            {
                (bool canGetAppIds, appIds) = await GetAzureAppIdsAsync(runner);
                if (canGetAppIds)
                {
                    return (true, usernames, tenants, appIds);
                }
            }
        }
        return (false, usernames, tenants, appIds);
    }

    /// <summary>
    /// Ensures the user is logged into Azure CLI. If not logged in, it will prompt for login.
    /// </summary>
    /// <param name="runner">the az cli runner</param>
    /// <returns>if successful, return true</returns>
    private static async Task<(bool exitCode, string? output)> EnsureUserIsLoggedInAsync(AzCliRunner runner)
    {
        try
        {
            (int exitCode, string? stdOut, string? stdErr) = await runner.RunAzCliAsync("account list --output json");

            if (stdOut is not null)
            {
                var result = StringUtil.ConvertStringToArray(stdOut);
                if (result.Length is 0)
                {
                    (exitCode, stdOut, stdErr) = await runner.RunAzCliAsync("login");
                }
            }
            return (exitCode == 0 && string.IsNullOrEmpty(stdErr) , stdOut);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking Azure login status: {ex.Message}");
            return (false, null);
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
            Console.WriteLine($"Error parsing Azure accounts JSON: {ex.Message}");
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
    /// <returns>if successful, returns true</returns>
    private static async Task<(bool success, List<string> appIds)> GetAzureAppIdsAsync(AzCliRunner runner)
    {
        List<string> appIds = [];
        try
        {
            
            (int exitCode, string? stdOut, string? stdErr) = await runner.RunAzCliAsync("ad app list --output json");

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
                    return (true, appIds);
                }
            }

            if (!string.IsNullOrEmpty(stdErr))
            {
                Console.WriteLine($"Error executing 'az ad app list': {stdErr}");
            }
        }
        catch (Exception ex)
        {
            // Handle any exceptions, like az CLI not being installed
            Console.WriteLine($"Error getting Azure apps: {ex.Message}");
        }
        return (false, appIds);
    }
}
