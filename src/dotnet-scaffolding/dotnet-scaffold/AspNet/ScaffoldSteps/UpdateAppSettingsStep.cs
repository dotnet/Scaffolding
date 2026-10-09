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
            try
            {
                var baseProjectPath = Path.GetDirectoryName(ProjectPath);
                if (string.IsNullOrEmpty(baseProjectPath))
                {
                    baseProjectPath = Directory.GetCurrentDirectory();
                }

                if (string.IsNullOrEmpty(ProjectPath) || !_fileSystem.DirectoryExists(baseProjectPath))
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
                        JsonNode? parsedSettings = ParseDevelopmentSettings(devSettingsJson);

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

                var azureAdConfig = CreateAzureAdConfiguration(Username, TenantId, ClientId, Domain, Instance, CallbackPath);

                JsonNode? existingAzureAd = developmentSettings["AzureAd"];

                if (!Overwrite && existingAzureAd is JsonObject existingAzureAdObject)
                {
                    string? conflictingKey = GetFirstConflictingAzureAdKey(existingAzureAdObject, azureAdConfig);
                    if (!string.IsNullOrEmpty(conflictingKey))
                    {
                        _logger.LogError($"Conflicting AzureAd value for key '{conflictingKey}' already exists in appsettings.Development.json. Re-run with '--overwrite' to replace existing managed settings.");
                        return Task.FromResult(false);
                    }
                }

                if (!Overwrite && existingAzureAd is not null && existingAzureAd is not JsonObject)
                {
                    _logger.LogError("Conflicting AzureAd value for key 'AzureAd' in appsettings.Development.json: the section is not a JSON object. Re-run with '--overwrite' to replace it.");
                    return Task.FromResult(false);
                }

                if (existingAzureAd is JsonObject existingAzureAdObjectForNoWrite &&
                    HasMatchingManagedAzureAdSettings(existingAzureAdObjectForNoWrite, azureAdConfig))
                {
                    _logger.LogInformation("No changes needed for AzureAd configuration in appsettings.Development.json.");
                    LogDevelopmentEnvironmentNotice();
                    return Task.FromResult(true);
                }

                JsonObject targetAzureAd = existingAzureAd switch
                {
                    JsonObject jsonObject => jsonObject,
                    _ => new JsonObject()
                };

                foreach (var setting in azureAdConfig)
                {
                    targetAzureAd[setting.Key] = setting.Value?.DeepClone();
                }

                if (existingAzureAd is not JsonObject)
                {
                    developmentSettings["AzureAd"] = targetAzureAd;
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                _fileSystem.WriteAllText(devSettingsPath, developmentSettings.ToJsonString(options));

                _logger.LogInformation($"Updated '{Path.GetFileName(devSettingsPath)}' with AzureAd development configuration.");
                LogDevelopmentEnvironmentNotice();
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update appsettings.Development.json: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        private void LogDevelopmentEnvironmentNotice()
        {
            _logger.LogInformation("The generated AzureAd app registration and settings are intended for development environments.");
        }

        internal static JsonNode? ParseDevelopmentSettings(string json)
        {
            return JsonNode.Parse(
                json,
                new JsonNodeOptions { PropertyNameCaseInsensitive = true },
                new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });
        }

        internal static JsonObject CreateAzureAdConfiguration(
            string? username, string? tenantId, string? clientId,
            string? domain = null, string? instance = null, string? callbackPath = null)
        {
            return new JsonObject
            {
                ["Instance"] = !string.IsNullOrWhiteSpace(instance) ? instance : "https://login.microsoftonline.com/",
                ["TenantId"] = tenantId,
                ["Domain"] = !string.IsNullOrWhiteSpace(domain) ? domain : $"{username}.onmicrosoft.com",
                ["ClientId"] = clientId,
                ["CallbackPath"] = !string.IsNullOrWhiteSpace(callbackPath) ? callbackPath : "/signin-oidc"
            };
        }

        internal static string? GetFirstConflictingAzureAdKey(JsonObject existingAzureAd, JsonObject generatedAzureAd)
        {
            foreach (var setting in generatedAzureAd)
            {
                string? existingValue = existingAzureAd[setting.Key]?.ToString();

                if (!string.IsNullOrEmpty(existingValue) &&
                    !JsonNode.DeepEquals(existingAzureAd[setting.Key], setting.Value))
                {
                    return setting.Key;
                }
            }

            return null;
        }

        private static bool HasMatchingManagedAzureAdSettings(JsonObject existingAzureAd, JsonObject generatedAzureAd)
        {
            foreach (var setting in generatedAzureAd)
            {
                if (!JsonNode.DeepEquals(existingAzureAd[setting.Key], setting.Value))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
