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
            bool changesMade = false;

            // update Azure AD Block
            var updatedAzureAdBlock = GetModifiedAzureAdBlock(appSettings, applicationParameters);
            if (updatedAzureAdBlock != null)
            {
                changesMade = true;
                appSettings["AzureAd"] = updatedAzureAdBlock;
            }

            if (_provisioningToolOptions.CallsGraph)
            {
                // update MicrosoftGraph Block
                var microsoftGraphBlock = GetApiBlock(appSettings, "MicrosoftGraph", DefaultProperties.DefaultScopes, DefaultProperties.MicrosoftGraphBaseUrl);
                if (microsoftGraphBlock != null)
                {
                    changesMade = true;
                    appSettings["MicrosoftGraph"] = microsoftGraphBlock;
                }
            }

            if (_provisioningToolOptions.CallsDownstreamApi)
            {
                // update DownstreamAPI Block
                var updatedDownstreamApiBlock = GetApiBlock(appSettings, "DownstreamApi", DefaultProperties.DefaultScopes, DefaultProperties.MicrosoftGraphBaseUrl);
                if (updatedDownstreamApiBlock != null)
                {
                    changesMade = true;
                    appSettings["DownstreamApi"] = updatedDownstreamApiBlock;
                }
            }

            if (!string.IsNullOrEmpty(_provisioningToolOptions.HostedApiScopes))
            {
                // update ServerApi Block
                var serverApiBlock = GetApiBlock(appSettings, "ServerApi", scopes: _provisioningToolOptions.HostedApiScopes, _provisioningToolOptions.HostedAppIdUri)
                if (serverApiBlock != null)
                {
                    changesMade = true;
                    appSettings["ServerApi"] = serverApiBlock;
                }
            }

            return changesMade ? appSettings : null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appSettings"></param>
        /// <param name="applicationParameters"></param>
        /// <returns></returns>
        internal JObject? GetModifiedAzureAdBlock(JObject appSettings, ApplicationParameters applicationParameters)
        {
            if (!appSettings.TryGetValue("AzureAd", out var azureAdToken))
            {
                // Create and return AzureAd block if none exists, differs for Blazor apps
                return JObject.FromObject(GetAzureAdParameters(applicationParameters, insertDefaults: true));
            }

            var existingParameters = JObject.FromObject(azureAdToken);
            var inputParameters = JObject.FromObject(GetAzureAdParameters(applicationParameters, insertDefaults: false));
            bool changesMade = ModifyAppSettingsObject(existingParameters, inputParameters);
            if (_provisioningToolOptions.CallsGraph || _provisioningToolOptions.CallsDownstreamApi)
            {
                changesMade |= ModifyCredentials(azureAdToken);
            }

            return changesMade ? existingParameters : null;
        }

        /// <summary>
        /// Create and return AzureAd block when none exists, fields differ for Blazor apps
        /// </summary>
        /// <param name="applicationParameters"></param>
        /// <param name="isBlazorWasm"></param>
        /// <returns></returns>
        internal static AzureAdBlock GetAzureAdParameters(ApplicationParameters applicationParameters, bool insertDefaults)
        {
            if (applicationParameters.IsBlazorWasm)
            {
                return new BlazorSettings
                {
                    Authority = applicationParameters.Authority ?? (insertDefaults ? DefaultProperties.Authority : null),
                    ClientId = applicationParameters.ClientId ?? (insertDefaults ? DefaultProperties.ClientId : null),
                    ValidateAuthority = DefaultProperties.ValidateAuthority
                };
            }
            if (applicationParameters.IsWebApi.GetValueOrDefault())
            {
                return new WebAPISettings
                {
                    Domain = applicationParameters.Domain ?? (insertDefaults ? DefaultProperties.Domain : null),
                    TenantId = applicationParameters.TenantId ?? (insertDefaults ? DefaultProperties.TenantId : null),
                    ClientId = applicationParameters.ClientId ?? (insertDefaults ? DefaultProperties.ClientId : null),
                    Instance = applicationParameters.Instance ?? (insertDefaults ? DefaultProperties.Instance : null),
                    CallbackPath = applicationParameters.CallbackPath ?? (insertDefaults ? DefaultProperties.CallbackPath : null),
                    Scopes = applicationParameters.CalledApiScopes ?? (insertDefaults ? DefaultProperties.DefaultScopes : null)
                };
            }

            return new WebAppSettings
            {
                Domain = applicationParameters.Domain ?? (insertDefaults ? DefaultProperties.Domain : null),
                TenantId = applicationParameters.TenantId ?? (insertDefaults ? DefaultProperties.TenantId : null),
                ClientId = applicationParameters.ClientId ?? (insertDefaults ? DefaultProperties.ClientId : null),
                Instance = applicationParameters.Instance ?? (insertDefaults ? DefaultProperties.Instance : null),
                CallbackPath = applicationParameters.CallbackPath ?? (insertDefaults ? DefaultProperties.CallbackPath : null)
            };
        }

        private bool ModifyAppSettingsObject(JObject existingSettings, JObject inputProperties)
        {
            bool changesMade = false;
            foreach ((var propertyName, var newValue) in inputProperties)
            {
                changesMade |= UpdateIfNecessary(existingSettings, propertyName, newValue);
            }

            return changesMade;
        }

        private bool ModifyCredentials(JToken azureAdToken)
        {
            bool changesMade = false;
            if (azureAdToken[PropertyNames.ClientSecret] == null)
            {
                changesMade = true;
                azureAdToken[PropertyNames.ClientSecret] = DefaultProperties.ClientSecret;
            }
            if (azureAdToken[PropertyNames.ClientCertificates] == null)
            {
                changesMade = true;
                azureAdToken[PropertyNames.ClientCertificates] = new JArray();
            }

            return changesMade;
        }

        private JObject? GetApiBlock(JObject appSettings, string key, string? scopes, string? baseUrl)
        {
            var inputParameters = JObject.FromObject(new ApiSettingsBlock
            {
                Scopes = string.IsNullOrEmpty(scopes) ? DefaultProperties.DefaultScopes : scopes,
                BaseUrl = string.IsNullOrEmpty(baseUrl) ? DefaultProperties.MicrosoftGraphBaseUrl : baseUrl
            });;

            if (appSettings.TryGetValue(key, out var apiToken))
            {
                // block exists
                var apiBlock = JObject.FromObject(apiToken);
                return ModifyAppSettingsObject(apiBlock, inputParameters) ? apiBlock : null;
            }
            // block does not exist, create a new one
            return inputParameters;
        }


        private JObject? GetModifiedMicrosoftGraphBlock(JObject appSettings)
        {
            if (!appSettings.TryGetValue("MicrosoftGraph", out var microsoftGraphToken))
            {
                return DefaultApiBlock();
            }

            var microsoftGraphBlock = JObject.FromObject(microsoftGraphToken);
            var inputParameters = JObject.FromObject(new ApiSettingsBlock
            {
                Scopes = DefaultProperties.DefaultScopes,
                BaseUrl = DefaultProperties.MicrosoftGraphBaseUrl
            });

            return ModifyAppSettingsObject(microsoftGraphBlock, inputParameters) ? microsoftGraphBlock : null;
        }

        internal JObject? GetModifiedDownstreamApiBlock(JObject appSettings)
        {
            var newBlock = JObject.FromObject(new ApiSettingsBlock());
            if (!appSettings.TryGetValue("DownstreamApi", out var downstreamApiToken))
            {
                return newBlock;
            }

            var existingBlock = JObject.FromObject(downstreamApiToken);

            return ModifyAppSettingsObject(existingBlock, newBlock) ? existingBlock : null;
        }

        internal static JObject DefaultApiBlock()
        {
            return JObject.FromObject(new ApiSettingsBlock
            {
                BaseUrl = DefaultProperties.MicrosoftGraphBaseUrl,
                Scopes = DefaultProperties.DefaultScopes
            });
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
            var existingValue = block[propertyName];
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
