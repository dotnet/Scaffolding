// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        const string MicrosoftGraphAppId = "00000003-0000-0000-c000-000000000000";
        const string ScopeType = "Scope";

        private const string DefaultCallbackPath = "signin-oidc";
        private const string BlazorWasmCallbackPath = "authentication/login-callback";

        GraphServiceClient? _graphServiceClient;

        internal async Task<ApplicationParameters?> CreateNewAppAsync(
            TokenCredential tokenCredential,
            ApplicationParameters applicationParameters,
            IConsoleLogger consoleLogger,
            string commandName)
        {
            var graphServiceClient = GetGraphServiceClient(tokenCredential);

            // Get the tenant
            Organization? tenant = await GetTenant(graphServiceClient);
            if (tenant != null && tenant.TenantType.Equals("AAD B2C", StringComparison.OrdinalIgnoreCase))
            {
                applicationParameters.IsB2C = true;
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
                // In .NET Core 3.1, Blazor uses MSAL.js 1.x (web redirect URIs)
                // whereas in .NET 5.0 and .NET 6.0, Blazor uses MSAL.js 2.x (SPA redirect URIs)
                switch (applicationParameters.TargetFramework)
                {
                    case "net5.0":
                    case "net6.0":
                        AddSpaPlatform(application, applicationParameters.WebRedirectUris);
                        break;
                    default:
                        AddWebAppPlatform(application, applicationParameters, withImplicitFlow: true);
                        break;
                }
            }

            IEnumerable<IGrouping<string, ResourceAndScope>>? scopesPerResource = await AddApiPermissions(
                applicationParameters,
                graphServiceClient,
                application).ConfigureAwait(false);

            Application createdApplication = await graphServiceClient.Applications
                .Request()
                .AddAsync(application);

            // Creates a service principal (needed for B2C)
            ServicePrincipal servicePrincipal = new ServicePrincipal
            {
                AppId = createdApplication.AppId,
            };

            // B2C does not allow user consent, and therefore we need to explicity create
            // a service principal and permission grants. It's also useful for Blazorwasm hosted
            // applications. We create it always.
            var createdServicePrincipal = await graphServiceClient.ServicePrincipals
                .Request()
                .AddAsync(servicePrincipal).ConfigureAwait(false);

            // B2C does not allow user consent, and therefore we need to explicity grant permissions
            if (applicationParameters.IsB2C)
            {
                await AddAdminConsentToApiPermissions(
                    graphServiceClient,
                    createdServicePrincipal,
                    scopesPerResource);
            }

            // For web API, we need to know the appId of the created app to compute the Identifier URI, 
            // and therefore we need to do it after the app is created (updating the app)
            if (applicationParameters.IsWebApi.GetValueOrDefault()
                && createdApplication.Api != null
                && (createdApplication.IdentifierUris == null || !createdApplication.IdentifierUris.Any()))
            {
                await ExposeScopes(graphServiceClient, createdApplication);

                // Blazorwasm hosted: add permission to server web API from client SPA
                if (applicationParameters.IsBlazorWasm)
                {
                    await AddApiPermissionFromBlazorwasmHostedSpaToServerApi(
                        graphServiceClient,
                        createdApplication,
                        createdServicePrincipal,
                        applicationParameters.IsB2C);
                }
            }
            ApplicationParameters? effectiveApplicationParameters = null;
            // Re-reading the app to be sure to have everything.
            createdApplication = (await graphServiceClient.Applications
                .Request()
                .Filter($"appId eq '{createdApplication.AppId}'")
                .GetAsync()).First();

            // log json console message here since we need the Microsoft.Graph.Application
            JsonResponse jsonResponse = new JsonResponse(commandName);
            if (createdApplication != null)
            {
                jsonResponse.State = State.Success;
                jsonResponse.Content = createdApplication;
                effectiveApplicationParameters = GetEffectiveApplicationParameters(tenant!, createdApplication, applicationParameters);

                // Add password credentials
                if (applicationParameters.CallsMicrosoftGraph || applicationParameters.CallsDownstreamApi)
                {
                    await AddPasswordCredentialsAsync(
                        graphServiceClient,
                        createdApplication.Id,
                        effectiveApplicationParameters,
                        consoleLogger);
                }
            }
            else
            {
                jsonResponse.State = State.Fail;
                jsonResponse.Content = Resources.FailedToCreateApp;
                consoleLogger.LogJsonMessage(jsonResponse);
            }

            consoleLogger.LogJsonMessage(jsonResponse);
            return effectiveApplicationParameters;
        }

        private static async Task<Organization?> GetTenant(GraphServiceClient graphServiceClient)
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
                    Console.WriteLine(ex.InnerException.Message);
                }
                else
                {
                    if (ex.Message.Contains("User was not found") || ex.Message.Contains("not found in tenant"))
                    {
                        Console.WriteLine("User was not found.\nUse both --tenant-id <tenant> --username <username@tenant>.\nAnd re-run the tool.");
                    }
                    else
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                Environment.Exit(1);
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
        internal async Task<JsonResponse> UpdateApplication(
            TokenCredential tokenCredential,
            ApplicationParameters? parameters,
            ProvisioningToolOptions toolOptions,
            string commandName)
        {
            if (parameters is null)
            {
                return new JsonResponse(commandName, State.Fail, string.Format(Resources.FailedToUpdateAppNull, nameof(ApplicationParameters)));
            }

            var graphServiceClient = GetGraphServiceClient(tokenCredential);

            var remoteApp = (await graphServiceClient.Applications.Request()
                .Filter($"appId eq '{parameters.ClientId}'").GetAsync()).FirstOrDefault(app => app.AppId.Equals(parameters.ClientId));

            if (remoteApp is null)
            {
                return new JsonResponse(commandName, State.Fail, string.Format(Resources.NotFound, parameters.ClientId));
            }

            var appUpdates = GetApplicationUpdates(remoteApp, toolOptions);
            if (appUpdates != null)
            {
                try
                {
                    // TODO: update other fields, see https://github.com/jmprieur/app-provisonning-tool/issues/10
                    await graphServiceClient.Applications[remoteApp.Id].Request().UpdateAsync(appUpdates).ConfigureAwait(false);
                    return new JsonResponse(commandName, State.Success, string.Format(Resources.SuccessfullyUpdatedApp, remoteApp.DisplayName, remoteApp.AppId));
                }
                catch (ServiceException se)
                {
                    return new JsonResponse(commandName, State.Fail, se.Error?.Message);
                }
            }

            return new JsonResponse(commandName, State.Success, string.Format(Resources.NoUpdateNecessary, remoteApp.DisplayName, remoteApp.AppId));
        }

        /// <summary>
        /// Determines whether redirect URIs or implicit grant settings need updating and makes the appropriate modifications based on project type
        /// </summary>
        /// <param name="existingApplication"></param>
        /// <param name="toolOptions"></param>
        /// <returns>Updated Application if changes were made, otherwise null</returns>
        private Application? GetApplicationUpdates(Application existingApplication, ProvisioningToolOptions toolOptions)
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
            needsUpdate |= UpdateImplicitGrantSettings(updatedApp, toolOptions);

            return needsUpdate ? updatedApp : null;
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
        /// Updates implicit grant settings if necessary
        /// </summary>
        /// <param name="updatedApp"></param>
        /// <param name="toolOptions"></param>
        /// <returns>true if ImplicitGrantSettings require updates, else false</returns>
        private bool UpdateImplicitGrantSettings(Application updatedApp, ProvisioningToolOptions toolOptions)
        {
            bool needsUpdate = false;
            var currentSettings = updatedApp.Web.ImplicitGrantSettings;

            if (toolOptions.IsBlazorWasm) // In the case of Blazor WASM, Access Tokens and Id Tokens must both be true.
            {
                if (currentSettings.EnableAccessTokenIssuance != true || currentSettings.EnableIdTokenIssuance != true)
                {
                    updatedApp.Web.ImplicitGrantSettings.EnableAccessTokenIssuance = true;
                    updatedApp.Web.ImplicitGrantSettings.EnableIdTokenIssuance = true;

                    needsUpdate = true;
                }
            }
            else // Otherwise we make changes only when the tool options differ from the existing settings.
            {
                if (toolOptions.EnableAccessToken.HasValue &&
                    currentSettings.EnableAccessTokenIssuance != toolOptions.EnableAccessToken.Value)
                {
                    updatedApp.Web.ImplicitGrantSettings.EnableAccessTokenIssuance = toolOptions.EnableAccessToken.Value;
                    needsUpdate = true;
                }

                if (toolOptions.EnableIdToken.HasValue &&
                    currentSettings.EnableIdTokenIssuance != toolOptions.EnableIdToken.Value)
                {
                    updatedApp.Web.ImplicitGrantSettings.EnableIdTokenIssuance = toolOptions.EnableIdToken.Value;
                    needsUpdate = true;
                }
            }

            return needsUpdate;
        }

        private async Task AddApiPermissionFromBlazorwasmHostedSpaToServerApi(
            GraphServiceClient graphServiceClient,
            Application createdApplication,
            ServicePrincipal createdServicePrincipal,
            bool isB2C)
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
                .Request()
                .UpdateAsync(applicationToUpdate).ConfigureAwait(false);

            if (isB2C)
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
                    .Request()
                    .AddAsync(oAuth2PermissionGrant).ConfigureAwait(false);
            }
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
        /// <param name="createdApplication"></param>
        /// <returns></returns>
        private static async Task ExposeScopes(GraphServiceClient graphServiceClient, Application createdApplication)
        {
            var updatedApp = new Application
            {
                IdentifierUris = new[] { $"api://{createdApplication.AppId}" },
            };
            var scopes = createdApplication.Api.Oauth2PermissionScopes?.ToList() ?? new List<PermissionScope>();
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

            await graphServiceClient.Applications[createdApplication.Id]
                .Request()
                .UpdateAsync(updatedApp).ConfigureAwait(false);
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
            IEnumerable<IGrouping<string, ResourceAndScope>>? scopesPerResource)
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

                    // TODO: See https://github.com/jmprieur/app-provisonning-tool/issues/9. 
                    // We need to process the case where the developer is not a tenant admin
                    await graphServiceClient.Oauth2PermissionGrants
                        .Request()
                        .AddAsync(oAuth2PermissionGrant);
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
            ApplicationParameters applicationParameters,
            GraphServiceClient graphServiceClient,
            Application application)
        {
            // Case where the app calls a downstream API
            List<RequiredResourceAccess> apiRequests = new List<RequiredResourceAccess>();
            string? calledApiScopes = applicationParameters?.CalledApiScopes;
            IEnumerable<IGrouping<string, ResourceAndScope>>? scopesPerResource = null;
            if (!string.IsNullOrEmpty(calledApiScopes))
            {
                string[] scopes = calledApiScopes.Split(' ', '\t', StringSplitOptions.RemoveEmptyEntries);
                scopesPerResource = scopes.Select(s => (!s.Contains('/'))
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

            // Explicit usage of MicrosoftGraph openid and offline_access in the case of Azure AD B2C.
            if (applicationParameters.IsB2C &&
                (applicationParameters.IsWebApp.GetValueOrDefault() || applicationParameters.IsBlazorWasm))
            {
                applicationParameters.CalledApiScopes ??= string.Empty;
                if (!applicationParameters.CalledApiScopes.Contains("openid"))
                {
                    applicationParameters.CalledApiScopes += " openid";
                }
                if (!applicationParameters.CalledApiScopes.Contains("offline_access"))
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
        private async Task AddPermission(
            GraphServiceClient graphServiceClient,
            List<RequiredResourceAccess> apiRequests,
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
                throw new ArgumentException($"Service principal named {g.Key} not found.", nameof(g));
            }

            // Keep the service principal ID for later
            foreach (ResourceAndScope r in g)
            {
                r.ResourceServicePrincipalId = spWithScopes.Id;
            }

            IEnumerable<string> scopes = g.Select(r => r.Scope.ToLower(CultureInfo.InvariantCulture));
            var permissionScopes = spWithScopes.Oauth2PermissionScopes
                .Where(s => scopes.Contains(s.Value.ToLower(CultureInfo.InvariantCulture)));

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
            apiRequests.Add(requiredResourceAccess);
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

        public async Task<ApplicationParameters?> ReadApplication(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            var graphServiceClient = GetGraphServiceClient(tokenCredential);
            // Get the tenant
            Organization? tenant = await GetTenant(graphServiceClient);
            var application = await GetApplication(tokenCredential, applicationParameters);
            if (application != null)
            {
                ApplicationParameters effectiveApplicationParameters = GetEffectiveApplicationParameters(
                    tenant!,
                    application,
                    applicationParameters);

                return effectiveApplicationParameters;
            }

            return null;
        }

        public async Task<Application?> GetApplication(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            var graphServiceClient = GetGraphServiceClient(tokenCredential);
            var apps = await graphServiceClient.Applications
                .Request()
                .Filter($"appId eq '{applicationParameters.ClientId}'")
                .GetAsync();

            var readApplication = apps.FirstOrDefault();

            if (readApplication == null)
            {
                return null;
            }
            return readApplication;
        }

        private ApplicationParameters GetEffectiveApplicationParameters(
            Organization tenant,
            Application application,
            ApplicationParameters originalApplicationParameters)
        {
            bool isB2C = (tenant.TenantType == "AAD B2C");
            var effectiveApplicationParameters = new ApplicationParameters
            {
                ApplicationDisplayName = application.DisplayName,
                ClientId = application.AppId,
                EffectiveClientId = application.AppId,
                IsAAD = !isB2C,
                IsB2C = isB2C,
                HasAuthentication = true,
                IsWebApi = application.Api?.Oauth2PermissionScopes?.Any() is true || application.AppRoles?.Any() is true,
                TenantId = tenant.Id,
                Domain = tenant.VerifiedDomains.FirstOrDefault(v => v.IsDefault.GetValueOrDefault())?.Name,
                CallsMicrosoftGraph = application.RequiredResourceAccess.Any(r => r.ResourceAppId == MicrosoftGraphAppId) && !isB2C,
                CallsDownstreamApi = application.RequiredResourceAccess.Any(r => r.ResourceAppId != MicrosoftGraphAppId),
                LogoutUrl = application.Web?.LogoutUrl,
                GraphEntityId = application.Id,

                // Parameters that cannot be infered from the registered app
                IsWebApp = originalApplicationParameters.IsWebApp, // TODO
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
                 : $"https://login.microsoftonline.com/{effectiveApplicationParameters.TenantId ?? effectiveApplicationParameters.Domain}/";
            effectiveApplicationParameters.Instance = isB2C
                ? $"https://{effectiveApplicationParameters.Domain1}.b2clogin.com/"
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
