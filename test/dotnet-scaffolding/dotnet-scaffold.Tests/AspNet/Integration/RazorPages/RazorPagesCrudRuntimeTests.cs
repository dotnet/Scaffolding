// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.RazorPages;

[Trait("Suite", "ScaffoldIntegration")]
[Trait("Family", "razorpages-crud")]
public class RazorPagesCrudRuntimeTests : IDisposable
{
    private static string TargetFramework => ScaffoldCliHelper.GetTestTargetFramework();
    private readonly string _testProjectDir;
    private readonly string _testProjectPath;
    private readonly ITestOutputHelper _output;

    public RazorPagesCrudRuntimeTests(ITestOutputHelper output)
    {
        _output = output;
        _testProjectDir = Path.Combine(Path.GetTempPath(), nameof(RazorPagesCrudRuntimeTests), Guid.NewGuid().ToString());
        _testProjectPath = Path.Combine(_testProjectDir, "TestProject.csproj");
        Directory.CreateDirectory(_testProjectDir);
        File.WriteAllText(Path.Combine(_testProjectDir, "NuGet.config"), ScaffoldCliHelper.GetTestNuGetConfig(TargetFramework));
    }

    public void Dispose()
    {
        Directory.Delete(_testProjectDir, recursive: true);
    }

    [Theory]
    [InlineData("Blog", "BlogId", "Blogs")]
    [InlineData("Id", "IdId", "Ids")]
    public Task Scaffold_WithExistingPluralDbSet_BuildsAndHandlesCrudRequests(
        string modelName, string keyName, string setName)
        => RunCrudAsync(modelName, keyName, setName, localDb: false);

    // Explicit opt-in keeps machines without Windows/LocalDB usable.
    [SkippableTheory]
    [InlineData("Blog", "BlogId", "Blogs")]
    public async Task Scaffold_WithLocalDb_BuildsAndHandlesCrudRequests(
        string modelName, string keyName, string setName)
    {
        Skip.IfNot(OperatingSystem.IsWindows() &&
            Environment.GetEnvironmentVariable("SCAFFOLDING_TEST_LOCALDB") == "1",
            "Set SCAFFOLDING_TEST_LOCALDB=1 on Windows with MSSQLLocalDB installed.");
        await RunCrudAsync(modelName, keyName, setName, localDb: true);
    }

    private async Task RunCrudAsync(string modelName, string keyName, string setName, bool localDb)
    {
        // Never delete a configured/user database: this invocation owns a unique database.
        string databaseName = "ScaffoldingCrud_" + Guid.NewGuid().ToString("N");
        string connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Integrated Security=true;TrustServerCertificate=true";
        try
        {
            await RunCrudCoreAsync(modelName, keyName, setName, localDb, connectionString);
        }
        finally
        {
            if (localDb)
            {
                await using var context = new DbContext(new DbContextOptionsBuilder()
                    .UseSqlServer(connectionString).Options);
                await context.Database.EnsureDeletedAsync();
            }
        }
    }

    private async Task RunCrudCoreAsync(string modelName, string keyName, string setName,
        bool localDb, string connectionString)
    {
        string provider = localDb ? "SqlServer" : "Sqlite";
        string databaseOptions = localDb
            ? $"UseSqlServer(@\"{connectionString}\")"
            : "UseSqlite(\"Data Source=crud.db\")";
        string efVersion = typeof(DbContext).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];
        File.WriteAllText(_testProjectPath, $$"""
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>{{TargetFramework}}</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.EntityFrameworkCore.{{provider}}" Version="{{efVersion}}" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(_testProjectDir, "Models.cs"), $$"""
            using Microsoft.EntityFrameworkCore;
            namespace TestProject.Models;
            public class {{modelName}}
            {
                public int {{keyName}} { get; set; }
                public string Url { get; set; } = "";
            }
            public class BloggingContext(DbContextOptions<BloggingContext> options) : DbContext(options)
            {
                public DbSet<{{modelName}}> {{setName}} { get; set; } = default!;
            }
            """);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"),
            "using TestProject.Models;\n" + ScaffoldCliHelper.GetMinimalProgramCs());
        var before = await ScaffoldCliHelper.RunBuildForFrameworkAsync(_testProjectDir, TargetFramework);
        Assert.True(before.ExitCode == 0, $"Before scaffolding:\n{before.Output}\n{before.Error}");

        var scaffold = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework, "razorpages-crud",
            "--project", _testProjectPath,
            "--model", modelName,
            "--dataContext", "BloggingContext",
            "--dbProvider", localDb ? "sqlserver-efcore" : "sqlite-efcore",
            "--page", "CRUD");
        Assert.True(scaffold.ExitCode == 0, $"Scaffolding:\n{scaffold.Output}\n{scaffold.Error}");
        string pages = Path.Combine(_testProjectDir, "Pages", modelName + "Pages");
        foreach (string page in new[] { "Index", "Create", "Edit", "Details", "Delete" })
        {
            Assert.True(File.Exists(Path.Combine(pages, page + ".cshtml")), scaffold.Output);
            string handler = File.ReadAllText(Path.Combine(pages, page + ".cshtml.cs"));
            Assert.Contains($"_context.{setName}", handler);
        }
        Assert.Single(Directory.GetFiles(_testProjectDir, "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText), text => text.Contains("class BloggingContext"));
        string context = File.ReadAllText(Path.Combine(_testProjectDir, "Models.cs"));
        Assert.Single(Regex.Matches(context, @"DbSet<"));
        File.WriteAllText(Path.Combine(_testProjectDir, "Pages", "_ViewImports.cshtml"),
            "@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers\n");
        var generatedBuild = await ScaffoldCliHelper.RunBuildForFrameworkAsync(_testProjectDir, TargetFramework);
        Assert.True(generatedBuild.ExitCode == 0,
            $"Scaffolded application:\n{generatedBuild.Output}\n{generatedBuild.Error}");

        // Keep generated pages, but use an isolated database and deterministic seed data.
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), $$"""
            using Microsoft.AspNetCore.Hosting.Server;
            using Microsoft.AspNetCore.Hosting.Server.Features;
            using Microsoft.EntityFrameworkCore;
            using TestProject.Models;
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddRazorPages();
            builder.Services.AddDbContext<BloggingContext>(options => options.{{databaseOptions}});
            var app = builder.Build();
            app.MapRazorPages();
            await using (var scope = app.Services.CreateAsyncScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<BloggingContext>();
                await context.Database.EnsureCreatedAsync();
                context.{{setName}}.Add(new {{modelName}} { Url = "original-value" });
                await context.SaveChangesAsync();
            }
            await app.StartAsync();
            var addresses = app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!.Addresses;
            await File.WriteAllTextAsync("server-address.tmp", addresses.Single());
            File.Move("server-address.tmp", "server-address.txt");
            await app.WaitForShutdownAsync();
            """);
        var build = await ScaffoldCliHelper.RunBuildForFrameworkAsync(_testProjectDir, TargetFramework);
        Assert.True(build.ExitCode == 0, $"Generated application:\n{build.Output}\n{build.Error}");

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ScaffoldCliHelper.GetDotNetPath(),
                WorkingDirectory = _testProjectDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.StartInfo.ArgumentList.Add(Path.Combine(_testProjectDir, "bin", "Debug", TargetFramework, "TestProject.dll"));
        process.StartInfo.ArgumentList.Add("--urls");
        process.StartInfo.ArgumentList.Add("http://127.0.0.1:0");
        process.StartInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        process.StartInfo.Environment["Logging__LogLevel__Default"] = "Warning";
        process.Start();
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        try
        {
            string addressFile = Path.Combine(_testProjectDir, "server-address.txt");
            for (int attempt = 0; attempt < 120 && !File.Exists(addressFile) && !process.HasExited; attempt++)
            {
                await Task.Delay(250);
            }
            Assert.False(process.HasExited, process.HasExited ? $"{await output}\n{await error}" : "");
            Assert.True(File.Exists(addressFile), "Application did not start within 30 seconds.");
            using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
            {
                BaseAddress = new Uri(await File.ReadAllTextAsync(addressFile))
            };
            string prefix = "/" + modelName + "Pages";
            string index = await client.GetStringAsync(prefix + "/Index");
            foreach (string page in new[] { "Edit", "Details", "Delete" })
            {
                string link = GetPageLink(index, page);
                Assert.Contains("id=1", link);
                using var response = await client.GetAsync(link);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Contains("original-value", await response.Content.ReadAsStringAsync());
                using var missingId = await client.GetAsync(prefix + "/" + page);
                Assert.Equal(HttpStatusCode.NotFound, missingId.StatusCode);
                using var missingEntity = await client.GetAsync(prefix + "/" + page + "?id=999");
                Assert.Equal(HttpStatusCode.NotFound, missingEntity.StatusCode);
            }

            string editLink = GetPageLink(index, "Edit");
            string editForm = await client.GetStringAsync(editLink);
            using var editContent = FormContent(editForm, modelName, keyName, "updated-value");
            using var edited = await client.PostAsync(editLink, editContent);
            Assert.Equal(HttpStatusCode.Redirect, edited.StatusCode);
            string details = await client.GetStringAsync(GetPageLink(index, "Details"));
            Assert.Contains("updated-value", details);
            Assert.DoesNotContain("original-value", details);

            string deleteLink = GetPageLink(index, "Delete");
            string deleteForm = await client.GetStringAsync(deleteLink);
            using var deleteContent = FormContent(deleteForm, modelName, keyName);
            using var deleted = await client.PostAsync(deleteLink, deleteContent);
            Assert.Equal(HttpStatusCode.Redirect, deleted.StatusCode);
            using var afterDelete = await client.GetAsync(GetPageLink(index, "Details"));
            Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
            Assert.DoesNotContain("updated-value", await client.GetStringAsync(prefix + "/Index"));
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
            await process.WaitForExitAsync();
            _output.WriteLine(await output);
            _output.WriteLine(await error);
        }
    }

    private static string GetPageLink(string html, string page)
    {
        Match match = Regex.Match(html, $"""href="([^"]*/{page}\?[^"]*)""");
        Assert.True(match.Success, $"Missing {page} link:\n{html}");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static FormUrlEncodedContent FormContent(string html, string modelName, string keyName, string? url = null)
    {
        var fields = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = GetInputValue(html, "__RequestVerificationToken"),
            [$"{modelName}.{keyName}"] = GetInputValue(html, $"{modelName}.{keyName}")
        };
        if (url is not null)
        {
            fields[$"{modelName}.Url"] = url;
        }
        return new FormUrlEncodedContent(fields);
    }

    private static string GetInputValue(string html, string name)
    {
        Match input = Regex.Match(html,
            $"<input\\b(?=[^>]*\\bname=\"{Regex.Escape(name)}\")(?=[^>]*\\bvalue=\"([^\"]*)\")[^>]*>");
        Assert.True(input.Success, $"Missing form field '{name}'.");
        return WebUtility.HtmlDecode(input.Groups[1].Value);
    }
}
