// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Syncfusion;

public class SyncfusionBlazorToolkitNet9IntegrationTests : SyncfusionBlazorToolkitIntegrationTestsBase
{
    protected override string TargetFramework => "net9.0";
    protected override string TestClassName => nameof(SyncfusionBlazorToolkitNet9IntegrationTests);

    [Fact]
    public async Task Scaffold_SyncfusionBlazorToolkit_Net9_CliInvocation()
    {
        WriteBlazorWebAppScaffold();

        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework, "syncfusion-blazor-toolkit", "--project", _testProjectPath);
        Assert.True(cliExitCode == 0,
            $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        string csproj = File.ReadAllText(_testProjectPath);
        Assert.Contains("Syncfusion.Blazor.Toolkit", csproj);

        string programCs = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        Assert.Contains("AddSyncfusionBlazorToolkit", programCs);
        Assert.Contains("using Syncfusion.Blazor.Toolkit", programCs);

        string imports = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "_Imports.razor"));
        Assert.Contains("@using Syncfusion.Blazor.Toolkit", imports);

        // Components/App.razor was NOT modified with an external
        // stylesheet link (2.0.0+ ships styles with the assembly).
        string appRazor = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "App.razor"));
        Assert.DoesNotContain("Syncfusion.Blazor.Toolkit/styles/fluent.min.css", appRazor);
    }

    /// <summary>
    /// Regression test: root-level <c>_Imports.razor</c> (no
    /// <c>Components/_Imports.razor</c>) must receive the
    /// <c>@using Syncfusion.Blazor.Toolkit</c> directive on disk. The
    /// scaffolder must not create a <c>Components/_Imports.razor</c> when the
    /// project only has a root one.
    /// </summary>
    [Fact]
    public async Task Scaffold_SyncfusionBlazorToolkit_Net9_RootImportsRazor_WritesUsingOnDisk()
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
    }

    /// <summary>
    /// Standalone Blazor WebAssembly layout on net9: Program.cs uses
    /// <c>WebAssemblyHostBuilder.CreateDefault</c>; the scaffolder must
    /// register services via that anchor.
    /// </summary>
    [Fact]
    public async Task Scaffold_SyncfusionBlazorToolkit_Net9_StandaloneWasm_RegistersAndAddsUsing()
    {
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

        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework, "syncfusion-blazor-toolkit", "--project", _testProjectPath);
        Assert.True(exitCode == 0, $"Output: {output}\nError: {error}");

        string programCs = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        Assert.Contains("AddSyncfusionBlazorToolkit", programCs);
        Assert.Contains("using Syncfusion.Blazor.Toolkit", programCs);

        string imports = File.ReadAllText(Path.Combine(_testProjectDir, "_Imports.razor"));
        Assert.Contains("@using Syncfusion.Blazor.Toolkit", imports);

        string indexHtml = File.ReadAllText(Path.Combine(_testProjectDir, "wwwroot", "index.html"));
        Assert.DoesNotContain("Syncfusion.Blazor.Toolkit/styles/fluent.min.css", indexHtml);
    }

    /// <summary>
    /// Pages/_Imports.razor only (no Components/_Imports.razor): the
    /// scaffolder should find and modify Pages/_Imports.razor on disk.
    /// </summary>
    [Fact]
    public async Task Scaffold_SyncfusionBlazorToolkit_Net9_PagesImportsRazor_WritesUsingOnDisk()
    {
        Directory.CreateDirectory(Path.Combine(_testProjectDir, "Pages"));
        File.WriteAllText(_testProjectPath, BlazorWebProjectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"),
            ScaffoldCliHelper.GetBlazorProgramCs("TestProject"));
        File.WriteAllText(Path.Combine(_testProjectDir, "Pages", "_Imports.razor"),
            "@using Microsoft.AspNetCore.Components\n");

        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework, "syncfusion-blazor-toolkit", "--project", _testProjectPath);
        Assert.True(exitCode == 0, $"Output: {output}\nError: {error}");

        var imports = File.ReadAllText(Path.Combine(_testProjectDir, "Pages", "_Imports.razor"));
        Assert.Contains("@using Syncfusion.Blazor.Toolkit", imports);

        Assert.False(File.Exists(Path.Combine(_testProjectDir, "Components", "_Imports.razor")),
            "Components/_Imports.razor should not be created when the project only has Pages/_Imports.razor.");
    }
}
