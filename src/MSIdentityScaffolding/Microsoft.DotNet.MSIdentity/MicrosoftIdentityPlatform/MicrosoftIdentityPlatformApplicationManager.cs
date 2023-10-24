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
using Microsoft.Graph.Models;

namespace Microsoft.DotNet.MSIdentity.MicrosoftIdentityPlatform
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
                var graphServiceClient = GetGraphServiceClient(tokenCredential, applicationParameters);

                // Get the tenant
                Organization? tenant = await GetTenant(graphServiceClient, consoleLogger);
                if (tenant != null)
                {
                    applicationParameters.IsB2C = string.Equals(tenant.TenantType, "AAD B2C", StringComparison.OrdinalIgnoreCase);
                    applicationParameters.IsCiam = string.Equals(tenant.TenantType, "CIAM", StringComparison.OrdinalIgnoreCase);
                }

                // Create the app.
                Application application = new Application()
                {
                    DisplayName = applicationParameters.ApplicationDisplayName,
                    SignInAudience = applicationParameters.IsCiam ? "AzureADMyOrg" : AppParameterAudienceToMicrosoftIdentityPlatformAppAudience(applicationParameters.SignInAudience!),
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

                IEnumerable<IGrouping<string, ResourceAndScope>>? scopesPerResource = await AddApiPermissions(
                    applicationParameters.CalledApiScopes,
                    graphServiceClient,
                    application);

                var createdApplication = await graphServiceClient.Applications
                     .PostAsync(application);

                if (createdApplication is null)
                {
                    consoleLogger.LogFailureAndExit(Resources.FailedToCreateApp);
                }

                await AddCurrentUserAsOwner(graphServiceClient, createdApplication);

                // Create service principal, necessary for Web API applications
                // and useful for Blazorwasm hosted applications. We create it always.
                var createdSp = await GetOrCreateSP(graphServiceClient, createdApplication!.AppId, consoleLogger);

                // B2C and CIAM don't allow user consent, and therefore we need to explicitly grant permissions
                if (applicationParameters.IsB2C || applicationParameters.IsCiam)
                {
                    await AddAdminConsentToApiPermissions(
                        graphServiceClient,
                        createdSp,
                        scopesPerResource);
                }

                // For web API, we need to know the appId of the created app to compute the Identifier URI,
                // and therefore we need to do it after the app is created (updating the app)
                if (applicationParameters.IsWebApi.GetValueOrDefault() && createdApplication.Api != null)
                {
                    await ExposeWebApiScopes(graphServiceClient, createdApplication, applicationParameters);

                    // Blazorwasm hosted: add permission to server web API from client SPA
                    if (applicationParameters.IsBlazorWasm)
                    {
                        await AddApiPermissionFromBlazorwasmHostedSpaToServerApi(
                            graphServiceClient,
                            createdApplication,
                            createdSp,
                            applicationParameters.IsB2C || applicationParameters.IsCiam);
                    }
                }
                // Re-reading the app to be sure to have everything.
                createdApplication = (await graphServiceClient.Applications
                    .GetAsync(options => options.QueryParameters.Filter = $"appId eq '{createdApplication.AppId}'"))?.Value?.FirstOrDefault();

                // log json console message inside this method since we need the Microsoft.Graph.Application
                if (createdApplication is null)
                {
                    consoleLogger.LogFailureAndExit(Resources.FailedToCreateApp);
                    return null;
                }

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
        /// Attempt to add current user as owner of app registration, exception indicates the owner already exists
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <param name="createdApplication"></param>
        /// <returns></returns>
        internal static async Task AddCurrentUserAsOwner(GraphServiceClient graphServiceClient, Application? createdApplication)
        {
            try
            {
                // Add the current user as a owner.
                User? me = await graphServiceClient.Me.GetAsync();
                var requestBody = new ReferenceCreate
                {
                    OdataId = $"https://graph.microsoft.com/beta/directoryObjects/{me?.Id}",
                };

                await graphServiceClient.Applications[createdApplication!.Id].Owners.Ref.PostAsync(requestBody);
            }
            catch (Graph.Models.ODataErrors.ODataError)
            {
                // Owner already exists in app registration
            }
        }

        internal static async Task<Organization?> GetTenant(GraphServiceClient graphServiceClient, IConsoleLogger consoleLogger)
        {
            Organization? tenant = null;
            try
            {
                tenant = (await graphServiceClient.Organization
                     .GetAsync())?.Value?.FirstOrDefault();
            }
            catch (Exception ex)
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
        /// In the Blazorwasm hosted scenario, we add permission to the server web API from client SPA
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <param name="createdApplication"></param>
        /// <param name="createdServicePrincipal"></param>
        /// <param name="isB2cOrCiam"></param>
        /// <returns></returns>
        internal async Task AddApiPermissionFromBlazorwasmHostedSpaToServerApi(
           GraphServiceClient graphServiceClient,
           Application createdApplication,
           ServicePrincipal createdServicePrincipal,
           bool isB2cOrCiam)
        {
            var requiredResourceAccess = new List<RequiredResourceAccess>();
            var resourcesAccessAndScopes = new List<ResourceAndScope>
            {
                new ResourceAndScope($"api://{createdApplication.AppId}", "access_as_user")
                {
                        ResourceServicePrincipalId = createdServicePrincipal.Id
                }
            };

            await AddPermission(
                graphServiceClient,
                requiredResourceAccess,
                resourcesAccessAndScopes.GroupBy(r => r.Resource).First()).ConfigureAwait(false);

            Application applicationToUpdate = new Application
            {
                RequiredResourceAccess = requiredResourceAccess
            };

            await graphServiceClient.Applications[createdApplication.Id]
                .PatchAsync(applicationToUpdate).ConfigureAwait(false);

            if (isB2cOrCiam)
            {
                var oAuth2PermissionGrant = new OAuth2PermissionGrant
                {
                    ClientId = createdServicePrincipal.Id,
                    ConsentType = "AllPrincipals",
                    PrincipalId = null,
                    ResourceId = createdServicePrincipal.Id,
                    Scope = "access_as_user"
                };

                await graphServiceClient.Oauth2PermissionGrants
                    .PostAsync(oAuth2PermissionGrant).ConfigureAwait(false);
            }
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
            ApplicationParameters parameters,
            ProvisioningToolOptions toolOptions,
            IConsoleLogger consoleLogger,
            StringBuilder? output = null)
        {
            var graphServiceClient = GetGraphServiceClient(tokenCredential, parameters);

            var remoteApp = (await graphServiceClient.Applications
               .GetAsync(options => options.QueryParameters.Filter = $"appId eq '{parameters.ClientId}'"))?.Value?.FirstOrDefault();

            if (remoteApp is null)
            {
                consoleLogger.LogFailureAndExit(string.Format(Resources.NotFound, parameters.ClientId));
                return;
            }

            (bool needsUpdates, Application appUpdates) = GetApplicationUpdates(remoteApp, toolOptions, parameters);
            output ??= new StringBuilder();
            // authorizing downstream API
            if (!string.IsNullOrEmpty(toolOptions.ApiScopes))
            {
                IEnumerable<IGrouping<string, ResourceAndScope>>? scopesPerResource = await AddApiPermissions(
                    toolOptions.ApiScopes,
                    graphServiceClient,
                    remoteApp);

                ServicePrincipal? servicePrincipal = await GetOrCreateSP(graphServiceClient, parameters.ClientId, consoleLogger);
                await AddAdminConsentToApiPermissions(
                    graphServiceClient,
                    servicePrincipal,
                    scopesPerResource);

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
                var updatedApp = await graphServiceClient.Applications[remoteApp.Id].
                    PatchAsync(appUpdates).ConfigureAwait(false);
                output.Append(string.Format(Resources.SuccessfullyUpdatedApp, remoteApp.DisplayName, remoteApp.AppId));
                consoleLogger.LogJsonMessage(State.Success, output: output.ToString());
            }
            catch (Exception e)
            {
                output.Append(e.Message);
                consoleLogger.LogFailureAndExit(output.ToString());
            }
        }

        internal static async Task<ServicePrincipal> GetOrCreateSP(GraphServiceClient graphServiceClient, string? clientId, IConsoleLogger consoleLogger)
        {
            ServicePrincipal? servicePrincipal = (await graphServiceClient.ServicePrincipals
                .GetAsync(options => options.QueryParameters.Filter = $"appId eq '{clientId}'"))?.Value?.FirstOrDefault();

            if (servicePrincipal is null)
            {
                // Create service principal, necessary for Web API applications
                // and useful for Blazorwasm hosted applications. We create it always.
                var sp = new ServicePrincipal
                {
                    AppId = clientId,
                };

                servicePrincipal = await graphServiceClient.ServicePrincipals
                    .PostAsync(sp).ConfigureAwait(false);
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
                && app.DelegatedPermissionIds != null
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

            updatedApp.Api.PreAuthorizedApplications = updatedApp.Api.PreAuthorizedApplications?.Append(preAuthorizedApp).ToList()
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
            var existingWebUris = updatedApp.Web!.RedirectUris ?? new List<string>();
            var existingSpaUris = updatedApp.Spa!.RedirectUris ?? new List<string>();

            // Collect all remote redirect URIs (Web and SPA)
            // If the project type changed, we still may want the Redirect URIs associated with the old project type
            var allRemoteUris = existingWebUris.Union(existingSpaUris).Distinct();

            // Validate local URIs
            var validatedLocalUris = toolOptions.RedirectUris.Where(uri => IsValidUri(uri));

            // Merge all redirect URIs
            var allRedirectUris = allRemoteUris.Union(validatedLocalUris);

            // Update callback paths based on the project type
            var processedRedirectUris = allRedirectUris.Select(uri => UpdateCallbackPath(uri, toolOptions.IsBlazorWasm)).Distinct().ToList();

            // If there are any differences between our processed list and the remote list, update the remote list (Web or SPA)
            if (toolOptions.IsBlazorWasm && processedRedirectUris.Except(existingSpaUris).Any())
            {
                updatedApp.Spa.RedirectUris = processedRedirectUris;
                return true;
            }
            else if (processedRedirectUris.Except(existingWebUris).Any())
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
            var currentSettings = app.Web?.ImplicitGrantSettings ?? new ImplicitGrantSettings();
            app.Web!.ImplicitGrantSettings ??= new ImplicitGrantSettings();

            // In the case of Blazor WASM and B2C, Access Tokens and Id Tokens must both be true.
            if ((toolOptions.IsBlazorWasm || isB2C || toolOptions.CallsDownstreamApi)
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
            string? applicationId,
            ApplicationParameters effectiveApplicationParameters,
            IConsoleLogger consoleLogger)
        {
            string password = string.Empty;
            var requestBody = new Microsoft.Graph.Applications.Item.AddPassword.AddPasswordPostRequestBody
            {
                PasswordCredential = new PasswordCredential
                {
                    DisplayName = "Password created by the provisioning tool"
                },
            };

            if (!string.IsNullOrEmpty(applicationId) && graphServiceClient != null)
            {
                try
                {
                    PasswordCredential? returnedPasswordCredential = await graphServiceClient.Applications[$"{applicationId}"]
                        .AddPassword.PostAsync(requestBody);
                    password = returnedPasswordCredential?.SecretText ?? string.Empty;
                    effectiveApplicationParameters.PasswordCredentials.Add(password);
                }
                catch (Exception e)
                {
                    string? errorMessage = (e is Microsoft.Graph.Models.ODataErrors.ODataError dataError) ? dataError.Error?.Message ?? dataError.Message : e.Message;
                    consoleLogger.LogMessage($"Failed to create password : {errorMessage}", LogMessageType.Error);
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
        internal static async Task ExposeScopes(GraphServiceClient graphServiceClient, string scopeIdentifier, string? graphEntityId, List<PermissionScope>? scopes = null)
        {
            var updatedApp = new Application
            {
                IdentifierUris = new List<string>(new[] { scopeIdentifier }),
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
                Value = "access_as_user",
            };

            scopes.Add(newScope);
            updatedApp.Api = new ApiApplication { Oauth2PermissionScopes = scopes };
            await graphServiceClient.Applications[graphEntityId]
                .PatchAsync(updatedApp);
        }

        /// <summary>
        /// Expose scopes for the B2C API.
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <param name="createdApplication"></param>
        /// <returns></returns>
        internal static async Task ExposeWebApiScopes(GraphServiceClient graphServiceClient, Application createdApplication, ApplicationParameters applicationParameters)
        {
            var scopes = createdApplication.Api?.Oauth2PermissionScopes?.ToList() ?? new List<PermissionScope>();
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
                        Scope = string.Join(" ", resourceAndScopes.Select(r => r.Scope)),
                    };

                    // Check if permissions already exist, otherwise will throw exception
                    try
                    {
                        // TODO: See https://github.com/jmprieur/app-provisonning-tool/issues/9.
                        // We need to process the case where the developer is not a tenant admin
                        var effectivePermissionGrant = await graphServiceClient.Oauth2PermissionGrants
                            .PostAsync(oAuth2PermissionGrant);
                    }
                    catch (Exception ex)
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
        private async Task<IEnumerable<IGrouping<string, ResourceAndScope>>?> AddApiPermissions(
            string? calledApiScopes,
            GraphServiceClient graphServiceClient,
            Application application)
        {
            // Case where the app calls a downstream API
            List<RequiredResourceAccess> apiRequests = new List<RequiredResourceAccess>();
            IEnumerable<IGrouping<string, ResourceAndScope>>? scopesPerResource = null;
            if (!string.IsNullOrEmpty(calledApiScopes))
            {
                string[] scopes = calledApiScopes.Split(' ', '\t', StringSplitOptions.RemoveEmptyEntries);
                scopesPerResource = scopes.Select(s => (!s.Contains('/', StringComparison.OrdinalIgnoreCase))
                // Microsoft Graph shortcut scopes (for instance "User.Read")
                ? new ResourceAndScope("https://graph.microsoft.com", s)
                // Proper AppIdUri/scope
                : new ResourceAndScope(s.Substring(0, s.LastIndexOf('/')), s[(s.LastIndexOf('/') + 1)..])
                ).GroupBy(r => r.Resource)
                .ToArray(); // We want to modify these elements to cache the service principal ID

                foreach (var g in scopesPerResource)
                {
                    await AddPermission(graphServiceClient, apiRequests, g);
                }
            }

            if (apiRequests.Any())
            {
                if (application.RequiredResourceAccess != null)
                {
                    application.RequiredResourceAccess.AddRange(apiRequests);
                }
                else
                {
                    application.RequiredResourceAccess = apiRequests;
                }
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
                RedirectUris = redirectUris.Select(uri => UpdateCallbackPath(uri, isBlazorWasm: true)).ToList()
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
            application.Web.RedirectUris = applicationParameters.WebRedirectUris.Select(uri => UpdateCallbackPath(uri)).ToList();

            // Logout URI
            application.Web.LogoutUrl = applicationParameters.LogoutUrl;

            // Explicit usage of MicrosoftGraph openid and offline_access, in the case of Azure AD B2C.
            if (applicationParameters.IsB2C || applicationParameters.IsBlazorWasm || applicationParameters.IsCiam)
            {
                if (applicationParameters.CalledApiScopes == null)
                {
                    applicationParameters.CalledApiScopes = string.Empty;
                }
                if (!applicationParameters.CalledApiScopes.Contains("openid", StringComparison.OrdinalIgnoreCase))
                {
                    applicationParameters.CalledApiScopes += " openid";
                }
                if (!applicationParameters.CalledApiScopes.Contains("offline_access", StringComparison.OrdinalIgnoreCase))
                {
                    applicationParameters.CalledApiScopes += " offline_access";
                }
                applicationParameters.CalledApiScopes = applicationParameters.CalledApiScopes.Trim();
            }
        }

        /// <summary>
        /// Adds API permissions
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <param name="apiRequests"></param>
        /// <param name="g"></param>
        /// <returns></returns>
        private static async Task AddPermission(
            GraphServiceClient graphServiceClient,
            List<RequiredResourceAccess> apiRequests,
            IGrouping<string, ResourceAndScope> g)
        {
            var spsWithScopes = (await graphServiceClient.ServicePrincipals
                .GetAsync(options => options.QueryParameters.Filter = $"servicePrincipalNames/any(t: t eq '{g.Key}')"))?.Value
                ?? new List<ServicePrincipal>();

            // Special case for B2C where the service principal does not contain the graph URL :(
            if (!spsWithScopes.Any() && g.Key == "https://graph.microsoft.com")
            {
                spsWithScopes = (await graphServiceClient.ServicePrincipals
                               .GetAsync(options => options.QueryParameters.Filter = $"AppId eq '{MicrosoftGraphAppId}'"))?.Value
                               ?? new List<ServicePrincipal>();
            }

            ServicePrincipal? spWithScopes = spsWithScopes.FirstOrDefault();

            if (spWithScopes == null)
            {
                return;
            }

            // Keep the service principal ID for later
            foreach (ResourceAndScope r in g)
            {
                r.ResourceServicePrincipalId = spWithScopes.Id;
            }

            IEnumerable<string> scopes = g.Select(r => r.Scope.ToLower(CultureInfo.InvariantCulture));
            IEnumerable<PermissionScope>? permissionScopes = null;
            IEnumerable<AppRole>? appRoles = null;

            if (!scopes.Contains(".default"))
            {
                permissionScopes = spWithScopes.Oauth2PermissionScopes?.Where(s => scopes.Contains(s.Value?.ToLower(CultureInfo.InvariantCulture)));
                appRoles = spWithScopes.AppRoles?.Where(s => scopes.Contains(s.Value?.ToLower(CultureInfo.InvariantCulture)));
            }

            if (permissionScopes != null | appRoles != null)
            {
                var resourceAccess = new List<ResourceAccess>();
                if (permissionScopes != null)
                {
                    resourceAccess.AddRange(permissionScopes.Select(p =>
                     new ResourceAccess
                     {
                         Id = p.Id,
                         Type = ScopeType
                     }));
                };

                if (appRoles != null)
                {
                    resourceAccess.AddRange(appRoles.Select(p =>
                     new ResourceAccess
                     {
                         Id = p.Id,
                         Type = ScopeType
                     }));
                };

                RequiredResourceAccess requiredResourceAccess = new RequiredResourceAccess
                {
                    ResourceAppId = spWithScopes.AppId,
                    ResourceAccess = resourceAccess
                };

                apiRequests.Add(requiredResourceAccess);
            }
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
            var graphServiceClient = GetGraphServiceClient(tokenCredential, applicationParameters);

            var apps = await graphServiceClient.Applications
                .GetAsync(options => options.QueryParameters.Filter = $"appId eq '{applicationParameters.ClientId}'");

            var readApplication = apps?.Value?.FirstOrDefault();
            if (readApplication != null)
            {
                try
                {
                    await graphServiceClient.Applications[$"{readApplication.Id}"]
                        .DeleteAsync();
                    unregisterSuccess = true;
                }
                catch (Exception)
                {
                    unregisterSuccess = false;
                    throw;
                }
            }

            return unregisterSuccess;
        }

        internal GraphServiceClient GetGraphServiceClient(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            _graphServiceClient ??= applicationParameters.IsGovernmentCloud
                ? new GraphServiceClient(new TokenCredentialAuthenticationProvider(tokenCredential, new string[] { "https://graph.microsoft.us/.default" }), "https://graph.microsoft.us/v1.0")
                : new GraphServiceClient(new TokenCredentialAuthenticationProvider(tokenCredential));

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

            var graphServiceClient = GetGraphServiceClient(tokenCredential, applicationParameters);
            Organization? tenant = await GetTenant(graphServiceClient, consoleLogger);

            var apps = await graphServiceClient.Applications
                .GetAsync(options => options.QueryParameters.Filter = $"appId eq '{applicationParameters.ClientId}'");
            var application = apps?.Value?.FirstOrDefault();
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

        private ApplicationParameters GetEffectiveApplicationParameters(
            Organization tenant,
            Application application,
            ApplicationParameters originalApplicationParameters)
        {
            bool isCiam = string.Equals(tenant.TenantType, "CIAM", StringComparison.OrdinalIgnoreCase);
            bool isB2C = string.Equals(tenant.TenantType, "AAD B2C", StringComparison.OrdinalIgnoreCase);
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
                Domain = tenant.VerifiedDomains?.FirstOrDefault(v => v.IsDefault.GetValueOrDefault())?.Name,
                CallsMicrosoftGraph = !isB2C && application.RequiredResourceAccess?.Any(r => r.ResourceAppId == MicrosoftGraphAppId) is true,
                CallsDownstreamApi = originalApplicationParameters.CallsDownstreamApi || application.RequiredResourceAccess?.Any(r => r.ResourceAppId != MicrosoftGraphAppId) is true,
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
                AppIdUri = originalApplicationParameters.AppIdUri,
                Instance = originalApplicationParameters.Instance,
                IsGovernmentCloud = originalApplicationParameters.IsGovernmentCloud
            };

            if (application.Api != null && application.IdentifierUris != null && application.IdentifierUris.Any())
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

            effectiveApplicationParameters.PasswordCredentials.AddRange(application.PasswordCredentials?.Select(p => p.Hint + "******************") ?? new List<string>());

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
