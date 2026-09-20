// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Identity;

[Trait("Suite", "ScaffoldIntegration")]
[Trait("Family", "identity")]
public class IdentityEndToEndTests
{
    [Theory]
    [InlineData("net10.0", "mvc")]
    [InlineData("net10.0", "webapp")]
    [InlineData("net11.0", "mvc")]
    [InlineData("net11.0", "webapp")]
    public async Task ScaffoldIdentity_ConfiguresCleanProject(string framework, string template)
    {
        using var project = new IdentityTestProject(framework, template);
        await project.CreateAsync();
        await project.ScaffoldAsync();
        AssertConfiguredProject(project);
        await project.BuildAsync();
        await project.AssertUnchangedSecondRunAsync();
        await AssertAccountLifecycleAsync(project);
    }

    [Theory]
    [InlineData("net10.0", "mvc")]
    [InlineData("net10.0", "webapp")]
    [InlineData("net11.0", "mvc")]
    [InlineData("net11.0", "webapp")]
    public async Task ScaffoldIdentity_PreservesDefaultIdentityUi(string framework, string template)
    {
        using var project = new IdentityTestProject(framework, template);
        await project.CreateAsync("Individual");
        var partialPath = Path.Combine(project.Directory, project.HostFolder, "Shared", "_LoginPartial.cshtml");
        var originalPartial = File.ReadAllText(partialPath);

        await project.ScaffoldAsync();
        Assert.Equal(originalPartial, File.ReadAllText(partialPath));
        Assert.False(File.Exists(Path.Combine(project.Directory, "Data", "ApplicationUser.cs")));
        await project.BuildAsync();
        await project.AssertUnchangedSecondRunAsync();
        await AssertAccountLifecycleAsync(project, applyMigration: false);
    }

    [Theory]
    [InlineData("builder.Services\n    .AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)", false)]
    [InlineData("var services = builder.Services;\nservices.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)", false)]
    [InlineData("builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)", false)]
    [InlineData("builder.Services.AddIdentity<CustomUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)", true)]
    public async Task ScaffoldIdentity_CompletesPartialRegistration(string registration, bool customUser)
    {
        using var project = new IdentityTestProject("net10.0", "mvc");
        await project.CreateAsync();
        var programPath = Path.Combine(project.Directory, "Program.cs");
        var program = File.ReadAllText(programPath).Replace(
            "builder.Services.AddControllersWithViews();",
            $"builder.Services.AddControllersWithViews();\n{registration}.AddEntityFrameworkStores<ApplicationDbContext>();",
            StringComparison.Ordinal);
        if (customUser)
        {
            File.WriteAllText(Path.Combine(project.Directory, "CustomUser.cs"), """
using Microsoft.AspNetCore.Identity;
namespace IdentityApp.Data;
public class CustomUser : IdentityUser {}
""");
            program = "using IdentityApp.Data;\n" + program;
        }
        File.WriteAllText(programPath, program);

        await project.ScaffoldAsync();
        if (customUser)
        {
            Assert.False(File.Exists(Path.Combine(project.Directory, "Data", "ApplicationUser.cs")));
        }
        var updated = File.ReadAllText(programPath);
        Assert.Contains("options.SignIn.RequireConfirmedAccount = true", updated);
        Assert.Equal(registration.Contains("AddDefaultIdentity", StringComparison.Ordinal) ? 1 : 0,
            Regex.Matches(updated, @"\.AddDefaultIdentity<").Count);
        await project.BuildAsync();
        await project.AssertUnchangedSecondRunAsync();
        await AssertAccountLifecycleAsync(project);
    }

    [Fact]
    public async Task ScaffoldIdentity_ResolvesRelativeProjectPath()
    {
        using var project = new IdentityTestProject("net10.0", "mvc");
        await project.CreateAsync();
        await project.ScaffoldAsync(relativeProjectPath: true);
        AssertConfiguredProject(project);
        await project.BuildAsync();
        await project.AssertUnchangedSecondRunAsync(relativeProjectPath: true);
    }

    [Fact]
    public async Task ScaffoldIdentity_UsesArtifactsOutput()
    {
        using var project = new IdentityTestProject("net10.0", "mvc");
        await project.CreateAsync();
        File.WriteAllText(Path.Combine(project.Directory, "Directory.Build.props"), """
<Project>
  <PropertyGroup>
    <UseArtifactsOutput>true</UseArtifactsOutput>
    <ArtifactsPath>$(MSBuildThisFileDirectory)artifacts</ArtifactsPath>
  </PropertyGroup>
</Project>
""");

        await project.ScaffoldAsync();
        AssertConfiguredProject(project);
        await project.BuildAsync();
        await project.AssertUnchangedSecondRunAsync();
    }

    private static void AssertConfiguredProject(IdentityTestProject project)
    {
        var program = File.ReadAllText(Path.Combine(project.Directory, "Program.cs"));
        Assert.Contains("AddDatabaseDeveloperPageExceptionFilter", program);
        Assert.Contains("AddRazorPages", program);
        Assert.Contains("UseMigrationsEndPoint", program);
        Assert.Contains("MapRazorPages", program);
        var shared = Path.Combine(project.Directory, project.HostFolder, "Shared");
        Assert.Contains("<partial name=\"_LoginPartial\" />", File.ReadAllText(Path.Combine(shared, "_Layout.cshtml")));
        var partial = File.ReadAllText(Path.Combine(shared, "_LoginPartial.cshtml"));
        Assert.Contains("asp-page=\"/Account/Login\"", partial);
        Assert.Contains("asp-page=\"/Account/Register\"", partial);
        var migrations = Path.Combine(project.Directory, "Data", "Migrations");
        Assert.NotEmpty(System.IO.Directory.GetFiles(migrations, "*_CreateIdentitySchema.cs"));
        Assert.NotEmpty(System.IO.Directory.GetFiles(migrations, "*ModelSnapshot.cs"));
        Assert.Empty(System.IO.Directory.GetFiles(project.Directory, "*.db", SearchOption.AllDirectories));
    }

    private static async Task AssertAccountLifecycleAsync(IdentityTestProject project, bool applyMigration = true)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var baseAddress = new Uri($"http://127.0.0.1:{port}");
        using var process = new Process
        {
            StartInfo = ScaffoldCliHelper.CreateDotNetStartInfo(project.Directory,
                "run", "--no-build", "--no-launch-profile", "--framework", project.Framework,
                "--project", project.Path, "--urls", baseAddress.ToString())
        };
        process.StartInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        process.Start();
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        try
        {
            using var handler = new HttpClientHandler { CookieContainer = new CookieContainer() };
            using var client = new HttpClient(handler) { BaseAddress = baseAddress, Timeout = TimeSpan.FromSeconds(30) };
            var registerPage = await WaitForPageAsync(client, "/Identity/Account/Register", process, output, error);
            var homePage = await client.GetStringAsync("/");
            Assert.Contains("/Identity/Account/Login", homePage);
            Assert.Contains("/Identity/Account/Register", homePage);
            if (applyMigration)
            {
                await ApplyMigrationAsync(client, registerPage);
            }

            registerPage = await client.GetStringAsync("/Identity/Account/Register");
            Assert.NotEmpty(await client.GetByteArrayAsync("/Identity/lib/bootstrap/dist/css/bootstrap.min.css"));
            Assert.NotEmpty(await client.GetByteArrayAsync("/Identity/lib/bootstrap/dist/js/bootstrap.bundle.min.js"));
            var email = $"identity-{Guid.NewGuid():N}@example.com";
            const string password = "Test1234!";
            var registration = await PostFormAsync(client, "/Identity/Account/Register", registerPage, new()
            {
                ["Input.Email"] = email,
                ["Input.Password"] = password,
                ["Input.ConfirmPassword"] = password
            });
            Assert.Contains("Register confirmation", registration, StringComparison.OrdinalIgnoreCase);
            var confirmation = await client.GetStringAsync(GetLink(registration, "ConfirmEmail"));
            Assert.Contains("Thank you for confirming your email", confirmation, StringComparison.OrdinalIgnoreCase);

            var login = await PostFormAsync(client, "/Identity/Account/Login",
                await client.GetStringAsync("/Identity/Account/Login"), new()
                {
                    ["Input.Email"] = email,
                    ["Input.Password"] = password,
                    ["Input.RememberMe"] = "false"
                });
            Assert.Contains($"Hello {email}!", login, StringComparison.OrdinalIgnoreCase);
            var profile = await client.GetStringAsync(GetLink(login, "/Account/Manage"));
            Assert.Contains("<h3>Profile</h3>", profile, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(email, profile, StringComparison.OrdinalIgnoreCase);

            var logout = await PostFormAsync(client, "/Identity/Account/Logout?returnUrl=%2F",
                await client.GetStringAsync("/"), new());
            Assert.Contains(">Login<", logout, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(">Register<", logout, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain($"Hello {email}!", logout, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
            await process.WaitForExitAsync();
            await Task.WhenAll(output, error);
        }
    }

    private static async Task ApplyMigrationAsync(HttpClient client, string registerPage)
    {
        using var failedRegistration = await client.PostAsync("/Identity/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = $"migration-probe-{Guid.NewGuid():N}@example.com",
            ["Input.Password"] = "Test1234!",
            ["Input.ConfirmPassword"] = "Test1234!",
            ["__RequestVerificationToken"] = GetAntiforgeryToken(registerPage)
        }));
        var errorPage = await failedRegistration.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.InternalServerError, failedRegistration.StatusCode);
        var context = MatchHtml(errorPage, "data-assemblyname=\"([^\"]+)\"");
        using var migration = await client.PostAsync("/ApplyDatabaseMigrations",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["context"] = context }));
        Assert.True(migration.StatusCode == HttpStatusCode.NoContent,
            $"Applying the migration returned {(int)migration.StatusCode}.\n{await migration.Content.ReadAsStringAsync()}");
    }

    private static async Task<string> PostFormAsync(HttpClient client, string uri, string page, Dictionary<string, string> fields)
    {
        fields["__RequestVerificationToken"] = GetAntiforgeryToken(page);
        using var response = await client.PostAsync(uri, new FormUrlEncodedContent(fields));
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"POST {uri} returned {(int)response.StatusCode}.\n{content}");
        return content;
    }

    private static string GetAntiforgeryToken(string page)
        => MatchHtml(page, "<input[^>]+name=\"__RequestVerificationToken\"[^>]+value=\"([^\"]+)");

    private static string GetLink(string page, string fragment)
        => MatchHtml(page, $"href=\"([^\"]*{Regex.Escape(fragment)}[^\"]*)\"");

    private static string MatchHtml(string page, string pattern)
    {
        var match = Regex.Match(page, pattern, RegexOptions.IgnoreCase);
        Assert.True(match.Success, $"Expected HTML matching '{pattern}'.\n{page}");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static async Task<string> WaitForPageAsync(HttpClient client, string path, Process process, Task<string> output, Task<string> error)
    {
        for (var attempt = 0; attempt < 60 && !process.HasExited; attempt++)
        {
            try
            {
                return await client.GetStringAsync(path);
            }
            catch (HttpRequestException ex) when (ex.StatusCode is null)
            {
                await Task.Delay(500);
            }
        }
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }
        await process.WaitForExitAsync();
        Assert.Fail($"The scaffolded application did not become reachable.\n{await output}\n{await error}");
        return string.Empty;
    }

    private sealed class IdentityTestProject : IDisposable
    {
        private readonly string _template;
        private string _scaffoldOutput = string.Empty;
        public string Framework { get; }
        public string Directory { get; }
        public string Path => System.IO.Path.Combine(Directory, "IdentityApp.csproj");
        public string HostFolder => _template == "webapp" ? "Pages" : "Views";

        public IdentityTestProject(string framework, string template)
        {
            Framework = framework;
            _template = template;
            Directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), nameof(IdentityEndToEndTests), Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(Directory);
            if (framework == "net11.0")
            {
                File.WriteAllText(System.IO.Path.Combine(Directory, "NuGet.config"), ScaffoldCliHelper.PreviewNuGetConfig);
            }
        }

        public async Task CreateAsync(string authentication = "None")
        {
            var sdks = await ScaffoldCliHelper.RunDotNetAsync(Directory, "--list-sdks");
            AssertSuccess(sdks);
            var sdkVersion = sdks.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0])
                .LastOrDefault(version => version.StartsWith(Framework[3..] + ".", StringComparison.Ordinal));
            Assert.False(string.IsNullOrEmpty(sdkVersion), $"An SDK for {Framework} is required.\n{sdks.Output}");
            File.WriteAllText(System.IO.Path.Combine(Directory, "global.json"),
                JsonSerializer.Serialize(new { sdk = new { version = sdkVersion, rollForward = "disable", allowPrerelease = true } }));

            var arguments = new List<string> { "new", _template, "--name", "IdentityApp", "--output", Directory, "--framework", Framework, "--auth", authentication };
            if (authentication == "Individual")
            {
                arguments.AddRange(["--use-local-db", "false"]);
            }
            else
            {
                arguments.Add("--no-restore");
            }
            AssertSuccess(await ScaffoldCliHelper.RunDotNetAsync(Directory, [.. arguments]));
        }

        public async Task ScaffoldAsync(bool relativeProjectPath = false)
        {
            var arguments = new List<string> { "--project", relativeProjectPath ? "IdentityApp.csproj" : Path, "--dataContext", "ApplicationDbContext", "--dbProvider", "sqlite-efcore" };
            if (Framework == "net11.0")
            {
                arguments.Add("--prerelease");
            }
            var result = relativeProjectPath
                ? await ScaffoldCliHelper.RunDotNetAsync(Directory,
                    ["exec", ScaffoldCliHelper.GetScaffoldAssemblyPath(Framework), "aspnet", "identity", .. arguments])
                : await ScaffoldCliHelper.RunScaffoldAsync(Framework, "identity", [.. arguments]);
            _scaffoldOutput = result.Output + Environment.NewLine + result.Error;
            AssertSuccess(result);
            Assert.True(string.IsNullOrWhiteSpace(result.Error), _scaffoldOutput);
            Assert.DoesNotContain("Unable to", result.Output, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Failed", result.Output, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(System.IO.Path.Combine(Directory, "Areas", "Identity", "Pages", "Account", "Login.cshtml")),
                $"Identity pages were not generated.\n{_scaffoldOutput}");
            Assert.True(System.IO.Directory.Exists(System.IO.Path.Combine(Directory, "Data", "Migrations")),
                $"Identity migrations were not generated.\n{result.Output}\n{result.Error}");
        }

        public async Task BuildAsync()
            => AssertSuccess(await ScaffoldCliHelper.RunBuildForFrameworkAsync(Directory, Framework));

        public async Task AssertUnchangedSecondRunAsync(bool relativeProjectPath = false)
        {
            var before = GetSourceHashes();
            var projectBefore = File.ReadAllText(Path);
            await ScaffoldAsync(relativeProjectPath);
            var after = GetSourceHashes();
            var changed = before.Keys.Union(after.Keys).Where(path => before.GetValueOrDefault(path) != after.GetValueOrDefault(path)).ToList();
            Assert.True(changed.Count == 0,
                $"Second scaffolding pass changed: {string.Join(", ", changed)}\nProject before:\n{projectBefore}\nProject after:\n{File.ReadAllText(Path)}\n{_scaffoldOutput}");
        }

        private SortedDictionary<string, string> GetSourceHashes()
            => new(System.IO.Directory.GetFiles(Directory, "*", SearchOption.AllDirectories)
                .Where(path => !System.IO.Path.GetRelativePath(Directory, path).Split(System.IO.Path.DirectorySeparatorChar)
                    .Any(segment => segment is "bin" or "obj" or "artifacts"))
                .ToDictionary(path => System.IO.Path.GetRelativePath(Directory, path),
                    path => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))), StringComparer.Ordinal);

        private static void AssertSuccess((int ExitCode, string Output, string Error) result)
            => Assert.True(result.ExitCode == 0, $"Command failed ({result.ExitCode}).\n{result.Output}\n{result.Error}");

        public void Dispose()
        {
            try
            {
                System.IO.Directory.Delete(Directory, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Unable to remove test directory '{Directory}': {ex.Message}");
            }
        }
    }
}
