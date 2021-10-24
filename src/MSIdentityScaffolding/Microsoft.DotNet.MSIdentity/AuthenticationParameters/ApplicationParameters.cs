// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.MSIdentity.AuthenticationParameters
{
    /// <summary>
    /// Application parameters.
    /// </summary>
    public class ApplicationParameters
    {
        /// <summary>
        /// Application display name.
        /// </summary>
        public string? ApplicationDisplayName { get; set; }

        /// <summary>
        /// Application project path
        /// <summary>
        public string? ProjectPath { get; set; }
        
        /// <summary>
        /// Tenant in which the application is created.
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>
        /// The tenant ID if it's not the default in the project template, or null otherwise.
        /// </summary>
        public string? EffectiveTenantId { get; set; }

        /// <summary>
        /// Domain of the tenant.
        /// </summary>
        public string? Domain { get; set; }

        /// <summary>
        /// Domain if it's not equal to the default in the template, or otherwise null.
        /// </summary>
        public string? EffectiveDomain { get; set; }

        /// <summary>
        /// First part of the domain.
        /// </summary>
        public string? Domain1
        {
            get
            {
                return Domain?.Replace(".onmicrosoft.com", string.Empty);
            }
        }

        /// <summary>
        /// URL that indicates a directory from which to request tokens.
        /// </summary>
        public string? Authority { get; set; }

        /// <summary>
        /// Gets or sets the Azure Active Directory instance, e.g. "https://login.microsoftonline.com".
        /// </summary>
        public string? Instance { get; set; }

        /// <summary>
        /// Client ID of the application.
        /// </summary>
        public string? ClientId { get; set; }

        /// <summary>
        /// The Client ID if it's not the default in the project template, or null otherwise.
        /// </summary>
        public string? EffectiveClientId { get; set; }

        /// <summary>
        /// Sign-in audience (tenantId or domain, organizations, common, consumers).
        /// </summary>
        public string? SignInAudience { get; set; }

        /// <summary>
        /// Is authenticated with AAD.
        /// </summary>
        public bool IsAAD { get; set; }

        /// <summary>
        /// Is authenticated with Azure AD B2C (set by reflection).
        /// </summary>
        public bool IsB2C { get; set; }


        // TODO: propose a fix for the blazorwasm project template
        
        /// <summary>
        /// Sign-up/sign-in policy in the case of B2C.
        /// </summary>
        /// <remarks>This is for the blazorwasm hosted template and more a workaround
        /// to the template. The default name of the policy appearing in 
        /// Client\wwwroot\appSettings.json:Authority should be the same as in 
        /// Server\appSettings.json:AzureADB2C:SignUpSignInPolicyId </remarks>
        public string? SusiPolicy { get; set; }

        /// <summary>
        /// The project has authentication.
        /// </summary>
        public bool HasAuthentication { get; set; }

        /// <summary>
        /// The project is a web API.
        /// </summary>
        public bool? IsWebApi { get; set; }

        /// <summary>
        /// The project is a web app.
        /// </summary>
        public bool? IsWebApp { get; set; }

        /// <summary>
        /// The project is a blazor web assembly.
        /// </summary>
        public bool? IsBlazorWasm { get; set; }

        /// <summary>
        /// The app calls Microsoft Graph.
        /// </summary>
        public bool CallsMicrosoftGraph { get; set; }

        /// <summary>
        /// The app calls a downstream API.
        /// </summary>
        public bool CallsDownstreamApi { get; set; }

        /// <summary>
        /// Scopes used to call the downsteam API, if any.
        /// </summary>
        public string? CalledApiScopes { get; set; }

        /// <summary>
        /// Web app redirect URIs.
        /// </summary>
        public List<string> WebRedirectUris { get; } = new List<string>();

        /// <summary>
        /// Callback path (path of the redirect URIs).
        /// </summary>
        public string? CallbackPath { set; get; }

        /// <summary>
        /// Logout URIs.
        /// </summary>
        public string? LogoutUrl { set; get; }

        /// <summary>
        /// Developer credentials.
        /// </summary>
        public List<string> PasswordCredentials { get; } = new List<string>();

        /// <summary>
        /// Identitier URIs for web APIs.
        /// </summary>
        public string? AppIdUri { set;  get; }

        /// <summary>
        /// API permissions.
        /// </summary>
        public List<ApiPermission> ApiPermissions { get; } = new List<ApiPermission>();

        /// <summary>
        /// Description of the app.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// SecretsId of the csproj.
        /// </summary>
        public string? SecretsId { get; internal set; }

        /// <summary>
        /// Target framework.
        /// </summary>
        public string? TargetFramework { get; internal set; }

        /// <summary>
        /// Authentication options with MSAL .NET.
        /// </summary>
        public string? MsalAuthenticationOptions { get; set; }

        /// <summary>
        /// Graph.Application and Graph.ServicePrincipal object ids.
        /// </summary>
        public string? GraphEntityId { get; set; }

        /// <summary>
        /// Sets a bool property (from its name).
        /// </summary>
        /// <param name="propertyName"></param>
        public void Sets(string propertyName)
        {
            var property = GetType().GetProperty(propertyName);
            if (property == null)
            {
                throw new ArgumentException(propertyName);
            }
            property.SetValue(this, true);
        }
    }
}
