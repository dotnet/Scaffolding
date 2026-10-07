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

        // Assert — Components/App.razor received the theme stylesheet link.
        string appRazor = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "App.razor"));
        Assert.Contains("Syncfusion.Blazor.Toolkit/styles/fluent.min.css", appRazor);
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

        // No double-inserted theme link.
        string appRazor = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "App.razor"));
        int linkCount = System.Text.RegularExpressions.Regex.Matches(
            appRazor,
            "Syncfusion\\.Blazor\\.Toolkit/styles/fluent\\.min\\.css").Count;
        Assert.Equal(1, linkCount);
    }
}
