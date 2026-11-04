    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.

    using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
    using Microsoft.DotNet.Scaffolding.Core.Steps;
    using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
    using Microsoft.DotNet.Scaffolding.Internal.Services;
    using Microsoft.Extensions.Logging;
    using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
    using System.Text.Json;

    namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps
    {
        internal class UpdateAppAuthorizationStep: ScaffoldStep
        {
            public required string ProjectPath { get; set; }
            public required string? ClientId { get; set; }
            public string? TenantId { get; set; }
            public string? Username { get; set; }
            public string[]? WebRedirectUris { get; set; }
            public string[]? SpaRedirectUris { get; set; }
            public bool AutoConfigureLocalUrls { get; set; } = true;
            public bool UseExistingApplication { get; set; }
            internal Func<string, AzCliResult>? AzCliCommand { get; set; }

            private readonly ILogger _logger;
            private readonly IFileSystem _fileSystem;
            private readonly ITelemetryService _telemetryService;

            public UpdateAppAuthorizationStep(ILogger<UpdateAppAuthorizationStep> logger, IFileSystem fileSystem, ITelemetryService telemetryService)
            {
                _logger = logger;
                _fileSystem = fileSystem;
                _telemetryService = telemetryService;
            }

            public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
            {
                var succeeded = false;
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    succeeded = UpdateAuthorization();
                    return Task.FromResult(succeeded);
                }
                finally
                {
                    if (!succeeded)
                    {
                        EntraRegistrationCleanup.TryCleanup(context, TenantId, Username, _logger);
                    }
                }
            }

            private bool UpdateAuthorization()
            {
                if (string.IsNullOrEmpty(ClientId))
                {
                    _logger.LogError("A client ID is required to update app authorization.");
                    return false;
                }

                if (AutoConfigureLocalUrls)
                {
                    ConfigureLocalRedirectUris(ProjectPath);
                }

                AzCliRunner? runner = AzCliCommand is null ? AzCliRunner.Create() : null;

                if (UseExistingApplication &&
                    (!ValidateAzureCliIdentityStep.ValidateIdentity(TenantId, Username, _logger, ReadAccount) ||
                    !TryMergeExistingWebRedirectUris(runner)))
                {
                    return false;
                }

                // Build the Azure CLI command with all parameters
                var command = $"ad app update --id {ClientId} --enable-id-token-issuance true";

                // Add Web redirect URIs if provided
                if (WebRedirectUris != null && WebRedirectUris.Length > 0)
                {
                    var webUrisJson = string.Join(" ", WebRedirectUris.Select(uri => $"\"{uri}\""));
                    command += $" --web-redirect-uris {webUrisJson}";
                }

                // Add SPA redirect URIs if provided
                if (SpaRedirectUris != null && SpaRedirectUris.Length > 0)
                {
                    var spaUrisJson = string.Join(" ", SpaRedirectUris.Select(uri => $"\"{uri}\""));
                    command += $" --public-client-redirect-uris {spaUrisJson}";
                }

                if (!ValidateAzureCliIdentityStep.ValidateIdentity(TenantId, Username, _logger, ReadAccount))
                {
                    return false;
                }

                var (exitCode, stdErr) = RunUpdate(command);
                if (exitCode != 0)
                {
                    _logger.LogError($"Failed to update app registration: {stdErr}");
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(stdErr))
                {
                    _logger.LogWarning("Azure CLI warning while updating app registration: {Warning}", stdErr);
                }

                _logger.LogInformation($"Updated App registration with ID token configuration and redirect URIs");

                return true;
            }

            protected virtual (int ExitCode, string? Output, string? Error) ReadAccount()
                => ValidateAzureCliIdentityStep.ReadAzureCliAccount();

            protected virtual (int ExitCode, string? Error) RunUpdate(string command)
            {
                var result = RunAzCli(AzCliCommand is null ? AzCliRunner.Create() : null, command);
                return (result.ExitCode, result.StdErr);
            }

            private bool TryMergeExistingWebRedirectUris(AzCliRunner? runner)
            {
                var command = $"ad app show --id {ClientId} --query web.redirectUris --output json";
                AzCliResult result = RunAzCli(runner, command);
                if (result.ExitCode != 0 || !string.IsNullOrEmpty(result.StdErr))
                {
                    _logger.LogError("Failed to read existing app registration redirect URIs: {Error}", result.StdErr);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(result.StdOut))
                {
                    _logger.LogError("Azure CLI returned no Web redirect URI data for the existing app registration.");
                    return false;
                }

                try
                {
                    string[]? existingUris = JsonSerializer.Deserialize<string[]>(result.StdOut);
                    if (existingUris is null)
                    {
                        _logger.LogError("Azure CLI returned invalid Web redirect URI data for the existing app registration.");
                        return false;
                    }

                    WebRedirectUris = existingUris
                        .Concat(WebRedirectUris ?? Array.Empty<string>())
                        .Distinct(StringComparer.Ordinal)
                        .ToArray();
                    return true;
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse existing app registration redirect URIs.");
                    return false;
                }
            }

            private AzCliResult RunAzCli(AzCliRunner? runner, string command)
            {
                if (AzCliCommand is not null)
                {
                    return AzCliCommand(command);
                }

                int exitCode = runner!.RunAzCli(command, out string? stdOut, out string? stdErr);
                return new AzCliResult(exitCode, stdOut, stdErr);
            }

            private void ConfigureLocalRedirectUris(string projectPath)
            {
                try
                {
                    // Find the launchSettings.json file
                    var launchSettingsPath = Path.Combine(
                        Path.GetDirectoryName(projectPath) ?? string.Empty,
                        "Properties",
                        "launchSettings.json");

                    if (!_fileSystem.FileExists(launchSettingsPath))
                    {
                        _logger.LogInformation("launchSettings.json not found, using default redirect URIs");
                        AddDefaultRedirectUris();
                        return;
                    }

                    // Parse the launchSettings.json
                    var launchSettingsContent = _fileSystem.ReadAllText(launchSettingsPath);
                    using var document = System.Text.Json.JsonDocument.Parse(launchSettingsContent);
                    var profiles = document.RootElement.GetProperty("profiles");

                    var localUrls = new HashSet<string>();

                    // Extract URLs from each profile
                    foreach (var profile in profiles.EnumerateObject())
                    {
                        if (profile.Value.TryGetProperty("applicationUrl", out var appUrlElement))
                        {
                            var appUrls = appUrlElement.GetString()?.Split(';');
                            if (appUrls != null)
                            {
                                foreach (var url in appUrls)
                                {
                                    if (!string.IsNullOrWhiteSpace(url))
                                    {
                                        var baseUrl = url.Trim().TrimEnd('/');
                                        localUrls.Add($"{baseUrl}/signin-oidc");
                                    }
                                }
                            }
                        }
                    }

                    // Add the collected URLs to the redirect URIs
                    if (localUrls.Count > 0)
                    {
                        WebRedirectUris = (WebRedirectUris ?? Array.Empty<string>())
                            .Concat(localUrls)
                            .Distinct(StringComparer.Ordinal)
                            .ToArray();

                        _logger.LogInformation($"Configured {localUrls.Count} local redirect URIs from launchSettings.json");
                    }
                    else
                    {
                        AddDefaultRedirectUris();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error configuring local redirect URIs: {ex.Message}");
                    AddDefaultRedirectUris();
                }
            }

            private void AddDefaultRedirectUris()
            {
                var defaultUris = new[]
                {
                    "https://localhost:5001/signin-oidc",
                    "https://localhost:7001/signin-oidc",
                    "https://localhost:44300/signin-oidc"
                };

                WebRedirectUris = (WebRedirectUris ?? Array.Empty<string>())
                    .Concat(defaultUris)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

                _logger.LogInformation("Added default localhost redirect URIs");
            }

            internal readonly record struct AzCliResult(int ExitCode, string? StdOut, string? StdErr);
        }
    }
