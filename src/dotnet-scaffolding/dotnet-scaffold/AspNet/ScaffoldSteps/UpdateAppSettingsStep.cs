// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings
{
    /// <summary>
    /// Scaffold step to update Azure AD development configuration in appsettings.Development.json.
    /// </summary>
    internal class UpdateAppSettingsStep : ScaffoldStep
    {
        // Required properties for the AzureAD configuration
        /// <summary>
        /// Path to the project file.
        /// </summary>
        public required string ProjectPath { get; set; }
        /// <summary>
        /// Username for Azure AD configuration.
        /// </summary>
        public string? Username { get; set; }
        /// <summary>
        /// Azure AD application client ID.
        /// </summary>
        public string? ClientId { get; set; }
        /// <summary>
        /// Azure AD domain.
        /// </summary>
        public string? Domain { get; set; }
        /// <summary>
        /// Azure AD instance URL.
        /// </summary>
        public string? Instance { get; set; }
        /// <summary>
        /// Azure AD tenant ID.
        /// </summary>
        public string? TenantId { get; set; }
        /// <summary>
        /// Callback path for authentication.
        /// </summary>
        public string? CallbackPath { get; set; }
        /// <summary>
        /// Azure AD client secret.
        /// </summary>
        public string? ClientSecret { get; set; }
        /// <summary>
        /// Indicates whether existing managed AzureAd values should be overwritten.
        /// </summary>
        public bool Overwrite { get; set; }

        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly ITelemetryService _telemetryService;

        /// <summary>
        /// Constructor for UpdateAppSettingsStep.
        /// </summary>
        /// <param name="logger">Logger for this step.</param>
        /// <param name="fileSystem">File system helper.</param>
        /// <param name="telemetryService">Telemetry service for logging events.</param>
        public UpdateAppSettingsStep(
            ILogger<UpdateAppSettingsStep> logger,
            IFileSystem fileSystem,
            ITelemetryService telemetryService)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _telemetryService = telemetryService;
        }

        /// <summary>
        /// Executes the step to update Azure AD configuration in appsettings files.
        /// </summary>
        /// <param name="context">Scaffolder context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
        {
            const string defaultInstance = "https://login.microsoftonline.com/";
            const string defaultCallbackPath = "/signin-oidc";

            try
            {
                var baseProjectPath = Path.GetDirectoryName(ProjectPath);

                if (string.IsNullOrEmpty(ProjectPath) || baseProjectPath is null || !_fileSystem.DirectoryExists(baseProjectPath))
                {
                    _logger.LogError($"Invalid project path: {ProjectPath}");
                    return Task.FromResult(false);
                }

                if (string.IsNullOrWhiteSpace(ClientId) || string.IsNullOrWhiteSpace(TenantId))
                {
                    _logger.LogError("Both ClientId and TenantId are required to write AzureAd development settings.");
                    return Task.FromResult(false);
                }

                if (string.IsNullOrWhiteSpace(Domain) && string.IsNullOrWhiteSpace(Username))
                {
                    _logger.LogError("Either Domain or Username is required to resolve AzureAd domain.");
                    return Task.FromResult(false);
                }

                var devSettingsPath = Path.Combine(baseProjectPath, "appsettings.Development.json");
                JsonObject developmentSettings;

                if (_fileSystem.FileExists(devSettingsPath))
                {
                    var devSettingsJson = _fileSystem.ReadAllText(devSettingsPath);

                    try
                    {
                        JsonNode? parsedSettings = JsonNode.Parse(devSettingsJson);

                        if (parsedSettings is null)
                        {
                            developmentSettings = new JsonObject();
                        }
                        else if (parsedSettings is JsonObject parsedObject)
                        {
                            developmentSettings = parsedObject;
                        }
                        else
                        {
                            _logger.LogError($"Failed to parse appsettings.Development.json file at {devSettingsPath}: root JSON value must be an object.");
                            return Task.FromResult(false);
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError($"Failed to parse appsettings.Development.json file at {devSettingsPath}: {ex.Message}");
                        return Task.FromResult(false);
                    }
                }
                else
                {
                    _logger.LogInformation($"Creating new appsettings.Development.json file at {devSettingsPath}");
                    developmentSettings = new JsonObject();
                }

                string resolvedDomain = !string.IsNullOrWhiteSpace(Domain)
                    ? Domain
                    : $"{Username}.onmicrosoft.com";

                var azureAdConfig = new JsonObject
                {
                    ["Instance"] = !string.IsNullOrWhiteSpace(Instance) ? Instance : defaultInstance,
                    ["TenantId"] = TenantId,
                    ["Domain"] = resolvedDomain,
                    ["ClientId"] = ClientId,
                    ["CallbackPath"] = !string.IsNullOrWhiteSpace(CallbackPath) ? CallbackPath : defaultCallbackPath
                };

                JsonNode? existingAzureAd = developmentSettings["AzureAd"];

                if (!Overwrite && existingAzureAd is JsonObject existingAzureAdObject && HasConflictingAzureAdSettings(existingAzureAdObject, azureAdConfig))
                {
                    _logger.LogError("Conflicting AzureAd values already exist in appsettings.Development.json. Re-run with '--overwrite' to replace existing managed settings.");
                    return Task.FromResult(false);
                }

                if (!Overwrite && existingAzureAd is not null && existingAzureAd is not JsonObject)
                {
                    _logger.LogError("The existing AzureAd section in appsettings.Development.json is not a JSON object. Re-run with '--overwrite' to replace it.");
                    return Task.FromResult(false);
                }

                if (JsonNode.DeepEquals(existingAzureAd, azureAdConfig))
                {
                    _logger.LogInformation("No changes needed for AzureAd configuration in appsettings.Development.json.");
                    return Task.FromResult(true);
                }

                developmentSettings["AzureAd"] = azureAdConfig;

                var options = new JsonSerializerOptions { WriteIndented = true };
                _fileSystem.WriteAllText(devSettingsPath, developmentSettings.ToJsonString(options));

                _logger.LogInformation($"Updated '{Path.GetFileName(devSettingsPath)}' with AzureAd development configuration.");
                _logger.LogInformation("The generated AzureAd app registration and settings are intended for development environments.");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update appsettings.Development.json: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        private static bool HasConflictingAzureAdSettings(JsonObject existingAzureAd, JsonObject generatedAzureAd)
        {
            foreach (string key in new[] { "Instance", "TenantId", "Domain", "ClientId", "CallbackPath" })
            {
                string? existingValue = existingAzureAd[key]?.ToString();
                string? generatedValue = generatedAzureAd[key]?.ToString();

                if (!string.IsNullOrEmpty(existingValue) &&
                    !string.Equals(existingValue, generatedValue, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
