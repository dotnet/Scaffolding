// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Aspire.Integration;

/// <summary>
/// Shared base class for Aspire database integration tests across .NET versions.
/// Tests validate that the CLI accepts aspire database commands and validates options correctly.
/// </summary>
[Trait("Suite", "ScaffoldIntegration")]
[Trait("Family", "aspire-database")]
public abstract class AspireDatabaseIntegrationTestsBase : IDisposable
{
    private const string SkipReason = "Aspire tests on separate branch";

    protected abstract string TargetFramework { get; }
    protected abstract string TestClassName { get; }

    protected readonly string _testDirectory;
    protected readonly string _appHostDir;
    protected readonly string _appHostProjectPath;
    protected readonly string _workerDir;
    protected readonly string _workerProjectPath;

    protected AspireDatabaseIntegrationTestsBase()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), TestClassName, Guid.NewGuid().ToString());
        _appHostDir = Path.Combine(_testDirectory, "TestApp.AppHost");
        _appHostProjectPath = Path.Combine(_appHostDir, "TestApp.AppHost.csproj");
        _workerDir = Path.Combine(_testDirectory, "TestApp.Api");
        _workerProjectPath = Path.Combine(_workerDir, "TestApp.Api.csproj");
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

    protected string AppHostProjectContent => $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>{TargetFramework}</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsAspireHost>true</IsAspireHost>
  </PropertyGroup>
</Project>";

    protected string WorkerProjectContent => $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>";

    protected static string AppHostProgramCs => @"var builder = DistributedApplication.CreateBuilder(args);
builder.Build().Run();
";

    protected static string WorkerProgramCs => @"var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
var app = builder.Build();
app.Run();
";

    protected void SetupProjects()
    {
        File.WriteAllText(_appHostProjectPath, AppHostProjectContent);
        File.WriteAllText(Path.Combine(_appHostDir, "Program.cs"), AppHostProgramCs);
        File.WriteAllText(_workerProjectPath, WorkerProjectContent);
        File.WriteAllText(Path.Combine(_workerDir, "Program.cs"), WorkerProgramCs);
    }

    [Fact(Skip = SkipReason)]
    public async Task AspireDatabase_FailsWithMissingType()
    {
        SetupProjects();

        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAspireAsync(
            TargetFramework,
            "database",
            "--apphost-project", _appHostProjectPath,
            "--project", _workerProjectPath);

        Assert.NotEqual(0, exitCode);
        var combined = output + error;
        if (!combined.Contains("fxr", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Contains("--type", combined, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact(Skip = SkipReason)]
    public async Task AspireDatabase_FailsWithInvalidType()
    {
        SetupProjects();

        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAspireAsync(
            TargetFramework,
            "database",
            "--type", "invalid-db",
            "--apphost-project", _appHostProjectPath,
            "--project", _workerProjectPath);

        Assert.NotEqual(0, exitCode);
    }

    [Fact(Skip = SkipReason)]
    public async Task AspireDatabase_FailsWithMissingAppHostProject()
    {
        SetupProjects();

        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAspireAsync(
            TargetFramework,
            "database",
            "--type", "sqlserver-efcore",
            "--project", _workerProjectPath);

        Assert.NotEqual(0, exitCode);
        var combined = output + error;
        if (!combined.Contains("fxr", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Contains("--apphost-project", combined, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact(Skip = SkipReason)]
    public async Task AspireDatabase_FailsWithMissingWorkerProject()
    {
        SetupProjects();

        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAspireAsync(
            TargetFramework,
            "database",
            "--type", "sqlserver-efcore",
            "--apphost-project", _appHostProjectPath);

        Assert.NotEqual(0, exitCode);
        var combined = output + error;
        if (!combined.Contains("fxr", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Contains("--project", combined, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory(Skip = SkipReason)]
    [InlineData("npgsql-efcore")]
    [InlineData("sqlserver-efcore")]
    public async Task AspireDatabase_AcceptsValidType(string dbType)
    {
        SetupProjects();

        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAspireAsync(
            TargetFramework,
            "database",
            "--type", dbType,
            "--apphost-project", _appHostProjectPath,
            "--project", _workerProjectPath);

        var combined = output + error;
        Assert.DoesNotContain("Missing/Invalid --type", combined);
        Assert.DoesNotContain("Missing/Invalid --apphost-project", combined);
        Assert.DoesNotContain("Missing/Invalid --project", combined);
    }
}
