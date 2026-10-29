// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Blazor;

[Trait("Suite", "ScaffoldIntegration")]
[Trait("Family", "blazor-igniteui")]
public class BlazorIgniteUIBaselineTests(ITestOutputHelper output)
{
    // The Ignite UI packages are published only to nuget.org, which the repository feeds do not mirror.
    private static readonly string[] s_packageSources = ["https://api.nuget.org/v3/index.json"];

    [Theory]
    [InlineData("net10.0")]
    [InlineData("net11.0")]
    public Task Scaffold_DefaultBlazorWebApp_MatchesBaseline(string framework)
    {
        // The default setup: '--project' only, so the default theme (light bootstrap) is linked. The Ignite UI packages
        // are not versioned with .NET, so the latest stable versions are installed for every framework.
        return ScaffolderBaselineRunner.RunAsync(
            output: output,
            scaffolder: "IgniteUI",
            framework: framework,
            template: "BlazorWebApp",
            scaffold: actual => ScaffoldCliHelper.RunScaffoldAsync(ScaffoldCliHelper.GetTestTargetFramework(), "blazor-igniteui", [
                "--project", Path.Combine(actual, "BlazorWebApp.csproj")
            ]),
            additionalPackageSources: s_packageSources);
    }
}
