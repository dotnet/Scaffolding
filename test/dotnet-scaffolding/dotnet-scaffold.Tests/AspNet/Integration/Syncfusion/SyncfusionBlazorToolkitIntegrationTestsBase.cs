// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Syncfusion;

/// <summary>
/// Shared base class for end-to-end integration tests of the
/// Syncfusion Blazor Toolkit scaffolder across .NET versions.
/// </summary>
[Trait("Suite", "ScaffoldIntegration")]
[Trait("Family", "syncfusion-blazor-toolkit")]
public abstract class SyncfusionBlazorToolkitIntegrationTestsBase : IDisposable
{
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

    /// <summary>
    /// Writes a minimal Blazor Web App layout (Components/_Imports.razor,
    /// Components/App.razor, Program.cs with MapRazorComponents) so the
    /// scaffolder has both an _Imports host and a theme host to target.
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
}
