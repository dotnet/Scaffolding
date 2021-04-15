// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core;

namespace Microsoft.DotNet.MSIdentity.DeveloperCredentials
{
    public class DeveloperCredentialsReader
    {
        public TokenCredential GetDeveloperCredentials(string? username, string? currentApplicationTenantId)
        {
#if AzureSDK
            * Tried but does not work if another tenant than the home tenant id is specified
                        DefaultAzureCredentialOptions defaultAzureCredentialOptions = new DefaultAzureCredentialOptions()
                        {
                            SharedTokenCacheTenantId = provisioningToolOptions.TenantId ?? currentApplicationTenantId,
                            SharedTokenCacheUsername = provisioningToolOptions.Username,
                        };
                        defaultAzureCredentialOptions.ExcludeManagedIdentityCredential = true;
                        defaultAzureCredentialOptions.ExcludeInteractiveBrowserCredential = true;
                        defaultAzureCredentialOptions.ExcludeAzureCliCredential = true;
                        defaultAzureCredentialOptions.ExcludeEnvironmentCredential = true;



                        DefaultAzureCredential credential = new DefaultAzureCredential(defaultAzureCredentialOptions);
                        return credential;
#endif
            TokenCredential tokenCredential = new MsalTokenCredential(
                currentApplicationTenantId,
                username);
            return tokenCredential;
        }
    }
}
