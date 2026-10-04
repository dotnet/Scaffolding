// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;

internal delegate void PrepareBaselineInput(string expectedProjectDirectory, string inputProjectDirectory);

internal static class ScaffolderBaselineRunner
{
    public static async Task RunAsync(
        ITestOutputHelper output,
        string scaffolder,
        string framework,
        string template,
        Func<string, Task<(int ExitCode, string Output, string Error)>> scaffold,
        PrepareBaselineInput? prepareInput = null)
    {
        var repoRoot = ScaffoldCliHelper.GetRepoRoot();
        var baselines = Path.Combine(repoRoot, "test", "dotnet-scaffolding", "baselines");
        var baseline = Path.Combine(baselines, scaffolder, framework, template);
        var input = Path.Combine(baselines, "Inputs", framework, template);
        var workingDirectory = Path.Combine(Path.GetTempPath(), nameof(ScaffolderBaselineRunner), Guid.NewGuid().ToString("N"));
        var expected = Path.Combine(workingDirectory, "expected");
        var actual = Path.Combine(workingDirectory, "actual");

        Directory.CreateDirectory(workingDirectory);
        try
        {
            File.Copy(Path.Combine(repoRoot, "global.json"), Path.Combine(workingDirectory, "global.json"));
            File.Copy(Path.Combine(repoRoot, "NuGet.config"), Path.Combine(workingDirectory, "NuGet.config"));
            await ScaffoldCliHelper.UseInstalledSdkAsync(workingDirectory, framework);
            GeneratedProjectBaseline.CopyProject(baseline, expected);
            await RunDotNetAsync(output, expected, "restore");
            await RunDotNetAsync(output, expected, "build", "--no-restore");

            GeneratedProjectBaseline.CopyProject(input, actual);
            prepareInput?.Invoke(expected, actual);
            await RunDotNetAsync(output, actual, "build");

            var result = await scaffold(actual);
            Assert.True(result.ExitCode == 0, $"Scaffolding failed.\n{result.Output}\n{result.Error}");
            var build = await ScaffoldCliHelper.RunBuildAsync(actual);
            Assert.True(build.ExitCode == 0, $"Generated project build failed.\n{build.Output}\n{build.Error}");

            GeneratedProjectBaseline.AssertMatches(
                GeneratedProjectBaseline.ReadFiles(baseline), GeneratedProjectBaseline.ReadFiles(actual));
        }
        catch
        {
            output.WriteLine($"Baseline test artifacts: {workingDirectory}");
            throw;
        }

        Directory.Delete(workingDirectory, recursive: true);
    }

    private static async Task RunDotNetAsync(ITestOutputHelper output, string directory, params string[] arguments)
    {
        var result = await ScaffoldCliHelper.RunDotNetAsync(directory, arguments);
        output.WriteLine($"dotnet {string.Join(" ", arguments)}\n{result.Output}\n{result.Error}");
        Assert.True(result.ExitCode == 0, $"dotnet {string.Join(" ", arguments)} failed.\n{result.Output}\n{result.Error}");
    }
}
