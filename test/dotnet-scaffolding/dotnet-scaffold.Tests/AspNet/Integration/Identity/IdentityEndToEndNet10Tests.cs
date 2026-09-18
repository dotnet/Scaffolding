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
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Identity;

[Trait("Suite", "ScaffoldIntegration")]
[Trait("Family", "identity")]
public class IdentityEndToEndNet10Tests
{
    [Theory]
    [InlineData("mvc", "Views")]
    [InlineData("webapp", "Pages")]
    public async Task ScaffoldIdentity_ConfiguresCleanProject(string templateName, string hostFolder)
    {
        var projectName = templateName == "mvc" ? "MvcNoAuth" : "RazorNoAuth";
        var testDirectory = Path.Combine(Path.GetTempPath(), nameof(IdentityEndToEndNet10Tests), Guid.NewGuid().ToString("N"));
        var projectDirectory = Path.Combine(testDirectory, projectName);
        var projectPath = Path.Combine(projectDirectory, $"{projectName}.csproj");

        Directory.CreateDirectory(testDirectory);
        try
        {
            var createResult = await RunDotNetAsync(
                testDirectory,
                "new", templateName,
                "--name", projectName,
                "--output", projectDirectory,
                "--framework", "net10.0",
                "--auth", "None",
                "--no-restore");
            Assert.True(createResult.ExitCode == 0, $"Project creation failed.{Environment.NewLine}{createResult.Output}{Environment.NewLine}{createResult.Error}");

            var scaffoldResult = await ScaffoldCliHelper.RunScaffoldAsync(
                "net10.0",
                "identity",
                "--project", projectPath,
                "--dataContext", "ApplicationDbContext",
                "--dbProvider", "sqlite-efcore");
            Assert.True(scaffoldResult.ExitCode == 0, $"Identity scaffolding failed.{Environment.NewLine}{scaffoldResult.Output}{Environment.NewLine}{scaffoldResult.Error}");

            AssertConfiguredProject(projectDirectory, hostFolder);

            var buildResult = await ScaffoldCliHelper.RunBuildForFrameworkAsync(projectDirectory, "net10.0");
            Assert.True(buildResult.ExitCode == 0, $"Scaffolded project failed to build.{Environment.NewLine}{buildResult.Output}{Environment.NewLine}{buildResult.Error}");

            var sourceHashes = GetSourceHashes(projectDirectory);
            var repeatResult = await ScaffoldCliHelper.RunScaffoldAsync(
                "net10.0",
                "identity",
                "--project", projectPath,
                "--dataContext", "ApplicationDbContext",
                "--dbProvider", "sqlite-efcore");
            Assert.True(repeatResult.ExitCode == 0, $"Repeated Identity scaffolding failed.{Environment.NewLine}{repeatResult.Output}{Environment.NewLine}{repeatResult.Error}");
            Assert.Equal(sourceHashes, GetSourceHashes(projectDirectory));

            await AssertIdentityEndpointsAsync(projectPath);
        }
        finally
        {
            try
            {
                Directory.Delete(testDirectory, recursive: true);
            }
            catch
            {
                // Best-effort cleanup; preserve any test failure.
            }
        }
    }

    [Theory]
    [InlineData("mvc")]
    [InlineData("webapp")]
    public async Task ScaffoldIdentity_SecondRunDoesNotChangeProjectWithDefaultIdentityUi(string templateName)
    {
        var projectName = templateName == "mvc" ? "MvcIdentity" : "RazorIdentity";
        var testDirectory = Path.Combine(Path.GetTempPath(), nameof(IdentityEndToEndNet10Tests), Guid.NewGuid().ToString("N"));
        var projectDirectory = Path.Combine(testDirectory, projectName);
        var projectPath = Path.Combine(projectDirectory, $"{projectName}.csproj");

        Directory.CreateDirectory(testDirectory);
        try
        {
            var createResult = await RunDotNetAsync(
                testDirectory,
                "new", templateName,
                "--name", projectName,
                "--output", projectDirectory,
                "--framework", "net10.0",
                "--auth", "Individual",
                "--use-local-db", "false");
            Assert.True(createResult.ExitCode == 0, $"Project creation failed.{Environment.NewLine}{createResult.Output}{Environment.NewLine}{createResult.Error}");

            var firstScaffoldResult = await ScaffoldCliHelper.RunScaffoldAsync(
                "net10.0",
                "identity",
                "--project", projectPath,
                "--dataContext", "ApplicationDbContext",
                "--dbProvider", "sqlite-efcore");
            Assert.True(firstScaffoldResult.ExitCode == 0, $"Initial Identity scaffolding failed.{Environment.NewLine}{firstScaffoldResult.Output}{Environment.NewLine}{firstScaffoldResult.Error}");

            var sourceHashes = GetSourceHashes(projectDirectory);
            var secondScaffoldResult = await ScaffoldCliHelper.RunScaffoldAsync(
                "net10.0",
                "identity",
                "--project", projectPath,
                "--dataContext", "ApplicationDbContext",
                "--dbProvider", "sqlite-efcore");
            Assert.True(secondScaffoldResult.ExitCode == 0, $"Repeated Identity scaffolding failed.{Environment.NewLine}{secondScaffoldResult.Output}{Environment.NewLine}{secondScaffoldResult.Error}");
            Assert.Equal(sourceHashes, GetSourceHashes(projectDirectory));
        }
        finally
        {
            try
            {
                Directory.Delete(testDirectory, recursive: true);
            }
            catch
            {
                // Best-effort cleanup; preserve any test failure.
            }
        }
    }

    [Theory]
    [InlineData("builder.Services.AddIdentity<ApplicationUser, IdentityRole>()\n    .AddEntityFrameworkStores<ApplicationDbContext>()\n    .AddDefaultTokenProviders()\n    .AddDefaultUI();")]
    [InlineData("builder.Services.AddIdentityCore<ApplicationUser>()\n    .AddRoles<IdentityRole>()\n    .AddEntityFrameworkStores<ApplicationDbContext>()\n    .AddSignInManager()\n    .AddDefaultTokenProviders()\n    .AddDefaultUI();")]
    public async Task ScaffoldIdentity_PreservesExistingIdentityRegistration(string identityRegistration)
    {
        var projectName = "MvcCustomIdentity";
        var testDirectory = Path.Combine(Path.GetTempPath(), nameof(IdentityEndToEndNet10Tests), Guid.NewGuid().ToString("N"));
        var projectDirectory = Path.Combine(testDirectory, projectName);
        var projectPath = Path.Combine(projectDirectory, $"{projectName}.csproj");

        Directory.CreateDirectory(testDirectory);
        try
        {
            var createResult = await RunDotNetAsync(
                testDirectory,
                "new", "mvc",
                "--name", projectName,
                "--output", projectDirectory,
                "--framework", "net10.0",
                "--auth", "None",
                "--no-restore");
            Assert.True(createResult.ExitCode == 0, $"Project creation failed.{Environment.NewLine}{createResult.Output}{Environment.NewLine}{createResult.Error}");

            var programPath = Path.Combine(projectDirectory, "Program.cs");
            var programContent = File.ReadAllText(programPath);
            var customIdentitySetup = $"""
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=identity.db"));
{identityRegistration}
""";
            programContent = programContent.Replace(
                "builder.Services.AddControllersWithViews();",
                $"builder.Services.AddControllersWithViews();{Environment.NewLine}{customIdentitySetup}",
                StringComparison.Ordinal);
            File.WriteAllText(programPath, programContent);

            var scaffoldResult = await ScaffoldCliHelper.RunScaffoldAsync(
                "net10.0",
                "identity",
                "--project", projectPath,
                "--dataContext", "ApplicationDbContext",
                "--dbProvider", "sqlite-efcore");
            Assert.True(scaffoldResult.ExitCode == 0, $"Identity scaffolding failed.{Environment.NewLine}{scaffoldResult.Output}{Environment.NewLine}{scaffoldResult.Error}");

            programContent = File.ReadAllText(programPath);
            Assert.DoesNotContain("AddDefaultIdentity", programContent);
            Assert.Equal(1, CountOccurrences(programContent, identityRegistration.Split('(')[0]));

            var buildResult = await ScaffoldCliHelper.RunBuildForFrameworkAsync(projectDirectory, "net10.0");
            Assert.True(buildResult.ExitCode == 0, $"Scaffolded project failed to build.{Environment.NewLine}{buildResult.Output}{Environment.NewLine}{buildResult.Error}");
        }
        finally
        {
            try
            {
                Directory.Delete(testDirectory, recursive: true);
            }
            catch
            {
                // Best-effort cleanup; preserve any test failure.
            }
        }
    }

    private static void AssertConfiguredProject(string projectDirectory, string hostFolder)
    {
        var programContent = File.ReadAllText(Path.Combine(projectDirectory, "Program.cs"));
        Assert.Contains("AddDatabaseDeveloperPageExceptionFilter", programContent);
        Assert.Contains("AddRazorPages", programContent);
        Assert.Contains("UseMigrationsEndPoint", programContent);
        Assert.DoesNotContain("UseAuthentication", programContent);
        Assert.Contains("MapRazorPages", programContent);
        Assert.Contains("WithStaticAssets", programContent);

        var sharedDirectory = Path.Combine(projectDirectory, hostFolder, "Shared");
        var layoutContent = File.ReadAllText(Path.Combine(sharedDirectory, "_Layout.cshtml"));
        var loginPartialContent = File.ReadAllText(Path.Combine(sharedDirectory, "_LoginPartial.cshtml"));
        Assert.Contains("<partial name=\"_LoginPartial\" />", layoutContent);
        Assert.Contains("asp-page=\"/Account/Login\"", loginPartialContent);
        Assert.Contains("asp-page=\"/Account/Register\"", loginPartialContent);

        var migrationsDirectory = Path.Combine(projectDirectory, "Data", "Migrations");
        Assert.True(Directory.Exists(migrationsDirectory));
        Assert.Contains(Directory.GetFiles(migrationsDirectory), path => path.EndsWith("_CreateIdentitySchema.cs", StringComparison.Ordinal));
        Assert.Contains(Directory.GetFiles(migrationsDirectory), path => path.EndsWith("ApplicationDbContextModelSnapshot.cs", StringComparison.Ordinal));
        Assert.Empty(Directory.GetFiles(projectDirectory, "*.db", SearchOption.AllDirectories));
    }

    private static async Task AssertIdentityEndpointsAsync(string projectPath)
    {
        var port = GetAvailablePort();
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ScaffoldCliHelper.GetDotNetPath(),
                WorkingDirectory = Path.GetDirectoryName(projectPath)!,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.StartInfo.ArgumentList.Add("run");
        process.StartInfo.ArgumentList.Add("--no-build");
        process.StartInfo.ArgumentList.Add("--project");
        process.StartInfo.ArgumentList.Add(projectPath);
        process.StartInfo.ArgumentList.Add("--urls");
        process.StartInfo.ArgumentList.Add($"http://127.0.0.1:{port}");

        var output = new StringBuilder();
        process.OutputDataReceived += (_, args) => output.AppendLine(args.Data);
        process.ErrorDataReceived += (_, args) => output.AppendLine(args.Data);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var rootContent = await GetWithRetryAsync(client, $"http://127.0.0.1:{port}/", process, output);
            Assert.Contains("/Identity/Account/Login", rootContent);
            Assert.Contains("/Identity/Account/Register", rootContent);

            await GetWithRetryAsync(client, $"http://127.0.0.1:{port}/Identity/Account/Login", process, output);
            await GetWithRetryAsync(client, $"http://127.0.0.1:{port}/Identity/Account/Register", process, output);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
    }

    private static async Task<string> GetWithRetryAsync(HttpClient client, string url, Process process, StringBuilder output)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            if (process.HasExited)
            {
                Assert.Fail($"The scaffolded application exited unexpectedly.{Environment.NewLine}{output}");
            }

            try
            {
                using var response = await client.GetAsync(url);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException) when (attempt < 29)
            {
                await Task.Delay(500);
            }
        }

        Assert.Fail($"The scaffolded application did not become reachable at '{url}'.{Environment.NewLine}{output}");
        return string.Empty;
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunDotNetAsync(string workingDirectory, params string[] arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ScaffoldCliHelper.GetDotNetPath(),
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await Task.WhenAll(outputTask, errorTask);
        await process.WaitForExitAsync();
        return (process.ExitCode, outputTask.Result, errorTask.Result);
    }

    private static SortedDictionary<string, string> GetSourceHashes(string projectDirectory)
    {
        return new SortedDictionary<string, string>(
            Directory.GetFiles(projectDirectory, "*", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(
                    path => Path.GetRelativePath(projectDirectory, path),
                    path => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))),
            StringComparer.Ordinal);
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
