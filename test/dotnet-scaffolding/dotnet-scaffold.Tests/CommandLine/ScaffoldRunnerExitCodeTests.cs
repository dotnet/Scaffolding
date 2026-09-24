// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.CommandLine;

public class ScaffoldRunnerExitCodeTests
{
    [Fact]
    public async Task RunAsync_PackageAddFailure_ReturnsNonzero()
    {
        var builder = Host.CreateScaffoldBuilder();
        builder.Services.AddSingleton<IEnvironmentService>(new EnvironmentService(new FileSystem()));
        builder.Services.AddSingleton<NuGetVersionService>();
        builder.Services.AddTransient<AddPackagesStep>();
        builder.AddScaffolder(ScaffolderCatagory.AspNet, "package-test")
            .WithStep<AddPackagesStep>(config =>
            {
                config.Step.Packages = [new Package("Microsoft.Identity.Web")];
                config.Step.ProjectPath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.csproj");
            });
        var runner = builder.Build();
        using var services = (ServiceProvider)builder.ServiceProvider!;

        Assert.Equal(1, await runner.RunAsync(["aspnet", "package-test"]));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(7, 7)]
    [InlineData(-1, 1)]
    [InlineData(int.MinValue, 1)]
    public async Task RunAsync_ReturnsPortableExitCode(int actionResult, int expectedExitCode)
    {
        var runner = new ScaffoldRunner(NullLogger<ScaffoldRunner>.Instance)
        {
            RootCommand = new System.CommandLine.RootCommand()
        };
        runner.AddHandler((_, _) => Task.FromResult(actionResult));

        Assert.Equal(expectedExitCode, await runner.RunAsync([]));
    }
}
