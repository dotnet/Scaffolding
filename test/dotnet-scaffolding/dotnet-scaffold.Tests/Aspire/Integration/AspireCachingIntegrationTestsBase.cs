// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Aspire.Integration;

/// <summary>
/// Shared base class for Aspire caching integration tests across .NET versions.
/// Tests validate that the CLI accepts aspire caching commands and validates options correctly.
/// Full end-to-end scaffolding requires the Aspire workload; these tests focus on CLI parsing and validation.
/// </summary>
[Trait("Suite", "ScaffoldIntegration")]
[Trait("Family", "aspire-caching")]
public abstract class AspireCachingIntegrationTestsBase : IDisposable
{
    protected abstract string TargetFramework { get; }
    protected abstract string TestClassName { get; }

    /// <summary>Whether to pass <c>--prerelease</c> to the scaffolder and restore preview packages (net11.0).</summary>
    protected bool Prerelease => TargetFramework == "net11.0";

    protected readonly string _testDirectory;
    protected readonly string _appHostDir;
    protected readonly string _appHostProjectPath;
    protected readonly string _workerDir;
    protected readonly string _workerProjectPath;

    protected AspireCachingIntegrationTestsBase()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), TestClassName, Guid.NewGuid().ToString());
        _appHostDir = Path.Combine(_testDirectory, "TestApp.AppHost");
        _appHostProjectPath = Path.Combine(_appHostDir, "TestApp.AppHost.csproj");
        _workerDir = Path.Combine(_testDirectory, "TestApp.Web");
        _workerProjectPath = Path.Combine(_workerDir, "TestApp.Web.csproj");
        Directory.CreateDirectory(_appHostDir);
        Directory.CreateDirectory(_workerDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try { Directory.Delete(_testDirectory, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
    }

    protected string AppHostProjectContent =>
        ScaffoldCliHelper.GetAspireAppHostProjectContent(TargetFramework, @"..\TestApp.Web\TestApp.Web.csproj");

    protected string WorkerProjectContent =>
        ScaffoldCliHelper.GetAspireWorkerProjectContent(TargetFramework);

    protected static string AppHostProgramCs =>
        ScaffoldCliHelper.GetAspireAppHostProgramCs("TestApp_Web");

    protected static string WorkerProgramCs =>
        ScaffoldCliHelper.GetAspireWorkerProgramCs();

    protected void SetupProjects()
    {
        ScaffoldCliHelper.WriteAspireNuGetConfig(_testDirectory, TargetFramework);
        File.WriteAllText(_appHostProjectPath, AppHostProjectContent);
        File.WriteAllText(Path.Combine(_appHostDir, "Program.cs"), AppHostProgramCs);
        File.WriteAllText(_workerProjectPath, WorkerProjectContent);
        File.WriteAllText(Path.Combine(_workerDir, "Program.cs"), WorkerProgramCs);
    }

    /// <summary>
    /// End-to-end: the Aspire caching scaffold applies the AppHost/worker code changes and the
    /// solution still builds afterward. Mirrors the AspNet build-before/scaffold/build-after pattern.
    /// </summary>
    [Fact]
    public async Task AspireCaching_ScaffoldsAndBuilds()
    {
        SetupProjects();

        // Building the AppHost also builds the referenced worker project.
        var (beforeExitCode, _, beforeError) = await ScaffoldCliHelper.RunBuildForFrameworkAsync(_appHostDir, TargetFramework);
        Assert.True(beforeExitCode == 0, $"AppHost should build before scaffolding. Error: {beforeError}");

        var args = new System.Collections.Generic.List<string>
        {
            "--type", "redis",
            "--apphost-project", _appHostProjectPath,
            "--project", _workerProjectPath
        };
        if (Prerelease)
        {
            args.Add("--prerelease");
        }

        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAspireAsync(
            TargetFramework,
            "caching",
            args.ToArray());
        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        Assert.False(cliOutput.Contains("error: NU"),
            $"Scaffolding should not produce NuGet errors for {TargetFramework}.\nOutput: {cliOutput}");

        // Assert the code modifications were applied to both projects.
        var appHostProgram = File.ReadAllText(Path.Combine(_appHostDir, "Program.cs"));
        Assert.Contains("AddRedis", appHostProgram);
        var workerProgram = File.ReadAllText(Path.Combine(_workerDir, "Program.cs"));
        Assert.Contains("AddRedisClient", workerProgram);

        // Verify the project still builds after scaffolding.
        var (afterExitCode, afterOutput, afterError) = await ScaffoldCliHelper.RunBuildForFrameworkAsync(_appHostDir, TargetFramework);
        Assert.True(afterExitCode == 0, $"Project should still build after scaffolding.\nOutput: {afterOutput}\nError: {afterError}");
    }

    [Fact]
    public async Task AspireCaching_FailsWithMissingType()
    {
        SetupProjects();

        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAspireAsync(
            TargetFramework,
            "caching",
            "--apphost-project", _appHostProjectPath,
            "--project", _workerProjectPath);

        Assert.NotEqual(0, exitCode);
        var combined = output + error;
        // Only assert specific validation text when the tool actually ran
        // (host/fxr errors mean the tool never started, e.g. missing Arcade SDK on local dev)
        if (!combined.Contains("fxr", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Contains("--type", combined, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task AspireCaching_FailsWithInvalidType()
    {
        SetupProjects();

        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAspireAsync(
            TargetFramework,
            "caching",
            "--type", "invalid-type",
            "--apphost-project", _appHostProjectPath,
            "--project", _workerProjectPath);

        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public async Task AspireCaching_FailsWithMissingAppHostProject()
    {
        SetupProjects();

        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAspireAsync(
            TargetFramework,
            "caching",
            "--type", "redis",
            "--project", _workerProjectPath);

        Assert.NotEqual(0, exitCode);
        var combined = output + error;
        if (!combined.Contains("fxr", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Contains("--apphost-project", combined, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task AspireCaching_FailsWithMissingWorkerProject()
    {
        SetupProjects();

        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAspireAsync(
            TargetFramework,
            "caching",
            "--type", "redis",
            "--apphost-project", _appHostProjectPath);

        Assert.NotEqual(0, exitCode);
        var combined = output + error;
        if (!combined.Contains("fxr", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Contains("--project", combined, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData("redis")]
    [InlineData("redis-with-output-caching")]
    public async Task AspireCaching_AcceptsValidType(string cachingType)
    {
        SetupProjects();

        // The command should pass validation (type + apphost + project all provided).
        // It may still fail in later steps if Aspire workload is not installed,
        // but the exit code or output should NOT contain a validation error about --type.
        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAspireAsync(
            TargetFramework,
            "caching",
            "--type", cachingType,
            "--apphost-project", _appHostProjectPath,
            "--project", _workerProjectPath);

        var combined = output + error;
        Assert.DoesNotContain("Missing/Invalid --type", combined);
        Assert.DoesNotContain("Missing/Invalid --apphost-project", combined);
        Assert.DoesNotContain("Missing/Invalid --project", combined);
    }
}

