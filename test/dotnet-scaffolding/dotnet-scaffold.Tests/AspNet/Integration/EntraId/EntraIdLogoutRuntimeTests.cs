// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias MicrosoftIdentityWeb;

using System;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicrosoftIdentityWeb::Microsoft.Identity.Web;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration;

[Trait("Suite", "ScaffoldIntegration")]
[Trait("Family", "entra-id")]
public class EntraIdLogoutRuntimeTests
{
    [Fact]
    public async Task Logout_WithAntiforgeryToken_ClearsCookieAndSignsOutOpenIdConnect()
    {
        await using var app = await CreateAppAsync();
        using var client = app.GetTestClient();

        var signInResponse = await client.PostAsync("/test/signin", content: null);
        var authCookie = GetCookie(signInResponse, CookieAuthenticationDefaults.CookiePrefix);
        var tokenResponse = await SendAsync(client, HttpMethod.Get, "/test/antiforgery", authCookie);
        var antiforgeryCookie = GetCookie(tokenResponse, ".AspNetCore.Antiforgery.");
        var requestToken = await tokenResponse.Content.ReadAsStringAsync();

        using var logoutContent = new FormUrlEncodedContent(
        [
            new("__RequestVerificationToken", requestToken),
            new("ReturnUrl", "/")
        ]);
        var logoutResponse = await SendAsync(
            client,
            HttpMethod.Post,
            "/authentication/logout",
            $"{authCookie}; {antiforgeryCookie}",
            logoutContent);

        Assert.Equal(HttpStatusCode.Redirect, logoutResponse.StatusCode);
        Assert.Contains(
            logoutResponse.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith(CookieAuthenticationDefaults.CookiePrefix, StringComparison.Ordinal) &&
                value.Contains("expires=", StringComparison.OrdinalIgnoreCase));
        Assert.True(app.Services.GetRequiredService<OpenIdConnectSignOutState>().WasSignedOut);
    }

    [Fact]
    public async Task Logout_CrossOriginWithoutAntiforgeryToken_IsRejected()
    {
        await using var app = await CreateAppAsync();
        using var client = app.GetTestClient();
        var signInResponse = await client.PostAsync("/test/signin", content: null);
        var authCookie = GetCookie(signInResponse, CookieAuthenticationDefaults.CookiePrefix);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/authentication/logout")
        {
            Content = new FormUrlEncodedContent([new("ReturnUrl", "/")])
        };
        request.Headers.Add("Origin", "https://attacker.example");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(app.Services.GetRequiredService<OpenIdConnectSignOutState>().WasSignedOut);
    }

    private static async Task<WebApplication> CreateAppAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAntiforgery();
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<OpenIdConnectSignOutState>();
        builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, RecordingOpenIdConnectHandler>(
                OpenIdConnectDefaults.AuthenticationScheme,
                _ => { });

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.MapPost("/test/signin", async context =>
        {
            var principal = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.Name, "Test User")], CookieAuthenticationDefaults.AuthenticationScheme));
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        });
        app.MapGet("/test/antiforgery", (HttpContext context, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            return tokens.RequestToken!;
        });
        app.MapGroup("/authentication").MapLoginAndLogout();
        await app.StartAsync();
        return app;
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        string cookie,
        HttpContent? content = null)
    {
        using var request = new HttpRequestMessage(method, path) { Content = content };
        request.Headers.Add("Cookie", cookie);
        return await client.SendAsync(request);
    }

    private static string GetCookie(HttpResponseMessage response, string namePrefix)
    {
        var setCookie = Assert.Single(
            response.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith(namePrefix, StringComparison.Ordinal));
        return setCookie[..setCookie.IndexOf(';')];
    }

    private sealed class OpenIdConnectSignOutState
    {
        public bool WasSignedOut { get; set; }
    }

    private sealed class RecordingOpenIdConnectHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        OpenIdConnectSignOutState state)
        : SignOutAuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            => Task.FromResult(AuthenticateResult.NoResult());

        protected override Task HandleSignOutAsync(AuthenticationProperties? properties)
        {
            state.WasSignedOut = true;
            Context.Response.Redirect(properties?.RedirectUri ?? "/");
            return Task.CompletedTask;
        }
    }
}
