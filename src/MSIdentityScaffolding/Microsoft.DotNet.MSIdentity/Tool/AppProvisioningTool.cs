// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Azure.Core;
using Microsoft.DotNet.MSIdentity.Properties;
using Microsoft.DotNet.MSIdentity.AuthenticationParameters;
using Microsoft.DotNet.MSIdentity.CodeReaderWriter;
using Microsoft.DotNet.MSIdentity.DeveloperCredentials;
using Microsoft.DotNet.MSIdentity.MicrosoftIdentityPlatformApplication;
using Microsoft.DotNet.MSIdentity.Project;
using Microsoft.DotNet.MSIdentity.Tool;
using Newtonsoft.Json.Linq;
using Directory = System.IO.Directory;
using ProjectDescription = Microsoft.DotNet.MSIdentity.Project.ProjectDescription;

namespace Microsoft.DotNet.MSIdentity
{
    /// <summary>
    /// 
    /// </summary>
    public class AppProvisioningTool : IMsAADTool
    {
        internal IConsoleLogger ConsoleLogger { get; }
        private ProvisioningToolOptions ProvisioningToolOptions { get; set; }

        private string CommandName { get; }

        private MicrosoftIdentityPlatformApplicationManager MicrosoftIdentityPlatformApplicationManager { get; } = new MicrosoftIdentityPlatformApplicationManager();

        private ProjectDescriptionReader ProjectDescriptionReader { get; } = new ProjectDescriptionReader();

        public AppProvisioningTool(string commandName, ProvisioningToolOptions provisioningToolOptions)
        {
            CommandName = commandName;
            ProvisioningToolOptions = provisioningToolOptions;
            ConsoleLogger = new ConsoleLogger(ProvisioningToolOptions.Json);
        }

        public async Task<ApplicationParameters?> Run()
        {
            //Debugger.Launch();
            //get csproj file path
            if (string.IsNullOrEmpty(ProvisioningToolOptions.ProjectFilePath))
            {
                var csProjfiles = Directory.EnumerateFiles(ProvisioningToolOptions.ProjectPath, "*.csproj");
                if (csProjfiles.Any())
                {
                    if (csProjfiles.Count() > 1)
                    {
                        ConsoleLogger.LogJsonMessage(new JsonResponse(CommandName, State.Fail, Resources.ProjectPathError));
                        ConsoleLogger.LogMessage(Resources.ProjectPathError, LogMessageType.Error);
                        return null;
                    }
                    var filePath = csProjfiles.First();
                    ProvisioningToolOptions.ProjectFilePath = filePath;
                }
            }

            string currentDirectory = Directory.GetCurrentDirectory();
            //if its current directory, update it using the ProjectPath
            if (ProvisioningToolOptions.ProjectPath.Equals(currentDirectory, StringComparison.OrdinalIgnoreCase))
            {
                ProvisioningToolOptions.ProjectPath = Path.GetDirectoryName(ProvisioningToolOptions.ProjectFilePath) ?? currentDirectory;
            }
            
            //get appsettings.json file path
            var appSettingsFile = Directory.EnumerateFiles(ProvisioningToolOptions.ProjectPath, "appsettings.json");
            if (appSettingsFile.Any())
            {
                var filePath = appSettingsFile.First();
                ProvisioningToolOptions.AppSettingsFilePath = filePath;
            }

            ProjectDescription? projectDescription = ProjectDescriptionReader.GetProjectDescription(
                ProvisioningToolOptions.ProjectTypeIdentifier,
                ProvisioningToolOptions.ProjectPath);

            if (projectDescription == null)
            {
                ConsoleLogger.LogMessage(string.Format(Resources.NoProjectFound, ProvisioningToolOptions.ProjectPath), LogMessageType.Error);
            }
            else
            {
                ConsoleLogger.LogMessage(string.Format(Resources.DetectedProjectType, projectDescription.Identifier));
                if (!string.IsNullOrEmpty(projectDescription.Identifier))
                {
                    string projectType = projectDescription.Identifier.Replace("dotnet-", "");
                    ProvisioningToolOptions.ProjectType ??= projectType;
                }
            }

            ProjectAuthenticationSettings projectSettings = InferApplicationParameters(
                ProvisioningToolOptions,
                ProjectDescriptionReader.projectDescriptions,
                projectDescription);

            // Get developer credentials
            TokenCredential tokenCredential = GetTokenCredential(
                ProvisioningToolOptions,
                projectSettings.ApplicationParameters.EffectiveTenantId ?? projectSettings.ApplicationParameters.EffectiveDomain);

            //for now, update project command is handlded seperately.
            //TODO: switch case to handle all the different commands.
            ApplicationParameters? applicationParameters = null;

            // Case of a blazorwasm hosted application. We need to create two applications:
            // - the hosted web API
            // - the SPA.
            if (projectSettings.ApplicationParameters.IsBlazorWasm.HasValue && projectSettings.ApplicationParameters.IsBlazorWasm.Value
                && projectSettings.ApplicationParameters.IsWebApi.HasValue && projectSettings.ApplicationParameters.IsWebApi.Value)
            {
                // Processes the hosted web API
                ProvisioningToolOptions provisioningToolOptionsBlazorServer = ProvisioningToolOptions.Clone();
                provisioningToolOptionsBlazorServer.ProjectPath = Path.Combine(ProvisioningToolOptions.ProjectPath, "Server");
                provisioningToolOptionsBlazorServer.AppDisplayName = string.Concat(provisioningToolOptionsBlazorServer.AppDisplayName ?? projectSettings.ApplicationParameters.ApplicationDisplayName, "-Server");
                provisioningToolOptionsBlazorServer.ProjectType = string.Empty;
                provisioningToolOptionsBlazorServer.ClientId = ProvisioningToolOptions.WebApiClientId;
                provisioningToolOptionsBlazorServer.WebApiClientId = null;
                AppProvisioningTool appProvisioningToolBlazorServer = new AppProvisioningTool(CommandName, provisioningToolOptionsBlazorServer);
                ApplicationParameters? applicationParametersServer = await appProvisioningToolBlazorServer.Run();

                /// Processes the Blazorwasm client
                ProvisioningToolOptions provisioningToolOptionsBlazorClient = ProvisioningToolOptions.Clone();
                provisioningToolOptionsBlazorClient.ProjectPath = Path.Combine(ProvisioningToolOptions.ProjectPath, "Client");
                provisioningToolOptionsBlazorClient.AppDisplayName = string.Concat(provisioningToolOptionsBlazorClient.AppDisplayName ?? projectSettings.ApplicationParameters.ApplicationDisplayName, "-Client");
                provisioningToolOptionsBlazorClient.ProjectType = string.Empty;
                provisioningToolOptionsBlazorClient.WebApiClientId = applicationParametersServer?.ClientId;
                provisioningToolOptionsBlazorClient.AppIdUri = applicationParametersServer?.AppIdUri;
                provisioningToolOptionsBlazorClient.CalledApiScopes = $"{applicationParametersServer?.AppIdUri}/access_as_user";
                AppProvisioningTool appProvisioningToolBlazorClient = new AppProvisioningTool(CommandName, provisioningToolOptionsBlazorClient);
                return await appProvisioningToolBlazorClient.Run();
            }

            switch (CommandName)
            {
                case Commands.UPDATE_PROJECT_COMMAND:
                    applicationParameters = await ReadMicrosoftIdentityApplication(tokenCredential, projectSettings.ApplicationParameters);
                    await UpdateProject(tokenCredential, applicationParameters, projectDescription);
                    return applicationParameters;

                case Commands.UPDATE_APP_REGISTRATION_COMMAND:
                    applicationParameters = await ReadMicrosoftIdentityApplication(tokenCredential, projectSettings.ApplicationParameters);
                    await UpdateApplication(tokenCredential, applicationParameters);
                    return applicationParameters;

                case Commands.UNREGISTER_APPLICATION_COMMAND:
                    await UnregisterApplication(tokenCredential, projectSettings.ApplicationParameters);
                    return null;

                case Commands.CREATE_APP_REGISTRATION_COMMAND:
                    return await CreateAppRegistration(tokenCredential, projectSettings.ApplicationParameters);

                case Commands.ADD_CLIENT_SECRET:
                    applicationParameters = await ReadMicrosoftIdentityApplication(tokenCredential, projectSettings.ApplicationParameters);
                    await AddClientSecret(tokenCredential, applicationParameters);
                    return applicationParameters;
            }

            // Case where the developer wants to have a B2C application, but the created application is an AAD one. The
            // tool needs to convert it
            if (!projectSettings.ApplicationParameters.IsB2C && !string.IsNullOrEmpty(ProvisioningToolOptions.SusiPolicyId))
            {
                if (projectDescription != null)
                {
                    projectSettings = ConvertAadApplicationToB2CApplication(projectDescription, projectSettings);
                }
            }

            // Case where there is no code for the authentication
            if (!projectSettings.ApplicationParameters.HasAuthentication)
            {
                ConsoleLogger.LogMessage(Resources.AuthNotEnabled);
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

        private async Task<ApplicationParameters?> CreateAppRegistration(TokenCredential tokenCredential, ApplicationParameters? applicationParameters)
        {
            ApplicationParameters? resultAppParameters = null;
            if (applicationParameters != null)
            {
                resultAppParameters = await MicrosoftIdentityPlatformApplicationManager.CreateNewAppAsync(tokenCredential, applicationParameters, ConsoleLogger, CommandName);
                if (resultAppParameters != null && !string.IsNullOrEmpty(resultAppParameters.ClientId))
                {
                    ConsoleLogger.LogMessage(string.Format(Resources.CreatedAppRegistration, resultAppParameters.ApplicationDisplayName, resultAppParameters.ClientId));
                }
                else
                {
                    string failMessage = Resources.FailedToCreateApp;
                    ConsoleLogger.LogMessage(failMessage, LogMessageType.Error);
                }
            }
            return resultAppParameters;
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
                                {
                                    BaseUrl = apiURL,
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
                ConsoleLogger.LogMessage(Resources.MicrosoftGraphNotSupported, LogMessageType.Error);
            }

            // reevaulate the project settings
            projectSettings = InferApplicationParameters(
                ProvisioningToolOptions,
                ProjectDescriptionReader.projectDescriptions,
                projectDescription);
            return projectSettings;
        }

        private void WriteSummary(Summary summary)
        {
            ConsoleLogger.LogMessage(Resources.Summary);
            foreach (Change change in summary.changes)
            {
                ConsoleLogger.LogMessage($"{change.Description}");
            }
        }

        private async Task WriteApplicationRegistration(Summary summary, ApplicationParameters reconcialedApplicationParameters, TokenCredential tokenCredential)
        {
            summary.changes.Add(new Change($"Writing the project AppId = {reconcialedApplicationParameters.ClientId}"));
            await MicrosoftIdentityPlatformApplicationManager.UpdateApplication(tokenCredential, reconcialedApplicationParameters, ProvisioningToolOptions);
        }

        private void WriteProjectConfiguration(Summary summary, ProjectAuthenticationSettings projectSettings, ApplicationParameters reconcialedApplicationParameters)
        {
            CodeWriter.WriteConfiguration(summary, projectSettings.Replacements, reconcialedApplicationParameters, ConsoleLogger);
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
                    ConsoleLogger.LogMessage($"Couldn't find app {applicationParameters.EffectiveClientId} in tenant {applicationParameters.EffectiveTenantId}. ", LogMessageType.Error);
                }
            }
            return currentApplicationParameters;
        }

        private async Task<ApplicationParameters?> ReadOrProvisionMicrosoftIdentityApplication(
            TokenCredential tokenCredential,
            ApplicationParameters applicationParameters)
        {
            ApplicationParameters? currentApplicationParameters = null;
            if (!string.IsNullOrEmpty(applicationParameters.EffectiveClientId) || (!string.IsNullOrEmpty(applicationParameters.ClientId) && !AzureAdDefaultProperties.ClientId.Equals(applicationParameters.ClientId, StringComparison.OrdinalIgnoreCase)))
            {
                currentApplicationParameters = await MicrosoftIdentityPlatformApplicationManager.ReadApplication(tokenCredential, applicationParameters);
                if (currentApplicationParameters == null)
                {
                    ConsoleLogger.LogMessage($"Couldn't find app {applicationParameters.EffectiveClientId} in tenant {applicationParameters.EffectiveTenantId}. ", LogMessageType.Error);
                }
            }

            if (currentApplicationParameters == null && !ProvisioningToolOptions.Unregister)
            {
                currentApplicationParameters = await MicrosoftIdentityPlatformApplicationManager.CreateNewAppAsync(tokenCredential, applicationParameters, ConsoleLogger, CommandName);
                if (currentApplicationParameters != null)
                {
                    ConsoleLogger.LogMessage($"Created app {currentApplicationParameters.ApplicationDisplayName} - {currentApplicationParameters.ClientId}. ");
                }
                else
                {
                    ConsoleLogger.LogMessage(Resources.FailedToCreateApp, LogMessageType.Error);
                }
            }
            return currentApplicationParameters;
        }

        private ProjectAuthenticationSettings InferApplicationParameters(
            ProvisioningToolOptions provisioningToolOptions,
            IEnumerable<ProjectDescription> projectDescriptions,
            ProjectDescription? projectDescription = null)
        {
            CodeReader reader = new CodeReader();
            ProjectAuthenticationSettings projectSettings = new ProjectAuthenticationSettings();
            if (projectDescription != null)
            {
                projectSettings = reader.ReadFromFiles(provisioningToolOptions.ProjectPath, projectDescription, projectDescriptions);
            }
            
            // Override with the tools options
            projectSettings.ApplicationParameters.ApplicationDisplayName ??= !string.IsNullOrEmpty(provisioningToolOptions.AppDisplayName) ? provisioningToolOptions.AppDisplayName : Path.GetFileName(provisioningToolOptions.ProjectPath);
            projectSettings.ApplicationParameters.ClientId = !string.IsNullOrEmpty(provisioningToolOptions.ClientId) ? provisioningToolOptions.ClientId : projectSettings.ApplicationParameters.ClientId;
            projectSettings.ApplicationParameters.TenantId = !string.IsNullOrEmpty(provisioningToolOptions.TenantId) ? provisioningToolOptions.TenantId : projectSettings.ApplicationParameters.TenantId;
            projectSettings.ApplicationParameters.CalledApiScopes = !string.IsNullOrEmpty(provisioningToolOptions.CalledApiScopes) ? provisioningToolOptions.CalledApiScopes : projectSettings.ApplicationParameters.CalledApiScopes;

            //there can mutliple project types
            if (!string.IsNullOrEmpty(provisioningToolOptions.ProjectType))
            {
                if (provisioningToolOptions.ProjectType.Equals("webapp", StringComparison.OrdinalIgnoreCase))
                {
                    projectSettings.ApplicationParameters.IsWebApp = projectSettings.ApplicationParameters.IsWebApp ?? true;
                }
                if (provisioningToolOptions.ProjectType.Equals("webapi", StringComparison.OrdinalIgnoreCase))
                {
                    projectSettings.ApplicationParameters.IsWebApi = projectSettings.ApplicationParameters.IsWebApi ?? true;
                }
                if (provisioningToolOptions.ProjectType.Equals("blazorwasm", StringComparison.OrdinalIgnoreCase))
                {
                    projectSettings.ApplicationParameters.IsBlazorWasm = projectSettings.ApplicationParameters.IsBlazorWasm ?? true;
                }
                if (provisioningToolOptions.ProjectType.Equals("blazorwasm-hosted", StringComparison.OrdinalIgnoreCase))
                {
                    projectSettings.ApplicationParameters.IsBlazorWasm = projectSettings.ApplicationParameters.IsBlazorWasm ?? true;
                }
            }
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
            bool unregisterSuccess = await MicrosoftIdentityPlatformApplicationManager.UnregisterAsync(tokenCredential, applicationParameters);
            JsonResponse jsonResponse = new JsonResponse(CommandName);
            if (unregisterSuccess)
            {
                string outputMessage = $"Unregistered the Azure AD w/ client id = {applicationParameters.ClientId}\n";
                jsonResponse.State = State.Success;
                jsonResponse.Content = outputMessage;
                ConsoleLogger.LogMessage(outputMessage);
                ConsoleLogger.LogJsonMessage(jsonResponse);
            }
            else
            {
                string outputMessage = $"Unable to unregister the Azure AD w/ client id = {applicationParameters.ClientId}\n";
                jsonResponse.State = State.Fail;
                jsonResponse.Content = outputMessage;
                ConsoleLogger.LogMessage(outputMessage);
                ConsoleLogger.LogJsonMessage(jsonResponse);
            }
        }

        private async Task UpdateApplication(TokenCredential tokenCredential, ApplicationParameters? applicationParameters)
        {
            bool updateSuccess = false;
            if (applicationParameters != null)
            {
                updateSuccess = await MicrosoftIdentityPlatformApplicationManager.UpdateApplication(
                                            tokenCredential,
                                            applicationParameters,
                                            ProvisioningToolOptions);
            }

            JsonResponse jsonResponse = new JsonResponse(CommandName);

            if (updateSuccess)
            {
                jsonResponse.Content = $"Success updating Azure AD app {applicationParameters?.ApplicationDisplayName} ({applicationParameters?.ClientId})";
                jsonResponse.State = State.Success;
            }
            else
            {
                jsonResponse.Content = $"Failed to update Azure AD app {applicationParameters?.ApplicationDisplayName} ({applicationParameters?.ClientId})";
                jsonResponse.State = State.Fail;
            }

            ConsoleLogger.LogMessage(jsonResponse.Content as string);
            ConsoleLogger.LogJsonMessage(jsonResponse);
        }

        private async Task AddClientSecret(TokenCredential tokenCredential, ApplicationParameters? applicationParameters)
        {
            JsonResponse jsonResponse = new JsonResponse(CommandName);

            if (applicationParameters != null && !string.IsNullOrEmpty(applicationParameters.GraphEntityId))
            {
                var graphServiceClient = MicrosoftIdentityPlatformApplicationManager.GetGraphServiceClient(tokenCredential);

                string? password = await MicrosoftIdentityPlatformApplicationManager.AddPasswordCredentialsAsync(
                        graphServiceClient,
                        applicationParameters.GraphEntityId,
                        applicationParameters,
                        ConsoleLogger);

                //if user wants to update user secrets
                if (ProvisioningToolOptions.UpdateUserSecrets)
                {
                    CodeWriter.AddUserSecrets(applicationParameters.IsB2C, ProvisioningToolOptions.ProjectPath, password, ConsoleLogger);
                }

                if (!string.IsNullOrEmpty(password))
                {
                    jsonResponse.State = State.Success;
                    jsonResponse.Content = new KeyValuePair<string, string>("ClientSecret", password);
                    string secretOutput = string.Format(Resources.ClientSecret, password);
                    ConsoleLogger.LogMessage(secretOutput);
                    ConsoleLogger.LogJsonMessage(jsonResponse);

                }
                else
                {
                    string failedOutput = string.Format(Resources.FailedClientSecretWithApp, applicationParameters.ApplicationDisplayName, applicationParameters.ClientId);
                    jsonResponse.State = State.Fail;
                    jsonResponse.Content = failedOutput;
                    ConsoleLogger.LogMessage(failedOutput);
                    ConsoleLogger.LogJsonMessage(jsonResponse);
                }
            }
            else
            {
                string failedOutput = Resources.FailedClientSecret;
                jsonResponse.State = State.Fail;
                jsonResponse.Content = failedOutput;
                ConsoleLogger.LogMessage(failedOutput);
                ConsoleLogger.LogJsonMessage(jsonResponse);
            }
        }

        private async Task UpdateProject(TokenCredential tokenCredential, ApplicationParameters? applicationParameters, ProjectDescription? projectDescription)
        {
            if (applicationParameters != null && !string.IsNullOrEmpty(ProvisioningToolOptions.ProjectFilePath) && projectDescription != null)
            {
                if (ProvisioningToolOptions.ConfigUpdate)
                {
                    ConsoleLogger.LogMessage("=============================================");
                    ConsoleLogger.LogMessage(Resources.UpdatingAppSettingsJson);
                    ConsoleLogger.LogMessage("=============================================\n");
                    //dotnet user secrets init
                    CodeWriter.InitUserSecrets(ProvisioningToolOptions.ProjectPath, ConsoleLogger);

                    //modify appsettings.json. 
                    ModifyAppSettings(applicationParameters);

                    //Add ClientSecret if the app wants to call graph/a downstream api.
                    if (ProvisioningToolOptions.CallsGraph || ProvisioningToolOptions.CallsDownstreamApi)
                    {
                        var graphServiceClient = MicrosoftIdentityPlatformApplicationManager.GetGraphServiceClient(tokenCredential);
                        //need ClientId and Microsoft.Graph.Application.Id(GraphEntityId)
                        if (graphServiceClient != null && !string.IsNullOrEmpty(applicationParameters.ClientId) && !string.IsNullOrEmpty(applicationParameters.GraphEntityId))
                        {
                            await MicrosoftIdentityPlatformApplicationManager.AddPasswordCredentialsAsync(
                                graphServiceClient,
                                applicationParameters.GraphEntityId,
                                applicationParameters,
                                ConsoleLogger);

                            string? password = applicationParameters.PasswordCredentials.LastOrDefault();
                            //if user wants to update user secrets
                            if (!string.IsNullOrEmpty(password) && ProvisioningToolOptions.UpdateUserSecrets)
                            {
                                CodeWriter.AddUserSecrets(applicationParameters.IsB2C, ProvisioningToolOptions.ProjectPath, password, ConsoleLogger);
                            }
                        }
                    }
                }

                if (ProvisioningToolOptions.PackagesUpdate)
                {
                    if (projectDescription.Packages != null)
                    {
                        ConsoleLogger.LogMessage("=============================================");
                        ConsoleLogger.LogMessage(Resources.UpdatingProjectPackages);
                        ConsoleLogger.LogMessage("=============================================\n");
                        
                        DependencyGraphService dependencyGraphService = new DependencyGraphService(ProvisioningToolOptions.ProjectFilePath);
                        var dependencyGraph = dependencyGraphService.GenerateDependencyGraph();
                        if (dependencyGraph != null)
                        {
                            var project = dependencyGraph.Projects.FirstOrDefault();
                            var tfm = project?.TargetFrameworks.FirstOrDefault();

                            if (tfm != null)
                            {
                                var shortTfm = tfm.FrameworkName?.GetShortFolderName();
                                if (!string.IsNullOrEmpty(shortTfm))
                                {
                                    foreach (var packageToInstall in projectDescription.Packages)
                                    {
                                        //if package doesn't exist, add it.
                                        if (!tfm.Dependencies.Where(x => x.Name.Equals(packageToInstall)).Any())
                                        {
                                            CodeWriter.AddPackage(packageToInstall, shortTfm, ConsoleLogger);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (ProvisioningToolOptions.CodeUpdate)
                {
                    ConsoleLogger.LogMessage("=============================================");
                    ConsoleLogger.LogMessage(Resources.UpdatingProjectFiles);
                    ConsoleLogger.LogMessage("=============================================\n");
                    //if project is not setup for auth, add updates to Startup.cs, .csproj.
                    ProjectModifier startupModifier = new ProjectModifier(applicationParameters, ProvisioningToolOptions, ConsoleLogger);
                    await startupModifier.AddAuthCodeAsync();
                }  
            }
        }           
                //Layout.cshtml
                //LoginPartial.cshtml
                //launchsettings.json --> update
    }
}
