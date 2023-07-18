// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.MSIdentity.AuthenticationParameters;
using Microsoft.DotNet.MSIdentity.DeveloperCredentials;

namespace Microsoft.DotNet.MSIdentity.Tool
{
    public class ProvisioningToolOptions : IDeveloperCredentialsOptions
    {
        public string ProjectPath { get; set; } = Directory.GetCurrentDirectory();

        /// <summary>
        /// Path to csproj file
        /// </summary>
        public string? ProjectFilePath { get; set; }

        /// <summary>
        /// Short target framework from the given ProjectFilePath. List to allow multiple tfms.
        /// eg. net6.0, net7.0 etc.
        /// </summary>
        public IList<string> ShortTfms { get; set; } = new List<string>();

        /// <summary>
        /// Path to appsettings.json file
        /// </summary>   
        public string? AppSettingsFilePath { get; set; }

        ///<summary>
        /// Display name for Azure AD/AD B2C app registration
        ///</summary>
        public string? AppDisplayName { get; set; }

        ///<summary>
        /// Web redirect URIs.
        ///</summary>
        public IList<string> RedirectUris { get; set; } = new List<string>();

        ///<summary>
        /// enable id tokens to be issued by the authZ endpoint
        ///</summary>
        public bool? EnableIdToken { get; set; }

        /// <summary>
        /// enable access token to be issued by the authZ endpoint
        /// </summary>
        public bool? EnableAccessToken { get; set; }

        /// <summary>
        /// Language/Framework for the project.
        /// </summary>
        public string LanguageOrFramework { get; set; } = "dotnet";

        /// <summary>
        /// Type of project. 
        /// For instance web app, web API, blazorwasm-hosted, ...
        /// </summary>
        public string? ProjectType { get; set; }

        /// <summary>
        /// Identifier of a project type. This is the concatenation of the framework
        /// and the project type. This is the identifier of the extension describing 
        /// the authentication pieces of the project.
        /// </summary>
        public string ProjectTypeIdentifier
        {
            get
            {
                return $"{LanguageOrFramework}-{ProjectType}";
            }
        }

        /// <summary>
        /// Identity (for instance joe@cotoso.com) that is allowed to
        /// provision the application in the tenant. Optional if you want
        /// to use the developer credentials (Visual Studio).
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Client secret for the application.
        /// </summary>
        public string? ClientSecret { get; set; }

        /// <summary>
        /// Client ID of the application (optional).
        /// </summary>
        public string? ClientId { get; set; }

        /// <summary>
        /// Client ID of the blazorwasm hosted web api application (optional).
        /// This is only used in the case of blazorwasm hosted. The name is after
        /// the blazorwasm template's parameter --api-client-id
        /// </summary>
        public string? WebApiClientId { get; set; }

        /// <summary>
        /// Tenant ID of the application (optional if the user belongs to
        /// only one tenant).
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>
        /// URL that indicates a directory that MSAL can request tokens from.
        /// e.g. https://login.microsoftonline.com/, https://login.microsoftonline.us/
        /// </summary>
        public string? Instance { get; set; }

        /// <summary>
        /// Required for the creation of a B2C application.
        /// Represents the sign-up/sign-in user flow.
        /// </summary>
        public string? SusiPolicyId { get; set; }

        /// <summary>
        /// Display Help.
        /// </summary>
        internal bool Help { get; set; }

        /// <summary>
        /// Unregister a previously created application.
        /// </summary>
        public bool Unregister { get; set; }

        /// <summary>
        /// Scopes for the called downstream API.
        /// </summary>
        public string? ApiScopes { get; set; }

        /// <summary>
        /// Scopes for the Blazor WASM hosted API.
        /// </summary>
        public string? HostedApiScopes { get; set; }

        /// <summary>
        /// Url for the called web API.
        /// </summary>
        public string? CalledApiUrl { get; set; }

        /// <summary>
        /// Calls Microsoft Graph.
        /// </summary>
        public bool CallsGraph { get; set; }

        /// <summary>
        /// Calls Downstream API
        /// </summary>
        public bool CallsDownstreamApi { get; set; }

        /// <summary>
        /// Add secrets to user secrets.json file.
        /// </summary>
        public bool UpdateUserSecrets { get; set; }

        /// <summary>
        /// Format for console output for list commands.
        /// </summary>
        public bool Json { get; set; } = false;

        /// <summary>
        /// Make config changes to appsettings.json 
        /// </summary>
        public bool ConfigUpdate { get; set; } = false;

        /// <summary>
        /// Make changes to Startup.cs 
        /// </summary>
        public bool CodeUpdate { get; set; } = false;

        /// <summary>
        /// Make PackageReferences in .csproj (add using `dotnet add package`)
        /// </summary>
        public bool PackagesUpdate { get; set; } = false;

        /// <summary>
        /// The App ID Uri for the blazorwasm hosted API. It's only used
        /// in the case of a blazorwasm hosted application.
        /// </summary>
        public string? HostedAppIdUri { get; set; }

        /// <summary>
        /// Provisions app registrations and applies code updates for Blazor WASM client and server in hosted scenario
        /// </summary>
        public bool IsBlazorWasmHostedServer => !string.IsNullOrEmpty(ClientProject);

        /// <summary>
        /// Path to csproj file of the Blazor WASM hosted client
        /// </summary>
        public string? ClientProject { get; set; }

        /// <summary>
        /// Determines if the project type is blazor wasm
        /// </summary>
        public bool IsBlazorWasm => ProjectTypes.BlazorWasm.Equals(ProjectType) || ProjectTypes.BlazorWasmClient.Equals(ProjectType);

        /// <summary>
        /// App registration ID associated with the Blazor WASM hosted client, Used for the Blazor WASM hosted server API in order to pre-authorize the client app
        /// </summary>
        public string? BlazorWasmClientAppId { get; internal set; }

        public bool IsGovernmentCloud => string.Equals(Instance, DefaultProperties.GovernmentCloudInstance, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Clones the options
        /// </summary>
        /// <returns></returns>
        public ProvisioningToolOptions Clone()
        {
            return new ProvisioningToolOptions()
            {
                HostedApiScopes = HostedApiScopes,
                CalledApiUrl = CalledApiUrl,
                CallsDownstreamApi = CallsDownstreamApi,
                UpdateUserSecrets = UpdateUserSecrets,
                CallsGraph = CallsGraph,
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                LanguageOrFramework = LanguageOrFramework,
                Help = Help,
                ProjectType = ProjectType,
                SusiPolicyId = SusiPolicyId,
                TenantId = TenantId,
                Unregister = Unregister,
                Username = Username,
                ProjectPath = ProjectPath,
                ProjectFilePath = ProjectFilePath,
                AppSettingsFilePath = AppSettingsFilePath,
                WebApiClientId = WebApiClientId,
                HostedAppIdUri = HostedAppIdUri,
                Json = Json,
                AppDisplayName = AppDisplayName,
                RedirectUris = RedirectUris
            };
        }
    }

    /// <summary>
    /// Extension methods for ProvisioningToolOptions.
    /// </summary>
    public static class ProvisioningToolOptionsExtensions
    {
        /// <summary>
        /// Identifier of a project type. This is the concatenation of the framework
        /// and the project type. This is the identifier of the extension describing 
        /// the authentication pieces of the project
        /// </summary>
        public static string GetProjectTypeIdentifier(this ProvisioningToolOptions provisioningToolOptions)
        {
            return $"{provisioningToolOptions.LanguageOrFramework}-{provisioningToolOptions.ProjectType}";
        }
    }
}
