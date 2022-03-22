using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Authentication.WebAssembly.Msal.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;

/// <summary>
/// Adds services and implements methods to use Microsoft Graph SDK.
/// </summary>
internal static class GraphClientExtensions
{
    /// <summary>
    /// Extension method for adding the Microsoft Graph SDK to IServiceCollection.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="scopes">The MS Graph scopes to request</param>
    /// <returns></returns>
    public static IServiceCollection AddMicrosoftGraphClient(this IServiceCollection services, params string[] scopes)
    {
        services.Configure<RemoteAuthenticationOptions<MsalProviderOptions>>(options =>
        {
            foreach (var scope in scopes)
            {
                options.ProviderOptions.AdditionalScopesToConsent.Add(scope);
            }
        });

        services.AddScoped<IAuthenticationProvider, GraphAuthenticationProvider>();
        services.AddScoped<IHttpProvider, HttpClientHttpProvider>(sp => new HttpClientHttpProvider(new HttpClient()));
        services.AddScoped(sp => new GraphServiceClient(
              sp.GetRequiredService<IAuthenticationProvider>(),
              sp.GetRequiredService<IHttpProvider>()));
        return services;
    }

    /// <summary>
    /// Implements IAuthenticationProvider interface.
    /// Tries to get an access token for Microsoft Graph.
    /// </summary>
    private class GraphAuthenticationProvider : IAuthenticationProvider
    {
        public GraphAuthenticationProvider(IAccessTokenProvider provider)
        {
            Provider = provider;
        }

        public IAccessTokenProvider Provider { get; }

        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            var result = await Provider.RequestAccessToken(new AccessTokenRequestOptions()
            {
                Scopes = new[] { "https://graph.microsoft.com/User.Read" }
            });

            if (result.TryGetToken(out var token))
            {
                request.Headers.Authorization ??= new AuthenticationHeaderValue("Bearer", token.Value);
            }
        }
    }

    private class HttpClientHttpProvider : IHttpProvider
    {
        private readonly HttpClient _client;

        public HttpClientHttpProvider(HttpClient client)
        {
            _client = client;
        }

        public ISerializer Serializer { get; } = new Serializer();

        public TimeSpan OverallTimeout { get; set; } = TimeSpan.FromSeconds(300);

        public void Dispose()
        {
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            return _client.SendAsync(request);
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            return _client.SendAsync(request, completionOption, cancellationToken);
        }
    }
}
