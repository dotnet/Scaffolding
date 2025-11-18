    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.

    using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
    using Microsoft.DotNet.Scaffolding.Core.Steps;
    using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
    using Microsoft.DotNet.Scaffolding.Internal.Services;
    using Microsoft.DotNet.Scaffolding.Core.Logging;

    namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps
    {
        internal class UpdateAppAuthorizationStep: ScaffoldStep
        {
            public required string ProjectPath { get; set; }
            public required string? ClientId { get; set; }
            public string[]? WebRedirectUris { get; set; } 
            public string[]? SpaRedirectUris { get; set; }
            public bool AutoConfigureLocalUrls { get; set; } = true;

            private readonly IScaffolderLogger _logger;
            private readonly IFileSystem _fileSystem;
            private readonly ITelemetryService _telemetryService;

            public UpdateAppAuthorizationStep(IScaffolderLogger logger, IFileSystem fileSystem, ITelemetryService telemetryService)
            {
                _logger = logger;
                _fileSystem = fileSystem;
                _telemetryService = telemetryService;
            }

            public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
            {
                if (string.IsNullOrEmpty(ClientId))
                {
                    return Task.FromResult(false);
                }

                if (AutoConfigureLocalUrls)
                {
                    ConfigureLocalRedirectUris(ProjectPath);
                }

                var runner = AzCliRunner.Create();

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

                var exitCode = runner.RunAzCli(command, out var stdOut, out var stdErr);
                if (exitCode != 0 || !string.IsNullOrEmpty(stdErr))
                {
                    _logger.LogError($"Failed to update app registration: {stdErr}");
                    return Task.FromResult(false);
                }

                _logger.LogInformation($"Updated App registration with ID token configuration and redirect URIs");

                return Task.FromResult(true);
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
                                        try
                                        {
                                            var baseUrl = url.Trim();
                                            localUrls.Add(baseUrl);
                                            localUrls.Add($"{baseUrl}/signin-oidc");
                                        }
                                        catch
                                        {
                                            _logger.LogError($"An error occurred while processing the URL: {url}");
                                        }
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
                            .Distinct()
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
                    "https://localhost:5001",
                    "https://localhost:5001/signin-oidc",
                    "https://localhost:7001",
                    "https://localhost:7001/signin-oidc",
                    "https://localhost:44300",
                    "https://localhost:44300/signin-oidc"
                };

                WebRedirectUris = (WebRedirectUris ?? Array.Empty<string>())
                    .Concat(defaultUris)
                    .Distinct()
                    .ToArray();
            
                _logger.LogInformation("Added default localhost redirect URIs");
            }
        }
    }

