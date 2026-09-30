// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Blazor;

[Trait("Suite", "ScaffoldIntegration")]
[Trait("Family", "blazor-crud")]
public class BlazorCrudBaselineTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("net10.0")]
    [InlineData("net11.0")]
    public Task Scaffold_DefaultBlazorWebApp_MatchesBaseline(string framework)
    {
        return ScaffolderBaselineRunner.RunAsync(
            output: output,
            scaffolder: "BlazorCrud",
            framework: framework,
            template: "BlazorWebApp",
            scaffold: actual => ScaffoldCliHelper.RunScaffoldAsync(framework, "blazor-crud", [
                "--project", Path.Combine(actual, "BlazorWebApp.csproj"),
                "--model", "Product",
                "--dataContext", "ApplicationDbContext",
                "--dbProvider", "sqlite-efcore",
                "--page", "CRUD",
                .. ScaffoldCliHelper.GetPrereleaseArguments(framework)
            ]),
            prepareInput: (expected, actual) =>
            {
                var models = Path.Combine(actual, "Models");
                Directory.CreateDirectory(models);
                File.Copy(Path.Combine(expected, "Models", "Product.cs"), Path.Combine(models, "Product.cs"));
            });
    }
}
