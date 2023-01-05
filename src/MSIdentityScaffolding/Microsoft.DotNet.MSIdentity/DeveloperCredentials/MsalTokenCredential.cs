// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
            IConsoleLogger consoleLogger)
        {
            _consoleLogger = consoleLogger;
            TenantId = tenantId ?? "organizations"; // MSA-passthrough
            Username = username;
            Instance = "https://login.microsoftonline.com";
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
            AuthenticationResult? result = null;
            var accounts = await app.GetAccountsAsync()!;
            IAccount? account;

            if (!string.IsNullOrEmpty(Username))
            {
                account = accounts.FirstOrDefault(account => account.Username == Username);
            }
            else
            {
                account = accounts.FirstOrDefault();
            }
            try
            {
                result = await app.AcquireTokenSilent(requestContext.Scopes, account)
                    .WithAuthority(Instance, TenantId)
                    .ExecuteAsync(cancellationToken);
            }
            catch (MsalUiRequiredException ex)
            {
                if (account == null && !string.IsNullOrEmpty(Username))
                {
                    _consoleLogger.LogFailureAndExit(
                        $"No valid tokens found in the cache.\n" +
                        $"Please sign-in to Visual Studio with this account: {Username}.\n\n" +
                        $"After signing-in, re-run the tool.");
                }
                result = await app.AcquireTokenInteractive(requestContext.Scopes)
                    .WithAccount(account)
                    .WithClaims(ex.Claims)
                    .WithAuthority(Instance, TenantId)
                    .ExecuteAsync(cancellationToken);
            }
            catch (MsalServiceException ex)
            {
                // AAD error codes: https://learn.microsoft.com/en-us/azure/active-directory/develop/reference-aadsts-error-codes
                if (ex.Message.Contains("AADSTS70002")) // "The client does not exist or is not enabled for consumers"
                {
                    // We want to exit here because this is probably an MSA without an AAD tenant.
                    _consoleLogger.LogFailureAndExit(
                        "An Azure AD tenant, and a user in that tenant, " +
                        "needs to be created for this account before an application can be created. " +
                        "See https://aka.ms/ms-identity-app/create-a-tenant. ");
                }

                // we want to exit here. Re-sign in will not resolve the issue.
                _consoleLogger.LogFailureAndExit(string.Join(Environment.NewLine, Resources.SignInError, ex.Message));
            }
            catch (Exception ex)
            {
                _consoleLogger.LogFailureAndExit(string.Join(Environment.NewLine, Resources.SignInError, ex.Message));
            }

            if (result is null)
            {
                _consoleLogger.LogFailureAndExit(Resources.FailedToAcquireToken);
            }

            return new AccessToken(result!.AccessToken, result.ExpiresOn);
        }
    }
}
