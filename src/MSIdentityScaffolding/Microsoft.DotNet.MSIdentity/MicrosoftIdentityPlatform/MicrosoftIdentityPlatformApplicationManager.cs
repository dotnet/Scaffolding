// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.DotNet.MSIdentity.AuthenticationParameters;
using Microsoft.DotNet.MSIdentity.Properties;
using Microsoft.DotNet.MSIdentity.Shared;
using Microsoft.DotNet.MSIdentity.Tool;
using Microsoft.Graph;

namespace Microsoft.DotNet.MSIdentity.MicrosoftIdentityPlatformApplication
{
    public class MicrosoftIdentityPlatformApplicationManager
    {
        private readonly StringBuilder _output = new StringBuilder();
        const string MicrosoftGraphAppId = "00000003-0000-0000-c000-000000000000";
        const string ScopeType = "Scope";
        private const string DefaultCallbackPath = "signin-oidc";
        private const string BlazorWasmCallbackPath = "authentication/login-callback";
        GraphServiceClient? _graphServiceClient;

        internal async Task<ApplicationParameters?> CreateNewAppAsync(
            TokenCredential tokenCredential,
            ApplicationParameters applicationParameters,
            IConsoleLogger consoleLogger)
        {
            try
            {
                var graphServiceClient = GetGraphServiceClient(tokenCredential);

                // Get the tenant
                Organization? tenant = await GetTenant(graphServiceClient, consoleLogger);
                if (tenant != null)
                {
                    applicationParameters.IsB2C = tenant.TenantType.Equals("AAD B2C", StringComparison.OrdinalIgnoreCase);
                    applicationParameters.IsCiam = tenant.TenantType.Equals("CIAM", StringComparison.OrdinalIgnoreCase);
                }

                // Create the app.
                Application application = new Application()
                {
                    DisplayName = applicationParameters.ApplicationDisplayName,
                    SignInAudience = AppParameterAudienceToMicrosoftIdentityPlatformAppAudience(applicationParameters.SignInAudience!),
                    Description = applicationParameters.Description
                };

                if (applicationParameters.IsWebApi.GetValueOrDefault())
                {
                    application.Api = new ApiApplication()
                    {
                        RequestedAccessTokenVersion = 2,
                    };
                }

                if (applicationParameters.IsWebApp.GetValueOrDefault())
                {
                    AddWebAppPlatform(application, applicationParameters);
                }
                else if (applicationParameters.IsBlazorWasm)
                {
                    AddSpaPlatform(application, applicationParameters.WebRedirectUris);
                }

                var createdApplication = await graphServiceClient.Applications
                    .Request()
                    .AddAsync(application);

                // Create service principal, necessary for Web API applications
                // and useful for Blazorwasm hosted applications. We create it always.
                var createdSp = await GetOrCreateSP(graphServiceClient, createdApplication.AppId, consoleLogger);

                // B2C & CIAM do not allow user consent, and therefore we need to explicitly grant permissions
                if (applicationParameters.IsB2C || applicationParameters.IsCiam)
                {
                    string scopes = GetMsGraphScopes(applicationParameters); // Explicit usage of MicrosoftGraph openid and offline_access in the case of Azure AD B2C.
                    await AddDownstreamApiPermissions(scopes, graphServiceClient, application, createdSp);
                }

                // For web API, we need to know the appId of the created app to compute the Identifier URI,
                // and therefore we need to do it after the app is created (updating the app)
                if (applicationParameters.IsWebApi.GetValueOrDefault() && createdApplication.Api != null)
                {
                    await ExposeWebApiScopes(graphServiceClient, createdApplication, applicationParameters);

                    // Re-reading the app to be sure to have everything.
                    createdApplication = (await graphServiceClient.Applications
                        .Request()
                        .Filter($"appId eq '{createdApplication.AppId}'")
                        .GetAsync()).FirstOrDefault();
                }

                // log json console message inside this method since we need the Microsoft.Graph.Application
                if (createdApplication is null)
                {
                    consoleLogger.LogFailureAndExit(Resources.FailedToCreateApp);
                    return null;
                }

                createdApplication!.AdditionalData.Add("IsB2C", applicationParameters.IsB2C);
                createdApplication!.AdditionalData.Add("IsCIAM", applicationParameters.IsCiam);

                ApplicationParameters? effectiveApplicationParameters = GetEffectiveApplicationParameters(tenant!, createdApplication, applicationParameters);

                // Add password credentials
                if (applicationParameters.CallsMicrosoftGraph || applicationParameters.CallsDownstreamApi)
                {
                    await AddPasswordCredentialsAsync(
                        graphServiceClient,
                        createdApplication.Id,
                        effectiveApplicationParameters,
                        consoleLogger);
                }

                var output = string.Format(Resources.CreatedAppRegistration, effectiveApplicationParameters.ApplicationDisplayName, effectiveApplicationParameters.ClientId);
                consoleLogger.LogJsonMessage(State.Success, content: createdApplication, output: output);

                return effectiveApplicationParameters;
            }
            catch (Exception ex)
            {
                var errorMessage = string.IsNullOrEmpty(ex.Message) ? ex.ToString() : ex.Message;
                consoleLogger.LogFailureAndExit(errorMessage);
                return null;
            }
        }

        /// <summary>
        /// Explicit usage of MicrosoftGraph openid and offline_access in the case of Azure AD B2C, CIAM.
        /// </summary>
        /// <param name="applicationParameters"></param>
        /// <returns></returns>
        private static string GetMsGraphScopes(ApplicationParameters applicationParameters)
        {
            var apiScopes = applicationParameters.CalledApiScopes ?? string.Empty;
            if (!apiScopes.Contains("openid", StringComparison.OrdinalIgnoreCase))
            {
                apiScopes += " openid";
            }
            if (!apiScopes.Contains("offline_access", StringComparison.OrdinalIgnoreCase))
            {
                apiScopes += " offline_access";
            }

            return apiScopes.Trim();
        }

        private static async Task<Organization?> GetTenant(GraphServiceClient graphServiceClient, IConsoleLogger consoleLogger)
        {
            Organization? tenant = null;
            try
            {
                tenant = (await graphServiceClient.Organization
                    .Request()
                    .GetAsync()).FirstOrDefault();
            }
            catch (ServiceException ex)
            {
                if (ex.InnerException != null)
                {
                    consoleLogger.LogFailureAndExit(ex.InnerException.Message);
                }
                else
                {
                    if (ex.Message.Contains("User was not found") || ex.Message.Contains("not found in tenant"))
                    {
                        consoleLogger.LogFailureAndExit("User was not found.\nUse both --tenant-id <tenant> --username <username@tenant>.\nAnd re-run the tool.");
                    }
                    else
                    {
                        consoleLogger.LogFailureAndExit(ex.Message);
                    }
                }
            }

            return tenant;
        }

        /// <summary>
        /// Compares the parameters of the remote App Registration with the input parameters given to the tool,
        /// if any updates need to be made, sends a request using the GraphServiceClient to update the app registration in Azure AD
        /// </summary>
        /// <param name="tokenCredential"></param>
        /// <param name="parameters"></param>
        /// <param name="toolOptions"></param>
        /// <param name="commandName"></param>
        /// <returns></returns>
        internal async Task UpdateApplication(
            TokenCredential tokenCredential,
            ApplicationParameters? parameters,
            ProvisioningToolOptions toolOptions,
            IConsoleLogger consoleLogger,
            StringBuilder? output = null)
        {
            if (parameters is null)
            {
                consoleLogger.LogFailureAndExit(string.Format(Resources.FailedToUpdateAppNull, nameof(ApplicationParameters)));
            }

            var graphServiceClient = GetGraphServiceClient(tokenCredential);

            var remoteApp = (await graphServiceClient.Applications.Request()
                .Filter($"appId eq '{parameters!.ClientId}'").GetAsync()).FirstOrDefault(app => app.AppId.Equals(parameters.ClientId));

            if (remoteApp is null)
            {
                consoleLogger.LogFailureAndExit(string.Format(Resources.NotFound, parameters.ClientId));
                return;
            }

            (bool needsUpdates, Application appUpdates) = GetApplicationUpdates(remoteApp, toolOptions, parameters);
            output ??= new StringBuilder();

            ServicePrincipal? servicePrincipal = null;
            // B2C & CIAM do not allow user consent, and therefore we need to explicitly grant permissions
            if (parameters.IsB2C || parameters.IsCiam) // TODO Test DownstreamAPI, Test B2C
            {
                servicePrincipal = await GetOrCreateSP(graphServiceClient, parameters.ClientId, consoleLogger);
                string scopes = GetMsGraphScopes(parameters);
                await AddDownstreamApiPermissions(scopes, graphServiceClient, appUpdates, servicePrincipal, output);
                needsUpdates = true;
            }

            if (!string.IsNullOrEmpty(toolOptions.ApiScopes)) // TODO Test DownstreamAPI, Test B2C
            {
                servicePrincipal ??= await GetOrCreateSP(graphServiceClient, parameters.ClientId, consoleLogger);
                await AddDownstreamApiPermissions(toolOptions.ApiScopes, graphServiceClient, appUpdates, servicePrincipal, output);
                needsUpdates = true;
            }

            if (!needsUpdates)
            {
                consoleLogger.LogJsonMessage(State.Success, output: string.Format(Resources.NoUpdateNecessary, remoteApp.DisplayName, remoteApp.AppId));
                return;
            }

            try
            {
                // TODO: update other fields, see https://github.com/jmprieur/app-provisonning-tool/issues/10
                var updatedApp = await graphServiceClient.Applications[remoteApp.Id].Request().UpdateAsync(appUpdates);
                output.Append(string.Format(Resources.SuccessfullyUpdatedApp, remoteApp.DisplayName, remoteApp.AppId));
                consoleLogger.LogJsonMessage(State.Success, output: output.ToString());
            }
            catch (ServiceException se)
            {
                output.Append(se.Error?.Message);
                consoleLogger.LogFailureAndExit(output.ToString());
            }
        }

        internal static async Task AddDownstreamApiPermissions(string? apiScopes, GraphServiceClient graphServiceClient, Application appUpdates, ServicePrincipal servicePrincipal, StringBuilder? output = null)
        {
            IEnumerable<IGrouping<string, ResourceAndScope>>? scopesPerResource = await AddApiPermissions(
                apiScopes,
                graphServiceClient,
                appUpdates).ConfigureAwait(false);

            await AddAdminConsentToApiPermissions(
                graphServiceClient,
                servicePrincipal,
                scopesPerResource,
                output);
        }

        private static async Task<ServicePrincipal> GetOrCreateSP(GraphServiceClient graphServiceClient, string? clientId, IConsoleLogger consoleLogger)
        {
            var servicePrincipal = (await graphServiceClient.ServicePrincipals
                                .Request()
                                .Filter($"appId eq '{clientId}'")
                                .GetAsync())?.FirstOrDefault();

            if (servicePrincipal is null)
            {
                // Create service principal, necessary for Web API applications
                // and useful for Blazorwasm hosted applications. We create it always.
                var sp = new ServicePrincipal
                {
                    AppId = clientId,
                };

                servicePrincipal = await graphServiceClient.ServicePrincipals
                    .Request()
                    .AddAsync(sp);
            }

            if (servicePrincipal is null)
            {
                consoleLogger.LogFailureAndExit(Resources.FailedToGetServicePrincipal);
            }

            return servicePrincipal!;
        }

        /// <summary>
        /// Determines whether redirect URIs or implicit grant settings need updating and makes the appropriate modifications based on project type
        /// </summary>
        /// <param name="existingApplication"></param>
        /// <param name="toolOptions"></param>
        /// <returns>Updated Application if changes were made, otherwise null</returns>
        internal static (bool needsUpdate, Application appUpdates) GetApplicationUpdates(
            Application existingApplication,
            ProvisioningToolOptions toolOptions,
            ApplicationParameters parameters)
        {
            bool needsUpdate = false;

            // All applications require Web, Blazor WASM applications also require SPA (Single Page Application)
            var updatedApp = new Application
            {
                Web = existingApplication.Web ?? new WebApplication(),
                Spa = toolOptions.IsBlazorWasm ? existingApplication.Spa ?? new SpaApplication() : existingApplication.Spa
            };

            // Make updates if necessary
            needsUpdate |= UpdateRedirectUris(updatedApp, toolOptions);
            needsUpdate |= UpdateImplicitGrantSettings(updatedApp, toolOptions, parameters.IsB2C);
            if (toolOptions.IsBlazorWasmHostedServer)
            {
                needsUpdate |= PreAuthorizeBlazorWasmClientApp(existingApplication, toolOptions, updatedApp);
            }

            return (needsUpdate, updatedApp);
        }

        internal static bool PreAuthorizeBlazorWasmClientApp(Application existingApplication, ProvisioningToolOptions toolOptions, Application updatedApp)
        {
            if (string.IsNullOrEmpty(toolOptions.BlazorWasmClientAppId))
            {
                return false;
            }

            var delegatedPermissionId = existingApplication.Api?.Oauth2PermissionScopes?.FirstOrDefault()?.Id.ToString();
            if (string.IsNullOrEmpty(delegatedPermissionId))
            {
                return false;
            }

            if (existingApplication.Api?.PreAuthorizedApplications?.Any(
                app => string.Equals(toolOptions.BlazorWasmClientAppId, app.AppId)
                && app.DelegatedPermissionIds.Any(id => id.Equals(delegatedPermissionId))) is true)
            {
                return false;
            }

            var preAuthorizedApp = new PreAuthorizedApplication
            {
                AppId = toolOptions.BlazorWasmClientAppId,
                DelegatedPermissionIds = new List<string> { delegatedPermissionId }
            };

            updatedApp.Api = existingApplication.Api ?? new ApiApplication();

            updatedApp.Api.PreAuthorizedApplications = updatedApp.Api.PreAuthorizedApplications?.Append(preAuthorizedApp)
                ?? new List<PreAuthorizedApplication> { preAuthorizedApp };

            return true;
        }

        /// <summary>
        /// Updates redirect URIs if necessary.
        /// </summary>
        /// <param name="updatedApp"></param>
        /// <param name="toolOptions"></param>
        /// <returns>true if redirect URIs are to be updated, else false</returns>
        private static bool UpdateRedirectUris(Application updatedApp, ProvisioningToolOptions toolOptions)
        {
            // Scenarios when redirect URIs need to be updated:
            // - New redirect URIs are added from the tool
            // - The app registration is being switched to a different project type (e.g. switching from Web App to a Blazor WASM)

            // Collect all remote redirect URIs (Web and SPA)
            // If the project type changed, we still may want the Redirect URIs associated with the old project type
            var allRemoteUris = updatedApp.Web.RedirectUris.Union(updatedApp.Spa.RedirectUris).Distinct();

            // Validate local URIs
            var validatedLocalUris = toolOptions.RedirectUris.Where(uri => IsValidUri(uri));

            // Merge all redirect URIs
            var allRedirectUris = allRemoteUris.Union(validatedLocalUris);

            // Update callback paths based on the project type
            var processedRedirectUris = allRedirectUris.Select(uri => UpdateCallbackPath(uri, toolOptions.IsBlazorWasm)).Distinct();

            // If there are any differences between our processed list and the remote list, update the remote list (Web or SPA)
            if (toolOptions.IsBlazorWasm && processedRedirectUris.Except(updatedApp.Spa.RedirectUris).Any())
            {
                updatedApp.Spa.RedirectUris = processedRedirectUris;
                return true;
            }
            else if (processedRedirectUris.Except(updatedApp.Web.RedirectUris).Any())
            {
                updatedApp.Web.RedirectUris = processedRedirectUris;
                return true;
            }

            return false;
        }

        /// <summary>
        /// URI is valid when scheme is https, if scheme is http then must be referencing localhost. IsLoopback checks for localhost, loopback and 127.0.0.1
        /// </summary>
        /// <param name="uriString"></param>
        /// <returns>true for valid URI, else false</returns>
        internal static bool IsValidUri(string uriString)
        {
            return Uri.TryCreate(uriString, UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback));
        }

        /// <summary>
        /// Updates the callback path for input redirectUri based on the project type,
        /// Blazor: "authentication/login-callback", other project types: "signin-oidc"
        /// </summary>
        /// <param name="redirectUri"></param>
        /// <param name="isBlazorWasm"></param>
        /// <returns>updated callback path</returns>
        private static string UpdateCallbackPath(string redirectUri, bool isBlazorWasm = false)
        {
            return new UriBuilder(redirectUri)
            {
                Path = isBlazorWasm ? BlazorWasmCallbackPath : DefaultCallbackPath
            }.Uri.ToString();
        }

        /// <summary>
        /// Updates application's implicit grant settings if necessary
        /// </summary>
        /// <param name="app"></param>
        /// <param name="toolOptions"></param>
        /// <returns>true if ImplicitGrantSettings require updates, else false</returns>
        internal static bool UpdateImplicitGrantSettings(Application app, ProvisioningToolOptions toolOptions, bool isB2C = false)
        {
            bool needsUpdate = false;
            var currentSettings = app.Web.ImplicitGrantSettings;

            // In the case of Blazor WASM and B2C, Access Tokens and Id Tokens must both be true.
            if ((toolOptions.IsBlazorWasm || isB2C)
                && (currentSettings.EnableAccessTokenIssuance is false
                || currentSettings.EnableIdTokenIssuance is false))
            {
                app.Web.ImplicitGrantSettings.EnableAccessTokenIssuance = true;
                app.Web.ImplicitGrantSettings.EnableIdTokenIssuance = true;
                needsUpdate = true;
            }
            // Otherwise we make changes only when the tool options differ from the existing settings.
            else
            {
                if (toolOptions.EnableAccessToken.HasValue && toolOptions.EnableAccessToken.Value != currentSettings.EnableAccessTokenIssuance)
                {
                    app.Web.ImplicitGrantSettings.EnableAccessTokenIssuance = toolOptions.EnableAccessToken.Value;
                    needsUpdate = true;
                }

                if (toolOptions.EnableIdToken.HasValue && toolOptions.EnableIdToken.Value != currentSettings.EnableIdTokenIssuance)
                {
                    app.Web.ImplicitGrantSettings.EnableIdTokenIssuance = toolOptions.EnableIdToken.Value;
                    needsUpdate = true;
                }
            }

            return needsUpdate;
        }

        /// <summary>
        /// Add a password credential to the app
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <param name="createdApplication"></param>
        /// <param name="effectiveApplicationParameters"></param>
        /// <returns></returns>
        internal static async Task<string> AddPasswordCredentialsAsync(
            GraphServiceClient graphServiceClient,
            string applicatonId,
            ApplicationParameters effectiveApplicationParameters,
            IConsoleLogger consoleLogger)
        {
            string? password = string.Empty;
            var passwordCredential = new PasswordCredential
            {
                DisplayName = "Secret created by dotnet-msidentity tool"
            };

            if (!string.IsNullOrEmpty(applicatonId) && graphServiceClient != null)
            {
                try
                {
                    PasswordCredential returnedPasswordCredential = await graphServiceClient.Applications[$"{applicatonId}"]
                        .AddPassword(passwordCredential)
                        .Request()
                        .PostAsync();
                    password = returnedPasswordCredential.SecretText;
                    effectiveApplicationParameters.PasswordCredentials.Add(password);
                }
                catch (ServiceException se)
                {
                    consoleLogger.LogMessage($"Failed to create password : {se.Error?.Message}", LogMessageType.Error);
                    throw;
                }
            }
            return password;
        }

        /// <summary>
        /// Expose scopes for the web API.
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <param name="appId"></param>
        /// <param name="graphEntityId"></param>
        /// <param name="scopes">existing scopes</param>
        /// <returns>Identifier URI for exposed scope</returns>
        internal static async Task ExposeScopes(GraphServiceClient graphServiceClient, string? scopeIdentifier, string? graphEntityId, List<PermissionScope>? scopes = null)
        {
            var updatedApp = new Application
            {
                IdentifierUris = new[] { scopeIdentifier }
            };

            scopes ??= new List<PermissionScope>();
            var newScope = new PermissionScope
            {
                Id = Guid.NewGuid(),
                AdminConsentDescription = "Allows the app to access the web API on behalf of the signed-in user",
                AdminConsentDisplayName = "Access the API on behalf of a user",
                Type = "User",
                IsEnabled = true,
                UserConsentDescription = "Allows this app to access the web API on your behalf",
                UserConsentDisplayName = "Access the API on your behalf",
                Value = DefaultProperties.ApiScopes,
            };

            scopes.Add(newScope);
            updatedApp.Api = new ApiApplication { Oauth2PermissionScopes = scopes };
            await graphServiceClient.Applications[graphEntityId]
                .Request()
                .UpdateAsync(updatedApp);
        }

        /// <summary>
        /// Expose scopes for the B2C API.
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <param name="createdApplication"></param>
        /// <returns></returns>
        internal static async Task ExposeWebApiScopes(GraphServiceClient graphServiceClient, Application createdApplication, ApplicationParameters applicationParameters)
        {
            var scopes = createdApplication.Api.Oauth2PermissionScopes?.ToList() ?? new List<PermissionScope>();
            var scopeName = (applicationParameters.IsB2C || applicationParameters.IsCiam)
                ? $"https://{createdApplication.PublisherDomain}/{createdApplication.AppId}"
                : $"api://{createdApplication.AppId}";
            await ExposeScopes(graphServiceClient, scopeName, createdApplication.Id, scopes);
        }

        /// <summary>
        /// Admin consent to API permissions
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <param name="scopesPerResource"></param>
        /// <returns></returns>
        private static async Task AddAdminConsentToApiPermissions(
            GraphServiceClient graphServiceClient,
            ServicePrincipal servicePrincipal,
            IEnumerable<IGrouping<string, ResourceAndScope>>? scopesPerResource,
            StringBuilder? output = null)
        {
            // Consent to the scopes
            if (scopesPerResource != null)
            {
                foreach (var g in scopesPerResource)
                {
                    IEnumerable<ResourceAndScope> resourceAndScopes = g;

                    var oAuth2PermissionGrant = new OAuth2PermissionGrant
                    {
                        ClientId = servicePrincipal.Id,
                        ConsentType = "AllPrincipals",
                        PrincipalId = null,
                        ResourceId = resourceAndScopes.FirstOrDefault()?.ResourceServicePrincipalId,
                        Scope = string.Join(" ", resourceAndScopes.Select(r => r.Scope))
                    };

                    // Check if permissions already exist, otherwise will throw exception
                    try
                    {
                        // TODO: See https://github.com/jmprieur/app-provisonning-tool/issues/9. 
                        // We need to process the case where the developer is not a tenant admin
                        await graphServiceClient.Oauth2PermissionGrants
                            .Request()
                            .AddAsync(oAuth2PermissionGrant);
                    }
                    catch (Microsoft.Graph.ServiceException ex)
                    {
                        if (ex.Message.Contains("Permission entry already exists"))
                        {
                            output?.AppendLine(string.Format(Resources.PermissionExists, g.Key));
                        }
                        else
                        {
                            output?.AppendLine(ex.Message);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add API permissions.
        /// </summary>
        /// <param name="applicationParameters"></param>
        /// <param name="graphServiceClient"></param>
        /// <param name="application"></param>
        /// <returns></returns>
        private static async Task<IEnumerable<IGrouping<string, ResourceAndScope>>?> AddApiPermissions(
            string? calledApiScopes,
            GraphServiceClient graphServiceClient,
            Application application)
        {
            // Case where the app calls a downstream API
            var apiRequests = application.RequiredResourceAccess ?? new List<RequiredResourceAccess>();

            IEnumerable<IGrouping<string, ResourceAndScope>>? scopesPerResource = null;
            if (!string.IsNullOrEmpty(calledApiScopes))
            {
                string[] scopes = calledApiScopes.Split(' ', '\t', StringSplitOptions.RemoveEmptyEntries);
                scopesPerResource = scopes.Select(s => (!s.Contains('/'))
                // Microsoft Graph shortcut scopes (for instance "User.Read")
                ? new ResourceAndScope("https://graph.microsoft.com", s)
                // Proper AppIdUri/scope
                : new ResourceAndScope(s[..s.LastIndexOf('/')], s[(s.LastIndexOf('/') + 1)..])
                ).GroupBy(r => r.Resource)
                .ToArray(); // We want to modify these elements to cache the service principal ID

                foreach (var grouping in scopesPerResource)
                {
                    var requiredResourceAccess = await AddPermission(graphServiceClient, grouping);
                    apiRequests = apiRequests.Append(requiredResourceAccess);
                }
            }

            if (apiRequests.Any())
            {
                application.RequiredResourceAccess = apiRequests;
            }

            return scopesPerResource;
        }

        /// <summary>
        /// Adds SPA redirect URIs, ensures that the callback paths are correct
        /// </summary>
        /// <param name="application"></param>
        /// <param name="redirectUris"></param>
        private static void AddSpaPlatform(Application application, List<string> redirectUris)
        {
            application.Spa = new SpaApplication
            {
                RedirectUris = redirectUris.Select(uri => UpdateCallbackPath(uri, isBlazorWasm: true))
            };
        }

        /// <summary>
        /// Adds the Web redirect URIs (and required scopes in the case of B2C web apis)
        /// </summary>
        /// <param name="application"></param>
        /// <param name="applicationParameters"></param>
        /// <param name="withImplicitFlow">Should it add the implicit flow access token (for Blazor in netcore3.1)</param>
        private static void AddWebAppPlatform(Application application, ApplicationParameters applicationParameters, bool withImplicitFlow = false)
        {
            application.Web = new WebApplication();

            // IdToken
            if (withImplicitFlow || (!applicationParameters.CallsDownstreamApi && !applicationParameters.CallsMicrosoftGraph))
            {
                application.Web.ImplicitGrantSettings = new ImplicitGrantSettings
                {
                    EnableIdTokenIssuance = true,
                    EnableAccessTokenIssuance = withImplicitFlow || applicationParameters.IsB2C
                };
            }

            // Redirect URIs
            application.Web.RedirectUris = applicationParameters.WebRedirectUris.Select(uri => UpdateCallbackPath(uri));

            // Logout URI
            application.Web.LogoutUrl = applicationParameters.LogoutUrl;
        }

        /// <summary>
        /// Adds API permissions
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <param name="apiRequests"></param>
        /// <param name="g"></param>
        /// <returns></returns>
        private static async Task<RequiredResourceAccess?> AddPermission(
            GraphServiceClient graphServiceClient,
            IGrouping<string, ResourceAndScope> g)
        {
            var spsWithScopes = await graphServiceClient.ServicePrincipals
                .Request()
                .Filter($"servicePrincipalNames/any(t: t eq '{g.Key}')")
                .GetAsync();

            // Special case for B2C where the service principal does not contain the graph URL :(
            if (!spsWithScopes.Any() && g.Key == "https://graph.microsoft.com")
            {
                spsWithScopes = await graphServiceClient.ServicePrincipals
                                .Request()
                                .Filter($"AppId eq '{MicrosoftGraphAppId}'")
                                .GetAsync();
            }

            ServicePrincipal? spWithScopes = spsWithScopes.FirstOrDefault();

            if (spWithScopes == null)
            {
                // Throw new ArgumentException($"Service principal named {g.Key} not found.", nameof(g));
                // TODO: Add to output: Warning could not find service principal
                return null;
            }

            // Keep the service principal ID for later
            foreach (ResourceAndScope r in g)
            {
                r.ResourceServicePrincipalId = spWithScopes.Id;
            }

            IEnumerable<string> scopes = g.Select(r => r.Scope.ToLower(CultureInfo.InvariantCulture));
            var permissionScopes = spWithScopes.Oauth2PermissionScopes?
                .Where(s => scopes.Contains(s.Value.ToLower(CultureInfo.InvariantCulture)));

            if (permissionScopes is null)
            {
                return null;
            }

            RequiredResourceAccess requiredResourceAccess = new RequiredResourceAccess
            {
                ResourceAppId = spWithScopes.AppId,
                ResourceAccess = new List<ResourceAccess>(permissionScopes.Select(p =>
                 new ResourceAccess
                 {
                     Id = p.Id,
                     Type = ScopeType
                 }))
            };

            return requiredResourceAccess;
        }

        /// <summary>
        /// Computes the audience
        /// </summary>
        /// <param name="audience"></param>
        /// <returns></returns>
        private string MicrosoftIdentityPlatformAppAudienceToAppParameterAudience(string audience)
        {
            return audience switch
            {
                "AzureADMyOrg" => "SingleOrg",
                "AzureADMultipleOrgs" => "MultiOrg",
                "AzureADandPersonalMicrosoftAccount" => "MultiOrgAndPersonal",
                "PersonalMicrosoftAccount" => "Personal",
                _ => "SingleOrg",
            };
        }

        private string AppParameterAudienceToMicrosoftIdentityPlatformAppAudience(string audience)
        {
            return audience switch
            {
                "SingleOrg" => "AzureADMyOrg",
                "MultiOrg" => "AzureADMultipleOrgs",
                "Personal" => "PersonalMicrosoftAccount",
                _ => "AzureADandPersonalMicrosoftAccount",
            };
        }

        internal async Task<bool> UnregisterAsync(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            bool unregisterSuccess = false;
            var graphServiceClient = GetGraphServiceClient(tokenCredential);

            var readApplication = (await graphServiceClient.Applications
               .Request()
               .Filter($"appId eq '{applicationParameters.ClientId}'")
               .GetAsync()).FirstOrDefault();

            if (readApplication != null)
            {
                try
                {
                    var clientId = readApplication.Id;
                    await graphServiceClient.Applications[$"{readApplication.Id}"]
                        .Request()
                        .DeleteAsync();
                    unregisterSuccess = true;
                }
                catch (ServiceException)
                {
                    unregisterSuccess = false;
                }
            }

            return unregisterSuccess;
        }

        internal GraphServiceClient GetGraphServiceClient(TokenCredential tokenCredential)
        {
            if (_graphServiceClient == null)
            {
                _graphServiceClient = new GraphServiceClient(new TokenCredentialAuthenticationProvider(tokenCredential));
            }
            return _graphServiceClient;
        }

        /// <summary>
        /// Reads application parameters from Azure AD for a given app registration client ID
        /// </summary>
        /// <param name="tokenCredential"></param>
        /// <param name="applicationParameters"></param>
        /// <param name="consoleLogger"></param>
        /// <returns></returns>
        public async Task<ApplicationParameters?> ReadApplication(TokenCredential tokenCredential, ApplicationParameters applicationParameters, IConsoleLogger consoleLogger)
        {
            if (string.IsNullOrEmpty(applicationParameters.EffectiveClientId) &&
               (string.IsNullOrEmpty(applicationParameters.ClientId) || DefaultProperties.ClientId.Equals(applicationParameters.ClientId, StringComparison.OrdinalIgnoreCase)))
            {
                var exception = new ArgumentException(nameof(applicationParameters.ClientId));
                consoleLogger.LogMessage(exception.Message, LogMessageType.Error);
                return null;
            }

            var graphServiceClient = GetGraphServiceClient(tokenCredential);
            Organization? tenant = await GetTenant(graphServiceClient, consoleLogger);

            var application = await GetApplication(tokenCredential, applicationParameters);
            if (application is null)
            {
                var errorMsg = string.Format(Resources.AppNotFound, applicationParameters.EffectiveClientId, applicationParameters.EffectiveTenantId);
                consoleLogger.LogMessage(errorMsg, LogMessageType.Error);
                return null;
            }

            ApplicationParameters effectiveApplicationParameters = GetEffectiveApplicationParameters(
                tenant!,
                application,
                applicationParameters);

            if (effectiveApplicationParameters is null)
            {
                consoleLogger.LogFailureAndExit(Resources.FailedToRetrieveApplicationParameters);
            }

            return effectiveApplicationParameters;
        }

        public async Task<Application?> GetApplication(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            var graphServiceClient = GetGraphServiceClient(tokenCredential);
            var apps = await graphServiceClient.Applications
                .Request()
                .Filter($"appId eq '{applicationParameters.ClientId}'")
                .GetAsync();

            return apps.FirstOrDefault();
        }

        private ApplicationParameters GetEffectiveApplicationParameters(
            Organization tenant,
            Application application,
            ApplicationParameters originalApplicationParameters)
        {
            bool isCiam = (tenant.TenantType == "CIAM");
            bool isB2C = (tenant.TenantType == "AAD B2C");
            var effectiveApplicationParameters = new ApplicationParameters
            {
                ApplicationDisplayName = application.DisplayName,
                ClientId = application.AppId,
                EffectiveClientId = application.AppId,
                IsAAD = !isB2C,
                IsB2C = isB2C,
                IsCiam = isCiam,
                HasAuthentication = true,
                IsWebApi = originalApplicationParameters.IsWebApi.GetValueOrDefault()
                || application.Api?.Oauth2PermissionScopes?.Any() is true
                || application.AppRoles?.Any() is true,
                TenantId = tenant.Id,
                Domain = tenant.VerifiedDomains.FirstOrDefault(v => v.IsDefault.GetValueOrDefault())?.Name,
                CallsMicrosoftGraph = application.RequiredResourceAccess.Any(r => r.ResourceAppId == MicrosoftGraphAppId) && !isB2C,
                CallsDownstreamApi = originalApplicationParameters.CallsDownstreamApi || application.RequiredResourceAccess.Any(r => r.ResourceAppId != MicrosoftGraphAppId),
                LogoutUrl = application.Web?.LogoutUrl,
                GraphEntityId = application.Id,

                // Parameters that cannot be infered from the registered app
                IsWebApp = originalApplicationParameters.IsWebApp,
                IsBlazorWasm = originalApplicationParameters.IsBlazorWasm,
                SusiPolicy = originalApplicationParameters.SusiPolicy,
                SecretsId = originalApplicationParameters.SecretsId,
                TargetFramework = originalApplicationParameters.TargetFramework,
                MsalAuthenticationOptions = originalApplicationParameters.MsalAuthenticationOptions,
                CalledApiScopes = originalApplicationParameters.CalledApiScopes,
                AppIdUri = originalApplicationParameters.AppIdUri
            };

            if (application.Api != null && application.IdentifierUris.Any())
            {
                effectiveApplicationParameters.AppIdUri = application.IdentifierUris.FirstOrDefault();
            }

            // Todo: might be a bit more complex in some cases for the B2C case.
            // TODO: handle b2c custom domains & domains ending in b2c.login.*
            // TODO: introduce the Instance?
            effectiveApplicationParameters.Authority = isB2C
                 ? $"https://{effectiveApplicationParameters.Domain1}.b2clogin.com/{effectiveApplicationParameters.Domain}/{effectiveApplicationParameters.SusiPolicy}/"
                 : isCiam ? $"https://{effectiveApplicationParameters.Domain1}.ciamlogin.com/{effectiveApplicationParameters.Domain}"
                 : $"https://login.microsoftonline.com/{effectiveApplicationParameters.TenantId ?? effectiveApplicationParameters.Domain}/";
            effectiveApplicationParameters.Instance = isB2C
                ? $"https://{effectiveApplicationParameters.Domain1}.b2clogin.com/"
                : isCiam ? $"https://{effectiveApplicationParameters.Domain1}.ciamlogin.com/"
                : originalApplicationParameters.Instance;

            effectiveApplicationParameters.PasswordCredentials.AddRange(application.PasswordCredentials.Select(p => p.Hint + "******************"));

            if (application.Spa != null && application.Spa.RedirectUris != null)
            {
                effectiveApplicationParameters.WebRedirectUris.AddRange(application.Spa.RedirectUris);
            }
            else if (application.Web != null && application.Web.RedirectUris != null)
            {
                effectiveApplicationParameters.WebRedirectUris.AddRange(application.Web.RedirectUris);
            }

            effectiveApplicationParameters.SignInAudience = MicrosoftIdentityPlatformAppAudienceToAppParameterAudience(effectiveApplicationParameters.SignInAudience!);
            return effectiveApplicationParameters;
        }
    }
}
