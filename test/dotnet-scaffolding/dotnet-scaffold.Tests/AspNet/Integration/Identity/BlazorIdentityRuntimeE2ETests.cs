// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Identity;

public class BlazorIdentityRuntimeE2ETests : BlazorIdentityIntegrationTestsBase
{
    protected override string TargetFramework => "net10.0";
    protected override string TestClassName => nameof(BlazorIdentityRuntimeE2ETests);

    [Fact]
    public async Task Scaffold_Run_And_Request_RegisterEndpoint_NoRuntimeDIError()
    {
        // Arrange: create project structure
        File.WriteAllText(_testProjectPath, ProjectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), ScaffoldCliHelper.GetBlazorProgramCs("TestProject"));
        ScaffoldCliHelper.SetupBlazorProjectStructure(_testProjectDir);
        // Ensure App.razor matches expectations
        File.WriteAllText(Path.Combine(_testProjectDir, "Components", "App.razor"), GloballyInteractiveAppContent);

        // Build before scaffolding
        var (preExitCode, preOut, preErr) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0, $"Project should build before scaffolding.\nOutput: {preOut}\nError: {preErr}");

        // Act: scaffold Blazor identity
        var (cliExitCode, cliOut, cliErr) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "blazor-identity",
            "--project", _testProjectPath,
            "--dataContext", "TestDbContext",
            "--dbProvider", "sqlite-efcore");

        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOut}\nError: {cliErr}");

        // Build scaffolded project for the target framework
        var (buildExit, buildOut, buildErr) = await RunBuildAsync(_testProjectDir);
        Assert.True(buildExit == 0, $"Project should build after scaffolding.\nOutput: {buildOut}\nError: {buildErr}");

        // Start the app: dotnet run -f {TargetFramework}
        var dotnet = ScaffoldCliHelper.GetDotNetPath();
        var startInfo = new ProcessStartInfo
        {
            FileName = dotnet,
            Arguments = $"run -f {TargetFramework}",
            WorkingDirectory = _testProjectDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        // Read output lines until we find the listening URL or timeout
        var output = string.Empty;
        var listenUrl = string.Empty;
        var timeout = TimeSpan.FromSeconds(60);
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            while (!process.StandardOutput.EndOfStream)
            {
                var line = await process.StandardOutput.ReadLineAsync();
                output += line + "\n";
                // Look for Kestrel listening line
                var m = Regex.Match(line ?? string.Empty, @"Now listening on: (?<url>https?://\S+)", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    listenUrl = m.Groups["url"].Value.TrimEnd('/');
                    break;
                }
            }

            if (!string.IsNullOrEmpty(listenUrl)) break;
            await Task.Delay(250);
            if (process.HasExited) break;
        }

        if (string.IsNullOrEmpty(listenUrl))
        {
            // Try to parse from accumulated output for older Kestrel messages
            var m2 = Regex.Match(output, @"Now listening on: (?<url>https?://\S+)", RegexOptions.IgnoreCase);
            if (m2.Success)
            {
                listenUrl = m2.Groups["url"].Value.TrimEnd('/');
            }
        }

        try
        {
            Assert.False(string.IsNullOrEmpty(listenUrl), $"Could not determine app listening URL. Output:\n{output}\nStdErr:\n{await process.StandardError.ReadToEndAsync()}");

            // Allow insecure certificates for dev server
            var handler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(20) };

            // Retry until endpoint responds or timeout
            var reqUrl = listenUrl + "/Account/Register";
            string body = string.Empty;
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
            HttpResponseMessage resp = null;
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    resp = await client.GetAsync(reqUrl);
                    body = await resp.Content.ReadAsStringAsync();
                    break;
                }
                catch
                {
                    await Task.Delay(250);
                }
            }

            Assert.NotNull(resp);
            Assert.True(resp.IsSuccessStatusCode, $"GET {reqUrl} returned {(int)resp.StatusCode}.\nBody:\n{body}");
            // Ensure common DI/runtime error symptoms are not present
            Assert.DoesNotContain("IdentityRedirectManager", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Value cannot be null", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Unhandled exception", body, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync();
                }
            }
            catch { }
        }
    }
}
