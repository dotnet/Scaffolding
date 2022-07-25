using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.MSIdentity.AuthenticationParameters;
using Microsoft.DotNet.MSIdentity.Tool;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.MSIdentity.MicrosoftIdentityPlatform
{
    internal class AppSettingsModifier
    {
        private const string MicrosoftGraph = nameof(MicrosoftGraph);
        private const string DownstreamApi = nameof(DownstreamApi);
        private const string ServerApi = nameof(ServerApi);
        internal static string AppSettingsFileName = "appsettings.json";

        internal static Dictionary<string, string?>? _propertiesDictionary;
        internal static Dictionary<string, string?> PropertiesDictionary => _propertiesDictionary ??= new Dictionary<string, string?>(
            typeof(DefaultProperties).GetFields().ToDictionary(x => x.Name, x => x.GetValue(x) as string));

        public AppSettingsModifier(ProvisioningToolOptions provisioningToolOptions)
        {
            _provisioningToolOptions = provisioningToolOptions;
        }

        private readonly ProvisioningToolOptions _provisioningToolOptions;

        /// <summary>
        /// Adds 'AzureAd', 'MicrosoftGraph' or 'DownstreamAPI' sections as appropriate. Fills them with default values if empty. 
        /// </summary>
        /// <param name="applicationParameters"></param>
        public void ModifyAppSettings(ApplicationParameters applicationParameters, IEnumerable<string> files)
        {
            // Default values can be found https://github.com/dotnet/aspnetcore/tree/main/src/ProjectTemplates/Web.ProjectTemplates/content
            // waiting for https://github.com/dotnet/runtime/issues/29690 + https://github.com/dotnet/runtime/issues/31068 to switch over to System.Text.Json

            /** TODO: 
             *       string jsonText = Encoding.UTF8.GetString(fileContent);
             * 
             *      return JsonSerializer.Deserialize<ProjectDescription>(jsonText, serializerOptionsWithComments);
             *      static readonly JsonSerializerOptions serializerOptionsWithComments = new JsonSerializerOptions()
             *      {
             *       ReadCommentHandling = JsonCommentHandling.Skip
             *      };
             */

            if (!File.Exists(_provisioningToolOptions.AppSettingsFilePath))
            {
                // If default appsettings file does not exist, try to find it, if not found, create in the default location
                var defaultAppSettingsPath = DefaultAppSettingsPath;
                _provisioningToolOptions.AppSettingsFilePath = File.Exists(defaultAppSettingsPath) ? defaultAppSettingsPath
                    : files.FirstOrDefault(f => f.Contains(AppSettingsFileName)) ?? defaultAppSettingsPath;
            }

            JObject appSettings;
            try
            {
                appSettings = JObject.Parse(System.IO.File.ReadAllText(_provisioningToolOptions.AppSettingsFilePath)) ?? new JObject();
            }
            catch
            {
                appSettings = new JObject();
            }

            var modifiedAppSettings = GetModifiedAppSettings(appSettings, applicationParameters);

            // TODO: save comments somehow, only write to appsettings.json if changes are made
            if (modifiedAppSettings != null)
            {
                System.IO.File.WriteAllText(_provisioningToolOptions.AppSettingsFilePath, modifiedAppSettings.ToString());
            }
        }

        private string DefaultAppSettingsPath => _provisioningToolOptions.IsBlazorWasm
            ? Path.Combine(_provisioningToolOptions.ProjectPath, "wwwroot", AppSettingsFileName)
            : Path.Combine(_provisioningToolOptions.ProjectPath, AppSettingsFileName);

        /// <summary>
        /// Modifies AppSettings.json if necessary, helper method for testing
        /// </summary>
        /// <param name="appSettings"></param>
        /// <param name="applicationParameters"></param>
        /// <returns>appSettings JObject if changes were made, else null</returns>
        internal JObject? GetModifiedAppSettings(JObject appSettings, ApplicationParameters applicationParameters)
        {
            // update Azure AD Block
            (bool changesMade, JObject? updatedAzureAdBlock) = GetModifiedAzureAdBlock(appSettings, applicationParameters);
            if (changesMade)
            {
                appSettings["AzureAd"] = updatedAzureAdBlock;
            }

            if (_provisioningToolOptions.CallsGraph)
            {
                // update MicrosoftGraph Block
                var microsoftGraphBlock = GetApiBlock(appSettings, MicrosoftGraph, DefaultProperties.MicrosoftGraphScopes, DefaultProperties.MicrosoftGraphBaseUrl);
                if (microsoftGraphBlock != null)
                {
                    changesMade = true;
                    appSettings["MicrosoftGraph"] = microsoftGraphBlock;
                }
            }

            if (_provisioningToolOptions.CallsDownstreamApi)
            {
                // update DownstreamAPI Block
                var updatedDownstreamApiBlock = GetApiBlock(appSettings, DownstreamApi, DefaultProperties.ApiScopes, DefaultProperties.MicrosoftGraphBaseUrl);
                if (updatedDownstreamApiBlock != null)
                {
                    changesMade = true;
                    appSettings["DownstreamApi"] = updatedDownstreamApiBlock;
                }
            }

            if (!string.IsNullOrEmpty(_provisioningToolOptions.HostedApiScopes)) // Blazor Wasm Hosted scenario
            {
                // update ServerApi Block
                var serverApiBlock = GetApiBlock(appSettings, ServerApi, scopes: _provisioningToolOptions.HostedApiScopes, _provisioningToolOptions.HostedAppIdUri);
                if (serverApiBlock != null)
                {
                    changesMade = true;
                    appSettings[ServerApi] = serverApiBlock;
                }
            }

            return changesMade ? appSettings : null;
        }

        /// <summary>
        /// Returns bool stating if changes were made and updated AzureAd appSettings JObject if any changes were made
        /// </summary>
        /// <param name="appSettings"></param>
        /// <param name="applicationParameters"></param>
        /// <returns>(bool changesMade, JObject? updatedBlock)</returns>
        internal (bool changesMade, JObject? updatedBlock) GetModifiedAzureAdBlock(JObject appSettings, ApplicationParameters applicationParameters)
        {
            var azAdToken = appSettings.GetValue("AzureAd") ?? appSettings.GetValue("AzureAdB2C"); // TODO test AzureAdB2C
            if (azAdToken is null)
            {
                return (true, new AzureAdBlock(applicationParameters).ToJObject());
            }

            var existingParameters = JObject.FromObject(azAdToken);
            var newBlock = new AzureAdBlock(applicationParameters, existingParameters).ToJObject();

            return (NeedsUpdate(existingParameters, newBlock), newBlock);
        }

        /// <summary>
        /// Checks all keys in updatedBlock, if any differ from existingBlock then update is necessary
        /// </summary>
        /// <param name="existingBlock"></param>
        /// <param name="newBlock"></param>
        /// <returns></returns>
        internal static bool NeedsUpdate(JObject existingBlock, JObject newBlock)
        {
            foreach ((var key, var updatedValue) in newBlock)
            {
                if (existingBlock.GetValue(key) != updatedValue)
                {
                    return true;
                }
            }

            return false;
        }

        internal static JObject? GetApiBlock(JObject appSettings, string key, string? scopes, string? baseUrl)
        {
            var inputParameters = JObject.FromObject(new ApiSettingsBlock
            {
                Scopes = string.IsNullOrEmpty(scopes) ? DefaultProperties.MicrosoftGraphScopes : scopes,
                BaseUrl = string.IsNullOrEmpty(baseUrl) ? DefaultProperties.MicrosoftGraphBaseUrl : baseUrl
            });

            if (appSettings.TryGetValue(key, out var apiToken))
            {
                // block exists
                var existingBlock = JObject.FromObject(apiToken);
                return ModifyAppSettingsObject(existingBlock, inputParameters) ? existingBlock : null;
            }

            // block does not exist, create a new one
            return inputParameters;
        }

        internal static bool ModifyAppSettingsObject(JObject existingSettings, JObject inputProperties)
        {
            bool changesMade = false;
            foreach ((var propertyName, var newValue) in inputProperties)
            {
                changesMade |= UpdateIfNecessary(existingSettings, propertyName, newValue);
            }

            return changesMade;
        }

        /// <summary>
        /// Updates property in appSettings block when either there is no existing property or the existing property does not match
        /// </summary>
        /// <param name="block"></param>
        /// <param name="propertyName"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        internal static bool UpdateIfNecessary(JObject block, string propertyName, JToken? newValue)
        {
            if (!block.TryGetValue(propertyName, out var existingValue))
            {
                block.Add(propertyName, newValue);
                return true;
            }

            (bool needsUpdate, JToken? update) = GetUpdatedValue(propertyName, existingValue, newValue);
            if (needsUpdate)
            {
                block[propertyName] = update;
            }

            return needsUpdate;
        }

        /// <summary>
        /// Helper method for unit tests
        /// </summary>
        /// <param name="inputProperty"></param>
        /// <param name="existingProperties"></param>
        /// <returns>updated value or null if no update necessary</returns>
        internal static (bool needsUpdate, JToken? update) GetUpdatedValue(string propertyName, JToken? existingValue, JToken? newValue)
        {
            bool needsUpdate = false;
            JToken? updatedValue = null;

            // If there is no existing property, update 
            if (existingValue is null || string.IsNullOrEmpty(existingValue.ToString()))
            {
                needsUpdate = true;
                updatedValue = string.IsNullOrEmpty(newValue?.ToString()) ? PropertiesDictionary.GetValueOrDefault(propertyName) : newValue;
            }

            // If newValue exists and it differs from the existing property, update value
            if (newValue != null && !string.IsNullOrEmpty(newValue.ToString()) && !newValue.Equals(existingValue))
            {
                needsUpdate = true;
                updatedValue = newValue;
            }

            return (needsUpdate, updatedValue);
        }
    }
}
