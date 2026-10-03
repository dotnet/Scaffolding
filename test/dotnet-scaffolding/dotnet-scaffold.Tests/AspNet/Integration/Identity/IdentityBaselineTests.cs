// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Identity;

[Trait("Suite", "ScaffoldIntegration")]
[Trait("Family", "identity")]
public class IdentityBaselineTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("net10.0", "Mvc")]
    [InlineData("net11.0", "RazorPages")]
    public Task Scaffold_ExistingIdentityDataLayer_MatchesBaseline(string framework, string template)
        => ScaffolderBaselineRunner.RunAsync(
            output: output,
            scaffolder: "Identity",
            framework: framework,
            template: template,
            prepareInput: PrepareDataLayer,
            scaffold: async actual =>
            {
                var project = Path.Combine(actual, $"{template}.csproj");
                string[] arguments =
                [
                    "--project", project,
                    "--dataContext", "ApplicationDbContext",
                    "--dbProvider", "sqlite-efcore",
                    .. ScaffoldCliHelper.GetPrereleaseArguments(framework)
                ];
                var result = await ScaffoldCliHelper.RunScaffoldAsync(ScaffoldCliHelper.GetTestTargetFramework(), "identity", arguments);
                Assert.True(result.ExitCode == 0, $"Scaffolding failed.\n{result.Output}\n{result.Error}");

                var before = GeneratedProjectBaseline.EnumerateFiles(actual)
                    .ToDictionary(path => Path.GetRelativePath(actual, path), File.ReadAllBytes);
                var repeated = await ScaffoldCliHelper.RunScaffoldAsync(ScaffoldCliHelper.GetTestTargetFramework(), "identity", arguments);
                Assert.True(repeated.ExitCode == 0, $"Repeated scaffolding failed.\n{repeated.Output}\n{repeated.Error}");
                var after = GeneratedProjectBaseline.EnumerateFiles(actual)
                    .ToDictionary(path => Path.GetRelativePath(actual, path), File.ReadAllBytes);
                Assert.Equal(before.Keys.Order(), after.Keys.Order());
                foreach (var (path, bytes) in before)
                {
                    Assert.Equal(bytes, after[path]);
                }
                return result;
            });

    private static void PrepareDataLayer(string expected, string actual)
    {
        GeneratedProjectBaseline.CopyProject(Path.Combine(expected, "Data"), Path.Combine(actual, "Data"));
        File.Copy(Directory.GetFiles(expected, "*.csproj").Single(),
            Directory.GetFiles(actual, "*.csproj").Single(), overwrite: true);
    }
}
