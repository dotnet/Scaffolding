// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
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
            bool succeeded = false;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                succeeded = UpdateSettings();
                return Task.FromResult(succeeded);
            }
            finally
            {
                if (!succeeded)
                {
                    CleanupRegistration(context);
                }
            }
        }

        protected virtual void CleanupRegistration(ScaffolderContext context)
            => EntraRegistrationCleanup.TryCleanup(context, TenantId, Username, _logger);

        private bool UpdateSettings()
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
                    return false;
                }

                if (string.IsNullOrWhiteSpace(ClientId) || string.IsNullOrWhiteSpace(TenantId))
                {
                    _logger.LogError("Both ClientId and TenantId are required to write AzureAd development settings.");
                    return false;
                }

                var devSettingsPath = Path.Combine(baseProjectPath, "appsettings.Development.json");
                JsonObject developmentSettings;
                string? originalContent = null;

                if (_fileSystem.FileExists(devSettingsPath))
                {
                    var devSettingsJson = _fileSystem.ReadAllText(devSettingsPath);
                    originalContent = devSettingsJson;

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
                            return false;
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError($"Failed to parse appsettings.Development.json file at {devSettingsPath}: {ex.Message}");
                        return false;
                    }
                }
                else
                {
                    _logger.LogInformation($"Creating new appsettings.Development.json file at {devSettingsPath}");
                    developmentSettings = new JsonObject();
                }

                var azureAdConfig = CreateAzureAdConfiguration(Username, TenantId, ClientId, Domain, Instance, CallbackPath);
                bool removedLegacyDomain = RemoveLegacyUsernameDerivedDomain(developmentSettings, Username, Domain);

                JsonObject? existingAzureAd = GetAzureAdSections(developmentSettings).FirstOrDefault();

                if (!Overwrite)
                {
                    string? conflictingKey = GetFirstConflictingAzureAdKey(developmentSettings, azureAdConfig);
                    if (!string.IsNullOrEmpty(conflictingKey))
                    {
                        _logger.LogError($"Conflicting AzureAd value for key '{conflictingKey}' already exists in appsettings.Development.json. Re-run with '--overwrite' to replace existing managed settings.");
                        return false;
                    }
                }

                if (!Overwrite && HasNonObjectAzureAdSection(developmentSettings))
                {
                    _logger.LogError("Conflicting AzureAd value for key 'AzureAd' in appsettings.Development.json: the section is not a JSON object. Re-run with '--overwrite' to replace it.");
                    return false;
                }

                if (existingAzureAd is not null &&
                    !removedLegacyDomain &&
                    !HasNonObjectAzureAdSection(developmentSettings) &&
                    !HasFlatManagedAzureAdSettings(developmentSettings, azureAdConfig) &&
                    HasMatchingManagedAzureAdSettings(developmentSettings, azureAdConfig))
                {
                    _logger.LogInformation("No changes needed for AzureAd configuration in appsettings.Development.json.");
                    LogDevelopmentEnvironmentNotice();
                    return true;
                }

                JsonObject targetAzureAd = existingAzureAd ?? new JsonObject();

                RemoveFlatManagedAzureAdSettings(developmentSettings, azureAdConfig);

                foreach (var setting in azureAdConfig)
                {
                    string targetKey = targetAzureAd
                        .FirstOrDefault(property => property.Key.Equals(setting.Key, StringComparison.OrdinalIgnoreCase)).Key
                        ?? setting.Key;
                    foreach (JsonObject section in GetAzureAdSections(developmentSettings))
                    {
                        foreach (string key in section
                            .Where(property => property.Key.Equals(setting.Key, StringComparison.OrdinalIgnoreCase))
                            .Select(property => property.Key).ToList())
                        {
                            section.Remove(key);
                        }
                    }
                    targetAzureAd[targetKey] = setting.Value?.DeepClone();
                }

                foreach (string key in developmentSettings
                    .Where(property => property.Key.Equals("AzureAd", StringComparison.OrdinalIgnoreCase) &&
                        (property.Value is not JsonObject ||
                         property.Value is JsonObject section && section.Count == 0))
                    .Select(property => property.Key).ToList())
                {
                    developmentSettings.Remove(key);
                }

                if (existingAzureAd is null)
                {
                    developmentSettings["AzureAd"] = targetAzureAd;
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                if (!PersistSettings(devSettingsPath, originalContent, developmentSettings.ToJsonString(options)))
                {
                    return false;
                }

                _logger.LogInformation($"Updated '{Path.GetFileName(devSettingsPath)}' with AzureAd development configuration.");
                LogDevelopmentEnvironmentNotice();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update appsettings.Development.json: {ex.Message}");
                return false;
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
                new JsonNodeOptions { PropertyNameCaseInsensitive = false },
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
            var configuration = new JsonObject
            {
                ["Instance"] = !string.IsNullOrWhiteSpace(instance) ? instance : "https://login.microsoftonline.com/",
                ["TenantId"] = tenantId,
                ["ClientId"] = clientId,
                ["CallbackPath"] = !string.IsNullOrWhiteSpace(callbackPath) ? callbackPath : "/signin-oidc"
            };

            if (!string.IsNullOrWhiteSpace(domain))
            {
                configuration["Domain"] = domain;
            }

            return configuration;
        }

        private static bool RemoveLegacyUsernameDerivedDomain(
            JsonObject developmentSettings,
            string? username,
            string? configuredDomain)
        {
            if (!string.IsNullOrWhiteSpace(configuredDomain) ||
                string.IsNullOrWhiteSpace(username) ||
                !username.Contains('@'))
            {
                return false;
            }

            string legacyDomain = $"{username}.onmicrosoft.com";
            bool removed = false;

            foreach (JsonObject section in GetAzureAdSections(developmentSettings))
            {
                foreach (string key in section
                    .Where(property => property.Key.Equals("Domain", StringComparison.OrdinalIgnoreCase) &&
                        property.Value?.ToString() == legacyDomain)
                    .Select(property => property.Key).ToList())
                {
                    removed = section.Remove(key) || removed;
                }
            }

            foreach (string key in developmentSettings
                .Where(property => property.Key.Equals("AzureAd:Domain", StringComparison.OrdinalIgnoreCase) &&
                    property.Value?.ToString() == legacyDomain)
                .Select(property => property.Key).ToList())
            {
                removed = developmentSettings.Remove(key) || removed;
            }

            return removed;
        }

        internal static string? GetFirstConflictingAzureAdKey(JsonObject developmentSettings, JsonObject generatedAzureAd)
        {
            foreach (var setting in generatedAzureAd)
            {
                foreach (JsonNode? value in GetNestedAzureAdValues(developmentSettings, setting.Key))
                {
                    if (IsConflictingValue(value, setting.Value))
                    {
                        return setting.Key;
                    }
                }
                foreach (var property in developmentSettings)
                {
                    if (property.Key.Equals($"AzureAd:{setting.Key}", StringComparison.OrdinalIgnoreCase) &&
                        IsConflictingValue(property.Value, setting.Value))
                    {
                        return setting.Key;
                    }
                }
            }

            return null;
        }

        private static IEnumerable<JsonObject> GetAzureAdSections(JsonObject developmentSettings)
            => developmentSettings
                .Where(property => property.Key.Equals("AzureAd", StringComparison.OrdinalIgnoreCase))
                .Select(property => property.Value)
                .OfType<JsonObject>();

        internal static bool HasNonObjectAzureAdSection(JsonObject developmentSettings)
            => developmentSettings.Any(property =>
                property.Key.Equals("AzureAd", StringComparison.OrdinalIgnoreCase) &&
                property.Value is not null && property.Value is not JsonObject);

        private static IEnumerable<JsonNode?> GetNestedAzureAdValues(JsonObject developmentSettings, string key)
            => GetAzureAdSections(developmentSettings)
                .SelectMany(section => section)
                .Where(property => property.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                .Select(property => property.Value);

        private static bool IsConflictingValue(JsonNode? existingValue, JsonNode? generatedValue)
        {
            return !string.IsNullOrEmpty(existingValue?.ToString()) &&
                !JsonNode.DeepEquals(existingValue, generatedValue);
        }

        private static bool HasFlatManagedAzureAdSettings(
            JsonObject developmentSettings,
            JsonObject generatedAzureAd)
        {
            foreach (var setting in generatedAzureAd)
            {
                if (developmentSettings.Any(property =>
                    property.Key.Equals($"AzureAd:{setting.Key}", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }

        private static void RemoveFlatManagedAzureAdSettings(
            JsonObject developmentSettings,
            JsonObject generatedAzureAd)
        {
            foreach (var setting in generatedAzureAd)
            {
                foreach (string key in developmentSettings
                    .Where(property => property.Key.Equals($"AzureAd:{setting.Key}", StringComparison.OrdinalIgnoreCase))
                    .Select(property => property.Key).ToList())
                {
                    developmentSettings.Remove(key);
                }
            }
        }

        private static bool HasMatchingManagedAzureAdSettings(JsonObject developmentSettings, JsonObject generatedAzureAd)
        {
            foreach (var setting in generatedAzureAd)
            {
                JsonNode?[] values = GetNestedAzureAdValues(developmentSettings, setting.Key).ToArray();
                if (values.Length != 1 || !JsonNode.DeepEquals(values[0], setting.Value))
                {
                    return false;
                }
            }

            return true;
        }

        private bool PersistSettings(string path, string? original, string updated)
        {
            try
            {
                _fileSystem.WriteAllText(path, updated);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist Entra ID settings. Restoring the attempted settings write.");
                try
                {
                    if (original is null)
                    {
                        if (_fileSystem.FileExists(path))
                        {
                            _fileSystem.DeleteFile(path);
                        }
                    }
                    else if (!_fileSystem.FileExists(path) || _fileSystem.ReadAllText(path) != original)
                    {
                        _fileSystem.WriteAllText(path, original);
                    }
                }
                catch (Exception restoreException)
                {
                    _logger.LogError(restoreException,
                        "Could not restore settings file {Path}. Restore this file manually before retrying.", path);
                }
                return false;
            }
        }
    }
}