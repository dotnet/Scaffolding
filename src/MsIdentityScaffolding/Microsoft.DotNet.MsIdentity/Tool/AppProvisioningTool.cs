// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.MSIdentity.Properties;
using Microsoft.DotNet.MSIdentity.AuthenticationParameters;
using Microsoft.DotNet.MSIdentity.CodeReaderWriter;
using Microsoft.DotNet.MSIdentity.DeveloperCredentials;
using Microsoft.DotNet.MSIdentity.MicrosoftIdentityPlatformApplication;
using Microsoft.DotNet.MSIdentity.Project;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.MSIdentity
{
    /// <summary>
    /// 
    /// </summary>
    public class AppProvisioningTool : IMsAADTool
    {
        private ProvisioningToolOptions ProvisioningToolOptions { get; set; }

        private string CommandName { get; }

        private MicrosoftIdentityPlatformApplicationManager MicrosoftIdentityPlatformApplicationManager { get; } = new MicrosoftIdentityPlatformApplicationManager();

        private ProjectDescriptionReader ProjectDescriptionReader { get; } = new ProjectDescriptionReader();

        public AppProvisioningTool(string commandName, ProvisioningToolOptions provisioningToolOptions)
        {
            CommandName = commandName;
            ProvisioningToolOptions = provisioningToolOptions;
        }

        public async Task<ApplicationParameters?> Run()
        {
            // If needed, infer project type from code
            ProjectDescription? projectDescription = ProjectDescriptionReader.GetProjectDescription(
                ProvisioningToolOptions.ProjectTypeIdentifier,
                ProvisioningToolOptions.ProjectPath);

            //get csproj file path
            var csProjfiles = Directory.EnumerateFiles(ProvisioningToolOptions.ProjectPath, "*.csproj");
            if (csProjfiles.Any())
            {
                var filePath = csProjfiles.First();
                ProvisioningToolOptions.ProjectCsProjPath = filePath;
            }
            //get appsettings.json file path
            var appSettingsFile = Directory.EnumerateFiles(ProvisioningToolOptions.ProjectPath, "appsettings.json");
            if (appSettingsFile.Any())
            {
                var filePath = appSettingsFile.First();
                ProvisioningToolOptions.AppSettingsFilePath = filePath;
            }

            if (projectDescription == null)
            {
                Console.WriteLine($"The code in {ProvisioningToolOptions.ProjectPath} wasn't recognized as supported by the tool. Rerun with --help for details.");
                return null;
            }
            else
            {
                Console.WriteLine($"Detected project type {projectDescription.Identifier}. ");
            }

            ProjectAuthenticationSettings projectSettings = InferApplicationParameters(
                ProvisioningToolOptions,
                projectDescription,
                ProjectDescriptionReader.projectDescriptions);

             // Get developer credentials
            TokenCredential tokenCredential = GetTokenCredential(
                ProvisioningToolOptions,
                projectSettings.ApplicationParameters.EffectiveTenantId ?? projectSettings.ApplicationParameters.EffectiveDomain);

            //for now, update project command is handlded seperately.
            //TODO: switch case to handle all the different commands.
            if (CommandName.Equals(Commands.UPDATE_PROJECT_COMMAND, StringComparison.OrdinalIgnoreCase))
            {
                // Read or provision Microsoft identity platform application
                ApplicationParameters? applicationParameters = await ReadMicrosoftIdentityApplication(
                    tokenCredential,
                    projectSettings.ApplicationParameters);

                if (applicationParameters != null)
                {
                    //modify appsettings.json. 
                    ModifyAppSettings(applicationParameters);

                    //Add ClientSecret if the app wants to call graph/a downstream api.
                    if (ProvisioningToolOptions.CallsGraph || ProvisioningToolOptions.CallsDownstreamApi)
                    {
                        var graphServiceClient =  MicrosoftIdentityPlatformApplicationManager.GetGraphServiceClient(tokenCredential);
                        //need ClientId and Microsoft.Graph.Application.Id(GraphEntityId)
                        if (graphServiceClient != null && !string.IsNullOrEmpty(applicationParameters.ClientId) && !string.IsNullOrEmpty(applicationParameters.GraphEntityId))
                        {
                            await MicrosoftIdentityPlatformApplicationManager.AddPasswordCredentials(
                                graphServiceClient,
                                applicationParameters.GraphEntityId,
                                applicationParameters);

                            string? password = applicationParameters.PasswordCredentials.LastOrDefault();
                            //if user wants to update user secrets
                            if (!string.IsNullOrEmpty(password) && ProvisioningToolOptions.UpdateUserSecrets)
                            {
                                CodeWriter.AddUserSecrets(applicationParameters.IsB2C, ProvisioningToolOptions.ProjectPath, password);
                            }
                        }
                    }
                }
                return applicationParameters;
            }
            // Case of a blazorwasm hosted application. We need to create two applications:
            // - the hosted web API
            // - the SPA.
            if (projectSettings.ApplicationParameters.IsBlazorWasm && projectSettings.ApplicationParameters.IsWebApi)
            {
                // Processes the hosted web API
                ProvisioningToolOptions provisioningToolOptionsBlazorServer = ProvisioningToolOptions.Clone();
                provisioningToolOptionsBlazorServer.ProjectPath = Path.Combine(ProvisioningToolOptions.ProjectPath, "Server");
                provisioningToolOptionsBlazorServer.ClientId = ProvisioningToolOptions.WebApiClientId;
                provisioningToolOptionsBlazorServer.WebApiClientId = null;
                AppProvisioningTool appProvisioningToolBlazorServer = new AppProvisioningTool(CommandName, provisioningToolOptionsBlazorServer);
                ApplicationParameters? applicationParametersServer = await appProvisioningToolBlazorServer.Run();

                /// Processes the Blazorwasm client
                ProvisioningToolOptions provisioningToolOptionsBlazorClient = ProvisioningToolOptions.Clone();
                provisioningToolOptionsBlazorClient.ProjectPath = Path.Combine(ProvisioningToolOptions.ProjectPath, "Client");
                provisioningToolOptionsBlazorClient.WebApiClientId = applicationParametersServer?.ClientId;
                provisioningToolOptionsBlazorClient.AppIdUri = applicationParametersServer?.AppIdUri;
                provisioningToolOptionsBlazorClient.CalledApiScopes = $"{applicationParametersServer?.AppIdUri}/access_as_user";
                AppProvisioningTool appProvisioningToolBlazorClient = new AppProvisioningTool(CommandName, provisioningToolOptionsBlazorClient);
                return await appProvisioningToolBlazorClient.Run();
            }

            // Case where the developer wants to have a B2C application, but the created application is an AAD one. The
            // tool needs to convert it
            if (!projectSettings.ApplicationParameters.IsB2C && !string.IsNullOrEmpty(ProvisioningToolOptions.SusiPolicyId))
            {
                projectSettings = ConvertAadApplicationToB2CApplication(projectDescription, projectSettings);
            }

            // Case where there is no code for the authentication
            if (!projectSettings.ApplicationParameters.HasAuthentication)
            {
                Console.WriteLine($"Authentication is not enabled yet in this project. An app registration will " +
                                  $"be created, but the tool does not add the code yet (work in progress). ");
            }

            // Unregister the app
            if (CommandName.Equals(Commands.UNREGISTER_APPLICATION_COMMAND, StringComparison.OrdinalIgnoreCase))
            {
                await UnregisterApplication(tokenCredential, projectSettings.ApplicationParameters);
                return null;
            }

            // Read or provision Microsoft identity platform application
            ApplicationParameters? effectiveApplicationParameters = await ReadOrProvisionMicrosoftIdentityApplication(
                tokenCredential,
                projectSettings.ApplicationParameters);

            Summary summary = new Summary();

            // Reconciliate code configuration and app registration
            if (effectiveApplicationParameters != null)
            {
                bool appNeedsUpdate = Reconciliate(
                projectSettings.ApplicationParameters,
                effectiveApplicationParameters);

                // Update app registration if needed
                if (appNeedsUpdate)
                {
                    await WriteApplicationRegistration(
                        summary,
                        effectiveApplicationParameters,
                        tokenCredential);
                }

                // Write code configuration if needed
                WriteProjectConfiguration(
                    summary,
                    projectSettings,
                    effectiveApplicationParameters);

                // Summarizes what happened
                WriteSummary(summary);
            }
               
            return effectiveApplicationParameters;
        }

        // add 'AzureAd', 'MicrosoftGraph' or 'DownstreamAPI' sections as appropriate. Fill them default values if empty.
        // Default values can be found https://github.com/dotnet/aspnetcore/tree/main/src/ProjectTemplates/Web.ProjectTemplates/content
        private void ModifyAppSettings(ApplicationParameters applicationParameters)
        {
            string? filePath = ProvisioningToolOptions.AppSettingsFilePath;
            if (!string.IsNullOrEmpty(filePath))
            {
                bool changesMade = false;
                //waiting for https://github.com/dotnet/runtime/issues/29690 + https://github.com/dotnet/runtime/issues/31068 to switch over to System.Text.Json
                JObject appSettings = JObject.Parse(File.ReadAllText(filePath));
                if (appSettings != null)
                {
                    var azureAdToken = appSettings["AzureAd"];
                    if (azureAdToken != null)
                    {
                        var azureAdProperty = azureAdToken.ToObject<AzureAdProperties>();
                        if (azureAdProperty != null)
                        {
                            // if property exists, and if suggested value is not already there.
                            if (!string.IsNullOrEmpty(azureAdProperty.Domain) &&
                                !azureAdProperty.Domain.Equals(applicationParameters.Domain, StringComparison.OrdinalIgnoreCase))
                            {
                                changesMade = true;
                                azureAdToken["Domain"] = applicationParameters.Domain ?? AzureAdDefaultProperties.Domain;
                            }

                            if (!string.IsNullOrEmpty(azureAdProperty.TenantId) &&
                                !azureAdProperty.TenantId.Equals(applicationParameters.TenantId, StringComparison.OrdinalIgnoreCase))
                            {
                                changesMade = true;
                                azureAdToken["TenantId"] = applicationParameters.TenantId ?? AzureAdDefaultProperties.TenantId;
                            }

                            if (!string.IsNullOrEmpty(azureAdProperty.ClientId) &&
                                !azureAdProperty.ClientId.Equals(applicationParameters.ClientId, StringComparison.OrdinalIgnoreCase))
                            {
                                changesMade = true;
                                azureAdToken["ClientId"] = applicationParameters.ClientId ?? AzureAdDefaultProperties.ClientId;
                            }

                            if (!string.IsNullOrEmpty(azureAdProperty.Instance) &&
                                !azureAdProperty.Instance.Equals(applicationParameters.Instance, StringComparison.OrdinalIgnoreCase))
                            {
                                changesMade = true;
                                azureAdToken["Instance"] = applicationParameters.Instance ?? AzureAdDefaultProperties.Instance;
                            }

                            if (!string.IsNullOrEmpty(azureAdProperty.CallbackPath) &&
                                !azureAdProperty.CallbackPath.Equals(applicationParameters.CallbackPath, StringComparison.OrdinalIgnoreCase))
                            {
                                changesMade = true;
                                azureAdToken["CallbackPath"] = applicationParameters.CallbackPath ?? AzureAdDefaultProperties.CallbackPath;
                            }
                        }
                    }
                    else 
                    {
                        changesMade = true;
                        appSettings.Add("AzureAd", JObject.FromObject(new 
                        {
                            Instance = applicationParameters.Instance ?? AzureAdDefaultProperties.Instance,
                            Domain = applicationParameters.Domain ?? AzureAdDefaultProperties.Domain,
                            TenantId = applicationParameters.TenantId ?? AzureAdDefaultProperties.TenantId,
                            ClientId = applicationParameters.ClientId ?? AzureAdDefaultProperties.ClientId,
                            CallbackPath = applicationParameters.CallbackPath ?? AzureAdDefaultProperties.CallbackPath
                        }));
                    }

                    if (ProvisioningToolOptions.CallsGraph || ProvisioningToolOptions.CallsDownstreamApi)
                    {

                        if (azureAdToken != null)
                        {
                            if (azureAdToken["ClientSecret"] == null)
                            {
                                changesMade = true;
                                azureAdToken["ClientSecret"] = "Client secret from app-registration. Check user secrets/azure portal.";
                            }

                            if (azureAdToken["ClientCertificates"] == null)
                            {
                                changesMade = true;
                                azureAdToken["ClientCertificates"] = new JArray();
                            }
                        }

                        if (ProvisioningToolOptions.CallsDownstreamApi)
                        {
                            if (appSettings["DownstreamApi"] == null)
                            {
                                changesMade = true;
                                string apiURL = !string.IsNullOrEmpty(ProvisioningToolOptions.CalledApiUrl) ? ProvisioningToolOptions.CalledApiUrl : "API_URL_HERE";
                                appSettings.Add("DownstreamApi", JObject.FromObject(new
                                {   BaseUrl = apiURL,
                                    Scopes = "user.read"
                                }));
                            }
                        }

                        if (ProvisioningToolOptions.CallsGraph)
                        {
                            if (appSettings["MicrosoftGraph"] == null)
                            {
                                changesMade = true;
                                appSettings.Add("MicrosoftGraph", JObject.FromObject(new
                                {
                                    BaseUrl = "https://graph.microsoft.com/v1.0",
                                    Scopes = "user.read"
                                }));
                            }
                        }
                    }
                } 


                //save comments somehow, only write to appsettings.json if changes are made
                if (appSettings != null && changesMade)
                {
                    File.WriteAllText(filePath, appSettings.ToString());
                }
            }
        }
        /// <summary>
        /// Converts an AAD application to a B2C application
        /// </summary>
        /// <param name="projectDescription"></param>
        /// <param name="projectSettings"></param>
        /// <returns></returns>
        private ProjectAuthenticationSettings ConvertAadApplicationToB2CApplication(ProjectDescription projectDescription, ProjectAuthenticationSettings projectSettings)
        {
            // Get all the files in which "AzureAD" needs to be replaced by "AzureADB2C"
            IEnumerable<string> filesWithReplacementsForB2C = projectSettings.Replacements
                .Where(r => r.ReplaceBy == "Application.ConfigurationSection")
                .Select(r => r.FilePath);

            foreach (string filePath in filesWithReplacementsForB2C)
            {
                string fileContent = File.ReadAllText(filePath);
                string updatedContent = fileContent.Replace("AzureAd", "AzureAdB2C");

                // Add the policies to the appsettings.json
                if (filePath.EndsWith("appsettings.json"))
                {
                    // Insert the policies
                    int indexCallbackPath = updatedContent.IndexOf("\"CallbackPath\"");
                    if (indexCallbackPath > 0)
                    {
                        updatedContent = updatedContent.Substring(0, indexCallbackPath)
                            + Resources.Policies
                            + updatedContent.Substring(indexCallbackPath);
                    }
                }
                File.WriteAllText(filePath, updatedContent);
            }

            if (projectSettings.ApplicationParameters.CallsMicrosoftGraph)
            {
                Console.WriteLine("You'll need to remove the calls to Microsoft Graph as it's not supported by B2C apps.");
            }

            // reevaulate the project settings
            projectSettings = InferApplicationParameters(
                ProvisioningToolOptions,
                projectDescription,
                ProjectDescriptionReader.projectDescriptions);
            return projectSettings;
        }

        private void WriteSummary(Summary summary)
        {
            Console.WriteLine("Summary");
            foreach (Change change in summary.changes)
            {
                Console.WriteLine($"{change.Description}");
            }
        }

        private async Task WriteApplicationRegistration(Summary summary, ApplicationParameters reconcialedApplicationParameters, TokenCredential tokenCredential)
        {
            summary.changes.Add(new Change($"Writing the project AppId = {reconcialedApplicationParameters.ClientId}"));
            await MicrosoftIdentityPlatformApplicationManager.UpdateApplication(tokenCredential, reconcialedApplicationParameters);
        }

        private void WriteProjectConfiguration(Summary summary, ProjectAuthenticationSettings projectSettings, ApplicationParameters reconcialedApplicationParameters)
        {
            CodeWriter.WriteConfiguration(summary, projectSettings.Replacements, reconcialedApplicationParameters);
        }

        private bool Reconciliate(ApplicationParameters applicationParameters, ApplicationParameters effectiveApplicationParameters)
        {
            // Redirect URIs that are needed by the code, but not yet registered 
            IEnumerable<string> missingRedirectUri = applicationParameters.WebRedirectUris.Except(effectiveApplicationParameters.WebRedirectUris);

            bool needUpdate = missingRedirectUri.Any();

            if (needUpdate)
            {
                effectiveApplicationParameters.WebRedirectUris.AddRange(missingRedirectUri);
            }

            // TODO:
            // See also https://github.com/jmprieur/app-provisonning-tool/issues/10
            /*
                 string? audience = ComputeAudienceToSet(applicationParameters.SignInAudience, effectiveApplicationParameters.SignInAudience);
                IEnumerable<ApiPermission> missingApiPermission = null;
                IEnumerable<string> missingExposedScopes = null;
                bool needUpdate = missingRedirectUri != null || audience != null || missingApiPermission != null || missingExposedScopes != null;
            */
            return needUpdate;
        }

        private async Task<ApplicationParameters?> ReadMicrosoftIdentityApplication(
            TokenCredential tokenCredential,
            ApplicationParameters applicationParameters)
        {
            ApplicationParameters? currentApplicationParameters = null;
            if (!string.IsNullOrEmpty(applicationParameters.EffectiveClientId) || (!string.IsNullOrEmpty(applicationParameters.ClientId) && !AzureAdDefaultProperties.ClientId.Equals(applicationParameters.ClientId, StringComparison.OrdinalIgnoreCase)))
            {
                currentApplicationParameters = await MicrosoftIdentityPlatformApplicationManager.ReadApplication(tokenCredential, applicationParameters);
                if (currentApplicationParameters == null)
                {
                    Console.Write($"Couldn't find app {applicationParameters.EffectiveClientId} in tenant {applicationParameters.EffectiveTenantId}. ");
                }
            }
            return currentApplicationParameters;
        }

            private async Task<ApplicationParameters?> ReadOrProvisionMicrosoftIdentityApplication(
            TokenCredential tokenCredential,
            ApplicationParameters applicationParameters)
        {
            ApplicationParameters? currentApplicationParameters = null;
            if (!string.IsNullOrEmpty(applicationParameters.EffectiveClientId) || (!string.IsNullOrEmpty(applicationParameters.ClientId) &&  !AzureAdDefaultProperties.ClientId.Equals(applicationParameters.ClientId, StringComparison.OrdinalIgnoreCase)))
            {
                currentApplicationParameters = await MicrosoftIdentityPlatformApplicationManager.ReadApplication(tokenCredential, applicationParameters);
                if (currentApplicationParameters == null)
                {
                    Console.Write($"Couldn't find app {applicationParameters.EffectiveClientId} in tenant {applicationParameters.EffectiveTenantId}. ");
                }
            }

            if (currentApplicationParameters == null && !ProvisioningToolOptions.Unregister)
            {
                currentApplicationParameters = await MicrosoftIdentityPlatformApplicationManager.CreateNewApp(tokenCredential, applicationParameters);
                Console.Write($"Created app {currentApplicationParameters.ClientId}. ");
            }
            return currentApplicationParameters;
        }

        private ProjectAuthenticationSettings InferApplicationParameters(
            ProvisioningToolOptions provisioningToolOptions,
            ProjectDescription projectDescription,
            IEnumerable<ProjectDescription> projectDescriptions)
        {
            CodeReader reader = new CodeReader();
            ProjectAuthenticationSettings projectSettings = reader.ReadFromFiles(provisioningToolOptions.ProjectPath, projectDescription, projectDescriptions);

            // Override with the tools options
            projectSettings.ApplicationParameters.ApplicationDisplayName ??= Path.GetFileName(provisioningToolOptions.ProjectPath);
            projectSettings.ApplicationParameters.ClientId = !string.IsNullOrEmpty(provisioningToolOptions.ClientId) ? provisioningToolOptions.ClientId : projectSettings.ApplicationParameters.ClientId;
            projectSettings.ApplicationParameters.TenantId = !string.IsNullOrEmpty(provisioningToolOptions.TenantId) ? provisioningToolOptions.TenantId : projectSettings.ApplicationParameters.TenantId;
            projectSettings.ApplicationParameters.CalledApiScopes = !string.IsNullOrEmpty(provisioningToolOptions.CalledApiScopes) ? provisioningToolOptions.CalledApiScopes : projectSettings.ApplicationParameters.CalledApiScopes;
            if (!string.IsNullOrEmpty(provisioningToolOptions.AppIdUri))
            {
                projectSettings.ApplicationParameters.AppIdUri = provisioningToolOptions.AppIdUri;
            }
            return projectSettings;
        }

        private TokenCredential GetTokenCredential(ProvisioningToolOptions provisioningToolOptions, string? currentApplicationTenantId)
        {
            DeveloperCredentialsReader developerCredentialsReader = new DeveloperCredentialsReader();
            return developerCredentialsReader.GetDeveloperCredentials(
                provisioningToolOptions.Username,
                currentApplicationTenantId ?? provisioningToolOptions.TenantId);
        }

        private async Task UnregisterApplication(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            await MicrosoftIdentityPlatformApplicationManager.Unregister(tokenCredential, applicationParameters);
        }
    }
}
