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
                var microsoftGraphBlock = GetModifiedMicrosoftGraphBlock(appSettings);
                if (microsoftGraphBlock != null)
                {
                    changesMade = true;
                    appSettings["MicrosoftGraph"] = microsoftGraphBlock;
                }
            }

            if (_provisioningToolOptions.CallsDownstreamApi)
            {
                // update DownstreamAPI Block
                var updatedDownstreamApiBlock = GetModifiedDownstreamApiBlock(appSettings);
                if (updatedDownstreamApiBlock != null)
                {
                    changesMade = true;
                    appSettings["DownstreamApi"] = updatedDownstreamApiBlock;
                }
            }

            if (!string.IsNullOrEmpty(_provisioningToolOptions.HostedApiScopes))
            {
                // update ServerAPI Block
                changesMade = true;
                appSettings["ServerApi"] = _provisioningToolOptions.HostedApiScopes;
            }

            return changesMade ? appSettings : null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appSettings"></param>
        /// <param name="applicationParameters"></param>
        /// <returns></returns>
        internal JToken? GetModifiedAzureAdBlock(JObject appSettings, ApplicationParameters applicationParameters)
        {
            var azureAdBlock = JObject.FromObject(GetAzureAdBlock(applicationParameters));
            if (!appSettings.TryGetValue("AzureAd", out var azureAdToken))
            {
                // Create and return AzureAd block if none exists, differs for Blazor apps
                return JToken.FromObject(azureAdBlock);
            }
           
            bool changesMade = ModifyAppSettingsObject(azureAdToken, azureAdBlock);

            if (_provisioningToolOptions.CallsGraph || _provisioningToolOptions.CallsDownstreamApi)
            {
                changesMade |= ModifyCredentials(azureAdToken);
            }

            return changesMade ? azureAdToken : null;
        }

        /// <summary>
        /// Create and return AzureAd block when none exists, fields differ for Blazor apps
        /// </summary>
        /// <param name="applicationParameters"></param>
        /// <param name="isBlazorWasm"></param>
        /// <returns></returns>
        internal static AppSettings GetAzureAdBlock(ApplicationParameters applicationParameters)
        {
            if (applicationParameters.IsBlazorWasm)
            {
                return new BlazorSettings
                {
                    Authority = applicationParameters.Authority ?? DefaultProperties.Authority,
                    ClientId = applicationParameters.ClientId ?? DefaultProperties.ClientId,
                    ValidateAuthority = DefaultProperties.ValidateAuthority
                };
            }
            if (applicationParameters.IsWebApi.GetValueOrDefault())
            {
                return new WebAPISettings
                {
                    Domain = applicationParameters.Domain ?? DefaultProperties.Domain,
                    TenantId = applicationParameters.TenantId ?? DefaultProperties.TenantId,
                    ClientId = applicationParameters.ClientId ?? DefaultProperties.ClientId,
                    Instance = applicationParameters.Instance ?? DefaultProperties.Instance,
                    CallbackPath = applicationParameters.CallbackPath ?? DefaultProperties.CallbackPath,
                    Scopes = applicationParameters.CalledApiScopes ?? DefaultProperties.DefaultScopes
                };
            }

            return new WebAppSettings
            {
                Domain = applicationParameters.Domain ?? DefaultProperties.Domain,
                TenantId = applicationParameters.TenantId ?? DefaultProperties.TenantId,
                ClientId = applicationParameters.ClientId ?? DefaultProperties.ClientId,
                Instance = applicationParameters.Instance ?? DefaultProperties.Instance,
                CallbackPath = applicationParameters.CallbackPath ?? DefaultProperties.CallbackPath
            };
        }

        private bool ModifyAppSettingsObject(JToken existingSettings, JObject inputProperties)
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

        private JToken? GetModifiedMicrosoftGraphBlock(JObject appSettings)
        {
            if (!appSettings.TryGetValue("MicrosoftGraph", out var microsoftGraphToken))
            {
                return DefaultApiBlock();
            }

            var inputParameters = JObject.FromObject(new ApiSettings
            {
                Scopes = DefaultProperties.DefaultScopes,
                BaseUrl = DefaultProperties.MicrosoftGraphBaseUrl 
            });

            return ModifyAppSettingsObject(microsoftGraphToken, inputParameters) ? microsoftGraphToken : null;
        }

        internal JToken? GetModifiedDownstreamApiBlock(JObject appSettings)
        {
            var downstreamApiBlock = JObject.FromObject(new ApiSettings());
            if (!appSettings.TryGetValue("DownstreamApi", out var downstreamApiToken))
            {
                return downstreamApiBlock;
            }

            return ModifyAppSettingsObject(downstreamApiToken, downstreamApiBlock) ? downstreamApiToken : null;
        }

        internal static JObject DefaultApiBlock()
        {
            return JObject.FromObject(new ApiSettings
            {
                BaseUrl = DefaultProperties.MicrosoftGraphBaseUrl,
                Scopes = DefaultProperties.DefaultScopes
            });
        }

        /// <summary>
        /// Updates property in appSettings block when either there is no existing property or the existing property does not match
        /// </summary>
        /// <param name="token"></param>
        /// <param name="propertyName"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        private bool UpdateIfNecessary(JToken token, string propertyName, JToken? newValue)
        {
            var existingValue = token[propertyName];
            var update = GetUpdatedValue(propertyName, existingValue, newValue);
            if (update != null)
            {
                token[propertyName] = update;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Helper method for unit tests
        /// </summary>
        /// <param name="inputProperty"></param>
        /// <param name="existingProperties"></param>
        /// <returns>updated value or null if no update necessary</returns>
        internal static JToken? GetUpdatedValue(string propertyName, JToken? existingValue, JToken? newValue)
        {
            // If there is no existing property, update 
            if (existingValue is null)
            {
                return newValue ?? PropertiesDictionary.GetValueOrDefault(propertyName);
            }

            // If there is not a new value, do nothing
            //if (newValue is null || !newValue.Any())
            //{
            //    return null; // No updates necessary
            //}

            // If newValue exists and it differs from the existing property, update value
            if (newValue != null && !newValue.Equals(existingValue))
            {
                return newValue;
            }

            return null; // No updates necessary
        }
    }
}
