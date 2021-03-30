using Azure.Core;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.MsIdentity.DeveloperCredentials
{
    internal class AzureManagementAuthenticationProvider : IAzureManagementAuthenticationProvider
    {
        readonly TokenCredential _tokenCredentials;
        readonly string[] _initialScopes;
        private const string AzureManagementAPIDefault = "https://management.azure.com/.default";
        private const string AzureManagementTenantsAPI = "https://management.azure.com/tenants?api-version=2020-01-01";

        public AzureManagementAuthenticationProvider(TokenCredential tokenCredentials)
        {
            _tokenCredentials = tokenCredentials;
            _initialScopes = new string[] { AzureManagementAPIDefault };
        }

        private async Task<HttpRequestMessage> AuthenticateRequestAsync(HttpRequestMessage request)
        {
            HttpRequestMessage authenticatedRequest = request;
            TokenRequestContext context = new TokenRequestContext(_initialScopes);
            AccessToken token = await _tokenCredentials.GetTokenAsync(context, CancellationToken.None);
            authenticatedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            return authenticatedRequest;
        }

        public async Task<string> ListTenantsAsync()
        {
            string content = string.Empty;
            var httpRequest = new HttpRequestMessage()
            {
                RequestUri = new Uri(AzureManagementTenantsAPI),
                Method = HttpMethod.Get
            };

            httpRequest = await AuthenticateRequestAsync(httpRequest);
            using (var client = new HttpClient())
            {
                var task = await client.SendAsync(httpRequest)
                    .ContinueWith(async (taskWithMssg) =>
                    {
                        var response = taskWithMssg.Result;
                        response.EnsureSuccessStatusCode();
                        if (response.IsSuccessStatusCode)
                        {
                            content =  await response.Content.ReadAsStringAsync();
                        }
                    });
                await task;
            }
            return content;
        }
    }
}
