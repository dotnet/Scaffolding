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

            /*// If ClientSecret is provided, set it in user secrets
            if (!string.IsNullOrEmpty(ClientSecret) && !string.IsNullOrEmpty(SecretName))
            {
                success = SetUserSecret(SecretName, ClientSecret);
                if (!success)
                {
                    _logger.LogError($"Failed to set client secret in user secrets.");
                    return Task.FromResult(false);
                }
            }
            else if (!string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(SecretName))
            {
                // Set a placeholder secret if ClientSecret is not provided but ClientId is
                var placeholderSecret = $"placeholder-secret-for-{ClientId}-replace-with-real-secret";
                success = SetUserSecret(SecretName, placeholderSecret);
                if (!success)
                {
                    _logger.LogError($"Failed to set placeholder client secret in user secrets.");
                    return Task.FromResult(false);
                }

                _logger.LogInformation($"Set placeholder client secret. Replace it with a real secret before running your application.");
            }*/

            _logger.LogInformation("Successfully configured user secrets for the project.");
            return Task.FromResult(true);
        }

        private bool InitializeUserSecrets()
        {
            try
            {
                // Check if user secrets are already initialized by looking for UserSecretsId in the project file
                string projectContent = _fileSystem.ReadAllText(ProjectPath);
                if (projectContent.Contains("<UserSecretsId>"))
                {
                    _logger.LogInformation("User secrets are already initialized for this project.");
                    ClientId = ExtractUserSecretsId(projectContent);
                    return true;
                }

                // Run 'dotnet user-secrets init' command
                var runner = DotnetCliRunner.CreateDotNet("user-secrets", new[] { "init", "--project", ProjectPath });
                int exitCode = runner.ExecuteAndCaptureOutput(out var stdOut, out var stdErr);

                if (exitCode != 0)
                {
                    _logger.LogError($"Error initializing user secrets: {stdErr}");
                    return false;
                }

                if (String.IsNullOrEmpty(stdOut))
                {
                    ClientId = ExtractUserSecretsId(projectContent);
                }

                _logger.LogInformation("User secrets initialized successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception when initializing user secrets: {ex.Message}");
                return false;
            }
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

        private bool SetUserSecret(string key, string value)
        {
            try
            {
                // Run 'dotnet user-secrets set' command
                var runner = DotnetCliRunner.CreateDotNet("user-secrets", new[] { "set", key, value, "--project", ProjectPath });
                int exitCode = runner.ExecuteAndCaptureOutput(out var stdOut, out var stdErr);

                if (exitCode != 0)
                {
                    _logger.LogError($"Error setting user secret: {stdErr}");
                    return false;
                }

                _logger.LogInformation($"Secret '{key}' set successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception when setting user secret: {ex.Message}");
                return false;
            }
        }

        private string? ExtractUserSecretsId(string projectContent)
        {
            // Use regex to extract the UserSecretsId value
            var match = Regex.Match(projectContent, @"<UserSecretsId>(.*?)</UserSecretsId>");
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
            return null;
        }
    }
}
