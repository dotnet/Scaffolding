// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Microsoft.DotNet.MSIdentity.MicrosoftIdentityPlatform
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

        public async Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
        {
            // Try with the Shared token cache credentials
            TokenRequestContext context = new TokenRequestContext(_initialScopes.ToArray());
            AccessToken token = await _tokenCredentials.GetTokenAsync(context, cancellationToken);

            request.Headers.Add("Authorization", $"Bearer {token.Token}");
        }
    }
}
