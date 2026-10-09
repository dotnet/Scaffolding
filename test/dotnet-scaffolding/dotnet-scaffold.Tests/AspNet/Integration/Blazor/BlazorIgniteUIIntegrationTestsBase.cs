// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Scaffolding.CodeModification;
using Microsoft.DotNet.Scaffolding.CodeModification.Helpers;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
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
    [InlineData("igniteUIBlazorMauiChanges.json")]
    [InlineData("igniteUIBlazorThemeChanges.json")]
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

    [Fact]
    public async Task MauiConfig_RegistersServicesAfterBlazorWebViewOutsideDebugBlock()
    {
        // MauiProgram.cs of the 'maui-blazor' template: the registration must not land in the '#if DEBUG' block,
        // whose '#endif' is attached to 'return builder.Build();'.
        const string mauiProgram = "using Microsoft.Extensions.Logging;\n\nnamespace MauiApp1;\n\npublic static class MauiProgram\n{\n\tpublic static MauiApp CreateMauiApp()\n\t{\n\t\tvar builder = MauiApp.CreateBuilder();\n\t\tbuilder\n\t\t\t.UseMauiApp<App>()\n\t\t\t.ConfigureFonts(fonts =>\n\t\t\t{\n\t\t\t\tfonts.AddFont(\"OpenSans-Regular.ttf\", \"OpenSansRegular\");\n\t\t\t});\n\n\t\tbuilder.Services.AddMauiBlazorWebView();\n\n#if DEBUG\n\t\tbuilder.Services.AddBlazorWebViewDeveloperTools();\n\t\tbuilder.Logging.AddDebug();\n#endif\n\n\t\treturn builder.Build();\n\t}\n}\n";
        var projectPath = Path.Combine(_testProjectDir, "MauiApp1.csproj");
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject(ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(),
            "MauiApp1", "MauiApp1", LanguageNames.CSharp, filePath: projectPath));
        var document = workspace.AddDocument(DocumentInfo.Create(DocumentId.CreateNewId(project.Id), "MauiProgram.cs",
            loader: TextLoader.From(TextAndVersion.Create(SourceText.From(mauiProgram), VersionStamp.Create())),
            filePath: Path.Combine(_testProjectDir, "MauiProgram.cs")));
        var codeService = new Mock<ICodeService>();
        codeService.Setup(service => service.GetWorkspaceAsync()).ReturnsAsync(workspace);
        codeService.Setup(service => service.TryApplyChanges(It.IsAny<Solution>()))
            .Returns((Solution solution) => workspace.TryApplyChanges(solution));

        async Task<string> ApplyConfigAsync()
        {
            var config = CodeModifierConfigHelper.GetCodeModifierConfig(GetCodeModificationConfigPath("igniteUIBlazorMauiChanges.json"));
            Assert.NotNull(config);
            Assert.True(await new ProjectModifier(projectPath, codeService.Object, Mock.Of<ILogger>(), config, []).RunAsync());
            return (await workspace.CurrentSolution.GetDocument(document.Id)!.GetTextAsync()).ToString();
        }

        var updated = await ApplyConfigAsync();

        Assert.Contains("using IgniteUI.Blazor.Controls;", updated);
        var webViewIndex = updated.IndexOf("builder.Services.AddMauiBlazorWebView();", StringComparison.Ordinal);
        var registrationIndex = updated.IndexOf("builder.Services.AddIgniteUIBlazor();", StringComparison.Ordinal);
        var debugIndex = updated.IndexOf("#if DEBUG", StringComparison.Ordinal);
        Assert.True(webViewIndex >= 0 && webViewIndex < registrationIndex && registrationIndex < debugIndex,
            $"AddIgniteUIBlazor() should follow AddMauiBlazorWebView() and precede the '#if DEBUG' block.\n{updated}");

        // Re-running the scaffolder must not register the services twice.
        Assert.Single(Regex.Matches(await ApplyConfigAsync(), Regex.Escape("AddIgniteUIBlazor")));
    }

    [Theory]
    [InlineData("Components", "App.razor", true, "<!DOCTYPE html>\n<html>\n<head>\n    <link rel=\"stylesheet\" href=\"@Assets[\"app.css\"]\" />\n    <HeadOutlet />\n</head>\n<body><Routes /></body>\n</html>\n",
        "    <HeadOutlet />\n    <link rel=\"stylesheet\" href=\"@Assets[\"_content/IgniteUI.Blazor/themes/dark/material.css\"]\" />\n</head>")]
    [InlineData("Pages", "_Host.cshtml", true, "@page \"/\"\n<html>\n<head>\n    <link href=\"css/site.css\" rel=\"stylesheet\" />\n</head>\n</html>\n",
        "    <link href=\"css/site.css\" rel=\"stylesheet\" />\n    <link href=\"_content/IgniteUI.Blazor/themes/dark/material.css\" rel=\"stylesheet\" />\n</head>")]
    [InlineData("wwwroot", "index.html", false, "<!DOCTYPE html>\n<html>\n<head>\n    <title>App</title>\n</head>\n<body></body>\n</html>\n",
        "    <title>App</title>\n    <link href=\"_content/IgniteUI.Blazor/themes/dark/material.css\" rel=\"stylesheet\" />\n</head>")]
    public async Task ThemeRecipe_LinksBeforeHeadAndSwapsThemeWithoutDuplicates(string folder, string fileName, bool inWorkspace, string hostPage, string expectedHead)
    {
        // The shared recipe, applied with the values the scaffolder analyzes, for every kind of host page: App.razor and
        // _Host.cshtml as workspace documents, a standalone app's wwwroot/index.html on disk.
        var hostPagePath = Path.Combine(_testProjectDir, folder, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(hostPagePath)!);
        File.WriteAllText(hostPagePath, hostPage);
        var projectPath = _testProjectPath;
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject(ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(),
            "TestProject", "TestProject", LanguageNames.CSharp, filePath: projectPath));
        DocumentId? documentId = null;
        if (inWorkspace)
        {
            var document = project.AddAdditionalDocument(fileName, SourceText.From(hostPage), filePath: hostPagePath);
            documentId = document.Id;
            Assert.True(workspace.TryApplyChanges(document.Project.Solution));
        }

        var codeService = new Mock<ICodeService>();
        codeService.Setup(service => service.GetWorkspaceAsync()).ReturnsAsync(workspace);
        codeService.Setup(service => service.TryApplyChanges(It.IsAny<Solution>()))
            .Returns((Solution solution) => workspace.TryApplyChanges(solution));

        async Task<string> ApplyThemeAsync(string stylesheetPath)
        {
            var current = await ReadHostPageAsync();
            var (options, properties) = IgniteUIBlazorHelper.GetThemeRecipeInputs(_testProjectDir, hostPagePath, current, stylesheetPath);
            var config = CodeModifierConfigHelper.GetCodeModifierConfig(GetCodeModificationConfigPath("igniteUIBlazorThemeChanges.json"));
            Assert.NotNull(config);
            config.EditCodeModifierConfig(properties);
            Assert.True(await new ProjectModifier(projectPath, codeService.Object, Mock.Of<ILogger>(), config, options).RunAsync());
            return await ReadHostPageAsync();
        }

        async Task<string> ReadHostPageAsync()
            => documentId is null
                ? File.ReadAllText(hostPagePath)
                : (await workspace.CurrentSolution.GetAdditionalDocument(documentId)!.GetTextAsync()).ToString();

        const string darkMaterial = "_content/IgniteUI.Blazor/themes/dark/material.css";
        var updated = await ApplyThemeAsync(darkMaterial);
        Assert.Contains(expectedHead, updated);

        // A re-run with the same theme changes nothing; a different theme swaps the path in place.
        Assert.Equal(updated, await ApplyThemeAsync(darkMaterial));
        var swapped = await ApplyThemeAsync("_content/IgniteUI.Blazor/themes/light/fluent.css");
        Assert.Equal(updated.Replace(darkMaterial, "_content/IgniteUI.Blazor/themes/light/fluent.css"), swapped);
        Assert.Single(Regex.Matches(swapped, Regex.Escape("_content/IgniteUI")));
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
    /// (with the default light bootstrap theme).
    /// </summary>
    /// <param name="buildBeforeScaffolding">
    /// Builds (and therefore restores) the project before scaffolding. Pass false to verify that the scaffolder works on
    /// a project that has never been restored.
    /// </param>
    /// <returns>The scaffolder's console output, for additional assertions by the caller.</returns>
    protected async Task<string> ScaffoldAllPackagesAndAssertAsync(bool buildBeforeScaffolding = true)
    {
        if (buildBeforeScaffolding)
        {
            var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
            Assert.True(preExitCode == 0,
                $"Project should build before scaffolding.\nExit code: {preExitCode}\nOutput: {preOutput}\nError: {preError}");
        }

        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework, "blazor-igniteui", "--project", _testProjectPath);
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
