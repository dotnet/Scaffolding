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

        public AppSettingsModifier(ProvisioningToolOptions provisioningToolOptions)
        {
            ProvisioningToolOptions = provisioningToolOptions;
        }

        public ProvisioningToolOptions ProvisioningToolOptions { get; }

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


            ProvisioningToolOptions.AppSettingsFilePath = GetAppSettingsFilePath(files);

            JObject appSettings;
            try
            {
                appSettings = JObject.Parse(System.IO.File.ReadAllText(ProvisioningToolOptions.AppSettingsFilePath)) ?? new JObject();
            }
            catch
            {
                appSettings = new JObject();
            }

            var modifiedAppSettings = GetModifiedAppSettings(appSettings, applicationParameters);

            // TODO: save comments somehow, only write to appsettings.json if changes are made
            if (modifiedAppSettings != null)
            {
                System.IO.File.WriteAllText(ProvisioningToolOptions.AppSettingsFilePath, modifiedAppSettings.ToString());
            }
        }

        /// <summary>
        /// First checks if default appsettings file exists, if not searches for the file.
        /// If the file does not exist anywhere, it will be created later.
        /// </summary>
        private string GetAppSettingsFilePath(IEnumerable<string> files)
        {
            if (File.Exists(ProvisioningToolOptions.AppSettingsFilePath))
            {
                return ProvisioningToolOptions.AppSettingsFilePath!;
            }

            // In the majority of cases, this will give the correct appsettings file path
            if (File.Exists(DefaultAppSettingsPath))
            {
                return DefaultAppSettingsPath;
            }

            // If default appsettings file does not exist, try to find it elsewhere
            var filePath = files.Where(f => f.Contains(AppSettingsFileName)).FirstOrDefault();

            // If not found, it will be created in the default location
            return filePath ?? DefaultAppSettingsPath;
        }

        private string DefaultAppSettingsPath => ProvisioningToolOptions.IsBlazorWasm
        ? Path.Combine(ProvisioningToolOptions.ProjectPath, "wwwroot", AppSettingsFileName)
        : Path.Combine(ProvisioningToolOptions.ProjectPath, AppSettingsFileName);

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

            if (ProvisioningToolOptions.CallsGraph) // TODO blazor
            {
                // update MicrosoftGraph Block
                var microsoftGraphBlock = GetModifiedMicrosoftGraphBlock(appSettings);
                if (microsoftGraphBlock != null)
                {
                    changesMade = true;
                    appSettings["MicrosoftGraph"] = microsoftGraphBlock;
                }
            }

            if (ProvisioningToolOptions.CallsDownstreamApi) // TODO blazor
            {
                // update DownstreamAPI Block
                var updatedDownstreamApiBlock = GetModifiedDownstreamApiBlock(appSettings);
                if (updatedDownstreamApiBlock != null)
                {
                    changesMade = true;
                    appSettings["DownstreamApi"] = updatedDownstreamApiBlock;
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
        private JToken? GetModifiedAzureAdBlock(JObject appSettings, ApplicationParameters applicationParameters)
        {
            if (!appSettings.TryGetValue("AzureAd", out var azureAdToken))
            {
                // Create and return AzureAd block if none exists, differs for Blazor apps
                return AzureAdBlock(applicationParameters);
            }

            bool changesMade = ModifyAppSettingsToken(azureAdToken, applicationParameters.AppSettingsProperties);

            if (ProvisioningToolOptions.CallsGraph || ProvisioningToolOptions.CallsDownstreamApi)
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
        private static JToken AzureAdBlock(ApplicationParameters applicationParameters)
        {
            return applicationParameters.IsBlazorWasm ? JToken.FromObject(new
            {
                Authority = applicationParameters.Authority ?? DefaultProperties.Authority,
                ClientId = applicationParameters.ClientId ?? DefaultProperties.ClientId,
                DefaultProperties.ValidateAuthority
            }) : JToken.FromObject(new
            {
                Domain = applicationParameters.Domain ?? DefaultProperties.Domain,
                TenantId = applicationParameters.TenantId ?? DefaultProperties.TenantId,
                ClientId = applicationParameters.ClientId ?? DefaultProperties.ClientId,
                Instance = applicationParameters.Instance ?? DefaultProperties.Instance,
                CallbackPath = applicationParameters.CallbackPath ?? DefaultProperties.CallbackPath
            });
        }

        private bool ModifyAppSettingsToken(JToken token, Dictionary<string, string?> inputProperties)
        {
            bool changesMade = false;
            foreach ((string propertyName, string? newValue) in inputProperties)
            {
                changesMade |= UpdateStringIfNecessary(token, propertyName, newValue);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appSettings"></param>
        /// <param name="changesMade"></param>
        /// <returns></returns>
        private JToken? GetModifiedMicrosoftGraphBlock(JObject appSettings)
        {
            if (!appSettings.TryGetValue("MicrosoftGraph", out var microsoftGraphToken))
            {
                return JToken.FromObject(DefaultProperties.MicrosoftGraphDefaults);
            }

            var inputParameters = new Dictionary<string, string?>
            {
                { PropertyNames.Scopes, DefaultProperties.MicrosoftGraphScopes },
                { PropertyNames.BaseUrl, DefaultProperties.MicrosoftGraphBaseUrl }
            };

            return ModifyAppSettingsToken(microsoftGraphToken, inputParameters) ? microsoftGraphToken : null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appSettings"></param>
        /// <param name="changesMade"></param>
        /// <returns></returns>
        private JToken? GetModifiedDownstreamApiBlock(JObject appSettings)
        {
            if (!appSettings.TryGetValue("DownstreamApi", out var downstreamApiToken))
            {
                return JToken.FromObject(DefaultProperties.MicrosoftGraphDefaults);
            }

            var inputParameters = new Dictionary<string, string?>
            {
                { PropertyNames.Scopes, "TODO SCOPES" },
                { PropertyNames.BaseUrl, "TODO Base URL" }
            };

            return ModifyAppSettingsToken(downstreamApiToken, inputParameters) ? downstreamApiToken : null;
        }

        /// <summary>
        /// Updates property in appSettings block when either there is no existing property or the existing property does not match
        /// </summary>
        /// <param name="inputProperty"></param>
        /// <param name="existingProperties"></param>
        /// <returns></returns>
        private bool UpdateStringIfNecessary(JToken token, string propertyName, string? newValue)
        {
            var existingValue = token[propertyName]?.ToString();
            var update = UpdatePropertyIfNecessary(propertyName, existingValue, newValue);
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
        /// <returns></returns>
        internal static string? UpdatePropertyIfNecessary(string propertyName, string? existingValue, string? newValue)
        {
            // If there is no existing property, update 
            if (string.IsNullOrEmpty(existingValue))
            {
                return string.IsNullOrEmpty(newValue) ? DefaultProperties.AllProperties[propertyName] : newValue;
            }

            // If newValue exists and it differs from the existing property, update value
            if (!string.IsNullOrEmpty(newValue) && !newValue.Equals(existingValue))
            {
                return newValue;
            }

            return null; // No updates necessary
        }
    }
}
