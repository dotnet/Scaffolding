// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.Helpers;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps
{
    /// <summary>
    /// Scaffold step for adding a client secret to an Azure AD application registration using the msidentity CLI.
    /// Extracts and stores the client secret in the scaffolding context.
    /// </summary>
    internal class AddClientSecretStep : ScaffoldStep
    {
        /// <summary>
        /// Gets or sets the project file path.
        /// </summary>
        public required string ProjectPath { get; set; }
        /// <summary>
        /// Gets or sets the Azure AD client ID.
        /// </summary>
        public string? ClientId { get; set; }
        /// <summary>
        /// Gets or sets the client secret value.
        /// </summary>
        public string? ClientSecret { get; set; }
        /// <summary>
        /// Gets or sets the secret name in user secrets.
        /// </summary>
        public string? SecretName { get; set; } = "AzureAd:ClientCredentials:0:ClientSecret";
        public bool UseExistingApplication { get; set; }
        /// <summary>
        /// Gets or sets the username for Azure AD.
        /// </summary>
        public string? Username { get; set; }
        /// <summary>
        /// Gets or sets the Azure AD tenant ID.
        /// </summary>
        public string? TenantId { get; set; }

        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IEnvironmentService _environmentService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddClientSecretStep"/> class.
        /// </summary>
        public AddClientSecretStep(
            ILogger<AddClientSecretStep> logger,
            IFileSystem fileSystem,
            IEnvironmentService environmentService)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _environmentService = environmentService;
        }

        /// <inheritdoc />
        public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
        {
            var succeeded = false;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                succeeded = ConfigureClientSecret(context);
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

        private bool ConfigureClientSecret(ScaffolderContext context)
        {
            // Validate project path
            if (string.IsNullOrEmpty(ProjectPath) || !_fileSystem.FileExists(ProjectPath))
            {
                _logger.LogError($"Invalid project path: {ProjectPath}");
                return false;
            }

            if (UseExistingApplication)
            {
                _logger.LogInformation("Using an existing application. No password credential will be created. Configure a supported AzureAd:ClientCredentials credential locally if one is not already configured.");
                _logger.LogWarning("Use a separate production app registration with managed identity/federated identity credentials or a certificate. Client secrets are for local development only.");
                return true;
            }

            _logger.LogInformation("Configuring a client credential for local development only...");

            if (MsIdentityToolHelper.EnsureMsIdentityIsInstalled(_logger, GetCliEnvVars()))
            {
                if (!InitializeUserSecrets())
                {
                    return false;
                }

                bool success = AddClientSecret(context);
                if (!success)
                {
                    _logger.LogError("Failed to add client secret.");
                    return false;
                }

                _logger.LogInformation("Successfully configured user secrets for the project.");
                _logger.LogWarning("This development client secret is not suitable for production. Use a separate production app registration with managed identity/federated identity credentials or a certificate.");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds a client secret to the Azure AD app registration and stores it in the context.
        /// </summary>
        private bool AddClientSecret(ScaffolderContext context)
        {
            try
            {
                // Fix for CS8620: Ensure the array passed to DotnetCliRunner.CreateDotNet is non-nullable
                var runner = DotnetCliRunner.CreateDotNet(
                    "msidentity",
                    ["--create-client-secret", "--tenant-id", TenantId ?? string.Empty, "--username", Username ?? string.Empty, "--client-id", ClientId ?? string.Empty, "--json"]
                );
                int exitCode = runner.ExecuteAndCaptureOutput(out var stdOut, out var stdErr);

                if (exitCode != 0)
                {
                    _logger.LogError("Error adding client secret. The msidentity command exited with code {ExitCode}.", exitCode);
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
                                        if (!StoreClientSecret(clientSecret))
                                        {
                                            _logger.LogError("A development password credential was created but could not be saved. Remove the unused credential from the app registration before retrying.");
                                            return false;
                                        }

                                        context.Properties["ClientSecret"] = clientSecret;
                                        ClientSecret = clientSecret;
                                        return true;
                                    }
                                }
                            }
                            _logger.LogError("JSON response doesn't contain expected success state or secret value");
                        }
                    }
                    catch (JsonException)
                    {
                        _logger.LogError("Failed to parse the client secret command's JSON response.");
                    }
                }
                else
                {
                    _logger.LogError("No output received from client secret command");
                }
            } 
            catch (Exception e) when (e is IOException or InvalidOperationException)
            {
                _logger.LogError("Unable to create or store the development client credential.");
            }

            return false;
        }

        internal bool InitializeUserSecrets()
        {
            var runner = DotnetCliRunner.CreateDotNet("user-secrets", ["init", "--project", ProjectPath]);
            int exitCode = runner.ExecuteAndCaptureOutput(out _, out _);
            if (exitCode != 0)
            {
                _logger.LogError("Failed to initialize user secrets for the project (exit code {ExitCode}).", exitCode);
                return false;
            }

            return true;
        }

        internal bool StoreClientSecret(string clientSecret)
        {
            if (string.IsNullOrWhiteSpace(SecretName))
            {
                _logger.LogError("A user-secrets key is required for the development client credential.");
                return false;
            }

            // Send the credential over stdin so it never appears in command-line diagnostics.
            string input = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["AzureAd:ClientCredentials:0:SourceType"] = "ClientSecret",
                [SecretName] = clientSecret
            });
            var runner = DotnetCliRunner.CreateDotNet("user-secrets", ["set", "--project", ProjectPath]);
            int exitCode = runner.ExecuteAndCaptureOutput(out _, out _, input);
            if (exitCode != 0)
            {
                _logger.LogError("Failed to save the development client credential in user secrets (exit code {ExitCode}).", exitCode);
                return false;
            }

            return true;
        }

        Dictionary<string, string> GetCliEnvVars()
        {
            // Get telemetry opt-out from environment service
            string? telemetryOptOut = _environmentService.GetEnvironmentVariable(TelemetryConstants.TELEMETRY_OPTOUT);
            string? telemetryState = _environmentService.GetEnvironmentVariable(TelemetryConstants.DOTNET_SCAFFOLD_TELEMETRY_STATE);
            Dictionary<string, string> envVars = [];

            if (!string.IsNullOrEmpty(telemetryOptOut))
            {
                envVars[TelemetryConstants.TELEMETRY_OPTOUT] = telemetryOptOut;
            }

            if (!string.IsNullOrEmpty(telemetryState))
            {
                envVars[TelemetryConstants.DOTNET_SCAFFOLD_TELEMETRY_STATE] = telemetryState;
            }

            return envVars;
        }
    }
}
