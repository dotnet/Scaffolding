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

public class RazorViewsNet11IntegrationTests : RazorViewsIntegrationTestsBase
{
    protected override string TargetFramework => "net11.0";
    protected override string TestClassName => nameof(RazorViewsNet11IntegrationTests);

    [Fact]
    public async Task ValidateViewsStep_ValidatesAndBuilds_Net11()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

        var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0,
            $"Project should build before scaffolding.\nExit code: {preExitCode}\nOutput: {preOutput}\nError: {preError}");

        var realFileSystem = new FileSystem();
        var step = new ValidateViewsStep(
            realFileSystem,
            NullLogger<ValidateViewsStep>.Instance,
            _testTelemetryService)
        {
            Project = _testProjectPath,
            Model = "Product",
            Page = "CRUD"
        };

        var result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result, "Validation step should fail because model class does not exist in the project.");

        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(postExitCode == 0,
            $"Project should build after validation step.\nExit code: {postExitCode}\nOutput: {postOutput}\nError: {postError}");
    }
}
