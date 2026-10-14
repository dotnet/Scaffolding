// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Syncfusion;

/// <summary>
/// Shared base class for end-to-end integration tests of the Syncfusion Blazor Toolkit
/// scaffolder across .NET versions. Each test gets a temporary project with its own
/// <c>NuGet.config</c> (<see cref="ScaffoldCliHelper.SyncfusionTestNuGetConfig"/>) so the
/// machine's feed configuration does not affect package restore.
/// </summary>
[Trait("Suite", "ScaffoldIntegration")]
[Trait("Family", "blazor-syncfusion-toolkit")]
public abstract class SyncfusionBlazorToolkitIntegrationTestsBase : IDisposable
{
    /// <summary>
    /// Name of the scaffolder as invoked on the CLI.
    /// </summary>
    public const string CommandName = "blazor-syncfusion-toolkit";

    protected abstract string TargetFramework { get; }
    protected abstract string TestClassName { get; }

    protected readonly string _testDirectory;
    protected readonly string _testProjectDir;
    protected readonly string _testProjectPath;

    protected SyncfusionBlazorToolkitIntegrationTestsBase()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), TestClassName, Guid.NewGuid().ToString());
        _testProjectDir = Path.Combine(_testDirectory, "TestProject");
        _testProjectPath = Path.Combine(_testProjectDir, "TestProject.csproj");
        Directory.CreateDirectory(_testProjectDir);

        // Fixture-scoped NuGet.config: Syncfusion.Blazor.Toolkit is published to nuget.org, which the
        // repository's own NuGet.config deliberately does not include.
        File.WriteAllText(
            Path.Combine(_testProjectDir, "NuGet.config"),
            ScaffoldCliHelper.SyncfusionTestNuGetConfig);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try { Directory.Delete(_testDirectory, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
        GC.SuppressFinalize(this);
    }

    protected string BlazorWebProjectContent => $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>TestProject</RootNamespace>
  </PropertyGroup>
</Project>";

    protected string BlazorWasmProjectContent => $@"<Project Sdk=""Microsoft.NET.Sdk.BlazorWebAssembly"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>TestProject</RootNamespace>
  </PropertyGroup>
</Project>";

    /// <summary>
    /// Writes a minimal Blazor Web App layout (Components/_Imports.razor,
    /// Components/App.razor, Program.cs with MapRazorComponents) so the
    /// scaffolder has an _Imports host to target. Syncfusion 2.0.0+
    /// requires no stylesheet link, so App.razor stays minimal.
    /// </summary>
    protected void WriteBlazorWebAppScaffold()
    {
        Directory.CreateDirectory(Path.Combine(_testProjectDir, "Components"));
        File.WriteAllText(_testProjectPath, BlazorWebProjectContent);
        File.WriteAllText(
            Path.Combine(_testProjectDir, "Program.cs"),
            ScaffoldCliHelper.GetBlazorProgramCs("TestProject"));
        File.WriteAllText(
            Path.Combine(_testProjectDir, "Components", "_Imports.razor"),
            ScaffoldCliHelper.GetBlazorImportsRazor());
        File.WriteAllText(
            Path.Combine(_testProjectDir, "Components", "App.razor"),
            ScaffoldCliHelper.GetBlazorAppRazor());
        File.WriteAllText(
            Path.Combine(_testProjectDir, "Components", "Routes.razor"),
            ScaffoldCliHelper.GetBlazorRoutesRazor());
    }

    /// <summary>
    /// Builds the generated project under the test project directory
    /// for the current <see cref="TargetFramework"/>. Used by runnable
    /// baseline / outcome verification tests.
    /// </summary>
    protected Task<(int ExitCode, string Output, string Error)> BuildGeneratedAsync()
        => ScaffoldCliHelper.RunBuildForFrameworkAsync(_testProjectDir, TargetFramework);

    /// <summary>
    /// Runs the Syncfusion Blazor Toolkit scaffolder against the
    /// prepared project.
    /// </summary>
    /// <remarks>
    /// The scaffolder always runs under the test runner's TFM (net11.0)
    /// because the dotnet-scaffold tool is only built for net11.0 in
    /// the dev build. The target framework of the generated test app
    /// remains <see cref="TargetFramework"/>.
    /// </remarks>
    protected Task<(int ExitCode, string Output, string Error)> ScaffoldAsync()
        => ScaffoldCliHelper.RunScaffoldAsync(
            ScaffoldCliHelper.GetTestTargetFramework(),
            CommandName,
            "--project",
            _testProjectPath);

    /// <summary>
    /// Asserts that the registration is a standalone call on a declared builder variable
    /// (<c>builder.Services.AddSyncfusionBlazorToolkit();</c>), not a mention of the method
    /// name or a call on the builder-creation expression itself.
    /// </summary>
    protected static void AssertValidServiceRegistration(string programCs)
    {
        Assert.Contains("AddSyncfusionBlazorToolkit", programCs);
        Assert.DoesNotContain(
            "WebApplication.CreateBuilder.Services",
            programCs);
        Assert.DoesNotContain(
            "WebAssemblyHostBuilder.CreateDefault.Services",
            programCs);
        // The standalone call must be present, not just any mention of
        // the method name (e.g. a chained call does not count).
        Assert.Contains("builder.Services.AddSyncfusionBlazorToolkit();", programCs);
    }
}