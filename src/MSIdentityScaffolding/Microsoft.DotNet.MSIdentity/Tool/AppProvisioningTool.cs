// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
    public class AppProvisioningTool : IMsAADTool
    {
        private static readonly string[] s_fileExtensions = new string[] { "*.cs", "*.cshtml", "*.razor", "*.html", "*.csproj" };
        private ProvisioningToolOptions ProvisioningToolOptions { get; set; }
        private string CommandName { get; }
        private MicrosoftIdentityPlatformApplicationManager MicrosoftIdentityPlatformApplicationManager { get; } = new MicrosoftIdentityPlatformApplicationManager();
        internal static IEnumerable<PropertyInfo>? _properties;
        internal static IEnumerable<PropertyInfo> Properties => _properties ??= typeof(Resources).GetProperties(BindingFlags.Static | BindingFlags.NonPublic);

        internal IEnumerable<string>? _filePaths;
        private IEnumerable<string> FilePaths => _filePaths ??= s_fileExtensions.SelectMany(
            ext => Directory.EnumerateFiles(ProvisioningToolOptions.ProjectPath, ext, SearchOption.AllDirectories));

        internal IConsoleLogger ConsoleLogger { get; }

        private ProjectDescriptionReader? _projectDescriptionReader;
        private ProjectDescriptionReader ProjectDescriptionReader => _projectDescriptionReader ??= new ProjectDescriptionReader(FilePaths);

        public AppProvisioningTool(string commandName, ProvisioningToolOptions provisioningToolOptions, bool silent = false) // TODO silent is temporary
        {
            CommandName = commandName;
            ProvisioningToolOptions = provisioningToolOptions;
            ConsoleLogger = new ConsoleLogger(ProvisioningToolOptions.Json, silent);
        }

        public async Task<ApplicationParameters?> Run()
        {
            if (!ValidateProjectPath())
            {
                Environment.Exit(1);
            }

            var projectDescription = ProjectDescriptionReader.GetProjectDescription(ProvisioningToolOptions.ProjectTypeIdentifier);
            if (projectDescription == null)
            {
                var errorMessage = string.Format(Resources.NoProjectDescriptionFound, ProvisioningToolOptions.ProjectTypeIdentifier);
                ConsoleLogger.LogJsonMessage(new JsonResponse(CommandName, State.Fail, output: errorMessage));
                Environment.Exit(1);
            }

            ConsoleLogger.LogMessage(string.Format(Resources.DetectedProjectType, projectDescription.Identifier));
            ProvisioningToolOptions.ProjectType ??= projectDescription.Identifier?.Replace("dotnet-", "");

            ProjectAuthenticationSettings projectSettings = InferApplicationParameters(
                ProvisioningToolOptions,
                ProjectDescriptionReader.ProjectDescriptions,
                projectDescription);

            // Get developer credentials
            TokenCredential tokenCredential = GetTokenCredential(
                ProvisioningToolOptions,
                ProvisioningToolOptions.TenantId ?? projectSettings.ApplicationParameters.EffectiveTenantId ?? projectSettings.ApplicationParameters.EffectiveDomain);

            ApplicationParameters applicationParameters;
            switch (CommandName)
            {
                case Commands.CREATE_APP_REGISTRATION_COMMAND:
                    return await CreateAppRegistration(tokenCredential, projectSettings.ApplicationParameters);

                case Commands.UPDATE_APP_REGISTRATION_COMMAND:
                    applicationParameters = await ReadMicrosoftIdentityApplication(tokenCredential, projectSettings.ApplicationParameters);
                    await UpdateAppRegistration(tokenCredential, applicationParameters);
                    return applicationParameters;

                case Commands.UPDATE_PROJECT_COMMAND:
                    applicationParameters = await ReadMicrosoftIdentityApplication(tokenCredential, projectSettings.ApplicationParameters);
                    await UpdateProject(tokenCredential, applicationParameters, projectDescription);
                    return applicationParameters;

                case Commands.ADD_CLIENT_SECRET:
                    applicationParameters = await ReadMicrosoftIdentityApplication(tokenCredential, projectSettings.ApplicationParameters);
                    await AddClientSecret(tokenCredential, applicationParameters);
                    return applicationParameters;

                case Commands.UNREGISTER_APPLICATION_COMMAND:
                    await UnregisterApplication(tokenCredential, projectSettings.ApplicationParameters);
                    return null;
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

        /// <summary>
        /// Ensures that ProjectPath is updated if ProjectFilePath argument exists
        /// </summary>
        /// <returns>true if valid else false</returns>
        private bool ValidateProjectPath()
        {
            if (string.IsNullOrEmpty(ProvisioningToolOptions.ProjectFilePath))
            {
                return true;
            }

            if (System.IO.File.Exists(ProvisioningToolOptions.ProjectFilePath)
                && Path.GetDirectoryName(ProvisioningToolOptions.ProjectFilePath) is string projectPath)
            {
                if (!projectPath.Equals(ProvisioningToolOptions.ProjectPath))
                {
                    ProvisioningToolOptions.ProjectPath = projectPath;
                }

                return true;
            }

            var errorMsg = string.Format(Resources.ProjectPathError, ProvisioningToolOptions.ProjectFilePath);
            ConsoleLogger.LogJsonMessage(new JsonResponse(CommandName, State.Fail, output: errorMsg));
            return false;
        }

        private ProjectAuthenticationSettings InferApplicationParameters(
            ProvisioningToolOptions provisioningToolOptions,
            IEnumerable<ProjectDescription> projectDescriptions,
            ProjectDescription? projectDescription = null)
        {
            var projectSettings = projectDescription != null
                ? new CodeReader().ReadFromFiles(projectDescription, projectDescriptions, FilePaths)
                : new ProjectAuthenticationSettings();

            // Override with the tools options
            projectSettings.ApplicationParameters.ApplicationDisplayName ??= !string.IsNullOrEmpty(provisioningToolOptions.AppDisplayName) ? provisioningToolOptions.AppDisplayName : Path.GetFileName(provisioningToolOptions.ProjectPath);
            projectSettings.ApplicationParameters.ClientId = !string.IsNullOrEmpty(provisioningToolOptions.ClientId) ? provisioningToolOptions.ClientId : projectSettings.ApplicationParameters.ClientId;
            projectSettings.ApplicationParameters.TenantId = !string.IsNullOrEmpty(provisioningToolOptions.TenantId) ? provisioningToolOptions.TenantId : projectSettings.ApplicationParameters.TenantId;
            projectSettings.ApplicationParameters.CalledApiScopes = !string.IsNullOrEmpty(provisioningToolOptions.HostedApiScopes) ? provisioningToolOptions.HostedApiScopes : projectSettings.ApplicationParameters.CalledApiScopes;
            projectSettings.ApplicationParameters.IsBlazorWasm = provisioningToolOptions.IsBlazorWasm;
            projectSettings.ApplicationParameters.WebRedirectUris.AddRange(ProvisioningToolOptions.RedirectUris);

            // there can multiple project types
            if (!string.IsNullOrEmpty(provisioningToolOptions.ProjectType))
            {
                if (provisioningToolOptions.ProjectType.Equals("webapp", StringComparison.OrdinalIgnoreCase)
                    || provisioningToolOptions.ProjectType.Equals("blazorserver", StringComparison.OrdinalIgnoreCase))
                {
                    projectSettings.ApplicationParameters.IsWebApp = projectSettings.ApplicationParameters.IsWebApp ?? true;
                }
                if (provisioningToolOptions.ProjectType.Equals("webapi", StringComparison.OrdinalIgnoreCase) || provisioningToolOptions.IsBlazorWasmHostedServer)
                {
                    projectSettings.ApplicationParameters.IsWebApi = projectSettings.ApplicationParameters.IsWebApi ?? true;
                }
            }
            if (!string.IsNullOrEmpty(provisioningToolOptions.HostedAppIdUri))
            {
                projectSettings.ApplicationParameters.AppIdUri = provisioningToolOptions.HostedAppIdUri;
            }

            return projectSettings;
        }

        /// <summary>
        /// Gets Token Credential
        /// </summary>
        /// <param name="provisioningToolOptions"></param>
        /// <param name="currentApplicationTenantId"></param>
        /// <returns></returns>
        internal static TokenCredential GetTokenCredential(ProvisioningToolOptions provisioningToolOptions, string? currentApplicationTenantId)
        {
            DeveloperCredentialsReader developerCredentialsReader = new DeveloperCredentialsReader();
            return developerCredentialsReader.GetDeveloperCredentials(
                provisioningToolOptions.Username,
                currentApplicationTenantId ?? provisioningToolOptions.TenantId);
        }

        /// <summary>
        /// Creates a new Azure AD App registration
        /// </summary>
        /// <param name="tokenCredential"></param>
        /// <param name="applicationParameters"></param>
        /// <returns></returns>
        private async Task<ApplicationParameters?> CreateAppRegistration(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            ApplicationParameters? resultAppParameters = await MicrosoftIdentityPlatformApplicationManager.CreateNewAppAsync(tokenCredential, applicationParameters, ConsoleLogger, CommandName);
            if (resultAppParameters is null || string.IsNullOrEmpty(resultAppParameters.ClientId))
            {
                string failMessage = Resources.FailedToCreateApp;
                ConsoleLogger.LogMessage(failMessage, LogMessageType.Error);
                return null;
            }

            ConsoleLogger.LogMessage(string.Format(Resources.CreatedAppRegistration, resultAppParameters.ApplicationDisplayName, resultAppParameters.ClientId));
            return resultAppParameters;
        }

        /// <summary>
        /// Reads application parameters from Azure AD for a given app registration client ID
        /// </summary>
        /// <param name="tokenCredential"></param>
        /// <param name="applicationParameters"></param>
        /// <returns></returns>
        private async Task<ApplicationParameters> ReadMicrosoftIdentityApplication(
            TokenCredential tokenCredential,
            ApplicationParameters applicationParameters)
        {
            var currentApplicationParameters = await MicrosoftIdentityPlatformApplicationManager.ReadApplication(tokenCredential, applicationParameters, ConsoleLogger);
            if (currentApplicationParameters is null)
            {
                Environment.Exit(1);
            }

            return currentApplicationParameters;
        }

        /// <summary>
        /// Updates app registration paramters in Azure AD
        /// </summary>
        /// <param name="tokenCredential"></param>
        /// <param name="applicationParameters"></param>
        /// <returns></returns>
        private async Task UpdateAppRegistration(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            StringBuilder output = new StringBuilder();
            if (ProvisioningToolOptions.IsBlazorWasmHostedServer) // Provision Blazor WASM Hosted client app registration
            {
                if (string.IsNullOrEmpty(applicationParameters.AppIdUri)) // Expose server API scopes
                {
                    var graphServiceClient = MicrosoftIdentityPlatformApplicationManager.GetGraphServiceClient(tokenCredential);
                    applicationParameters.AppIdUri = await MicrosoftIdentityPlatformApplicationManager.ExposeScopes(graphServiceClient, applicationParameters.ClientId, applicationParameters.GraphEntityId);
                }

                var clientApplicationParameters = await ConfigureBlazorWasmHostedClientAsync(serverApplicationParameters: applicationParameters);
                ProvisioningToolOptions.BlazorWasmClientAppId = clientApplicationParameters.ClientId;
                output.AppendLine(string.Format(Resources.ConfiguredBlazorWasmClient, clientApplicationParameters.ApplicationDisplayName, clientApplicationParameters.ClientId));
            }

            var jsonResponse = await MicrosoftIdentityPlatformApplicationManager.UpdateApplication(
                                        tokenCredential,
                                        applicationParameters,
                                        ProvisioningToolOptions,
                                        CommandName);

            output.Append(jsonResponse.Output);
            var response = new JsonResponse(CommandName, jsonResponse.State, output: output.ToString());

            ConsoleLogger.LogJsonMessage(response);
        }

        /// <summary>
        /// For Blazor WASM Hosted scenario, provisions and configures client app registration and client code updates
        /// </summary>
        /// <param name="serverApplicationParameters"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        private async Task<ApplicationParameters> ConfigureBlazorWasmHostedClientAsync(ApplicationParameters serverApplicationParameters)
        {
            // Processes the Blazorwasm client
            var clientToolOptions = ProvisioningToolOptions.Clone();

            clientToolOptions.CodeUpdate = true;
            clientToolOptions.ProjectPath = Path.GetDirectoryName(ProvisioningToolOptions.ClientProject) ?? string.Empty;
            clientToolOptions.ProjectFilePath = ProvisioningToolOptions.ClientProject ?? string.Empty;
            clientToolOptions.ClientId = null;
            clientToolOptions.ClientProject = null;
            clientToolOptions.ProjectType = "blazorwasm-client";
            clientToolOptions.AppDisplayName = string.Concat(clientToolOptions.AppDisplayName ?? serverApplicationParameters.ApplicationDisplayName, "-Client");
            clientToolOptions.HostedAppIdUri = serverApplicationParameters.AppIdUri;
            clientToolOptions.HostedApiScopes = $"{serverApplicationParameters.AppIdUri}/{DefaultProperties.ApiScopes}";

            // Provision client app registration
            var provisionClientAppRegistration = new AppProvisioningTool(Commands.CREATE_APP_REGISTRATION_COMMAND, clientToolOptions, silent: true);
            var clientApplicationParameters = await provisionClientAppRegistration.Run();
            if (clientApplicationParameters == null)
            {
                var exception = new ArgumentNullException(nameof(clientApplicationParameters));

                ConsoleLogger.LogJsonMessage(new JsonResponse(CommandName, State.Fail, output: exception.Message));
                throw exception;
            }

            // Update program.cs file
            clientToolOptions.ClientId = clientApplicationParameters.ClientId;
            var updateCode = new AppProvisioningTool(Commands.UPDATE_PROJECT_COMMAND, clientToolOptions, silent: true);
            clientApplicationParameters = await updateCode.Run();
            if (clientApplicationParameters == null)
            {
                var exception = new ArgumentNullException(nameof(clientApplicationParameters));

                ConsoleLogger.LogJsonMessage(new JsonResponse(CommandName, State.Fail, output: exception.Message));
                throw exception;
            }

            return clientApplicationParameters;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokenCredential"></param>
        /// <param name="applicationParameters"></param>
        /// <param name="projectDescription"></param>
        /// <returns></returns>
        private async Task UpdateProject(TokenCredential tokenCredential, ApplicationParameters applicationParameters, ProjectDescription projectDescription)
        {
            if (ProvisioningToolOptions.CodeUpdate || ProvisioningToolOptions.ConfigUpdate)
            {
                ConsoleLogger.LogMessage("=============================================");
                ConsoleLogger.LogMessage(Resources.UpdatingAppSettingsJson);
                ConsoleLogger.LogMessage("=============================================\n");
                // modify appsettings.json.
                var appSettingsModifier = new AppSettingsModifier(ProvisioningToolOptions);
                appSettingsModifier.ModifyAppSettings(applicationParameters, FilePaths);
            }

            if (ProvisioningToolOptions.ConfigUpdate)
            {
                // dotnet user secrets init
                CodeWriter.InitUserSecrets(ProvisioningToolOptions.ProjectPath, ConsoleLogger);

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
                ProjectModifier startupModifier = new ProjectModifier(ProvisioningToolOptions, FilePaths, ConsoleLogger);
                await startupModifier.AddAuthCodeAsync();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokenCredential"></param>
        /// <param name="applicationParameters"></param>
        /// <returns></returns>
        private async Task AddClientSecret(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            string? output;
            JsonResponse jsonResponse = new JsonResponse(CommandName);

            if (string.IsNullOrEmpty(applicationParameters.GraphEntityId))
            {
                jsonResponse.State = State.Fail;
                jsonResponse.Output = Resources.FailedClientSecret;
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

                    // if user wants to update user secrets
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
                    jsonResponse.Output = se.Error?.Code; // TODO refactor
                }

                ConsoleLogger.LogJsonMessage(jsonResponse);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokenCredential"></param>
        /// <param name="applicationParameters"></param>
        /// <returns></returns>
        private async Task UnregisterApplication(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            bool unregisterSuccess = await MicrosoftIdentityPlatformApplicationManager.UnregisterAsync(tokenCredential, applicationParameters);
            JsonResponse jsonResponse = new JsonResponse(CommandName);
            if (unregisterSuccess)
            {
                string outputMessage = $"Unregistered the Azure AD w/ client id = {applicationParameters.ClientId}\n";
                jsonResponse.State = State.Success;
                jsonResponse.Output = outputMessage;
                ConsoleLogger.LogJsonMessage(jsonResponse);
            }
            else
            {
                string outputMessage = $"Unable to unregister the Azure AD w/ client id = {applicationParameters.ClientId}\n";
                jsonResponse.State = State.Fail;
                jsonResponse.Output = outputMessage;
                ConsoleLogger.LogJsonMessage(jsonResponse);
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
                .Where(r => string.Equals(r.ReplaceBy, "Application.ConfigurationSection", StringComparison.OrdinalIgnoreCase))
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="summary"></param>
        private void WriteSummary(Summary summary)
        {
            ConsoleLogger.LogMessage(Resources.Summary);
            foreach (Change change in summary.changes)
            {
                ConsoleLogger.LogMessage($"{change.Description}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="summary"></param>
        /// <param name="reconcialedApplicationParameters"></param>
        /// <param name="tokenCredential"></param>
        /// <returns></returns>
        private async Task WriteApplicationRegistration(Summary summary, ApplicationParameters reconcialedApplicationParameters, TokenCredential tokenCredential)
        {
            summary.changes.Add(new Change($"Writing the project AppId = {reconcialedApplicationParameters.ClientId}"));
            await MicrosoftIdentityPlatformApplicationManager.UpdateApplication(tokenCredential, reconcialedApplicationParameters, ProvisioningToolOptions, CommandName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="summary"></param>
        /// <param name="projectSettings"></param>
        /// <param name="reconcialedApplicationParameters"></param>
        private void WriteProjectConfiguration(Summary summary, ProjectAuthenticationSettings projectSettings, ApplicationParameters reconcialedApplicationParameters)
        {
            CodeWriter.WriteConfiguration(summary, projectSettings.Replacements, reconcialedApplicationParameters, ConsoleLogger);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="applicationParameters"></param>
        /// <param name="effectiveApplicationParameters"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokenCredential"></param>
        /// <param name="applicationParameters"></param>
        /// <returns></returns>
        private async Task<ApplicationParameters?> ReadOrProvisionMicrosoftIdentityApplication(
            TokenCredential tokenCredential,
            ApplicationParameters applicationParameters)
        {
            ApplicationParameters? currentApplicationParameters = null;

            if (!string.IsNullOrEmpty(applicationParameters.EffectiveClientId) || (!string.IsNullOrEmpty(applicationParameters.ClientId) && !DefaultProperties.ClientId.Equals(applicationParameters.ClientId, StringComparison.OrdinalIgnoreCase)))
            {
                currentApplicationParameters = await MicrosoftIdentityPlatformApplicationManager.ReadApplication(tokenCredential, applicationParameters, ConsoleLogger);
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
    }
}
