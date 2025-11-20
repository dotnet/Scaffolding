// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

internal static class AzCliHelper
{
    private const string usernamesPropertyKey = "entraIdUsernames";
    private const string tenantsPropertyKey = "entraIdTenants";
    private const string appIdsPropertyKey = "entraIdAppIds";
    private const string azCliErrorsPropertyKey = "entraIdAzCliErrors";

    /// <summary>
    /// Get the usernames from Azure AD dynamically
    /// </summary>
    public static List<string> GetUsernameParameterValuesDynamically(IFlowContext context)
    {
        return GetParameterValue(context, usernamesPropertyKey);
    }

    /// <summary>
    ///  Get the tenant IDs from Azure AD dynamically
    /// </summary>
    public static List<string> GetTenantParameterValuesDynamically(IFlowContext context)
    {
        return GetParameterValue(context, tenantsPropertyKey);
    }

    /// <summary>
    /// Get the application IDs from Azure AD dynamically
    /// </summary>
    public static List<string> GetAppIdParameterValuesDynamically(IFlowContext context)
    {
        return GetParameterValue(context, appIdsPropertyKey);
    }

    /// <summary>
    /// Get the errors from the Az CLI if any
    /// </summary>
    public static string? GetAzCliErrors(IFlowContext context)
    {
        return context.GetValue<string>(azCliErrorsPropertyKey) ?? null;
    }

    private static List<string> GetParameterValue(IFlowContext context, string propertyKey)
    {
        List<string>? values = context.GetValue<List<string>>(propertyKey);
        if (values is not null)
        {
            return values;
        }
        // Trigger retrieval if not already done
        SetAzureProperties(context);

        values = context.GetValue<List<string>>(propertyKey) ?? [];

        return values;
    }

    /// <summary>
    /// Sets Azure-related properties in the specified flow context, including usernames, tenant IDs, application IDs,
    /// and any Azure CLI error messages.
    /// </summary>
    /// <remarks>This method updates the context with information retrieved from Azure, making these
    /// properties available for subsequent operations within the flow. If Azure CLI errors are encountered, the error
    /// message is also set in the context.</remarks>
    /// <param name="context">The flow context in which the Azure properties will be set. Cannot be null.</param>
    private static void SetAzureProperties(IFlowContext context)
    {
        GetAzureInformation(out List<string> usernamesResult, out List<string> tenantsResult, out List<string> appIdsResult, out string? azCliErrors);

        if (!string.IsNullOrEmpty(azCliErrors))
        {
            context.Set(azCliErrorsPropertyKey, azCliErrors);
        }

        context.Set(usernamesPropertyKey, usernamesResult);
        context.Set(tenantsPropertyKey, tenantsResult);
        context.Set(appIdsPropertyKey, appIdsResult);

        static void GetAzureInformation(out List<string> usernames, out List<string> tenants, out List<string> appIds, out string? azCliErrors)
        {
            // Create a runner to execute the 'az account list' command with json output format
            var runner = AzCliRunner.Create();

            if (EnsureUserIsLoggedIn(runner, out string? output, out string? loginErrors) && !string.IsNullOrEmpty(output))
            {
                if (GetAzureUsernamesAndTenatIds(output, out usernames, out tenants, out string? usernameTenantError))
                {
                    if (GetAzureAppIds(runner, out appIds, out string? appErrors))
                    {
                        azCliErrors = null;
                        return;
                    }
                    else
                    {
                        azCliErrors = appErrors;
                    }
                }
                else
                {
                    azCliErrors = usernameTenantError;
                }
            }
            else
            {
                azCliErrors = loginErrors;
            }
            usernames = [];
            tenants = [];
            appIds = [];
        }
    }

    /// <summary>
    /// Ensures the user is logged into Azure CLI. If not logged in, it will prompt for login.
    /// </summary>
    /// <param name="runner">the az cli runner</param>
    /// <param name="output">the CLI output if available</param>
    /// <param name="failingCommand">the login error if any</param>
    /// <returns>if successful, return true</returns>
    private static bool EnsureUserIsLoggedIn(AzCliRunner runner, out string? output, out string? failingCommand)
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
            failingCommand = string.IsNullOrEmpty(stdErr) ? null : $"az account list";
            return exitCode == 0 && string.IsNullOrEmpty(stdErr);
        }
        catch (Exception)
        {
            output = null;
            failingCommand = $"az account list";
            return false;
        }
    }

    /// <summary>
    /// Gets Azure usernames and tenant IDs from the JSON output of 'az account list'.
    /// </summary>
    /// <param name="output">the output from the account list</param>
    /// <param name="usernames">the usernames if available</param>
    /// <param name="tenants">the tenant ids if available</param>
    /// <param name="usernameTenantError">the error retrieving the username and tenants if any</param>
    /// <returns>if successful, return true</returns>
    private static bool GetAzureUsernamesAndTenatIds(string output, out List<string> usernames, out List<string> tenants, out string? usernameTenantError)
    {
        tenants = [];
        HashSet<string> uniqueUsernames = [];

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
                            uniqueUsernames.Add(username);
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
            usernames = [.. uniqueUsernames];
        }
        catch (Exception ex)
        {
            usernames = [];
            tenants = [];
            usernameTenantError = $"Error parsing Azure accounts JSON: {ex.Message}";
            return false;
        }

        usernameTenantError = null;
        return true;
    }

    /// <summary>
    /// Gets Azure application IDs using the Azure CLI.
    /// </summary>
    /// <param name="runner">the az cli runner</param>
    /// <param name="appIds"> the appIds</param>
    /// <param name="failingCommand">the error retrieving the app IDs if any</param>
    /// <returns>if successful, returns true</returns>
    private static bool GetAzureAppIds(AzCliRunner runner, out List<string> appIds, out string? failingCommand)
    {
        try
        {
            failingCommand = null;
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
                failingCommand = $"az ad app list";
            }
        }
        catch (Exception ex)
        {
            appIds = [];
            failingCommand = ex.Message;
        }

        failingCommand ??= "Error getting Azure apps";
        return false;
    }
}
