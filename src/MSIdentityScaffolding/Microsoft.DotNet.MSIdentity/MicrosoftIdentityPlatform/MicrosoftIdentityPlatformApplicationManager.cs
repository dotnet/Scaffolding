// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.DotNet.MSIdentity.AuthenticationParameters;
using Microsoft.DotNet.MSIdentity.Shared;
using Microsoft.DotNet.MSIdentity.Tool;
using Microsoft.Graph;

namespace Microsoft.DotNet.MSIdentity.MicrosoftIdentityPlatformApplication
{
    public class MicrosoftIdentityPlatformApplicationManager
    {
        const string MicrosoftGraphAppId = "00000003-0000-0000-c000-000000000000";
        const string ScopeType = "Scope";

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

            if (applicationParameters.IsWebApi == true)
            {
                application.Api = new ApiApplication()
                {
                    RequestedAccessTokenVersion = 2,
                };
            }


            // In .NET Core 3.1, Blazor uses MSAL.js 1.x (web redirect URIs)
            // whereas in .NET 5.0, Blazor uses MSAL.js 2.x (SPA redirect URIs)
            if (applicationParameters.IsBlazorWasm == true && applicationParameters.TargetFramework != "netcoreapp3.1")
            {
                AddSpaPlatform(applicationParameters, application);
            }
            else
            {
                AddWebAppPlatform(applicationParameters, application);
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
            if (applicationParameters.IsWebApi == true
                && createdApplication.Api != null
                && (createdApplication.IdentifierUris == null || !createdApplication.IdentifierUris.Any()))
            {
                await ExposeScopes(graphServiceClient, createdApplication);

                // Blazorwasm hosted: add permission to server web API from client SPA
                if (applicationParameters.IsBlazorWasm == true) // TODO z try this
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

            //log json console message here since we need the Microsoft.Graph.Application
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
                jsonResponse.Content = "Failed to create Azure AD/AD B2C app registration";
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

        internal async Task<JsonResponse> UpdateApplication(
            TokenCredential tokenCredential,
            ApplicationParameters? applicationParameters,
            ProvisioningToolOptions toolOptions,
            string commandName)
        {
            JsonResponse jsonResponse = new JsonResponse(commandName);
            if (applicationParameters is null)
            {
                jsonResponse.Content = $"Failed to update Azure AD app, reconciledApplicationParameters == null";
                jsonResponse.State = State.Fail;
            }
            else
            {
                var graphServiceClient = GetGraphServiceClient(tokenCredential);

                var existingApplication = (await graphServiceClient.Applications
                   .Request()
                   .Filter($"appId eq '{applicationParameters.ClientId}'")
                   .GetAsync()).First();

                bool needsUpdate = false;

                // Update the redirect URIs
                var updatedApp = new Application
                {
                    Web = existingApplication.Web ?? new WebApplication()
                };

                var existingRedirectUris = updatedApp.Web.RedirectUris;
                if (applicationParameters.IsBlazorWasm == true)
                { 
                    updatedApp.Spa = existingApplication.Spa ?? new SpaApplication();
                    existingRedirectUris = updatedApp.Spa.RedirectUris;
                }

                var processedUris = ValidateUris(toolOptions.RedirectUris).Select(uri => UpdateUriPath(uri, applicationParameters.IsBlazorWasm));
                var updatedRedirectUris = existingRedirectUris.Union(processedUris);
                if (updatedRedirectUris.Count() > existingRedirectUris.Count())
                {
                    needsUpdate = true; 
                    if (applicationParameters.IsBlazorWasm == true) {
                        updatedApp.Spa.RedirectUris = updatedRedirectUris;
                    }
                    else
                    {
                        updatedApp.Web.RedirectUris = updatedRedirectUris;
                    }
                }

                if (updatedApp.Web.ImplicitGrantSettings == null)
                {
                    updatedApp.Web.ImplicitGrantSettings = new ImplicitGrantSettings();
                }

                // update implicit grant settings if need be.
                if (applicationParameters.IsBlazorWasm == true)
                {
                    // disable access token and disable id token
                    if (updatedApp.Web.ImplicitGrantSettings.EnableAccessTokenIssuance != false)
                    {
                        needsUpdate = true;
                        updatedApp.Web.ImplicitGrantSettings.EnableAccessTokenIssuance = false;
                    }

                    if (updatedApp.Web.ImplicitGrantSettings.EnableIdTokenIssuance != false)
                    {
                        needsUpdate = true;
                        updatedApp.Web.ImplicitGrantSettings.EnableIdTokenIssuance = false;
                    }
                }
                else
                {
                    if (toolOptions.EnableAccessToken.HasValue &&
                        (toolOptions.EnableAccessToken.Value != updatedApp.Web.ImplicitGrantSettings.EnableAccessTokenIssuance))
                    {
                        needsUpdate = true;
                        updatedApp.Web.ImplicitGrantSettings.EnableAccessTokenIssuance = toolOptions.EnableAccessToken.Value;
                    }

                    if (toolOptions.EnableIdToken.HasValue &&
                        (toolOptions.EnableIdToken.Value != updatedApp.Web.ImplicitGrantSettings.EnableIdTokenIssuance))
                    {
                        needsUpdate = true;
                        updatedApp.Web.ImplicitGrantSettings.EnableIdTokenIssuance = toolOptions.EnableIdToken.Value;
                    }
                }

                if (needsUpdate)
                {
                    try
                    {
                        // TODO: update other fields. 
                        // See https://github.com/jmprieur/app-provisonning-tool/issues/10
                        await graphServiceClient.Applications[existingApplication.Id]
                            .Request()
                            .UpdateAsync(updatedApp).ConfigureAwait(false);
                    }
                    catch (ServiceException se)
                    {
                        jsonResponse.Content = se.Error?.Message;
                        jsonResponse.State = State.Fail;
                        return jsonResponse;
                    }
                }

                jsonResponse.Content = $"Success updating Azure AD app {updatedApp.DisplayName} ({updatedApp.AppId})";
                jsonResponse.State = State.Success;
            }

            return jsonResponse;
        }

        //checks for valid https uris.
        internal static IList<Uri> ValidateUris(IList<string> redirectUris)
        {
            IList<Uri> validUris = new List<Uri>();
            foreach (var uri in redirectUris)
            {
                //either https or http referencing localhost. IsLoopback checks for localhost, loopback and 127.0.0.1
                if (Uri.TryCreate(uri, UriKind.Absolute, out Uri? uriResult) &&
                    (uriResult.Scheme == Uri.UriSchemeHttps || (uriResult.Scheme == Uri.UriSchemeHttp && uriResult.IsLoopback)))
                {
                    validUris.Add(uriResult);
                }
            }

            return validUris;
        }

        // Path for Redirect URIs depends on whether the app registration is for a Blazor project
        private static string UpdateUriPath(Uri uri, bool? isBlazorWasm)
        {
            return new UriBuilder(uri)
            {
                Path = isBlazorWasm == true ? "authentication/login-callback" : "signin-oidc"
            }.Uri.ToString();
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
        /// Adds a SPA redirect URI
        /// </summary>
        /// <param name="applicationParameters"></param>
        /// <param name="application"></param>
        private static void AddSpaPlatform(ApplicationParameters applicationParameters, Application application)
        {
            application.Spa = new SpaApplication();
            application.Spa.RedirectUris = applicationParameters.WebRedirectUris;
        }

        /// <summary>
        /// Adds the Web redirect URIs (and required scopes in the case of B2C web apis)
        /// </summary>
        /// <param name="applicationParameters"></param>
        /// <param name="application"></param>
        /// <param name="withImplicitFlow">Should it add the implicit flow access token (for Blazor in netcore3.1)</param>
        private static void AddWebAppPlatform(ApplicationParameters applicationParameters, Application application, bool withImplicitFlow = false)
        {
            application.Web = new WebApplication();

            // IdToken
            if ((!applicationParameters.CallsDownstreamApi && !applicationParameters.CallsMicrosoftGraph)
                || withImplicitFlow)
            {
                application.Web.ImplicitGrantSettings = new ImplicitGrantSettings();
                application.Web.ImplicitGrantSettings.EnableIdTokenIssuance = true;
                if (applicationParameters.IsB2C || withImplicitFlow)
                {
                    application.Web.ImplicitGrantSettings.EnableAccessTokenIssuance = true;
                }
            }

            // Redirect URIs
            application.Web.RedirectUris = applicationParameters.WebRedirectUris;

            // Logout URI
            application.Web.LogoutUrl = applicationParameters.LogoutUrl;

            // Explicit usage of MicrosoftGraph openid and offline_access, in the case of Azure AD B2C.
            if (applicationParameters.IsB2C && applicationParameters.IsWebApp == true || applicationParameters.IsBlazorWasm == true)
            {
                if (applicationParameters.CalledApiScopes == null)
                {
                    applicationParameters.CalledApiScopes = string.Empty;
                }
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
            Application appRegistration,
            ApplicationParameters originalParameters)
        {
            bool isB2C = (tenant.TenantType.Equals("AAD B2C"));
            var effectiveApplicationParameters = new ApplicationParameters
            {
                ApplicationDisplayName = appRegistration.DisplayName,
                ClientId = appRegistration.AppId,
                EffectiveClientId = appRegistration.AppId,
                IsAAD = !isB2C,
                IsB2C = isB2C,
                HasAuthentication = true,
                IsWebApi = appRegistration.Api?.Oauth2PermissionScopes?.Any() == true || appRegistration.AppRoles?.Any()  == true,
                IsWebApp = appRegistration.Web != null,
                TenantId = tenant.Id,
                Domain = tenant.VerifiedDomains.FirstOrDefault(v => v.IsDefault == true)?.Name,
                CallsMicrosoftGraph = appRegistration.RequiredResourceAccess.Any(r => r.ResourceAppId == MicrosoftGraphAppId) && !isB2C,
                CallsDownstreamApi = appRegistration.RequiredResourceAccess.Any(r => r.ResourceAppId != MicrosoftGraphAppId),
                LogoutUrl = appRegistration.Web?.LogoutUrl,
                GraphEntityId = appRegistration.Id,
                // Parameters that cannot be infered from the registered app
                IsBlazorWasm = originalParameters.IsBlazorWasm,
                SusiPolicy = originalParameters.SusiPolicy,
                SecretsId = originalParameters.SecretsId,
                TargetFramework = originalParameters.TargetFramework,
                MsalAuthenticationOptions = originalParameters.MsalAuthenticationOptions,
                CalledApiScopes = originalParameters.CalledApiScopes,
                AppIdUri = originalParameters.AppIdUri
            };

            if (appRegistration.Api != null && appRegistration.IdentifierUris.Any())
            {
                effectiveApplicationParameters.AppIdUri = appRegistration.IdentifierUris.FirstOrDefault();
            }

            // Todo: might be a bit more complex in some cases for the B2C case.
            // TODO: handle b2c custom domains & domains ending in b2c.login.*
            // TODO: introduce the Instance?
            effectiveApplicationParameters.Authority = isB2C
                 ? $"https://{effectiveApplicationParameters.Domain1}.b2clogin.com/{effectiveApplicationParameters.Domain}/{effectiveApplicationParameters.SusiPolicy}/"
                 : $"https://login.microsoftonline.com/{effectiveApplicationParameters.TenantId ?? effectiveApplicationParameters.Domain}/";
            effectiveApplicationParameters.Instance = isB2C
                ? $"https://{effectiveApplicationParameters.Domain1}.b2clogin.com/"
                : originalParameters.Instance;

            effectiveApplicationParameters.PasswordCredentials.AddRange(appRegistration.PasswordCredentials.Select(p => p.Hint + "******************"));

            if (appRegistration.Spa != null && appRegistration.Spa.RedirectUris != null)
            {
                effectiveApplicationParameters.WebRedirectUris.AddRange(appRegistration.Spa.RedirectUris);
            }
            else if (appRegistration.Web != null && appRegistration.Web.RedirectUris != null)
            {
                effectiveApplicationParameters.WebRedirectUris.AddRange(appRegistration.Web.RedirectUris);
            }

            effectiveApplicationParameters.SignInAudience = MicrosoftIdentityPlatformAppAudienceToAppParameterAudience(effectiveApplicationParameters.SignInAudience!);
            return effectiveApplicationParameters;
        }
    }
}
