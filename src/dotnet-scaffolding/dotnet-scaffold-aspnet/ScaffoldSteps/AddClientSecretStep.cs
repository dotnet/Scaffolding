// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps
{
    internal class AddClientSecretStep : ScaffoldStep
    {
        // Required properties
        public required string ProjectPath { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? SecretName { get; set; } = "Authentication:AzureAd:ClientSecret";
        public string? Username { get; set; }
        public string? TenantId { get; set; }

        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly ITelemetryService _telemetryService;

        public AddClientSecretStep(
            ILogger<AddClientSecretStep> logger,
            IFileSystem fileSystem,
            ITelemetryService telemetryService)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _telemetryService = telemetryService;
        }

        public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
        {
            // Validate project path
            if (string.IsNullOrEmpty(ProjectPath) || !_fileSystem.FileExists(ProjectPath))
            {
                _logger.LogError($"Invalid project path: {ProjectPath}");
                return Task.FromResult(false);
            }

            _logger.LogInformation("Initializing user secrets for project...");

            // Initialize user secrets
            bool success = AddClientSecret(context);
            if (!success)
            {
                _logger.LogError("Failed to add client secret.");
                return Task.FromResult(false);
            }

            _logger.LogInformation("Successfully configured user secrets for the project.");
            return Task.FromResult(true);
        }

        private bool AddClientSecret(ScaffolderContext context)
        {
            try
            {
                // Fix for CS8620: Ensure the array passed to DotnetCliRunner.CreateDotNet is non-nullable
                var runner = DotnetCliRunner.CreateDotNet(
                    "msidentity",
                    new[] { "--create-client-secret", "--tenant-id", TenantId ?? string.Empty, "--username", Username ?? string.Empty, "--client-id", ClientId ?? string.Empty, "--json" }
                );
                int exitCode = runner.ExecuteAndCaptureOutput(out var stdOut, out var stdErr);

                if (exitCode != 0)
                {
                    _logger.LogError($"Error adding client secret: {stdErr}");
                    return false;
                }

                if (!string.IsNullOrEmpty(stdOut))
                {
                    try
                    {
                        // Parse JSON response
                        using (JsonDocument doc = JsonDocument.Parse(stdOut))
                        {
                            // Check if the operation was successful
                            if (doc.RootElement.TryGetProperty("State", out JsonElement stateElement) && 
                                stateElement.GetString() == "Success")
                            {
                                // Extract the client secret from the Content.Value property
                                if (doc.RootElement.TryGetProperty("Content", out JsonElement contentElement) &&
                                    contentElement.TryGetProperty("Value", out JsonElement valueElement))
                                {
                                    string? clientSecret = valueElement.GetString();
                                    if (!string.IsNullOrEmpty(clientSecret))
                                    {
                                        _logger.LogInformation("Successfully extracted client secret from JSON output");
                                        context.Properties["ClientSecret"] = clientSecret;
                                        ClientSecret = clientSecret; // Also store in the property
                                        return true;
                                    }
                                }
                            }
                            _logger.LogError("JSON response doesn't contain expected success state or secret value");
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError($"Failed to parse JSON output: {ex.Message}");
                    }
                }
                else
                {
                    _logger.LogError("No output received from client secret command");
                }
            } 
            catch(Exception e)
            {
                _logger.LogError($"Error adding client secret: {e.Message}");
            }

            return false;
        }
    }
}
