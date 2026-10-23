// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Identity;

[Trait("Suite", "ScaffoldIntegration")]
[Trait("Family", "identity-api")]
public class IdentityApiBaselineTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("net10.0")]
    [InlineData("net11.0")]
    public Task Scaffold_DefaultEmptyWebApp_MatchesBaseline(string framework)
    {
        return ScaffolderBaselineRunner.RunAsync(
            output: output,
            scaffolder: "IdentityApi",
            framework: framework,
            template: "EmptyWebApp",
            scaffold: actual => ScaffoldCliHelper.RunDotNetAsync(actual, [
                ScaffoldCliHelper.GetScaffoldAssemblyPath(), "aspnet", "identity-api",
                "--project", Path.Combine(actual, "EmptyWebApp.csproj"),
                "--dataContext", "ApplicationDbContext",
                "--dbProvider", "sqlite-efcore",
                .. ScaffoldCliHelper.GetPrereleaseArguments(framework)
            ]));
    }
}
