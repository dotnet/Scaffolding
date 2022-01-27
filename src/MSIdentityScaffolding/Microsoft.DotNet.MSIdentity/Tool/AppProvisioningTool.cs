// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.DotNet.MSIdentity.AuthenticationParameters;
using Microsoft.DotNet.MSIdentity.CodeReaderWriter;
using Microsoft.DotNet.MSIdentity.DeveloperCredentials;
using Microsoft.DotNet.MSIdentity.MicrosoftIdentityPlatform;
using Microsoft.DotNet.MSIdentity.MicrosoftIdentityPlatformApplication;
using Microsoft.DotNet.MSIdentity.Project;
using Microsoft.DotNet.MSIdentity.Properties;
using Microsoft.DotNet.MSIdentity.Shared;
using Microsoft.DotNet.MSIdentity.Tool;
using Microsoft.Graph;
using Directory = System.IO.Directory;
using ProjectDescription = Microsoft.DotNet.MSIdentity.Project.ProjectDescription;

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
        internal AppSettingsModifier AppSettingsModifier { get => new AppSettingsModifier(ProvisioningToolOptions); }

        internal static PropertyInfo[]? _properties;
        internal static PropertyInfo[] Properties => _properties ??= typeof(Resources).GetProperties(BindingFlags.Static | BindingFlags.NonPublic)
            .Where(p => p.PropertyType == typeof(byte[])).ToArray();

        internal IEnumerable<string>? _files;
        internal IEnumerable<string> Files => _files ??=
            Directory.EnumerateFiles(ProvisioningToolOptions.ProjectPath, "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(ProvisioningToolOptions.ProjectPath, "*.cshtml", SearchOption.AllDirectories))
            .Concat(Directory.EnumerateFiles(ProvisioningToolOptions.ProjectPath, "*.razor", SearchOption.AllDirectories))
            .Concat(Directory.EnumerateFiles(ProvisioningToolOptions.ProjectPath, "*.html", SearchOption.AllDirectories));

        internal IConsoleLogger ConsoleLogger { get; }

        private ProjectDescriptionReader? _projectDescriptionReader;
        private ProjectDescriptionReader ProjectDescriptionReader => _projectDescriptionReader ??= new ProjectDescriptionReader(Files);

        public AppProvisioningTool(string commandName, ProvisioningToolOptions provisioningToolOptions)
        {
            CommandName = commandName;
            ProvisioningToolOptions = provisioningToolOptions;
            ConsoleLogger = new ConsoleLogger(ProvisioningToolOptions.Json);
        }

        public async Task<ApplicationParameters?> Run()
        {
            // Get csproj file path if it is not input from the tool
            if (string.IsNullOrEmpty(ProvisioningToolOptions.ProjectFilePath))
            {
                // Enumerate all files because we do not yet know the correct ProjectPath, later we cache all files and pass the list around
                var csProjfiles = Directory.EnumerateFiles(ProvisioningToolOptions.ProjectPath, "*.csproj");
                if (csProjfiles is null || csProjfiles.Count() != 1)
                {
                    ConsoleLogger.LogJsonMessage(new JsonResponse(CommandName, State.Fail, Resources.ProjectPathError));
                    ConsoleLogger.LogMessage(Resources.ProjectPathError, LogMessageType.Error);
                    return null;
                }

                ProvisioningToolOptions.ProjectFilePath = csProjfiles.First();
            }

            // Update the ProjectPath if necessary
            if (ProvisioningToolOptions.ProjectPath.Equals(Directory.GetCurrentDirectory(), StringComparison.OrdinalIgnoreCase)
                && Path.GetDirectoryName(ProvisioningToolOptions.ProjectFilePath) is string actualProjectPath)
            {
                ProvisioningToolOptions.ProjectPath = actualProjectPath;
            }

            var projectDescription = ProjectDescriptionReader.GetProjectDescription(
                ProvisioningToolOptions.ProjectTypeIdentifier);

            if (projectDescription == null)
            {
                ConsoleLogger.LogMessage(string.Format(Resources.NoProjectFound, ProvisioningToolOptions.ProjectPath), LogMessageType.Error);
            }
            else
            {
                ConsoleLogger.LogMessage(string.Format(Resources.DetectedProjectType, projectDescription.Identifier));
                ProvisioningToolOptions.ProjectType ??= projectDescription.Identifier?.Replace("dotnet-", "");
            }

            ProjectAuthenticationSettings projectSettings = InferApplicationParameters(
                ProvisioningToolOptions,
                ProjectDescriptionReader.ProjectDescriptions,
                projectDescription);

            // Get developer credentials
            TokenCredential tokenCredential = GetTokenCredential(
                ProvisioningToolOptions,
                ProvisioningToolOptions.TenantId ?? projectSettings.ApplicationParameters.EffectiveTenantId ?? projectSettings.ApplicationParameters.EffectiveDomain);

            // Case of a blazorwasm hosted application. We need to create two applications:
            // - the hosted web API
            // - the SPA.
            if (projectSettings.ApplicationParameters.IsBlazorWasm
                && projectSettings.ApplicationParameters.IsWebApi.GetValueOrDefault())
            {
                // Processes the hosted web API
                ProvisioningToolOptions provisioningToolOptionsBlazorServer = ProvisioningToolOptions.Clone();
                provisioningToolOptionsBlazorServer.ProjectPath = Path.Combine(ProvisioningToolOptions.ProjectPath, "Server");
                provisioningToolOptionsBlazorServer.AppDisplayName = string.Concat(
                    provisioningToolOptionsBlazorServer.AppDisplayName ?? projectSettings.ApplicationParameters.ApplicationDisplayName, "-Server");
                provisioningToolOptionsBlazorServer.ProjectType = string.Empty;
                provisioningToolOptionsBlazorServer.ClientId = ProvisioningToolOptions.WebApiClientId;
                provisioningToolOptionsBlazorServer.WebApiClientId = null;
                AppProvisioningTool appProvisioningToolBlazorServer = new AppProvisioningTool(CommandName, provisioningToolOptionsBlazorServer);
                ApplicationParameters? applicationParametersServer = await appProvisioningToolBlazorServer.Run();

                /// Processes the Blazorwasm client
                ProvisioningToolOptions provisioningToolOptionsBlazorClient = ProvisioningToolOptions.Clone();
                provisioningToolOptionsBlazorClient.ProjectPath = Path.Combine(ProvisioningToolOptions.ProjectPath, "Client");
                provisioningToolOptionsBlazorClient.AppDisplayName = string.Concat(
                    provisioningToolOptionsBlazorClient.AppDisplayName ?? projectSettings.ApplicationParameters.ApplicationDisplayName, "-Client");
                provisioningToolOptionsBlazorClient.ProjectType = string.Empty;
                provisioningToolOptionsBlazorClient.WebApiClientId = applicationParametersServer?.ClientId;
                provisioningToolOptionsBlazorClient.AppIdUri = applicationParametersServer?.AppIdUri;
                provisioningToolOptionsBlazorClient.CalledApiScopes = $"{applicationParametersServer?.AppIdUri}/access_as_user";
                AppProvisioningTool appProvisioningToolBlazorClient = new AppProvisioningTool(CommandName, provisioningToolOptionsBlazorClient);
                return await appProvisioningToolBlazorClient.Run();
            }


            //for now, update project command is handlded seperately.
            ApplicationParameters? applicationParameters;
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
                string fileContent = System.IO.File.ReadAllText(filePath);
                string updatedContent = fileContent.Replace("AzureAd", "AzureAdB2C");

                // Add the policies to the appsettings.json
                if (filePath.EndsWith(AppSettingsModifier.AppSettingsFileName))
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
                System.IO.File.WriteAllText(filePath, updatedContent);
            }

            if (projectSettings.ApplicationParameters.CallsMicrosoftGraph)
            {
                ConsoleLogger.LogMessage(Resources.MicrosoftGraphNotSupported, LogMessageType.Error);
            }

            // reevaulate the project settings
            projectSettings = InferApplicationParameters(
                ProvisioningToolOptions,
                ProjectDescriptionReader.ProjectDescriptions,
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
            await MicrosoftIdentityPlatformApplicationManager.UpdateApplication(tokenCredential, reconcialedApplicationParameters, ProvisioningToolOptions, CommandName);
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
            if (!string.IsNullOrEmpty(applicationParameters.EffectiveClientId)
                || (!string.IsNullOrEmpty(applicationParameters.ClientId)
                && !DefaultProperties.ClientId.Equals(applicationParameters.ClientId, StringComparison.OrdinalIgnoreCase)))
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

            if (!string.IsNullOrEmpty(applicationParameters.EffectiveClientId) || (!string.IsNullOrEmpty(applicationParameters.ClientId) && !DefaultProperties.ClientId.Equals(applicationParameters.ClientId, StringComparison.OrdinalIgnoreCase)))
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
            var projectSettings = projectDescription != null
                ? new CodeReader().ReadFromFiles(projectDescription, projectDescriptions, Files)
                : new ProjectAuthenticationSettings();

            // Override with the tools options
            projectSettings.ApplicationParameters.ApplicationDisplayName ??= !string.IsNullOrEmpty(provisioningToolOptions.AppDisplayName) ? provisioningToolOptions.AppDisplayName : Path.GetFileName(provisioningToolOptions.ProjectPath);
            projectSettings.ApplicationParameters.ClientId = !string.IsNullOrEmpty(provisioningToolOptions.ClientId) ? provisioningToolOptions.ClientId : projectSettings.ApplicationParameters.ClientId;
            projectSettings.ApplicationParameters.TenantId = !string.IsNullOrEmpty(provisioningToolOptions.TenantId) ? provisioningToolOptions.TenantId : projectSettings.ApplicationParameters.TenantId;
            projectSettings.ApplicationParameters.CalledApiScopes = !string.IsNullOrEmpty(provisioningToolOptions.CalledApiScopes) ? provisioningToolOptions.CalledApiScopes : projectSettings.ApplicationParameters.CalledApiScopes;
            projectSettings.ApplicationParameters.IsBlazorWasm = provisioningToolOptions.IsBlazorWasm;

            // there can mutliple project types
            if (!string.IsNullOrEmpty(provisioningToolOptions.ProjectType))
            {
                if (provisioningToolOptions.ProjectType.Equals("webapp", StringComparison.OrdinalIgnoreCase)
                    || provisioningToolOptions.ProjectType.Equals("blazorserver", StringComparison.OrdinalIgnoreCase))
                {
                    projectSettings.ApplicationParameters.IsWebApp = projectSettings.ApplicationParameters.IsWebApp ?? true;
                }
                if (provisioningToolOptions.ProjectType.Equals("webapi", StringComparison.OrdinalIgnoreCase))
                {
                    projectSettings.ApplicationParameters.IsWebApi = projectSettings.ApplicationParameters.IsWebApi ?? true;
                }
                if (provisioningToolOptions.ProjectType.Equals("blazorwasm-hosted", StringComparison.OrdinalIgnoreCase))
                {
                    projectSettings.ApplicationParameters.IsBlazorWasm = true;
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
            if (applicationParameters != null)
            {
                var jsonResponse = await MicrosoftIdentityPlatformApplicationManager.UpdateApplication(
                                            tokenCredential,
                                            applicationParameters,
                                            ProvisioningToolOptions,
                                            CommandName);

                ConsoleLogger.LogMessage(jsonResponse.Content as string);
                ConsoleLogger.LogJsonMessage(jsonResponse);
            }
        }

        private async Task AddClientSecret(TokenCredential tokenCredential, ApplicationParameters? applicationParameters)
        {
            JsonResponse jsonResponse = new JsonResponse(CommandName);
            string? output;

            if (applicationParameters == null || string.IsNullOrEmpty(applicationParameters.GraphEntityId))
            {
                output = Resources.FailedClientSecret;
                jsonResponse.State = State.Fail;
                jsonResponse.Content = output;
            }
            else
            {
                var graphServiceClient = MicrosoftIdentityPlatformApplicationManager.GetGraphServiceClient(tokenCredential);

                try
                {
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
                        output = string.Format(Resources.ClientSecret, password);
                        jsonResponse.State = State.Success;
                        jsonResponse.Content = new KeyValuePair<string, string>("ClientSecret", password);
                    }
                    else
                    {
                        output = string.Format(Resources.FailedClientSecretWithApp, applicationParameters.ApplicationDisplayName, applicationParameters.ClientId);
                        jsonResponse.State = State.Fail;
                        jsonResponse.Content = "TODO Empty password";
                    }
                }
                catch (ServiceException se)
                {
                    output = se.Error?.ToString();
                    jsonResponse.State = State.Fail;
                    jsonResponse.Content = se.Error?.Code;
                }

                ConsoleLogger.LogMessage(output);
                ConsoleLogger.LogJsonMessage(jsonResponse);
            }
        }

        private async Task UpdateProject(TokenCredential tokenCredential, ApplicationParameters? applicationParameters, ProjectDescription? projectDescription)
        {
            if (applicationParameters is null || string.IsNullOrEmpty(ProvisioningToolOptions.ProjectFilePath) || projectDescription is null)
            {
                return;
            }

            if (ProvisioningToolOptions.ConfigUpdate)
            {
                ConsoleLogger.LogMessage("=============================================");
                ConsoleLogger.LogMessage(Resources.UpdatingAppSettingsJson);
                ConsoleLogger.LogMessage("=============================================\n");
                // dotnet user secrets init
                CodeWriter.InitUserSecrets(ProvisioningToolOptions.ProjectPath, ConsoleLogger);

                // modify appsettings.json if not updated from Code Update
                if (!ProvisioningToolOptions.CodeUpdate)
                {
                    AppSettingsModifier.ModifyAppSettings(applicationParameters, Files);
                }

                // Add ClientSecret if the app wants to call graph/a downstream api.
                if (ProvisioningToolOptions.CallsGraph || ProvisioningToolOptions.CallsDownstreamApi)
                {
                    var graphServiceClient = MicrosoftIdentityPlatformApplicationManager.GetGraphServiceClient(tokenCredential);
                    // need ClientId and Microsoft.Graph.Application.Id(GraphEntityId)
                    if (graphServiceClient != null && !string.IsNullOrEmpty(applicationParameters.ClientId) && !string.IsNullOrEmpty(applicationParameters.GraphEntityId))
                    {
                        await MicrosoftIdentityPlatformApplicationManager.AddPasswordCredentialsAsync(
                            graphServiceClient,
                            applicationParameters.GraphEntityId,
                            applicationParameters,
                            ConsoleLogger);

                        string? password = applicationParameters.PasswordCredentials.LastOrDefault();
                        // if user wants to update user secrets
                        if (!string.IsNullOrEmpty(password) && ProvisioningToolOptions.UpdateUserSecrets)
                        {
                            CodeWriter.AddUserSecrets(applicationParameters.IsB2C, ProvisioningToolOptions.ProjectPath, password, ConsoleLogger);
                        }
                    }
                }
            }

            if (ProvisioningToolOptions.PackagesUpdate)
            {
                List<string> packages = projectDescription.CommonPackages?.ToList() ?? new List<string>();
                if (ProvisioningToolOptions.CallsDownstreamApi && projectDescription.DownstreamApiPackages != null)
                {
                    packages.AddRange(projectDescription.DownstreamApiPackages);
                }
                if (ProvisioningToolOptions.CallsGraph && projectDescription.MicrosoftGraphPackages != null)
                {
                    packages.AddRange(projectDescription.MicrosoftGraphPackages);
                }
                if (!ProvisioningToolOptions.CallsDownstreamApi && !ProvisioningToolOptions.CallsGraph && projectDescription.BasePackages != null)
                {
                    packages.AddRange(projectDescription.BasePackages);
                }
                if (packages != null)
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
                                foreach (var packageToInstall in packages)
                                {
                                    // if package doesn't exist, add it.
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
                // if project is not setup for auth, add updates to Startup.cs, .csproj.
                ProjectModifier startupModifier = new ProjectModifier(ProvisioningToolOptions, Files, ConsoleLogger);
                await startupModifier.AddAuthCodeAsync();

                // modify appsettings.json.
                AppSettingsModifier.ModifyAppSettings(applicationParameters, Files);
            }
        }
    }
}
