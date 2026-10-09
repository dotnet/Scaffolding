// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Syncfusion;

public class SyncfusionBlazorToolkitNet11IntegrationTests : SyncfusionBlazorToolkitIntegrationTestsBase
{
    protected override string TargetFramework => "net11.0";
    protected override string TestClassName => nameof(SyncfusionBlazorToolkitNet11IntegrationTests);

    [Fact]
    public async Task Scaffold_SyncfusionBlazorToolkit_Net11_CliInvocation()
    {
        WriteBlazorWebAppScaffold();

        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework, "syncfusion-blazor-toolkit", "--project", _testProjectPath);
        Assert.True(cliExitCode == 0,
            $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        // Package reference was added.
        string csproj = File.ReadAllText(_testProjectPath);
        Assert.Contains("Syncfusion.Blazor.Toolkit", csproj);

        // Program.cs was modified to register services and import the namespace.
        string programCs = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        Assert.Contains("AddSyncfusionBlazorToolkit", programCs);
        Assert.Contains("using Syncfusion.Blazor.Toolkit", programCs);

        // Components/_Imports.razor received the @using directive.
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
    public async Task Scaffold_SyncfusionBlazorToolkit_Net11_RootImportsRazor_WritesUsingOnDisk()
    {
        File.WriteAllText(_testProjectPath, BlazorWebProjectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"),
            ScaffoldCliHelper.GetBlazorProgramCs("TestProject"));
        File.WriteAllText(Path.Combine(_testProjectDir, "_Imports.razor"),
            "@using Microsoft.AspNetCore.Components\n");
        Directory.CreateDirectory(Path.Combine(_testProjectDir, "wwwroot"));
        File.WriteAllText(Path.Combine(_testProjectDir, "wwwroot", "index.html"),
            "<!DOCTYPE html><html><head></head><body></body></html>");

        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework, "syncfusion-blazor-toolkit", "--project", _testProjectPath);
        Assert.True(exitCode == 0, $"Output: {output}\nError: {error}");

        var imports = File.ReadAllText(Path.Combine(_testProjectDir, "_Imports.razor"));
        Assert.Contains("@using Syncfusion.Blazor.Toolkit", imports);

        Assert.False(File.Exists(Path.Combine(_testProjectDir, "Components", "_Imports.razor")),
            "Components/_Imports.razor should not be created when the project only has a root _Imports.razor.");
    }

    /// <summary>
    /// Standalone Blazor WebAssembly layout on net11: Program.cs uses
    /// <c>WebAssemblyHostBuilder.CreateDefault</c>; the scaffolder must
    /// register services via that anchor.
    /// </summary>
    [Fact]
    public async Task Scaffold_SyncfusionBlazorToolkit_Net11_StandaloneWasm_RegistersAndAddsUsing()
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
}
