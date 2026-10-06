// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias MicrosoftIdentityWeb;

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
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
    [Theory]
    [InlineData("net10.0")]
    [InlineData("net11.0")]
    public async Task GeneratedLogoutForm_ClearsCookieAndSignsOutOpenIdConnect(string targetFramework)
    {
        var generatedForm = GetGeneratedFormContract(targetFramework);
        await using var app = await CreateAppAsync(generatedForm);
        using var client = app.GetTestClient();

        var signInResponse = await client.PostAsync("/test/signin", content: null);
        var authCookie = GetCookie(signInResponse, CookieAuthenticationDefaults.CookiePrefix);
        var formResponse = await SendAsync(client, HttpMethod.Get, "/test/logout-form", authCookie);
        var formHtml = await formResponse.Content.ReadAsStringAsync();
        var antiforgeryCookie = GetCookie(formResponse, ".AspNetCore.Antiforgery.");
        var action = GetHtmlAttribute(formHtml, "form", "action");
        var requestToken = GetInputValue(formHtml, "__RequestVerificationToken");
        var returnUrl = GetInputValue(formHtml, "ReturnUrl");

        using var logoutContent = new FormUrlEncodedContent(
        [
            new("__RequestVerificationToken", requestToken),
            new("ReturnUrl", returnUrl)
        ]);
        var logoutResponse = await SendAsync(
            client,
            HttpMethod.Post,
            action,
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

    private static async Task<WebApplication> CreateAppAsync(GeneratedFormContract? generatedForm = null)
    {
        generatedForm ??= new("/authentication/logout", "ReturnUrl");
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
        app.MapGet("/test/logout-form", async (HttpContext context, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync($$"""
                <form action="{{generatedForm.Action}}" method="post">
                    <input type="hidden" name="{{tokens.FormFieldName}}" value="{{HtmlEncoder.Default.Encode(tokens.RequestToken!)}}" />
                    <input type="hidden" name="{{generatedForm.ReturnUrlFieldName}}" value="/" />
                    <button type="submit">Logout</button>
                </form>
                """);
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

    private static string GetHtmlAttribute(string html, string element, string attribute)
    {
        var match = Regex.Match(
            html,
            $"<{element}\\b[^>]*\\b{attribute}=\"([^\"]+)\"",
            RegexOptions.IgnoreCase);
        Assert.True(match.Success, $"Could not find the '{attribute}' attribute on the '{element}' element.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static string GetInputValue(string html, string name)
    {
        var match = Regex.Match(
            html,
            $"""<input\b(?=[^>]*\bname="{Regex.Escape(name)}")(?=[^>]*\bvalue="([^"]*)")[^>]*>""",
            RegexOptions.IgnoreCase);
        Assert.True(match.Success, $"Could not find the '{name}' form field.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static GeneratedFormContract GetGeneratedFormContract(string targetFramework)
    {
        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var templatePath = Path.GetFullPath(Path.Combine(
            assemblyDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "dotnet-scaffolding",
            "dotnet-scaffold",
            "AspNet",
            "Templates",
            targetFramework,
            "BlazorEntraId",
            "LoginOrLogout.tt"));
        var template = File.ReadAllText(templatePath);

        var action = GetHtmlAttribute(template, "form", "action");
        Assert.Contains("<AntiforgeryToken />", template);
        var returnUrlFieldName = Regex.Match(
            template,
            """<input\b(?=[^>]*\btype="hidden")(?=[^>]*\bname="([^"]+)")(?=[^>]*\bvalue="@currentUrl")[^>]*>""",
            RegexOptions.IgnoreCase);
        Assert.True(returnUrlFieldName.Success, "Could not find the generated return URL field.");

        return new($"/{action.TrimStart('/')}", returnUrlFieldName.Groups[1].Value);
    }

    private sealed record GeneratedFormContract(string Action, string ReturnUrlFieldName);

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
