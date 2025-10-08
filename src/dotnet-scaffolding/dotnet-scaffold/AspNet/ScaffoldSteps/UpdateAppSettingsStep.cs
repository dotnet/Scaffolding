// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings
{
    /// <summary>
    /// Scaffold step to update Azure AD configuration in appsettings.json and appsettings.Development.json files.
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
                // Validate project path
                if (string.IsNullOrEmpty(ProjectPath) || baseProjectPath is null || !_fileSystem.DirectoryExists(baseProjectPath))
                {
                    _logger.LogError($"Invalid project path: {ProjectPath}");
                    return Task.FromResult(false);
                }                

                // Find or create appsettings.json file
                var appSettingsFileSearch = _fileSystem.EnumerateFiles(baseProjectPath, "appsettings.json", SearchOption.AllDirectories);

                string? appSettingsFile = null;

                if (appSettingsFileSearch.Any())
                {
                    appSettingsFile = appSettingsFileSearch.FirstOrDefault();
                }

                JsonNode? content;
                bool writeContent = false;

                // If appsettings.json doesn't exist, create a new one
                if (string.IsNullOrEmpty(appSettingsFile) || !_fileSystem.FileExists(appSettingsFile))
                {
                    appSettingsFile = Path.Combine(ProjectPath, "appsettings.json");
                    content = new JsonObject();
                    writeContent = true;
                    _logger.LogInformation($"Creating new appsettings.json file at {appSettingsFile}");
                }
                else
                {
                    // Read existing appsettings.json
                    var jsonString = _fileSystem.ReadAllText(appSettingsFile);
                    try
                    {
                        content = JsonNode.Parse(jsonString);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError($"Failed to parse appsettings.json file at {appSettingsFile}: {ex.Message}");
                        return Task.FromResult(false);
                    }
                }

                if (content is null)
                {
                    _logger.LogError($"Failed to parse or create appsettings.json file at {appSettingsFile}");
                    return Task.FromResult(false);
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
                    else
                    {
                        writeContent = true;
                        azureAdObject["Domain"] = $"{Username}.onmicrosoft.com";
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

                // Write the updated content if changes were made
                if (writeContent && !string.IsNullOrEmpty(appSettingsFile))
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };

                    try
                    {
                        _fileSystem.WriteAllText(appSettingsFile, content.ToJsonString(options));
                        _logger.LogInformation($"Updated '{Path.GetFileName(appSettingsFile)}' with AzureAd configuration");

                        // Also check for appsettings.Development.json and update it if present
                        UpdateDevelopmentSettings(ProjectPath, content);

                        return Task.FromResult(true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to write appsettings.json: {ex.Message}");
                        return Task.FromResult(false);
                    }
                }
                else
                {
                    _logger.LogInformation("No changes needed for AzureAd configuration in appsettings.json");
                }

                return Task.FromResult(true);
            }
            catch (Exception e)
            {
                AnsiConsole.WriteLine(e.ToString());
            }

            return Task.FromResult(false);
            
        }

        /// <summary>
        /// Updates the appsettings.Development.json file with AzureAd configuration.
        /// </summary>
        /// <param name="baseProjectPath">Base project path.</param>
        /// <param name="content">Content of the appsettings.json file.</param>
        private void UpdateDevelopmentSettings(string baseProjectPath, JsonNode content)
        {
            var devSettingsPath = Path.Combine(baseProjectPath, "appsettings.Development.json");

            if (_fileSystem.FileExists(devSettingsPath))
            {
                try
                {
                    _logger.LogInformation("Updating appsettings.Development.json with AzureAd configuration");

                    JsonNode? devContent;
                    var devJsonString = _fileSystem.ReadAllText(devSettingsPath);

                    try
                    {
                        devContent = JsonNode.Parse(devJsonString);
                    }
                    catch
                    {
                        // If there's an error parsing, create a new object
                        devContent = new JsonObject();
                    }

                    if (devContent != null && content["AzureAd"] != null)
                    {
                        // Copy the AzureAd section to development settings
                        devContent["AzureAd"] = content["AzureAd"]?.DeepClone();

                        var options = new JsonSerializerOptions { WriteIndented = true };
                        _fileSystem.WriteAllText(devSettingsPath, devContent.ToJsonString(options));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to update appsettings.Development.json: {ex.Message}");
                    // Continue execution even if this fails
                }
            }
        }
    }
}
