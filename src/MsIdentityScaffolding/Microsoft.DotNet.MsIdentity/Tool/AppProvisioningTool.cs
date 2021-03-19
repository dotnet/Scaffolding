// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.DotNet.MsIdentity.Properties;
using Microsoft.DotNet.MsIdentity.AuthenticationParameters;
using Microsoft.DotNet.MsIdentity.CodeReaderWriter;
using Microsoft.DotNet.MsIdentity.DeveloperCredentials;
using Microsoft.DotNet.MsIdentity.MicrosoftIdentityPlatformApplication;
using Microsoft.DotNet.MsIdentity.Project;


namespace Microsoft.DotNet.MsIdentity
{
    /// <summary>
    /// 
    /// </summary>
    public class AppProvisioningTool : IMsAADTool
    {
        private ProvisioningToolOptions ProvisioningToolOptions { get; set; }

        private MicrosoftIdentityPlatformApplicationManager MicrosoftIdentityPlatformApplicationManager { get; } = new MicrosoftIdentityPlatformApplicationManager();

        private ProjectDescriptionReader ProjectDescriptionReader { get; } = new ProjectDescriptionReader();

        public AppProvisioningTool(ProvisioningToolOptions provisioningToolOptions)
        {
            ProvisioningToolOptions = provisioningToolOptions;
        }

        public async Task<ApplicationParameters?> Run()
        {
            // If needed, infer project type from code
            ProjectDescription? projectDescription = ProjectDescriptionReader.GetProjectDescription(
                ProvisioningToolOptions.ProjectTypeIdentifier,
                ProvisioningToolOptions.ProjectPath);

            //need different error formatting when the output format is json.
            if (projectDescription == null)
            {
                Console.WriteLine($"The code in {ProvisioningToolOptions.ProjectPath} wasn't recognized as supported by the tool. Rerun with --help for details.");
                return null;
            }
            else
            {
                if (ProvisioningToolOptions.Json == null || ProvisioningToolOptions.Json == false)
                {
                    Console.WriteLine($"Detected project type {projectDescription.Identifier}. ");
                }
            }

            ProjectAuthenticationSettings projectSettings = InferApplicationParameters(
                ProvisioningToolOptions,
                projectDescription,
                ProjectDescriptionReader.projectDescriptions);

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
                AppProvisioningTool appProvisioningToolBlazorServer = new AppProvisioningTool(provisioningToolOptionsBlazorServer);
                ApplicationParameters? applicationParametersServer = await appProvisioningToolBlazorServer.Run();

                /// Processes the Blazorwasm client
                ProvisioningToolOptions provisioningToolOptionsBlazorClient = ProvisioningToolOptions.Clone();
                provisioningToolOptionsBlazorClient.ProjectPath = Path.Combine(ProvisioningToolOptions.ProjectPath, "Client");
                provisioningToolOptionsBlazorClient.WebApiClientId = applicationParametersServer?.ClientId;
                provisioningToolOptionsBlazorClient.AppIdUri = applicationParametersServer?.AppIdUri;
                provisioningToolOptionsBlazorClient.CalledApiScopes = $"{applicationParametersServer?.AppIdUri}/access_as_user";
                AppProvisioningTool appProvisioningToolBlazorClient = new AppProvisioningTool(provisioningToolOptionsBlazorClient);
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

            // Get developer credentials
            TokenCredential tokenCredential = GetTokenCredential(
                ProvisioningToolOptions,
                projectSettings.ApplicationParameters.EffectiveTenantId ?? projectSettings.ApplicationParameters.EffectiveDomain);

            // Unregister the app
            if (ProvisioningToolOptions.Unregister)
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
                if (ProvisioningToolOptions.Json ?? false)
                {
                    var jsonParameters = JsonSerializer.Serialize(effectiveApplicationParameters);
                    Console.WriteLine(jsonParameters);
                }
                else 
                {
                    bool appNeedsUpdate = Reconciliate(
                    projectSettings.ApplicationParameters,
                    effectiveApplicationParameters);

                    // Update appp registration if needed
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
            }
               
            return effectiveApplicationParameters;
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
            CodeWriter codeWriter = new CodeWriter();
            codeWriter.WriteConfiguration(summary, projectSettings.Replacements, reconcialedApplicationParameters);
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

        private async Task<ApplicationParameters?> ReadOrProvisionMicrosoftIdentityApplication(
            TokenCredential tokenCredential,
            ApplicationParameters applicationParameters)
        {
            ApplicationParameters? currentApplicationParameters = null;
            if (!string.IsNullOrEmpty(applicationParameters.EffectiveClientId))
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
                if (ProvisioningToolOptions.Json == null || ProvisioningToolOptions.Json == false)
                {
                    Console.Write($"Created app {currentApplicationParameters.ClientId}. ");
                }
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
            projectSettings.ApplicationParameters.ClientId ??= provisioningToolOptions.ClientId;
            projectSettings.ApplicationParameters.TenantId ??= provisioningToolOptions.TenantId;
            projectSettings.ApplicationParameters.CalledApiScopes ??= provisioningToolOptions.CalledApiScopes;
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
