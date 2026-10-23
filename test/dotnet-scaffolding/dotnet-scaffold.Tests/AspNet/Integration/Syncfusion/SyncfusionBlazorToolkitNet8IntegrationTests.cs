// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Syncfusion;

public class SyncfusionBlazorToolkitNet8IntegrationTests : SyncfusionBlazorToolkitFixtureBase
{
    protected override string TargetFramework => "net8.0";
    protected override string TestClassName => nameof(SyncfusionBlazorToolkitNet8IntegrationTests);

    [Fact]
    public async Task Scaffold_BlazorSyncfusionToolkit_Net8_CliInvocation_AddsPackageServiceAndImport()
    {
        WriteBlazorWebAppScaffold();

        var (cliExitCode, cliOutput, cliError) = await ScaffoldAsync();
        Assert.True(cliExitCode == 0,
            $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        // Package reference was added.
        string csproj = File.ReadAllText(_testProjectPath);
        Assert.Contains("Syncfusion.Blazor.Toolkit", csproj);

        // Program.cs received both the AddSyncfusionBlazorToolkit call
        // and the using directive, and the call survives a build.
        string programCs = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        AssertValidServiceRegistration(programCs);
        Assert.Contains("using Syncfusion.Blazor.Toolkit", programCs);

        // Components/_Imports.razor received the @using directive.
        string imports = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "_Imports.razor"));
        Assert.Contains("@using Syncfusion.Blazor.Toolkit", imports);

        // No external stylesheet link was added — 2.0.0+ ships styles
        // with the assembly.
        string appRazor = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "App.razor"));
        Assert.DoesNotContain("Syncfusion.Blazor.Toolkit/styles/fluent.min.css", appRazor);
    }

    [Fact]
    public async Task Scaffold_BlazorSyncfusionToolkit_Net8_IsIdempotent()
    {
        WriteBlazorWebAppScaffold();

        var first = await ScaffoldAsync();
        Assert.True(first.ExitCode == 0, $"First run failed.\nOutput: {first.Output}\nError: {first.Error}");

        var second = await ScaffoldAsync();
        Assert.True(second.ExitCode == 0, $"Second run failed (must be idempotent).\nOutput: {second.Output}\nError: {second.Error}");

        string imports = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "_Imports.razor"));
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(
            imports, "@using Syncfusion\\.Blazor\\.Toolkit"));

        string programCs = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(
            programCs, "AddSyncfusionBlazorToolkit"));

        string appRazor = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "App.razor"));
        Assert.DoesNotContain("Syncfusion.Blazor.Toolkit/styles/fluent.min.css", appRazor);
    }

    [Fact]
    public async Task Scaffold_BlazorSyncfusionToolkit_Net8_RootImportsRazor_WritesUsingOnDisk()
    {
        // Classic layout: _Imports at project root, no Components/_Imports.
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
        Assert.False(File.Exists(Path.Combine(_testProjectDir, "Components", "_Imports.razor")),
            "Components/_Imports.razor should not be created when the project only has a root _Imports.razor.");

        var indexHtml = File.ReadAllText(Path.Combine(_testProjectDir, "wwwroot", "index.html"));
        Assert.DoesNotContain("Syncfusion.Blazor.Toolkit/styles/fluent.min.css", indexHtml);
    }

    [Fact]
    public async Task Scaffold_BlazorSyncfusionToolkit_Net8_StandaloneWasm_RegistersAndAddsUsing()
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
        AssertValidServiceRegistration(programCs);
        Assert.Contains("using Syncfusion.Blazor.Toolkit", programCs);

        string imports = File.ReadAllText(Path.Combine(_testProjectDir, "_Imports.razor"));
        Assert.Contains("@using Syncfusion.Blazor.Toolkit", imports);

        string indexHtml = File.ReadAllText(Path.Combine(_testProjectDir, "wwwroot", "index.html"));
        Assert.DoesNotContain("Syncfusion.Blazor.Toolkit/styles/fluent.min.css", indexHtml);
    }

    [Fact]
    public async Task Scaffold_BlazorSyncfusionToolkit_Net8_PagesImportsRazor_WritesUsingOnDisk()
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
        Assert.False(File.Exists(Path.Combine(_testProjectDir, "Components", "_Imports.razor")),
            "Components/_Imports.razor should not be created when the project only has Pages/_Imports.razor.");
    }

    [Fact]
    public async Task Scaffold_BlazorSyncfusionToolkit_Net8_RollsBackPackageVersion_OnRerun()
    {
        // A rerun against a project that already references an older version
        // must update it to the current one via the add-packages step.
        WriteBlazorWebAppScaffold();
        File.WriteAllText(_testProjectPath,
            BlazorWebProjectContent.Replace(
                "</Project>",
                "<ItemGroup><PackageReference Include=\"Syncfusion.Blazor.Toolkit\" Version=\"1.0.0\" /></ItemGroup></Project>"));

        var (exitCode, output, error) = await ScaffoldAsync();
        Assert.True(exitCode == 0, $"Output: {output}\nError: {error}");

        string csproj = File.ReadAllText(_testProjectPath);
        Assert.Contains("Syncfusion.Blazor.Toolkit", csproj);
        // Version is no longer "1.0.0".
        Assert.DoesNotContain("Version=\"1.0.0\"", csproj);
    }
}