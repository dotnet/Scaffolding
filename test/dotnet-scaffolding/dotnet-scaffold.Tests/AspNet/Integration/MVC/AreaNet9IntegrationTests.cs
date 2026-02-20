// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.MVC;

public class AreaNet9IntegrationTests : AreaIntegrationTestsBase
{
    protected override string TargetFramework => "net9.0";
    protected override string TestClassName => nameof(AreaNet9IntegrationTests);

    [Fact]
    public async Task ExecuteAsync_ScaffoldsCorrectDirectoriesAndBuilds_Net9()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

        var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0,
            $"Project should build before scaffolding.\nExit code: {preExitCode}\nOutput: {preOutput}\nError: {preError}");

        var realFileSystem = new FileSystem();
        var realEnvironmentService = new EnvironmentService(realFileSystem);
        var step = new AreaScaffolderStep(
            realFileSystem,
            NullLogger<AreaScaffolderStep>.Instance,
            realEnvironmentService)
        {
            Project = _testProjectPath,
            Name = "TestArea"
        };

        bool scaffoldResult = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.True(scaffoldResult, "Scaffolding should succeed.");

        string areasDir = Path.Combine(_testProjectDir, "Areas");
        Assert.True(Directory.Exists(areasDir), "Areas directory should be created.");

        string namedAreaDir = Path.Combine(areasDir, "TestArea");
        Assert.True(Directory.Exists(namedAreaDir), "Named area directory should be created.");
        Assert.True(Directory.Exists(Path.Combine(namedAreaDir, "Controllers")), "Controllers should exist.");
        Assert.True(Directory.Exists(Path.Combine(namedAreaDir, "Models")), "Models should exist.");
        Assert.True(Directory.Exists(Path.Combine(namedAreaDir, "Data")), "Data should exist.");
        Assert.True(Directory.Exists(Path.Combine(namedAreaDir, "Views")), "Views should exist.");

        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(postExitCode == 0,
            $"Project should build after scaffolding.\nExit code: {postExitCode}\nOutput: {postOutput}\nError: {postError}");
    }
}
