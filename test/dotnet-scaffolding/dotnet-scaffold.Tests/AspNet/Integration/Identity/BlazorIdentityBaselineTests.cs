// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Identity;

[Trait("Suite", "ScaffoldIntegration")]
[Trait("Family", "blazor-identity")]
public class BlazorIdentityBaselineTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("net10.0")]
    [InlineData("net11.0")]
    public Task Scaffold_DefaultBlazorWebApp_MatchesBaseline(string framework)
    {
        string[] prerelease = framework == "net11.0" ? ["--prerelease"] : [];
        return ScaffolderBaselineRunner.RunAsync(
            output: output,
            scaffolder: "BlazorIdentity",
            framework: framework,
            template: "BlazorWebApp",
            scaffold: actual => ScaffoldCliHelper.RunScaffoldAsync("net11.0", "blazor-identity", [
                "--project", Path.Combine(actual, "BlazorWebApp.csproj"),
                "--dataContext", "ApplicationDbContext",
                "--dbProvider", "sqlite-efcore",
                .. prerelease
            ]));
    }
}
