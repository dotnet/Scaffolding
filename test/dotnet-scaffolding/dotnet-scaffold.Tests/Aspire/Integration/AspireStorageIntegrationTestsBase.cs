// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Aspire.Integration;

/// <summary>
/// Shared base class for Aspire storage integration tests across .NET versions.
/// Tests validate that the CLI accepts aspire storage commands and validates options correctly.
/// </summary>
[Trait("Suite", "ScaffoldIntegration")]
[Trait("Family", "aspire-storage")]
public abstract class AspireStorageIntegrationTestsBase : IDisposable
{
    protected abstract string TargetFramework { get; }
    protected abstract string TestClassName { get; }

    protected readonly string _testDirectory;
    protected readonly string _appHostDir;
    protected readonly string _appHostProjectPath;
    protected readonly string _workerDir;
    protected readonly string _workerProjectPath;

    protected AspireStorageIntegrationTestsBase()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), TestClassName, Guid.NewGuid().ToString());
        _appHostDir = Path.Combine(_testDirectory, "TestApp.AppHost");
        _appHostProjectPath = Path.Combine(_appHostDir, "TestApp.AppHost.csproj");
        _workerDir = Path.Combine(_testDirectory, "TestApp.Worker");
        _workerProjectPath = Path.Combine(_workerDir, "TestApp.Worker.csproj");
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

    [Fact]
    public async Task AspireStorage_FailsWithMissingType()
    {
        SetupProjects();

        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAspireAsync(
            TargetFramework,
            "storage",
            "--apphost-project", _appHostProjectPath,
            "--project", _workerProjectPath);

        Assert.NotEqual(0, exitCode);
        var combined = output + error;
        if (!combined.Contains("fxr", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Contains("--type", combined, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task AspireStorage_FailsWithInvalidType()
    {
        SetupProjects();

        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAspireAsync(
            TargetFramework,
            "storage",
            "--type", "invalid-storage",
            "--apphost-project", _appHostProjectPath,
            "--project", _workerProjectPath);

        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public async Task AspireStorage_FailsWithMissingAppHostProject()
    {
        SetupProjects();

        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAspireAsync(
            TargetFramework,
            "storage",
            "--type", "azure-storage-blobs",
            "--project", _workerProjectPath);

        Assert.NotEqual(0, exitCode);
        var combined = output + error;
        if (!combined.Contains("fxr", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Contains("--apphost-project", combined, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task AspireStorage_FailsWithMissingWorkerProject()
    {
        SetupProjects();

        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAspireAsync(
            TargetFramework,
            "storage",
            "--type", "azure-storage-blobs",
            "--apphost-project", _appHostProjectPath);

        Assert.NotEqual(0, exitCode);
        var combined = output + error;
        if (!combined.Contains("fxr", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Contains("--project", combined, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData("azure-storage-queues")]
    [InlineData("azure-storage-blobs")]
    [InlineData("azure-data-tables")]
    public async Task AspireStorage_AcceptsValidType(string storageType)
    {
        SetupProjects();

        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAspireAsync(
            TargetFramework,
            "storage",
            "--type", storageType,
            "--apphost-project", _appHostProjectPath,
            "--project", _workerProjectPath);

        var combined = output + error;
        Assert.DoesNotContain("Missing/Invalid --type", combined);
        Assert.DoesNotContain("Missing/Invalid --apphost-project", combined);
        Assert.DoesNotContain("Missing/Invalid --project", combined);
    }
}
