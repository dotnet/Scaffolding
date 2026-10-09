// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Syncfusion;

public class SyncfusionBlazorToolkitNet8IntegrationTests : SyncfusionBlazorToolkitIntegrationTestsBase
{
    protected override string TargetFramework => "net8.0";
    protected override string TestClassName => nameof(SyncfusionBlazorToolkitNet8IntegrationTests);

    [Fact]
    public async Task Scaffold_SyncfusionBlazorToolkit_Net8_CliInvocation()
    {
        // Arrange — write a complete Blazor Web App layout.
        WriteBlazorWebAppScaffold();

        // Act — invoke CLI scaffolder.
        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "syncfusion-blazor-toolkit",
            "--project", _testProjectPath);
        Assert.True(cliExitCode == 0,
            $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        // Assert — package reference was added.
        string csproj = File.ReadAllText(_testProjectPath);
        Assert.Contains("Syncfusion.Blazor.Toolkit", csproj);

        // Assert — Program.cs was modified to register services and import the namespace.
        string programCs = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        Assert.Contains("AddSyncfusionBlazorToolkit", programCs);
        Assert.Contains("using Syncfusion.Blazor.Toolkit", programCs);

        // Assert — Components/_Imports.razor received the @using directive.
        string imports = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "_Imports.razor"));
        Assert.Contains("@using Syncfusion.Blazor.Toolkit", imports);

        // Assert — Components/App.razor was NOT modified with an external
        // stylesheet link. Syncfusion.Blazor.Toolkit 2.0.0+ ships styles
        // with the assembly and activates them via
        // AddSyncfusionBlazorToolkit(), so no <link> tag is required.
        string appRazor = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "App.razor"));
        Assert.DoesNotContain("Syncfusion.Blazor.Toolkit/styles/fluent.min.css", appRazor);
    }

    [Fact]
    public async Task Scaffold_SyncfusionBlazorToolkit_Net8_IsIdempotent()
    {
        WriteBlazorWebAppScaffold();

        // First run.
        var (firstExitCode, firstOutput, firstError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework, "syncfusion-blazor-toolkit", "--project", _testProjectPath);
        Assert.True(firstExitCode == 0,
            $"First CLI run should succeed.\nOutput: {firstOutput}\nError: {firstError}");

        // Second run — should be a no-op.
        var (secondExitCode, secondOutput, secondError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework, "syncfusion-blazor-toolkit", "--project", _testProjectPath);
        Assert.True(secondExitCode == 0,
            $"Second CLI run should succeed (idempotent).\nOutput: {secondOutput}\nError: {secondError}");

        // No double-added @using lines.
        string imports = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "_Imports.razor"));
        int usingCount = System.Text.RegularExpressions.Regex.Matches(
            imports,
            "@using Syncfusion\\.Blazor\\.Toolkit").Count;
        Assert.Equal(1, usingCount);

        // Program.cs contains a single registration.
        string programCs = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        int regCount = System.Text.RegularExpressions.Regex.Matches(
            programCs,
            "AddSyncfusionBlazorToolkit").Count;
        Assert.Equal(1, regCount);

        // Components/App.razor must not contain an external theme
        // stylesheet link (2.0.0+ ships styles with the assembly).
        string appRazor = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "App.razor"));
        Assert.DoesNotContain("Syncfusion.Blazor.Toolkit/styles/fluent.min.css", appRazor);
    }

    /// <summary>
    /// Regression test for the bug where the scaffolder would silently skip
    /// the @using directive when the project only hosts a root-level
    /// <c>_Imports.razor</c> (no <c>Components/_Imports.razor</c>). With the
    /// exact-relative-path resolution in <c>RoslynExtensions</c> and the
    /// always-on disk-write fallback in <c>ProjectModifier</c>, the root
    /// file must be modified on disk regardless of which AdditionalDocument
    /// the workspace returns.
    /// </summary>
    [Fact]
    public async Task Scaffold_SyncfusionBlazorToolkit_Net8_RootImportsRazor_WritesUsingOnDisk()
    {
        // Arrange — classic layout: _Imports at project root, no Components/_Imports.
        File.WriteAllText(_testProjectPath, BlazorWebProjectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"),
            ScaffoldCliHelper.GetBlazorProgramCs("TestProject"));
        File.WriteAllText(Path.Combine(_testProjectDir, "_Imports.razor"),
            "@using Microsoft.AspNetCore.Components\n");
        Directory.CreateDirectory(Path.Combine(_testProjectDir, "wwwroot"));
        File.WriteAllText(Path.Combine(_testProjectDir, "wwwroot", "index.html"),
            "<!DOCTYPE html><html><head></head><body></body></html>");

        // Act
        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework, "syncfusion-blazor-toolkit", "--project", _testProjectPath);
        Assert.True(exitCode == 0, $"Output: {output}\nError: {error}");

        // Assert — on disk, not only summary text
        var imports = File.ReadAllText(Path.Combine(_testProjectDir, "_Imports.razor"));
        Assert.Contains("@using Syncfusion.Blazor.Toolkit", imports);

        // The Components/_Imports.razor file must NOT have been created or
        // modified, since the project only has the root one.
        Assert.False(File.Exists(Path.Combine(_testProjectDir, "Components", "_Imports.razor")),
            "Components/_Imports.razor should not be created when the project only has a root _Imports.razor.");

        // No external stylesheet link should be added to index.html.
        var indexHtml = File.ReadAllText(Path.Combine(_testProjectDir, "wwwroot", "index.html"));
        Assert.DoesNotContain("Syncfusion.Blazor.Toolkit/styles/fluent.min.css", indexHtml);
    }

    /// <summary>
    /// Standalone Blazor WebAssembly layout: root <c>_Imports.razor</c>
    /// (no Components folder) and <c>wwwroot/index.html</c> as the document
    /// host. The scaffolder must register services in <c>Program.cs</c>
    /// via the <c>WebAssemblyHostBuilder.CreateDefault</c> anchor and add
    /// the @using directive to the root <c>_Imports.razor</c> on disk.
    /// </summary>
    [Fact]
    public async Task Scaffold_SyncfusionBlazorToolkit_Net8_StandaloneWasm_RegistersAndAddsUsing()
    {
        // Arrange — standalone WASM: SDK = BlazorWebAssembly, root
        // _Imports.razor, wwwroot/index.html, Program.cs with
        // WebAssemblyHostBuilder.CreateDefault.
        File.WriteAllText(_testProjectPath,
            "<Project Sdk=\"Microsoft.NET.Sdk.BlazorWebAssembly\">" +
            $"<PropertyGroup><TargetFramework>{TargetFramework}</TargetFramework>" +
            "<ImplicitUsings>enable</ImplicitUsings>" +
            "<RootNamespace>TestProject</RootNamespace></PropertyGroup></Project>");
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"),
            "using TestProject;\n" +
            "var builder = WebAssemblyHostBuilder.CreateDefault(args);\n" +
            "builder.RootComponents.Add<App>(\"#app\");\n" +
            "await builder.Build().RunAsync();\n");
        File.WriteAllText(Path.Combine(_testProjectDir, "_Imports.razor"),
            "@using Microsoft.AspNetCore.Components\n");
        Directory.CreateDirectory(Path.Combine(_testProjectDir, "wwwroot"));
        File.WriteAllText(Path.Combine(_testProjectDir, "wwwroot", "index.html"),
            "<!DOCTYPE html><html><head><title>Test</title></head><body></body></html>");

        // Act
        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework, "syncfusion-blazor-toolkit", "--project", _testProjectPath);
        Assert.True(exitCode == 0, $"Output: {output}\nError: {error}");

        // Assert — Program.cs received the AddSyncfusionBlazorToolkit call.
        string programCs = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        Assert.Contains("AddSyncfusionBlazorToolkit", programCs);
        Assert.Contains("using Syncfusion.Blazor.Toolkit", programCs);

        // Assert — root _Imports.razor received the @using directive.
        string imports = File.ReadAllText(Path.Combine(_testProjectDir, "_Imports.razor"));
        Assert.Contains("@using Syncfusion.Blazor.Toolkit", imports);

        // Assert — No external stylesheet link added to index.html.
        string indexHtml = File.ReadAllText(Path.Combine(_testProjectDir, "wwwroot", "index.html"));
        Assert.DoesNotContain("Syncfusion.Blazor.Toolkit/styles/fluent.min.css", indexHtml);
    }

    /// <summary>
    /// Pages/_Imports.razor only (no Components/_Imports.razor): the
    /// scaffolder should find and modify Pages/_Imports.razor on disk,
    /// not create a new Components/_Imports.razor.
    /// </summary>
    [Fact]
    public async Task Scaffold_SyncfusionBlazorToolkit_Net8_PagesImportsRazor_WritesUsingOnDisk()
    {
        // Arrange — Pages/_Imports.razor only.
        Directory.CreateDirectory(Path.Combine(_testProjectDir, "Pages"));
        File.WriteAllText(_testProjectPath, BlazorWebProjectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"),
            ScaffoldCliHelper.GetBlazorProgramCs("TestProject"));
        File.WriteAllText(Path.Combine(_testProjectDir, "Pages", "_Imports.razor"),
            "@using Microsoft.AspNetCore.Components\n");

        // Act
        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework, "syncfusion-blazor-toolkit", "--project", _testProjectPath);
        Assert.True(exitCode == 0, $"Output: {output}\nError: {error}");

        // Assert — Pages/_Imports.razor received the @using directive.
        var imports = File.ReadAllText(Path.Combine(_testProjectDir, "Pages", "_Imports.razor"));
        Assert.Contains("@using Syncfusion.Blazor.Toolkit", imports);

        // The Components/_Imports.razor file must NOT have been created.
        Assert.False(File.Exists(Path.Combine(_testProjectDir, "Components", "_Imports.razor")),
            "Components/_Imports.razor should not be created when the project only has Pages/_Imports.razor.");
    }
}
