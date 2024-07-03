// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.DotNet.MSIdentity.Properties;
using Microsoft.DotNet.MSIdentity.Shared;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace Microsoft.DotNet.MSIdentity.DeveloperCredentials
{
    public class MsalTokenCredential : TokenCredential
    {
#pragma warning disable S1075 // URIs should not be hardcoded
        private const string RedirectUri = "http://localhost";
#pragma warning restore S1075 // URIs should not be hardcoded

        private readonly IConsoleLogger _consoleLogger;

        public MsalTokenCredential(
            string? tenantId,
            string? username,
            string? instance,
            IConsoleLogger consoleLogger)
        {
            _consoleLogger = consoleLogger;
            TenantId = tenantId ?? "organizations"; // MSA-passthrough
            Username = username;
            Instance = instance ?? "https://login.microsoftonline.com"; // default instance
        }

        private IPublicClientApplication? App { get; set; }
        private string? TenantId { get; set; }
        private string Instance { get; set; }
        private string? Username { get; set; }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return GetTokenAsync(requestContext, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private async Task<IPublicClientApplication> GetOrCreateApp()
        {
            if (App == null)
            {
                // On Windows, USERPROFILE is guaranteed to be set
                string userProfile = Environment.GetEnvironmentVariable("USERPROFILE")!;
                string cacheDir = Path.Combine(userProfile, @"AppData\Local\.IdentityService");

                // TODO: what about the other platforms?
                string clientId = "04b07795-8ddb-461a-bbee-02f9e1bf7b46"; // TODO switch client ID if WAM
                var storageProperties =
                     new StorageCreationPropertiesBuilder(
                         "msal.cache",
                         cacheDir)
                     /*
                     .WithLinuxKeyring(
                         Config.LinuxKeyRingSchema,
                         Config.LinuxKeyRingCollection,
                         Config.LinuxKeyRingLabel,
                         Config.LinuxKeyRingAttr1,
                         Config.LinuxKeyRingAttr2)
                     .WithMacKeyChain(
                         Config.KeyChainServiceName,
                         Config.KeyChainAccountName)
                     */
                     .Build();

                App = PublicClientApplicationBuilder.Create(clientId)
                  .WithAuthority(Instance, TenantId)
                  .WithRedirectUri(RedirectUri)
                  .Build();

                // This hooks up the cross-platform cache into MSAL
                var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties).ConfigureAwait(false);
                cacheHelper.RegisterCache(App.UserTokenCache);
            }
            return App;
        }

        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            var app = await GetOrCreateApp();
            var accounts = await app.GetAccountsAsync()!;
            IAccount? account = string.IsNullOrEmpty(Username)
                ? accounts.FirstOrDefault()
                : accounts.FirstOrDefault(account => string.Equals(account.Username, Username, StringComparison.OrdinalIgnoreCase));

            AuthenticationResult? result = account is null
                ? await GetAuthenticationWithoutAccount(requestContext.Scopes, app, cancellationToken)
                : await GetAuthenticationWithAccount(requestContext.Scopes, app, account, cancellationToken);

            if (result is null || result.AccessToken is null)
            {
                _consoleLogger.LogFailureAndExit(Resources.FailedToAcquireToken);
            }

            // Note: In the future, the token type *could* be POP instead of Bearer
            return new AccessToken(result!.AccessToken!, result.ExpiresOn);
        }

        private async Task<AuthenticationResult?> GetAuthenticationWithAccount(string[] scopes, IPublicClientApplication app, IAccount? account, CancellationToken cancellationToken)
        {
            AuthenticationResult? result = null;
            try
            {
                result = await app.AcquireTokenSilent(scopes, account)
                    .WithTenantId(TenantId)
                    .ExecuteAsync(cancellationToken);
            }
            catch (MsalUiRequiredException ex)
            {
                try
                {
                    result = await app.AcquireTokenInteractive(scopes)
                        .WithAccount(account)
                        .WithClaims(ex.Claims)
                        .WithTenantId(TenantId)
                        .WithUseEmbeddedWebView(false)
                        .ExecuteAsync(cancellationToken);
                }
                catch (Exception e)
                {
                    _consoleLogger.LogFailureAndExit(string.Join(Environment.NewLine, Resources.SignInError, e.Message));
                }
            }
            catch (Exception ex)
            {
                // AAD error codes: https://learn.microsoft.com/en-us/azure/active-directory/develop/reference-aadsts-error-codes
                var errorMessage = ex.Message.Contains("AADSTS70002") // "The client does not exist or is not enabled for consumers"
                    ? Resources.ClientDoesNotExist
                    : string.Join(Environment.NewLine, Resources.SignInError, ex.Message);

                // we want to exit here. Re-sign in will not resolve the issue.
                _consoleLogger.LogFailureAndExit(errorMessage);
            }

            return result;
        }

        /// <summary>
        /// If there are no matching accounts in the msal cache, we need to make a call to AcquireTokenInteractive in order to populate the cache.
        /// </summary>
        /// <param name="requestContext"></param>
        /// <param name="app"></param>
        /// <param name="result"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<AuthenticationResult?> GetAuthenticationWithoutAccount(string[] scopes, IPublicClientApplication app, CancellationToken cancellationToken)
        {
            AuthenticationResult? result = null;
            try
            {
                result = await app.AcquireTokenInteractive(scopes)
                   .WithTenantId(TenantId)
                   .WithUseEmbeddedWebView(false)
                   .ExecuteAsync(cancellationToken);
            }
            catch (MsalUiRequiredException ex) // Need to get Claims, hence the nested try/catch
            {
                try
                {
                    result = await app.AcquireTokenInteractive(scopes)
                        .WithClaims(ex.Claims)
                        .WithTenantId(TenantId)
                        .WithUseEmbeddedWebView(false)
                        .ExecuteAsync(cancellationToken);
                }
                catch (Exception e)
                {
                    _consoleLogger.LogFailureAndExit(string.Join(Environment.NewLine, Resources.SignInError, e.Message));
                }
            }
            catch (Exception e)
            {
                _consoleLogger.LogFailureAndExit(string.Join(Environment.NewLine, Resources.SignInError, e.Message));
            }

            return result;
        }
    }
}
