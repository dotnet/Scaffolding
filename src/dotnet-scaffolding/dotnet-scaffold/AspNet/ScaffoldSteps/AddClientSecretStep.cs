// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.Helpers;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Scaffolding.Core.Logging;
using System.Text.Json;

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
        public string? SecretName { get; set; } = "Authentication:AzureAd:ClientSecret";
        /// <summary>
        /// Gets or sets the username for Azure AD.
        /// </summary>
        public string? Username { get; set; }
        /// <summary>
        /// Gets or sets the Azure AD tenant ID.
        /// </summary>
        public string? TenantId { get; set; }

        private readonly IScaffolderLogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IEnvironmentService _environmentService;

        private const string _msIdentityToolName = "Microsoft.dotnet-msidentity";

        /// <summary>
        /// Initializes a new instance of the <see cref="AddClientSecretStep"/> class.
        /// </summary>
        public AddClientSecretStep(
            IScaffolderLogger logger,
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
            // Validate project path
            if (string.IsNullOrEmpty(ProjectPath) || !_fileSystem.FileExists(ProjectPath))
            {
                _logger.LogError($"Invalid project path: {ProjectPath}");
                return Task.FromResult(false);
            }

            _logger.LogInformation("Initializing user secrets for project...");

            if (EnsureMsIdentityIsInstalled())
            {
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
            return Task.FromResult(false);
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

        private bool EnsureMsIdentityIsInstalled()
        {
            try
            {
                if (IsMsIdentityAlreadyInstalled())
                {
                    return true;
                }

                // Determine if prerelease is needed
                bool isPreRelease = ToolHelper.IsToolPrerelease();
                string commandName = "tool";

                // install the tool locally
                List<string> args = ["install", _msIdentityToolName];
                if (isPreRelease)
                {
                    args.Add("--prerelease");
                }

                DotnetCliRunner runner = DotnetCliRunner.CreateDotNet(commandName, [.. args], GetCliEnvVars());
                int exitCode = runner.ExecuteAndCaptureOutput(out string? stdOut, out string? stdErr);
                if (exitCode == 0)
                {
                    _logger.LogInformation($"Successfully installed {_msIdentityToolName}...");
                    return true;
                }
                else
                {
                    _logger.LogError($"Failed to install {_msIdentityToolName}: {stdErr}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception during tool installation: {ex.Message}");
            }
            return false;
        }

        bool IsMsIdentityAlreadyInstalled()
        {
            try
            {
                // Check if the tool is installed globally
                DotnetCliRunner globalRunner = DotnetCliRunner.CreateDotNet("tool", ["list", "-g"]);
                int globalExitCode = globalRunner.ExecuteAndCaptureOutput(out string? globalStdOut, out string? globalStdErr);
                if (globalExitCode == 0 && !string.IsNullOrEmpty(globalStdOut) && globalStdOut.Contains(_msIdentityToolName, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation($"{_msIdentityToolName} is already installed globally.");
                    return true;
                }

                // Check if the tool is installed locally
                DotnetCliRunner localRunner = DotnetCliRunner.CreateDotNet("tool", ["list"]);
                int localExitCode = localRunner.ExecuteAndCaptureOutput(out string? localStdOut, out string? localStdErr);
                if (localExitCode == 0 && !string.IsNullOrEmpty(localStdOut) && localStdOut.Contains(_msIdentityToolName, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation($"{_msIdentityToolName} is already installed locally.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception checking the installation of dotnet MsIdentity: {ex.Message}");
            }
            return false;
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
