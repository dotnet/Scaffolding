// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Identity;

[Trait("Suite", "ScaffoldIntegration")]
[Trait("Family", "identity-api")]
public class IdentityApiCliIntegrationTests
{
    [Theory]
    [InlineData("net10.0")]
    [InlineData("net11.0")]
    public async Task ScaffoldBuildRerunAndExerciseIdentityFlows(string framework)
    {
        var directory = await CreateProjectAsync(framework);
        try
        {
            await AssertBuildAsync(directory, framework);
            await ScaffoldAsync(directory, framework);
            var programPath = Path.Combine(directory, "Program.cs");
            var startup = File.ReadAllText(programPath);
            Assert.Equal(1, startup.Split("AddIdentityApiEndpoints<").Length - 1);
            Assert.Equal(1, startup.Split("AddAuthorization(").Length - 1);
            Assert.Equal(1, startup.Split("MapScaffoldedIdentityApi<").Length - 1);
            var endpointPath = Path.Combine(directory, "IdentityApi", "IdentityApiEndpoints.cs");
            var endpoints = File.ReadAllText(endpointPath);
            var userPath = Path.Combine(directory, "Data", "ApplicationUser.cs");
            var contextPath = Path.Combine(directory, "Data", "ApplicationDbContext.cs");
            Assert.True(File.Exists(contextPath));
            Assert.Contains("Permission is hereby granted", File.ReadAllText(Path.Combine(directory, "IdentityApi", "LICENSE.txt")));
            var packages = XDocument.Load(Path.Combine(directory, "TestProject.csproj"))
                .Descendants("PackageReference").Select(p => (string?)p.Attribute("Include")).ToArray();
            Assert.Contains("Microsoft.AspNetCore.Identity.EntityFrameworkCore", packages);
            Assert.Contains("Microsoft.EntityFrameworkCore.Sqlite", packages);
            Assert.DoesNotContain("Microsoft.AspNetCore.Identity.UI", packages);
            var settings = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "appsettings.json")));
            Assert.False(string.IsNullOrEmpty(settings.RootElement.GetProperty("ConnectionStrings").GetProperty("ApplicationDbContext").GetString()));
            await AssertBuildAsync(directory, framework);

            File.AppendAllText(endpointPath, "\n// endpoint customization\n");
            File.AppendAllText(userPath, "\n// user customization\n");
            File.AppendAllText(contextPath, "\n// context customization\n");
            var customizedUser = File.ReadAllText(userPath);
            var customizedContext = File.ReadAllText(contextPath);
            await ScaffoldAsync(directory, framework);
            Assert.Equal(startup, File.ReadAllText(programPath));
            Assert.Equal(endpoints + "\n// endpoint customization\n", File.ReadAllText(endpointPath));
            Assert.Equal(customizedUser, File.ReadAllText(userPath));
            Assert.Equal(customizedContext, File.ReadAllText(contextPath));
            await ScaffoldAsync(directory, framework, overwrite: true);
            Assert.Equal(startup, File.ReadAllText(programPath));
            Assert.Equal(endpoints, File.ReadAllText(endpointPath));
            Assert.Equal(customizedUser, File.ReadAllText(userPath));
            Assert.Equal(customizedContext, File.ReadAllText(contextPath));

            File.WriteAllText(Path.Combine(directory, "IdentityApiTestHost.cs"), TestHostSource);
            File.WriteAllText(programPath, "using Microsoft.AspNetCore.DataProtection;\nusing TestProject.Data;\n" + startup
                .Replace("var app = builder.Build();", """
                    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "keys")));
                    builder.Services.AddSingleton<IEmailSender<ApplicationUser>, TestEmailSender>();
                    builder.Services.Configure<IdentityOptions>(options => options.SignIn.RequireConfirmedEmail = true);
                    var app = builder.Build();
                    """)
                .Replace("app.Run();", "await IdentityApiTestHost.RunAsync(app);"));
            await AssertBuildAsync(directory, framework);
            await ExerciseHttpFlowsAsync(directory, framework);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(directory)!, recursive: true);
        }
    }

    [Theory]
    [InlineData("net10.0")]
    [InlineData("net11.0")]
    public async Task RejectsExistingFrameworkMappingWithoutChangingProject(string framework)
    {
        var directory = await CreateProjectAsync(framework);
        try
        {
            var programPath = Path.Combine(directory, "Program.cs");
            var original = """
                var builder = WebApplication.CreateBuilder(args);
                builder.Services.AddIdentityApiEndpoints<Microsoft.AspNetCore.Identity.IdentityUser>();
                builder.Services.AddAuthorization();
                var app = builder.Build();
                app.MapIdentityApi<Microsoft.AspNetCore.Identity.IdentityUser>();
                app.Run();
                """;
            File.WriteAllText(programPath, original);
            var projectPath = Path.Combine(directory, "TestProject.csproj");
            var project = File.ReadAllText(projectPath);
            var result = await RunScaffoldAsync(directory, framework);
            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("already configures", result.Output + result.Error);
            Assert.DoesNotContain("Adding package", result.Output);
            Assert.Equal(original, File.ReadAllText(programPath));
            Assert.Equal(project, File.ReadAllText(projectPath));
            Assert.False(Directory.Exists(Path.Combine(directory, "Data")));
            Assert.False(Directory.Exists(Path.Combine(directory, "IdentityApi")));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(directory)!, recursive: true);
        }
    }

    [Theory]
    [InlineData("net11.0", true)]
    [InlineData("net10.0", false)]
    public async Task ReusesCustomIdentityUserAndContext(string framework, bool preconfiguredContext)
    {
        var directory = await CreateProjectAsync(framework);
        try
        {
            await ScaffoldAsync(directory, framework);
            var userPath = Path.Combine(directory, "Data", "ApplicationUser.cs");
            var user = File.ReadAllText(userPath);
            var contextPath = Path.Combine(directory, "Data", "ApplicationDbContext.cs");
            const string context = """
                namespace Existing.Data;
                public class ApplicationDbContext(Microsoft.EntityFrameworkCore.DbContextOptions<ApplicationDbContext> options)
                    : Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<Existing.Users.Member>(options)
                {
                }
                """;
            const string member = """
                namespace Existing.Users;
                public class Member : Microsoft.AspNetCore.Identity.IdentityUser
                {
                    public string? DisplayName { get; set; }
                }
                """;
            File.WriteAllText(contextPath, context);
            var memberPath = Path.Combine(directory, "Member.cs");
            File.WriteAllText(memberPath, member);
            var startup = ScaffoldCliHelper.GetMinimalProgramCs();
            if (preconfiguredContext)
            {
                startup = """
                    using Microsoft.EntityFrameworkCore;
                    using ExistingContext = Existing.Data.ApplicationDbContext;
                    var builder = WebApplication.CreateBuilder(args);
                    builder.Services.AddDbContext<ExistingContext>(options => options.UseSqlite("Data Source=custom.db"));
                    var app = builder.Build();
                    app.Run();
                    """;
            }
            else
            {
                startup = startup.Replace("app.Run();", "await app.RunAsync();");
            }
            File.WriteAllText(Path.Combine(directory, "Program.cs"), startup);
            var settingsPath = Path.Combine(directory, "appsettings.json");
            var settings = File.ReadAllText(settingsPath);
            await AssertBuildAsync(directory, framework);
            await ScaffoldAsync(directory, framework, overwrite: true);
            Assert.Equal(context, File.ReadAllText(contextPath));
            Assert.Equal(member, File.ReadAllText(memberPath));
            Assert.Equal(user, File.ReadAllText(userPath));
            Assert.Equal(settings, File.ReadAllText(settingsPath));
            var program = File.ReadAllText(Path.Combine(directory, "Program.cs"));
            Assert.Contains("AddIdentityApiEndpoints<global::Existing.Users.Member>", program);
            Assert.Contains("MapScaffoldedIdentityApi<global::Existing.Users.Member>", program);
            Assert.Equal(1, program.Split(".AddDbContext<").Length - 1);
            if (preconfiguredContext)
            {
                Assert.Contains("Data Source=custom.db", program);
                Assert.DoesNotContain("identityConnectionString", program);
            }
            await AssertBuildAsync(directory, framework);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(directory)!, recursive: true);
        }
    }

    [Theory]
    [InlineData("net10.0")]
    [InlineData("net11.0")]
    public async Task RejectsNonIdentityContextBeforeModifyingFiles(string framework)
    {
        var directory = await CreateProjectAsync(framework);
        try
        {
            await ScaffoldAsync(directory, framework);
            var startup = ScaffoldCliHelper.GetMinimalProgramCs();
            File.WriteAllText(Path.Combine(directory, "Program.cs"), startup);
            var contextPath = Path.Combine(directory, "Data", "ApplicationDbContext.cs");
            const string context = """
                public class ApplicationDbContext(Microsoft.EntityFrameworkCore.DbContextOptions<ApplicationDbContext> options)
                    : Microsoft.EntityFrameworkCore.DbContext(options)
                {
                }
                """;
            File.WriteAllText(contextPath, context);
            await AssertBuildAsync(directory, framework);
            var result = await RunScaffoldAsync(directory, framework);
            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("must derive from IdentityDbContext", result.Output + result.Error);
            Assert.DoesNotContain("Adding package", result.Output);
            Assert.Equal(startup, File.ReadAllText(Path.Combine(directory, "Program.cs")));
            Assert.Equal(context, File.ReadAllText(contextPath));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(directory)!, recursive: true);
        }
    }

    private static async Task<string> CreateProjectAsync(string framework)
    {
        var root = Path.Combine(Path.GetTempPath(), nameof(IdentityApiCliIntegrationTests), Guid.NewGuid().ToString());
        var directory = ScaffoldCliHelper.SetupTestProject(root, framework, includeProgram: true);
        try
        {
            await ScaffoldCliHelper.UseInstalledSdkAsync(directory, framework);
            File.WriteAllText(Path.Combine(directory, "NuGet.config"), ScaffoldCliHelper.PreviewNuGetConfig);
            return directory;
        }
        catch
        {
            Directory.Delete(root, recursive: true);
            throw;
        }
    }

    private static Task<(int ExitCode, string Output, string Error)> RunScaffoldAsync(string directory, string framework, bool overwrite = false)
    {
        var args = new System.Collections.Generic.List<string>
        {
            "--project", "TestProject.csproj",
            "--dataContext", "ApplicationDbContext", "--dbProvider", "sqlite-efcore"
        };
        args.AddRange(ScaffoldCliHelper.GetPrereleaseArguments(framework));
        if (overwrite)
        {
            args.Add("--overwrite");
        }
        var tool = ScaffoldCliHelper.GetScaffoldAssemblyPath();
        Assert.True(File.Exists(tool), $"Build the CLI before running integration tests: {tool}");
        return ScaffoldCliHelper.RunDotNetAsync(directory, [tool, "aspnet", "identity-api", .. args]);
    }

    private static async Task ScaffoldAsync(string directory, string framework, bool overwrite = false)
    {
        var result = await RunScaffoldAsync(directory, framework, overwrite);
        Assert.True(result.ExitCode == 0, $"Scaffolding failed:\n{result.Output}\n{result.Error}");
    }

    private static async Task AssertBuildAsync(string directory, string framework)
    {
        var result = await ScaffoldCliHelper.RunBuildForFrameworkAsync(directory, framework);
        Assert.True(result.ExitCode == 0, $"Build failed:\n{result.Output}\n{result.Error}");
    }

    private static async Task ExerciseHttpFlowsAsync(string directory, string framework)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                WorkingDirectory = directory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.StartInfo.ArgumentList.Add(Path.Combine(directory, "bin", "Debug", framework, "TestProject.dll"));
        ScaffoldCliHelper.ConfigureDotNetEnvironment(process.StartInfo);
        process.Start();
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            var addressFile = Path.Combine(directory, "address.txt");
            var deadline = DateTime.UtcNow.AddSeconds(60);
            while (!File.Exists(addressFile) && !process.HasExited && DateTime.UtcNow < deadline)
            {
                await Task.Delay(100);
            }
            if (!File.Exists(addressFile))
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
                await process.WaitForExitAsync();
                Assert.Fail($"The scaffolded app did not start:\n{await stdout}\n{await stderr}");
            }

            using var client = new HttpClient(new HttpClientHandler { UseCookies = false })
            {
                BaseAddress = new Uri(File.ReadAllText(addressFile)),
                Timeout = TimeSpan.FromSeconds(30)
            };
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/identity/manage/info")).StatusCode);
            using var invalid = await client.PostAsJsonAsync("/identity/register", new { email = "invalid", password = "bad" });
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            var validation = await invalid.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(validation.GetProperty("errors").TryGetProperty("InvalidEmail", out _));

            const string email = "user@example.com";
            const string password = "Password1!";
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/identity/register", new { email, password })).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/identity/login", new { email, password })).StatusCode);
            var confirmation = File.ReadAllText(Path.Combine(directory, "confirmation.txt"));
            Assert.Equal(client.BaseAddress!.Authority, new Uri(confirmation).Authority);
            Assert.StartsWith("/identity/confirmEmail", new Uri(confirmation).AbsolutePath);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(confirmation)).StatusCode);
            using var login = await client.PostAsJsonAsync("/identity/login", new { email, password });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            var tokens = await login.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Bearer", tokens.GetProperty("tokenType").GetString());
            Assert.True(tokens.GetProperty("expiresIn").GetInt64() > 0);
            var accessToken = tokens.GetProperty("accessToken").GetString();
            var refreshToken = tokens.GetProperty("refreshToken").GetString();
            Assert.False(string.IsNullOrEmpty(accessToken));
            Assert.False(string.IsNullOrEmpty(refreshToken));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var infoResponse = await client.GetAsync("/identity/manage/info");
            Assert.Equal(HttpStatusCode.OK, infoResponse.StatusCode);
            var info = await infoResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(email, info.GetProperty("email").GetString());
            Assert.True(info.GetProperty("isEmailConfirmed").GetBoolean());
            client.DefaultRequestHeaders.Authorization = null;
            using var refresh = await client.PostAsJsonAsync("/identity/refresh", new { refreshToken });
            Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
            var refreshed = await refresh.Content.ReadFromJsonAsync<JsonElement>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.GetProperty("accessToken").GetString());
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/identity/manage/info")).StatusCode);
            client.DefaultRequestHeaders.Authorization = null;
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/identity/refresh", new { refreshToken = "invalid" })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/identity/forgotPassword", new { email = "unknown@example.com" })).StatusCode);
            Assert.False(File.Exists(Path.Combine(directory, "reset.txt")));
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/identity/forgotPassword", new { email })).StatusCode);
            const string newPassword = "ChangedPassword2!";
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/identity/resetPassword",
                new { email, resetCode = File.ReadAllText(Path.Combine(directory, "reset.txt")), newPassword })).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/identity/login", new { email, password })).StatusCode);
            using var staleRefresh = await client.PostAsJsonAsync("/identity/refresh", new { refreshToken });
            Assert.Equal(HttpStatusCode.Unauthorized, staleRefresh.StatusCode);
            using var cookies = new HttpClient(new HttpClientHandler { CookieContainer = new CookieContainer(), AllowAutoRedirect = false })
            {
                BaseAddress = client.BaseAddress
            };
            using var cookieLogin = await cookies.PostAsJsonAsync("/identity/login?useCookies=true", new { email, password = newPassword });
            Assert.Equal(HttpStatusCode.OK, cookieLogin.StatusCode);
            Assert.True(cookieLogin.Headers.Contains("Set-Cookie"));
            Assert.Equal(HttpStatusCode.OK, (await cookies.GetAsync("/identity/manage/info")).StatusCode);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
            await process.WaitForExitAsync();
            await Task.WhenAll(stdout, stderr);
        }
    }

    private const string TestHostSource = """
        using System.Net;
        using Microsoft.AspNetCore.Hosting.Server;
        using Microsoft.AspNetCore.Hosting.Server.Features;
        using Microsoft.AspNetCore.Identity;
        using Microsoft.EntityFrameworkCore;
        using TestProject.Data;

        internal static class IdentityApiTestHost
        {
            public static async Task RunAsync(WebApplication app)
            {
                using (var scope = app.Services.CreateScope())
                {
                    await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreatedAsync();
                }
                app.Urls.Add("http://127.0.0.1:0");
                await app.StartAsync();
                var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!;
                File.WriteAllText("address.tmp", addresses.Addresses.Single());
                File.Move("address.tmp", "address.txt");
                await app.WaitForShutdownAsync();
            }
        }

        internal sealed class TestEmailSender : IEmailSender<ApplicationUser>
        {
            public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
            {
                File.WriteAllText("confirmation.txt", WebUtility.HtmlDecode(confirmationLink));
                return Task.CompletedTask;
            }
            public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
            {
                File.WriteAllText("reset.txt", WebUtility.HtmlDecode(resetCode));
                return Task.CompletedTask;
            }
            public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
                throw new NotSupportedException("The Identity API should send a reset code.");
        }
        """;
}
