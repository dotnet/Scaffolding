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
        internal const string DefaultInstance = "https://login.microsoftonline.com/";
        internal const string DefaultCallbackPath = "/signin-oidc";

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
        /// Indicates whether existing managed AzureAd values should be overwritten.
        /// </summary>
        public bool Overwrite { get; set; }

        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly ITelemetryService _telemetryService;

        /// <summary>
        /// Constructor for UpdateAppSettingsStep.
        /// </summary>
        public UpdateAppSettingsStep(
            ILogger<UpdateAppSettingsStep> logger,
            IFileSystem fileSystem,
            ITelemetryService telemetryService)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _telemetryService = telemetryService;
        }

        /// <inheritdoc />
        public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
        {
            string? projectDirectory = Path.GetDirectoryName(ProjectPath);
            if (string.IsNullOrEmpty(projectDirectory))
            {
                projectDirectory = Directory.GetCurrentDirectory();
            }

            if (string.IsNullOrEmpty(ProjectPath) || !_fileSystem.DirectoryExists(projectDirectory))
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

            string developmentSettingsPath = Path.Combine(projectDirectory, "appsettings.Development.json");

            try
            {
                JsonObject developmentSettings = ReadDevelopmentSettings(developmentSettingsPath);
                JsonObject generatedAzureAd = CreateAzureAdConfiguration(
                    Username,
                    TenantId,
                    ClientId,
                    Domain,
                    Instance,
                    CallbackPath);
                JsonNode? existingAzureAd = developmentSettings["AzureAd"];

                if (!Overwrite && existingAzureAd is JsonObject existingAzureAdObject)
                {
                    string? conflictingKey = GetFirstConflictingAzureAdKey(existingAzureAdObject, generatedAzureAd);
                    if (conflictingKey is not null)
                    {
                        LogConflict(conflictingKey);
                        return Task.FromResult(false);
                    }
                }
                else if (!Overwrite && existingAzureAd is not null)
                {
                    LogConflict("AzureAd");
                    return Task.FromResult(false);
                }

                context.Properties["CallbackPath"] = generatedAzureAd["CallbackPath"]!.ToString();

                if (existingAzureAd is JsonObject matchingAzureAd &&
                    HasMatchingManagedAzureAdSettings(matchingAzureAd, generatedAzureAd))
                {
                    _logger.LogInformation("No changes needed for AzureAd configuration in appsettings.Development.json.");
                    LogDevelopmentEnvironmentNotice();
                    return Task.FromResult(true);
                }

                JsonObject targetAzureAd = existingAzureAd as JsonObject ?? new JsonObject();
                foreach ((string key, JsonNode? value) in generatedAzureAd)
                {
                    targetAzureAd[key] = value?.DeepClone();
                }

                if (existingAzureAd is not JsonObject)
                {
                    developmentSettings["AzureAd"] = targetAzureAd;
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                _fileSystem.WriteAllText(developmentSettingsPath, developmentSettings.ToJsonString(options));
                _logger.LogInformation($"Updated '{Path.GetFileName(developmentSettingsPath)}' with AzureAd development configuration.");
                LogDevelopmentEnvironmentNotice();
                return Task.FromResult(true);
            }
            catch (JsonException ex)
            {
                _logger.LogError($"Failed to parse appsettings.Development.json file at {developmentSettingsPath}: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update appsettings.Development.json: {ex.Message}");
            }

            return Task.FromResult(false);
        }

        private JsonObject ReadDevelopmentSettings(string developmentSettingsPath)
        {
            if (!_fileSystem.FileExists(developmentSettingsPath))
            {
                _logger.LogInformation($"Creating new appsettings.Development.json file at {developmentSettingsPath}");
                return new JsonObject(new JsonNodeOptions { PropertyNameCaseInsensitive = true });
            }

            JsonNode? parsedSettings = ParseDevelopmentSettings(_fileSystem.ReadAllText(developmentSettingsPath));
            if (parsedSettings is null)
            {
                return new JsonObject(new JsonNodeOptions { PropertyNameCaseInsensitive = true });
            }

            if (parsedSettings is not JsonObject developmentSettings)
            {
                throw new JsonException("The root JSON value must be an object.");
            }

            return developmentSettings;
        }

        private void LogConflict(string key)
        {
            _logger.LogError(
                $"Conflicting AzureAd value for key '{key}' already exists in appsettings.Development.json. " +
                "Re-run with '--overwrite' to replace existing managed settings.");
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
            string? username,
            string? tenantId,
            string? clientId,
            string? domain = null,
            string? instance = null,
            string? callbackPath = null)
        {
            return new JsonObject
            {
                ["Instance"] = !string.IsNullOrWhiteSpace(instance) ? instance : DefaultInstance,
                ["TenantId"] = tenantId,
                ["Domain"] = ResolveAzureAdDomain(domain, username),
                ["ClientId"] = clientId,
                ["CallbackPath"] = !string.IsNullOrWhiteSpace(callbackPath) ? callbackPath : DefaultCallbackPath
            };
        }

        internal static string? GetFirstConflictingAzureAdKey(
            JsonObject existingAzureAd,
            JsonObject generatedAzureAd)
        {
            foreach ((string key, JsonNode? value) in generatedAzureAd)
            {
                string? existingValue = existingAzureAd[key]?.ToString();
                if (!string.IsNullOrEmpty(existingValue) &&
                    !JsonNode.DeepEquals(existingAzureAd[key], value))
                {
                    return key;
                }
            }

            return null;
        }

        private static string ResolveAzureAdDomain(string? configuredDomain, string? username)
        {
            if (!string.IsNullOrWhiteSpace(configuredDomain))
            {
                return configuredDomain;
            }

            string resolvedUsername = username ?? string.Empty;
            int atSignIndex = resolvedUsername.LastIndexOf('@');
            return atSignIndex >= 0 && atSignIndex < resolvedUsername.Length - 1
                ? resolvedUsername[(atSignIndex + 1)..]
                : $"{resolvedUsername}.onmicrosoft.com";
        }

        private static bool HasMatchingManagedAzureAdSettings(
            JsonObject existingAzureAd,
            JsonObject generatedAzureAd)
        {
            foreach ((string key, JsonNode? value) in generatedAzureAd)
            {
                if (!JsonNode.DeepEquals(existingAzureAd[key], value))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
