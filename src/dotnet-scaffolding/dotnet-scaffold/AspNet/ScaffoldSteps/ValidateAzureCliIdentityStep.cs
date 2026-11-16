// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

internal class ValidateAzureCliIdentityStep(ILogger<ValidateAzureCliIdentityStep> logger) : ScaffoldStep
{
    public required string TenantId { get; set; }
    public required string Username { get; set; }

    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ValidateIdentity(TenantId, Username, logger, ReadAccount));
    }

    internal static bool ValidateIdentity(string? tenantId, string? username, ILogger logger,
        Func<(int ExitCode, string? Output, string? Error)> readAccount,
        Func<string, (int ExitCode, string? Output, string? Error)>? readTenantIssuer = null)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(username))
        {
            logger.LogError("A tenant ID and username are required to validate the Azure CLI identity.");
            return false;
        }

        var (exitCode, output, error) = readAccount();
        if (exitCode == 0 && !string.IsNullOrWhiteSpace(output))
        {
            try
            {
                using var document = JsonDocument.Parse(output);
                var account = document.RootElement;
                if (account.ValueKind == JsonValueKind.Object &&
                    account.TryGetProperty("tenantId", out var tenant) &&
                    tenant.ValueKind == JsonValueKind.String &&
                    account.TryGetProperty("user", out var user) &&
                    user.ValueKind == JsonValueKind.Object &&
                    user.TryGetProperty("type", out var type) &&
                    type.ValueKind == JsonValueKind.String &&
                    string.Equals(type.GetString(), "user", StringComparison.OrdinalIgnoreCase) &&
                    user.TryGetProperty("name", out var name) &&
                    name.ValueKind == JsonValueKind.String &&
                    string.Equals(name.GetString(), username, StringComparison.OrdinalIgnoreCase) &&
                    TenantMatches(tenantId, tenant.GetString(), logger, readTenantIssuer ?? ReadTenantIssuer))
                {
                    return true;
                }
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Unable to parse the Azure CLI account.");
            }
        }

        logger.LogError(
            "The Azure CLI active tenant and user must match the selected Entra ID identity before scaffolding. " +
            "Run 'az login --tenant {TenantId} --allow-no-subscriptions', sign in as {Username}, " +
            "and verify 'az account show' before retrying. Azure CLI error: {Error}",
            tenantId, username, error);
        return false;
    }

    private static bool TenantMatches(string selectedTenant, string? activeTenant, ILogger logger,
        Func<string, (int ExitCode, string? Output, string? Error)> readTenantIssuer)
    {
        if (string.Equals(selectedTenant, activeTenant, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        if (Guid.TryParse(selectedTenant, out var selectedId))
        {
            return Guid.TryParse(activeTenant, out var activeId) && selectedId == activeId;
        }
        if (!selectedTenant.Contains('.') || Uri.CheckHostName(selectedTenant) != UriHostNameType.Dns)
        {
            return false;
        }

        var (exitCode, output, error) = readTenantIssuer(selectedTenant);
        if (exitCode == 0 && Uri.TryCreate(output?.Trim(), UriKind.Absolute, out var issuer) &&
            issuer.Scheme == Uri.UriSchemeHttps &&
            issuer.Authority.Equals("login.microsoftonline.com", StringComparison.OrdinalIgnoreCase) &&
            issuer.Segments.Length == 3 && issuer.Segments[2] == "v2.0" &&
            Guid.TryParse(issuer.Segments[1].TrimEnd('/'), out var resolvedId) &&
            Guid.TryParse(activeTenant, out var activeTenantId))
        {
            return resolvedId == activeTenantId;
        }

        logger.LogError("Could not resolve selected tenant domain {TenantId} using Microsoft Entra tenant metadata. Azure CLI error: {Error}",
            selectedTenant, error);
        return false;
    }

    private static (int ExitCode, string? Output, string? Error) ReadTenantIssuer(string tenantDomain)
    {
        var exitCode = AzCliRunner.Create().RunAzCli(
            $"rest --method get --url \"https://login.microsoftonline.com/{tenantDomain}/v2.0/.well-known/openid-configuration\" " +
            "--skip-authorization-header --query issuer --output tsv",
            out var output, out var error);
        return (exitCode, output, error);
    }

    protected virtual (int ExitCode, string? Output, string? Error) ReadAccount()
        => ReadAzureCliAccount();

    internal static (int ExitCode, string? Output, string? Error) ReadAzureCliAccount()
    {
        var exitCode = AzCliRunner.Create().RunAzCli("account show --output json", out var output, out var error);
        return (exitCode, output, error);
    }
}
