// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Syncfusion;

/// <summary>
/// Baseline coverage for the Syncfusion Blazor Toolkit scaffolder.
///
/// <para>The default path is checked against the repository's baseline
/// system: <see cref="ScaffolderBaselineRunner"/> copies the checked-in
/// Blazor Web App input for the framework, runs the scaffolder on it,
/// builds the result, and compares every file with the checked-in expected
/// project under <c>test/dotnet-scaffolding/baselines/BlazorToolkit</c>. The
/// expected project is itself built first, so the baseline is a runnable
/// application rather than a snapshot of text. Configuration variations
/// (WebAssembly, Auto + client, existing registrations, ...) stay in the
/// focused integration tests next to this file.</para>
/// </summary>
[Trait("Suite", "ScaffoldIntegration")]
[Trait("Family", "blazor-syncfusion-toolkit")]
public class SyncfusionBlazorToolkitRunnableBaselineTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("net10.0")]
    [InlineData("net11.0")]
    public Task Scaffold_DefaultBlazorWebApp_MatchesBaseline(string framework)
    {
        return ScaffolderBaselineRunner.RunAsync(
            output: output,
            scaffolder: "BlazorToolkit",
            framework: framework,
            template: "BlazorWebApp",
            scaffold: actual => ScaffoldCliHelper.RunScaffoldAsync(
                ScaffoldCliHelper.GetTestTargetFramework(),
                "blazor-syncfusion-toolkit",
                "--project", Path.Combine(actual, "BlazorWebApp.csproj")),
            // Syncfusion.Blazor.Toolkit is published to nuget.org, which the repository feeds do not include.
            nugetConfig: ScaffoldCliHelper.SyncfusionTestNuGetConfig);
    }
}
