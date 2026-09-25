// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration;

/// <summary>
/// Shared base class for Ignite UI for Blazor ('blazor-igniteui') integration tests across .NET versions.
/// Verifies the code modification configs shipped for the target framework and provides the
/// fixtures used by the CLI invocation tests in the per-framework subclasses.
/// </summary>
[Trait("Suite", "ScaffoldIntegration")]
[Trait("Family", "blazor-igniteui")]
public abstract class BlazorIgniteUIIntegrationTestsBase : IDisposable
{
    protected abstract string TargetFramework { get; }
    protected abstract string TestClassName { get; }

    protected const string LitePackageName = "IgniteUI.Blazor.Lite";
    protected const string GridLitePackageName = "IgniteUI.Blazor.GridLite";
    protected const string ControlsUsing = "@using IgniteUI.Blazor.Controls";
    protected const string LiteBootstrapLightStylesheet = "_content/IgniteUI.Blazor/themes/light/bootstrap.css";

    protected readonly string _testDirectory;
    protected readonly string _testProjectDir;
    protected readonly string _testProjectPath;

    protected BlazorIgniteUIIntegrationTestsBase()
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
    }

    protected string ProjectContent => $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>";

    #region Code Modification Configs

    [Theory]
    [InlineData("igniteUIBlazorChanges.json")]
    [InlineData("igniteUIBlazorWasmChanges.json")]
    public void CodeModificationConfig_ExistsForTargetFramework(string configFileName)
    {
        var configPath = GetCodeModificationConfigPath(configFileName);
        Assert.True(File.Exists(configPath), $"{configFileName} should exist for {TargetFramework} at '{configPath}'");
    }

    [Theory]
    [InlineData("igniteUIBlazorChanges.json")]
    [InlineData("igniteUIBlazorWasmChanges.json")]
    public void CodeModificationConfig_RegistersIgniteUIServicesInProgramCs(string configFileName)
    {
        var content = File.ReadAllText(GetCodeModificationConfigPath(configFileName));

        Assert.False(string.IsNullOrWhiteSpace(content), $"{configFileName} should not be empty");
        Assert.Contains("\"Program.cs\"", content);
        Assert.Contains("AddIgniteUIBlazor()", content);
        Assert.Contains("IgniteUI.Blazor.Controls", content);
    }

    [Fact]
    public void HostedConfig_InsertsBeforeAppBuild()
    {
        var content = File.ReadAllText(GetCodeModificationConfigPath("igniteUIBlazorChanges.json"));
        Assert.Contains("var app = WebApplication.CreateBuilder.Build();", content);
        Assert.Contains("WebApplication.CreateBuilder.Services.AddIgniteUIBlazor()", content);
    }

    [Fact]
    public void WasmConfig_InsertsBeforeRunAsync()
    {
        var content = File.ReadAllText(GetCodeModificationConfigPath("igniteUIBlazorWasmChanges.json"));
        Assert.Contains("await builder.Build().RunAsync();", content);
        Assert.Contains("builder.Services.AddIgniteUIBlazor()", content);
    }

    #endregion

    #region Helper Methods

    protected static string GetActualTemplatesBasePath()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        var basePath = Path.Combine(assemblyDirectory!, "..", "..", "..", "..", "..", "src", "dotnet-scaffolding", "dotnet-scaffold", "AspNet", "Templates");
        return Path.GetFullPath(basePath);
    }

    protected string GetCodeModificationConfigPath(string configFileName)
        => Path.Combine(GetActualTemplatesBasePath(), TargetFramework, "CodeModificationConfigs", configFileName);

    protected Task<(int ExitCode, string Output, string Error)> RunBuildAsync(string workingDirectory)
        => ScaffoldCliHelper.RunBuildForFrameworkAsync(workingDirectory, TargetFramework);

    /// <summary>
    /// Writes a minimal Blazor Web App (server project) into the test project directory.
    /// </summary>
    protected void SetupBlazorWebAppProject(string? extraProjectXml = null)
    {
        var projectContent = extraProjectXml is null
            ? ProjectContent
            : ProjectContent.Replace("</PropertyGroup>", $"{extraProjectXml}\n  </PropertyGroup>");
        File.WriteAllText(_testProjectPath, projectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), ScaffoldCliHelper.GetBlazorProgramCs("TestProject"));
        ScaffoldCliHelper.SetupBlazorProjectStructure(_testProjectDir);
    }

    /// <summary>
    /// Runs the scaffolder and asserts the Blazor Web App was wired up for both Ignite UI packages
    /// ('--package All' with the default light bootstrap theme).
    /// </summary>
    /// <returns>The scaffolder's console output, for additional assertions by the caller.</returns>
    protected async Task<string> ScaffoldAllPackagesAndAssertAsync(params string[] extraCliArgs)
    {
        var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0,
            $"Project should build before scaffolding.\nExit code: {preExitCode}\nOutput: {preOutput}\nError: {preError}");

        string[] args =
        [
            "--project", _testProjectPath,
            "--package", "All",
            .. extraCliArgs
        ];
        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(TargetFramework, "blazor-igniteui", args);
        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        // packages
        var projectContent = File.ReadAllText(_testProjectPath);
        Assert.Contains(LitePackageName, projectContent);
        Assert.Contains(GridLitePackageName, projectContent);

        // service registration
        var programContent = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        Assert.Contains("using IgniteUI.Blazor.Controls;", programContent);
        Assert.Contains("builder.Services.AddIgniteUIBlazor();", programContent);
        Assert.True(programContent.IndexOf("AddIgniteUIBlazor", StringComparison.Ordinal) < programContent.IndexOf("var app = builder.Build();", StringComparison.Ordinal),
            $"AddIgniteUIBlazor() should be registered before the app is built.\n{programContent}");

        // _Imports.razor
        var importsContent = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "_Imports.razor"));
        Assert.Contains(ControlsUsing, importsContent);

        // theme stylesheet in the host page
        var appRazorContent = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "App.razor"));
        Assert.Contains(LiteBootstrapLightStylesheet, appRazorContent);
        Assert.Contains("rel=\"stylesheet\"", appRazorContent);

        Assert.False(cliOutput.Contains("error: NU"),
            $"Scaffolding should not produce NuGet errors for {TargetFramework}.\nOutput: {cliOutput}");
        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(postExitCode == 0,
            $"Project should build after scaffolding.\nExit code: {postExitCode}\nOutput: {postOutput}\nError: {postError}");

        return cliOutput;
    }

    #endregion
}
