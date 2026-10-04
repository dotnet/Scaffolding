// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System;
using System.Linq;
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
        return ScaffolderBaselineRunner.RunAsync(
            output: output,
            scaffolder: "BlazorIdentity",
            framework: framework,
            template: "BlazorWebApp",
            scaffold: async actual =>
            {
                string[] arguments =
                [
                    "--project", Path.Combine(actual, "BlazorWebApp.csproj"),
                    "--dataContext", "ApplicationDbContext",
                    "--dbProvider", "sqlite-efcore",
                    .. ScaffoldCliHelper.GetPrereleaseArguments(framework)
                ];
                var result = await ScaffoldCliHelper.RunScaffoldAsync(ScaffoldCliHelper.GetTestTargetFramework(), "blazor-identity", arguments);
                Assert.True(result.ExitCode == 0, $"Scaffolding failed.\n{result.Output}\n{result.Error}");
                Assert.Contains("Identity scaffolding does not create migrations or update the database.", result.Output);
                Assert.False(Directory.Exists(Path.Combine(actual, "Data", "Migrations")));
                Assert.Empty(Directory.GetFiles(actual, "*.db", SearchOption.AllDirectories));
                var before = GeneratedProjectBaseline.EnumerateFiles(actual)
                    .ToDictionary(path => Path.GetRelativePath(actual, path), File.ReadAllBytes, StringComparer.Ordinal);
                var repeated = await ScaffoldCliHelper.RunScaffoldAsync(ScaffoldCliHelper.GetTestTargetFramework(), "blazor-identity", arguments);
                Assert.True(repeated.ExitCode == 0, $"Repeated scaffolding failed.\n{repeated.Output}\n{repeated.Error}");
                var after = GeneratedProjectBaseline.EnumerateFiles(actual)
                    .ToDictionary(path => Path.GetRelativePath(actual, path), File.ReadAllBytes, StringComparer.Ordinal);
                Assert.Equal(before.Keys.Order(), after.Keys.Order());
                foreach (var (path, bytes) in before)
                {
                    Assert.Equal(bytes, after[path]);
                }
                return result;
            });
    }
}
