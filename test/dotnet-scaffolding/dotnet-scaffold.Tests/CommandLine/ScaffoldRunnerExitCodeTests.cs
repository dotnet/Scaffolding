// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.TextTemplating;
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

    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    public async Task RunAsync_CommandActionMapsScaffolderResultToExitCode(bool stepSucceeds, int expectedExitCode)
    {
        var builder = Host.CreateScaffoldBuilder();
        builder.Services.AddSingleton(new DeterministicStep(stepSucceeds));
        builder.AddScaffolder(ScaffolderCatagory.AspNet, "result-map")
            .WithStep<DeterministicStep>();
        var runner = builder.Build();

        int exitCode = await runner.RunAsync(["aspnet", "result-map"]);

        Assert.Equal(expectedExitCode, exitCode);
    }

    [Fact]
    public async Task RunAsync_TextTemplatingFailure_ReturnsNonzeroAndSkipsLaterStep()
    {
        string outputPath = Path.Combine(Path.GetTempPath(), nameof(RunAsync_TextTemplatingFailure_ReturnsNonzeroAndSkipsLaterStep), Guid.NewGuid().ToString("N"), "Employee.cs");
        var builder = Host.CreateScaffoldBuilder();
        builder.Services.AddSingleton(new DeterministicStep(true));
        builder.Services.AddSingleton(new RecordingStep());
        builder.Services.AddTransient<TextTemplatingStep>(_ => new TextTemplatingStep(NullLogger<TextTemplatingStep>.Instance)
        {
            TextTemplatingProperties = []
        });
        builder.AddScaffolder(ScaffolderCatagory.AspNet, "templating-failure")
            .WithStep<DeterministicStep>()
            .WithStep<TextTemplatingStep>(config =>
            {
                config.Step.TextTemplatingProperties =
                [
                    new TextTemplatingProperty
                    {
                        TemplatePath = "Employee.tt",
                        TemplateType = typeof(object),
                        OutputPath = outputPath,
                        TemplateModelName = "Model",
                        TemplateModel = new object()
                    }
                ];
            })
            .WithStep<RecordingStep>();
        var runner = builder.Build();
        using var services = Assert.IsType<ServiceProvider>(builder.ServiceProvider);
        var recordingStep = services.GetRequiredService<RecordingStep>();
        try
        {
            int exitCode = await runner.RunAsync(["aspnet", "templating-failure"]);

            Assert.Equal(1, exitCode);
            Assert.Equal(0, recordingStep.ExecutionCount);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            string? outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

    private sealed class DeterministicStep(bool succeeds) : ScaffoldStep
    {
        public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(succeeds);
    }

    private sealed class RecordingStep : ScaffoldStep
    {
        public int ExecutionCount { get; private set; }

        public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
        {
            ExecutionCount++;
            return Task.FromResult(true);
        }
    }
}
