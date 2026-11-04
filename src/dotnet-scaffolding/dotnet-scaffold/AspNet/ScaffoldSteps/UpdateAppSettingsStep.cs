// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.Extensions.Logging;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings
{
    /// <summary>
    /// Scaffold step to update Azure AD configuration in appsettings.json and appsettings.Development.json files.
    /// </summary>
    internal class UpdateAppSettingsStep : ScaffoldStep
    {
        private static readonly JsonNodeOptions ConfigurationNodeOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly JsonDocumentOptions ConfigurationJsonOptions = new()
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

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
            var succeeded = false;
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
                // Validate project path
                if (string.IsNullOrEmpty(ProjectPath) || baseProjectPath is null || !_fileSystem.DirectoryExists(baseProjectPath))
                {
                    _logger.LogError($"Invalid project path: {ProjectPath}");
                    return false;
                }

                // Find or create appsettings.json file
                var appSettingsFileSearch = _fileSystem.EnumerateFiles(baseProjectPath, "appsettings.json", SearchOption.AllDirectories);

                string? appSettingsFile = null;

                if (appSettingsFileSearch.Any())
                {
                    appSettingsFile = appSettingsFileSearch.FirstOrDefault();
                }

                JsonNode? content;
                string? originalContent = null;
                bool writeContent = false;

                // If appsettings.json doesn't exist, create a new one
                if (string.IsNullOrEmpty(appSettingsFile) || !_fileSystem.FileExists(appSettingsFile))
                {
                    appSettingsFile = Path.Combine(baseProjectPath, "appsettings.json");
                    content = new JsonObject();
                    writeContent = true;
                    _logger.LogInformation($"Creating new appsettings.json file at {appSettingsFile}");
                }
                else
                {
                    // Read existing appsettings.json
                    var jsonString = _fileSystem.ReadAllText(appSettingsFile);
                    originalContent = jsonString;
                    try
                    {
                        content = JsonNode.Parse(jsonString, ConfigurationNodeOptions, ConfigurationJsonOptions);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError($"Failed to parse appsettings.json file at {appSettingsFile}: {ex.Message}");
                        return false;
                    }
                }

                if (content is null)
                {
                    _logger.LogError($"Failed to parse or create appsettings.json file at {appSettingsFile}");
                    return false;
                }

                // Look for the "AzureAd" node or create it if it doesn't exist
                const string azureAdNodeName = "AzureAd";
                if (content[azureAdNodeName] is null)
                {
                    writeContent = true;
                    content[azureAdNodeName] = new JsonObject();
                }

                // Update AzureAd configuration properties
                if (content[azureAdNodeName] is JsonObject azureAdObject)
                {
                    // Update properties only if they have values
                    if (!string.IsNullOrEmpty(ClientId) && (azureAdObject["ClientId"] is null || azureAdObject["ClientId"]?.ToString() != ClientId))
                    {
                        writeContent = true;
                        azureAdObject["ClientId"] = ClientId;
                    }
                    if (!string.IsNullOrEmpty(Domain) && (azureAdObject["Domain"] is null || azureAdObject["Domain"]?.ToString() != Domain))
                    {
                        writeContent = true;
                        azureAdObject["Domain"] = Domain;
                    }
                    else if (string.IsNullOrEmpty(Domain) &&
                        !string.IsNullOrEmpty(Username) && Username.Contains('@') &&
                        azureAdObject["Domain"]?.ToString() == $"{Username}.onmicrosoft.com")
                    {
                        // Remove only the malformed value generated by the old username-based default.
                        azureAdObject.Remove("Domain");
                        writeContent = true;
                    }

                    if (!string.IsNullOrEmpty(TenantId) && (azureAdObject["TenantId"] is null || azureAdObject["TenantId"]?.ToString() != TenantId))
                    {
                        writeContent = true;
                        azureAdObject["TenantId"] = TenantId;
                    }

                    if (!string.IsNullOrEmpty(Instance) && (azureAdObject["Instance"] is null || azureAdObject["Instance"]?.ToString() != Instance))
                    {
                        writeContent = true;
                        azureAdObject["Instance"] = Instance;
                    }
                    else
                    {
                        writeContent = true;
                        azureAdObject["Instance"] = "https://login.microsoftonline.com/";
                    }

                    if (!string.IsNullOrEmpty(CallbackPath) && (azureAdObject["CallbackPath"] is null || azureAdObject["CallbackPath"]?.ToString() != CallbackPath))
                    {
                        writeContent = true;
                        azureAdObject["CallbackPath"] = CallbackPath;
                    }
                    else
                    {
                        writeContent = true;
                        azureAdObject["CallbackPath"] = "/signin-oidc";
                    }
                    // Update the AzureAd node
                    content[azureAdNodeName] = azureAdObject;
                }
                else
                {
                    _logger.LogError("The AzureAd section in {AppSettingsFile} must be a JSON object.", appSettingsFile);
                    return false;
                }

                // Write the updated content if changes were made
                if (writeContent && !string.IsNullOrEmpty(appSettingsFile))
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };

                    var changes = new List<(string Path, string? Original, string Updated)>
                    {
                        (appSettingsFile, originalContent, content.ToJsonString(options))
                    };
                    if (!PrepareDevelopmentSettings(baseProjectPath, content, changes))
                    {
                        return false;
                    }
                    return PersistSettings(changes);
                }
                else
                {
                    _logger.LogInformation("No changes needed for AzureAd configuration in appsettings.json");
                }

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to persist Entra ID application settings.");
            }

            return false;

        }

        /// <summary>
        /// Prepares the appsettings.Development.json update before any settings are written.
        /// </summary>
        /// <param name="baseProjectPath">Base project path.</param>
        /// <param name="content">Content of the appsettings.json file.</param>
        /// <param name="changes">Pending settings writes and their original contents.</param>
        private bool PrepareDevelopmentSettings(string baseProjectPath, JsonNode content,
            List<(string Path, string? Original, string Updated)> changes)
        {
            var devSettingsPath = Path.Combine(baseProjectPath, "appsettings.Development.json");

            if (_fileSystem.FileExists(devSettingsPath))
            {
                try
                {
                    JsonNode? devContent;
                    var devJsonString = _fileSystem.ReadAllText(devSettingsPath);

                    try
                    {
                        devContent = JsonNode.Parse(devJsonString, ConfigurationNodeOptions, ConfigurationJsonOptions);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Failed to parse appsettings.Development.json at {Path}.", devSettingsPath);
                        return false;
                    }

                    if (devContent is not JsonObject)
                    {
                        _logger.LogError("appsettings.Development.json at {Path} must contain a JSON object.", devSettingsPath);
                        return false;
                    }

                    if (content["AzureAd"] is JsonObject sourceAzureAd)
                    {
                        if (devContent["AzureAd"] is not null && devContent["AzureAd"] is not JsonObject)
                        {
                            _logger.LogError("The AzureAd section in {Path} must be a JSON object.", devSettingsPath);
                            return false;
                        }

                        var developmentAzureAd = devContent["AzureAd"] as JsonObject ?? new JsonObject();
                        foreach (var property in new[] { "ClientId", "TenantId", "Instance", "CallbackPath" })
                        {
                            if (sourceAzureAd.TryGetPropertyValue(property, out var value))
                            {
                                developmentAzureAd[property] = value?.DeepClone();
                            }
                        }
                        if (!string.IsNullOrEmpty(Domain))
                        {
                            developmentAzureAd["Domain"] = Domain;
                        }
                        else if (!string.IsNullOrEmpty(Username) && Username.Contains('@') &&
                            developmentAzureAd["Domain"]?.ToString() == $"{Username}.onmicrosoft.com")
                        {
                            developmentAzureAd.Remove("Domain");
                        }
                        devContent["AzureAd"] = developmentAzureAd;

                        var options = new JsonSerializerOptions { WriteIndented = true };
                        changes.Add((devSettingsPath, devJsonString, devContent.ToJsonString(options)));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to prepare appsettings.Development.json.");
                    return false;
                }
            }
            return true;
        }

        private bool PersistSettings(List<(string Path, string? Original, string Updated)> changes)
        {
            var attempted = new List<(string Path, string? Original)>();
            try
            {
                foreach (var change in changes)
                {
                    // A failed write may have truncated the file, so include it in recovery.
                    attempted.Add((change.Path, change.Original));
                    _fileSystem.WriteAllText(change.Path, change.Updated);
                    _logger.LogInformation("Updated '{FileName}' with AzureAd configuration", Path.GetFileName(change.Path));
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist Entra ID settings. Restoring attempted settings writes.");
                foreach (var change in attempted.AsEnumerable().Reverse())
                {
                    try
                    {
                        if (change.Original is null)
                        {
                            if (_fileSystem.FileExists(change.Path))
                            {
                                _fileSystem.DeleteFile(change.Path);
                            }
                        }
                        else if (!_fileSystem.FileExists(change.Path) ||
                            _fileSystem.ReadAllText(change.Path) != change.Original)
                        {
                            _fileSystem.WriteAllText(change.Path, change.Original);
                        }
                    }
                    catch (Exception restoreException)
                    {
                        _logger.LogError(restoreException,
                            "Could not restore settings file {Path}. Restore this file manually before retrying.", change.Path);
                    }
                }
                return false;
            }
        }
    }
}
