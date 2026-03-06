// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.MVC;

public class AreaNet10IntegrationTests : AreaIntegrationTestsBase
{
    protected override string TargetFramework => "net10.0";
    protected override string TestClassName => nameof(AreaNet10IntegrationTests);

    [Fact]
    public async Task Scaffold_Area_Net10_CliInvocation()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), ScaffoldCliHelper.GetMinimalProgramCs());

        var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0,
            $"Project should build before scaffolding.\nExit code: {preExitCode}\nOutput: {preOutput}\nError: {preError}");

        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "area",
            "--project", _testProjectPath,
            "--name", "TestArea");
        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        string areasDir = Path.Combine(_testProjectDir, "Areas");
        Assert.True(Directory.Exists(areasDir), "Areas directory should be created.");

        string namedAreaDir = Path.Combine(areasDir, "TestArea");
        Assert.True(Directory.Exists(namedAreaDir), "Named area directory should be created.");
        Assert.True(Directory.Exists(Path.Combine(namedAreaDir, "Controllers")), "Controllers should exist.");
        Assert.True(Directory.Exists(Path.Combine(namedAreaDir, "Models")), "Models should exist.");
        Assert.True(Directory.Exists(Path.Combine(namedAreaDir, "Data")), "Data should exist.");
        Assert.True(Directory.Exists(Path.Combine(namedAreaDir, "Views")), "Views should exist.");

        // Assert — no NuGet errors and project builds after scaffolding
        Assert.False(cliOutput.Contains("error: NU"),
            $"Scaffolding should not produce NuGet errors for {TargetFramework}.\nOutput: {cliOutput}");
        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(postExitCode == 0,
            $"Project should build after scaffolding.\nExit code: {postExitCode}\nOutput: {postOutput}\nError: {postError}");
    }
}
