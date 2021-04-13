// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core;
using Microsoft.Graph;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.MSIdentity.MicrosoftIdentityPlatformApplication
{
    /// <summary>
    /// Graph SDK authentication provider based on an Azure SDK token credential provider.
    /// </summary>
    internal class TokenCredentialAuthenticationProvider : IAuthenticationProvider
    {
        public TokenCredentialAuthenticationProvider(
            TokenCredential tokenCredentials,
            IEnumerable<string>? initialScopes = null)
        {
            _tokenCredentials = tokenCredentials;
            _initialScopes = initialScopes ?? new string[] { "https://graph.microsoft.com/.default" };
        }

        readonly TokenCredential _tokenCredentials;
        readonly IEnumerable<string> _initialScopes;

        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            // Try with the Shared token cache credentials

            TokenRequestContext context = new TokenRequestContext(_initialScopes.ToArray());
            AccessToken token = await _tokenCredentials.GetTokenAsync(context, CancellationToken.None);

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        }
    }
}
