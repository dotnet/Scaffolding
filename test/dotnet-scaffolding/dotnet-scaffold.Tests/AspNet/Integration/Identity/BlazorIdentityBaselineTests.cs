// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Identity;

[Trait("Suite", "ScaffoldIntegration")]
[Trait("Family", "blazor-identity")]
public class BlazorIdentityBaselineTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("net10.0")]
    [InlineData("net11.0")]
    public async Task Scaffold_DefaultBlazorWebApp_MatchesBaseline(string framework)
    {
        var baselines = Path.Combine(ScaffoldCliHelper.GetRepoRoot(), "test", "dotnet-scaffolding", "baselines");
        var scenario = Path.Combine(baselines, "BlazorIdentity", framework);
        var inputScenario = Path.Combine(baselines, "Inputs", framework, "BlazorWebApp");
        var baseline = Path.Combine(scenario, "BaselineApp");
        var workingDirectory = Path.Combine(Path.GetTempPath(), nameof(BlazorIdentityBaselineTests), Guid.NewGuid().ToString("N"));
        var expected = Path.Combine(workingDirectory, "expected");
        var actual = Path.Combine(workingDirectory, "actual");
        var update = Environment.GetEnvironmentVariable("UPDATE_BLAZOR_IDENTITY_BASELINES") == "1";

        Directory.CreateDirectory(workingDirectory);
        try
        {
            File.Copy(Path.Combine(inputScenario, "global.json"), Path.Combine(workingDirectory, "global.json"));
            File.Copy(Path.Combine(baselines, "NuGet.config"), Path.Combine(workingDirectory, "NuGet.config"));
            GeneratedProjectBaseline.CopyProject(baseline, expected);
            await RunAsync(expected, "restore", "--packages", Path.Combine(workingDirectory, "packages"));
            await RunAsync(expected, "build", "--no-restore");
            CreatePinnedPackageSource(workingDirectory);

            GeneratedProjectBaseline.CopyProject(Path.Combine(inputScenario, "BaselineApp"), actual);
            await RunAsync(actual, "build");
            var input = GeneratedProjectBaseline.ReadFiles(actual);

            string[] prerelease = framework == "net11.0" ? ["--prerelease"] : [];
            var result = await ScaffoldCliHelper.RunScaffoldAsync(
                "net11.0", "blazor-identity", [
                    "--project", Path.Combine(actual, "BaselineApp.csproj"),
                    "--dataContext", "ApplicationDbContext",
                    "--dbProvider", "sqlite-efcore",
                    .. prerelease
                ]);
            Assert.True(result.ExitCode == 0, $"Scaffolding failed.\n{result.Output}\n{result.Error}");
            var build = await ScaffoldCliHelper.RunBuildAsync(actual);
            Assert.True(build.ExitCode == 0, $"Generated project build failed.\n{build.Output}\n{build.Error}");

            var generated = GeneratedProjectBaseline.ReadFiles(actual);
            var manifestPath = Path.Combine(scenario, "scaffolded-files.txt");
            if (update)
            {
                foreach (var path in GeneratedProjectBaseline.EnumerateFiles(baseline).ToArray())
                {
                    File.Delete(path);
                }

                GeneratedProjectBaseline.CopyProject(actual, baseline);
                File.WriteAllLines(manifestPath, generated.Keys
                    .Where(path => !input.TryGetValue(path, out var content) || generated[path] != content)
                    .Order(StringComparer.Ordinal));
                output.WriteLine($"Updated {baseline}. Review the source and scaffolded-files.txt diffs before accepting.");
            }

            GeneratedProjectBaseline.AssertMatches(input, GeneratedProjectBaseline.ReadFiles(baseline), generated,
                File.ReadAllLines(manifestPath));
        }
        catch
        {
            // Preserve the actual application and restore/build logs for diagnosing a failed comparison.
            output.WriteLine($"Baseline test artifacts: {workingDirectory}");
            throw;
        }

        Directory.Delete(workingDirectory, recursive: true);
    }

    private async Task RunAsync(string directory, params string[] arguments)
    {
        var result = await ScaffoldCliHelper.RunDotNetAsync(directory, arguments);
        output.WriteLine($"dotnet {string.Join(" ", arguments)}\n{result.Output}\n{result.Error}");
        Assert.True(result.ExitCode == 0, $"dotnet {string.Join(" ", arguments)} failed.\n{result.Output}\n{result.Error}");
    }

    private static void CreatePinnedPackageSource(string workingDirectory)
    {
        // dotnet add package's latest-version lookup does not honor source mapping.
        // Offer only the baseline's restored dependency closure, with no live feeds.
        var feed = Path.Combine(workingDirectory, "pinned-packages");
        Directory.CreateDirectory(feed);
        foreach (var package in Directory.EnumerateFiles(
            Path.Combine(workingDirectory, "packages"), "*.nupkg", SearchOption.AllDirectories))
        {
            File.Copy(package, Path.Combine(feed, Path.GetFileName(package)));
        }

        new XDocument(new XElement("configuration",
            new XElement("packageSources", new XElement("clear"),
                new XElement("add", new XAttribute("key", "baseline"), new XAttribute("value", feed)))))
            .Save(Path.Combine(workingDirectory, "NuGet.config"));
    }
}
