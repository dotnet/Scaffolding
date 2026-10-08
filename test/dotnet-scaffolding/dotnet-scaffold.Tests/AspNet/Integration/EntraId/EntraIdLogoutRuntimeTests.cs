// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration;

[Trait("Suite", "ScaffoldIntegration")]
[Trait("Family", "entra-id")]
public class EntraIdLogoutRuntimeTests
{
    [Theory]
    [InlineData("net10.0")]
    [InlineData("net11.0")]
    public async Task GeneratedApplication_LogoutFormIsCsrfProtectedAndSignsOut(string targetFramework)
    {
        string testDirectory = Path.Combine(Path.GetTempPath(), nameof(EntraIdLogoutRuntimeTests), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDirectory);

        try
        {
            CreateGeneratedApplication(testDirectory, targetFramework);
            var (buildExitCode, buildOutput, buildError) =
                await ScaffoldCliHelper.RunBuildForFrameworkAsync(testDirectory, targetFramework);
            Assert.True(
                buildExitCode == 0,
                $"Generated application should build.\nOutput: {buildOutput}\nError: {buildError}");

            int port = GetAvailablePort();
            using var process = StartGeneratedApplication(testDirectory, targetFramework, port);
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();

            try
            {
                using var client = CreateClient(port);
                await WaitUntilReadyAsync(client, process, outputTask, errorTask);

                var signInResponse = await client.PostAsync("/test/signin", content: null);
                var authCookie = GetCookie(signInResponse, ".AspNetCore.Cookies");
                var formResponse = await SendAsync(client, HttpMethod.Get, "/", authCookie);
                var formHtml = await formResponse.Content.ReadAsStringAsync();
                var antiforgeryCookie = GetCookie(formResponse, ".AspNetCore.Antiforgery.");
                var action = GetHtmlAttribute(formHtml, "form", "action");
                var requestToken = GetInputValue(formHtml, "__RequestVerificationToken");
                var returnUrl = GetInputValue(formHtml, "ReturnUrl");

                using var crossOriginContent = new FormUrlEncodedContent([new("ReturnUrl", returnUrl)]);
                using var crossOriginRequest = new HttpRequestMessage(HttpMethod.Post, action)
                {
                    Content = crossOriginContent
                };
                crossOriginRequest.Headers.Add("Origin", "https://attacker.example");
                crossOriginRequest.Headers.Add("Cookie", authCookie);
                var crossOriginResponse = await client.SendAsync(crossOriginRequest);

                Assert.Equal(HttpStatusCode.BadRequest, crossOriginResponse.StatusCode);
                Assert.Equal("false", await client.GetStringAsync("/test/signout-state"));

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
                    value => value.StartsWith(".AspNetCore.Cookies", StringComparison.Ordinal) &&
                        value.Contains("expires=", StringComparison.OrdinalIgnoreCase));
                Assert.Equal("true", await client.GetStringAsync("/test/signout-state"));
            }
            finally
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }

                await process.WaitForExitAsync();
            }
        }
        finally
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }

    private static void CreateGeneratedApplication(string testDirectory, string targetFramework)
    {
        File.WriteAllText(Path.Combine(testDirectory, "NuGet.config"), ScaffoldCliHelper.PreviewNuGetConfig);
        File.WriteAllText(Path.Combine(testDirectory, "GeneratedEntraApp.csproj"), $$"""
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>{{targetFramework}}</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.Identity.Web" Version="4.10.0" />
              </ItemGroup>
            </Project>
            """);

        string componentsDirectory = Path.Combine(testDirectory, "Components");
        Directory.CreateDirectory(componentsDirectory);
        File.WriteAllText(Path.Combine(componentsDirectory, "_Imports.razor"), """
            @using Microsoft.AspNetCore.Components.Authorization
            @using Microsoft.AspNetCore.Components.Forms
            @using Microsoft.AspNetCore.Components.Routing
            @using Microsoft.AspNetCore.Components.Web
            @using Microsoft.AspNetCore.Components.Web.Infrastructure
            """);
        File.WriteAllText(Path.Combine(componentsDirectory, "App.razor"), """
            <!DOCTYPE html>
            <html>
            <head><title>Generated Entra App</title></head>
            <body>
                <Routes />
            </body>
            </html>
            """);
        File.WriteAllText(Path.Combine(componentsDirectory, "Routes.razor"), """
            <Router AppAssembly="typeof(Program).Assembly">
                <Found Context="routeData">
                    <RouteView RouteData="routeData" />
                </Found>
            </Router>
            """);
        string pagesDirectory = Path.Combine(componentsDirectory, "Pages");
        Directory.CreateDirectory(pagesDirectory);
        File.WriteAllText(Path.Combine(pagesDirectory, "Home.razor"), """
            @page "/"

            <LoginOrLogout />
            """);
        File.WriteAllText(
            Path.Combine(componentsDirectory, "LoginOrLogout.razor"),
            GenerateLoginOrLogout(targetFramework));
        File.WriteAllText(Path.Combine(testDirectory, "Program.cs"), GetProgramContent());
    }

    private static string GenerateLoginOrLogout(string targetFramework)
        => targetFramework switch
        {
            "net10.0" => new Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.BlazorEntraId.LoginOrLogout().TransformText(),
            "net11.0" => new Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net11.BlazorEntraId.LoginOrLogout().TransformText(),
            _ => throw new ArgumentOutOfRangeException(nameof(targetFramework))
        };

    private static string GetProgramContent() => """
        using System.Security.Claims;
        using System.Text.Encodings.Web;
        using GeneratedEntraApp.Components;
        using Microsoft.AspNetCore.Authentication;
        using Microsoft.AspNetCore.Authentication.Cookies;
        using Microsoft.AspNetCore.Authentication.OpenIdConnect;
        using Microsoft.Extensions.Options;
        using Microsoft.Identity.Web;

        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddRazorComponents();
        builder.Services.AddCascadingAuthenticationState();
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
                new ClaimsIdentity(
                    [new Claim(ClaimTypes.Name, "Test User")],
                    CookieAuthenticationDefaults.AuthenticationScheme));
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        });
        app.MapGet("/test/signout-state", (OpenIdConnectSignOutState state) => state.WasSignedOut);
        app.MapGroup("/authentication").MapLoginAndLogout();
        app.MapRazorComponents<App>();
        app.Run();

        public sealed class OpenIdConnectSignOutState
        {
            public bool WasSignedOut { get; set; }
        }

        public sealed class RecordingOpenIdConnectHandler(
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
        """;

    private static Process StartGeneratedApplication(string testDirectory, string targetFramework, int port)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ScaffoldCliHelper.GetDotNetPath(),
                WorkingDirectory = testDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.StartInfo.ArgumentList.Add("run");
        process.StartInfo.ArgumentList.Add("--no-build");
        process.StartInfo.ArgumentList.Add("--framework");
        process.StartInfo.ArgumentList.Add(targetFramework);
        process.StartInfo.ArgumentList.Add("--urls");
        process.StartInfo.ArgumentList.Add($"http://127.0.0.1:{port}");
        process.StartInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        process.Start();
        return process;
    }

    private static HttpClient CreateClient(int port)
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false
        };
        return new HttpClient(handler)
        {
            BaseAddress = new Uri($"http://127.0.0.1:{port}")
        };
    }

    private static async Task WaitUntilReadyAsync(
        HttpClient client,
        Process process,
        Task<string> outputTask,
        Task<string> errorTask)
    {
        string? lastResponse = null;
        for (int attempt = 0; attempt < 60; attempt++)
        {
            if (process.HasExited)
            {
                Assert.Fail(
                    $"Generated application exited before startup.\nOutput: {await outputTask}\nError: {await errorTask}");
            }

            try
            {
                using var response = await client.GetAsync("/");
                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                lastResponse = $"{(int)response.StatusCode} {response.StatusCode}: {await response.Content.ReadAsStringAsync()}";
            }
            catch (HttpRequestException)
            {
            }

            await Task.Delay(500);
        }

        Assert.Fail($"Generated application did not become ready within 30 seconds. Last response: {lastResponse}");
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

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
