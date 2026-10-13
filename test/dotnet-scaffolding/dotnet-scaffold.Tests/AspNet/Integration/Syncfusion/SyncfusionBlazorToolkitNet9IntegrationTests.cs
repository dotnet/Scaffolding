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
    public async Task Scaffold_BlazorSyncfusionToolkit_Net9_CliInvocation_AddsPackageServiceAndImport()
    {
        WriteBlazorWebAppScaffold();

        var (cliExitCode, cliOutput, cliError) = await ScaffoldAsync();
        Assert.True(cliExitCode == 0,
            $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        string csproj = File.ReadAllText(_testProjectPath);
        Assert.Contains("Syncfusion.Blazor.Toolkit", csproj);

        string programCs = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        Assert.Contains("AddSyncfusionBlazorToolkit", programCs);
        Assert.Contains("using Syncfusion.Blazor.Toolkit", programCs);

        string imports = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "_Imports.razor"));
        Assert.Contains("@using Syncfusion.Blazor.Toolkit", imports);

        string appRazor = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "App.razor"));
        Assert.DoesNotContain("Syncfusion.Blazor.Toolkit/styles/fluent.min.css", appRazor);
    }

    [Fact]
    public async Task Scaffold_BlazorSyncfusionToolkit_Net9_RootImportsRazor_WritesUsingOnDisk()
    {
        File.WriteAllText(_testProjectPath, BlazorWebProjectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"),
            ScaffoldCliHelper.GetBlazorProgramCs("TestProject"));
        File.WriteAllText(Path.Combine(_testProjectDir, "_Imports.razor"),
            "@using Microsoft.AspNetCore.Components\n");
        Directory.CreateDirectory(Path.Combine(_testProjectDir, "wwwroot"));
        File.WriteAllText(Path.Combine(_testProjectDir, "wwwroot", "index.html"),
            "<!DOCTYPE html><html><head></head><body></body></html>");

        var (exitCode, output, error) = await ScaffoldAsync();
        Assert.True(exitCode == 0, $"Output: {output}\nError: {error}");

        var imports = File.ReadAllText(Path.Combine(_testProjectDir, "_Imports.razor"));
        Assert.Contains("@using Syncfusion.Blazor.Toolkit", imports);
        Assert.False(File.Exists(Path.Combine(_testProjectDir, "Components", "_Imports.razor")));

        var indexHtml = File.ReadAllText(Path.Combine(_testProjectDir, "wwwroot", "index.html"));
        Assert.DoesNotContain("Syncfusion.Blazor.Toolkit/styles/fluent.min.css", indexHtml);
    }

    [Fact]
    public async Task Scaffold_BlazorSyncfusionToolkit_Net9_StandaloneWasm_RegistersAndAddsUsing()
    {
        File.WriteAllText(_testProjectPath, BlazorWasmProjectContent);
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

        var (exitCode, output, error) = await ScaffoldAsync();
        Assert.True(exitCode == 0, $"Output: {output}\nError: {error}");

        string programCs = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        Assert.Contains("AddSyncfusionBlazorToolkit", programCs);
        Assert.Contains("using Syncfusion.Blazor.Toolkit", programCs);

        string imports = File.ReadAllText(Path.Combine(_testProjectDir, "_Imports.razor"));
        Assert.Contains("@using Syncfusion.Blazor.Toolkit", imports);

        string indexHtml = File.ReadAllText(Path.Combine(_testProjectDir, "wwwroot", "index.html"));
        Assert.DoesNotContain("Syncfusion.Blazor.Toolkit/styles/fluent.min.css", indexHtml);
    }

    [Fact]
    public async Task Scaffold_BlazorSyncfusionToolkit_Net9_PagesImportsRazor_WritesUsingOnDisk()
    {
        Directory.CreateDirectory(Path.Combine(_testProjectDir, "Pages"));
        File.WriteAllText(_testProjectPath, BlazorWebProjectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"),
            ScaffoldCliHelper.GetBlazorProgramCs("TestProject"));
        File.WriteAllText(Path.Combine(_testProjectDir, "Pages", "_Imports.razor"),
            "@using Microsoft.AspNetCore.Components\n");

        var (exitCode, output, error) = await ScaffoldAsync();
        Assert.True(exitCode == 0, $"Output: {output}\nError: {error}");

        var imports = File.ReadAllText(Path.Combine(_testProjectDir, "_Imports.razor"));
        Assert.Contains("@using Syncfusion.Blazor.Toolkit", imports);
    }
}