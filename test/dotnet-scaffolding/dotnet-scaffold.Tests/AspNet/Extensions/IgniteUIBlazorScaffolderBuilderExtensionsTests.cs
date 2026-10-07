// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Extensions;

public class IgniteUIBlazorScaffolderBuilderExtensionsTests
{
    private static Mock<IScaffoldBuilder> CreateBuilder<TStep>() where TStep : Microsoft.DotNet.Scaffolding.Core.Steps.ScaffoldStep
    {
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<TStep>(It.IsAny<Action<ScaffoldStepConfigurator<TStep>>>(), It.IsAny<Action<ScaffoldStepConfigurator<TStep>>>()))
            .Returns(mockBuilder.Object);
        return mockBuilder;
    }

    private static void VerifyStepAdded<TStep>(Mock<IScaffoldBuilder> mockBuilder) where TStep : Microsoft.DotNet.Scaffolding.Core.Steps.ScaffoldStep
        => mockBuilder.Verify(b => b.WithStep<TStep>(It.IsAny<Action<ScaffoldStepConfigurator<TStep>>>(), It.IsAny<Action<ScaffoldStepConfigurator<TStep>>>()), Times.Once);

    [Fact]
    public void WithIgniteUIBlazorAddPackagesStep_AddsWrappedAddPackagesStep()
    {
        var mockBuilder = CreateBuilder<WrappedAddPackagesStep>();

        IScaffoldBuilder result = mockBuilder.Object.WithIgniteUIBlazorAddPackagesStep();

        Assert.NotNull(result);
        VerifyStepAdded<WrappedAddPackagesStep>(mockBuilder);
    }

    [Fact]
    public void WithIgniteUIBlazorWasmAddPackagesStep_AddsWrappedAddPackagesStep()
    {
        var mockBuilder = CreateBuilder<WrappedAddPackagesStep>();

        IScaffoldBuilder result = mockBuilder.Object.WithIgniteUIBlazorWasmAddPackagesStep();

        Assert.NotNull(result);
        VerifyStepAdded<WrappedAddPackagesStep>(mockBuilder);
    }

    [Fact]
    public void WithIgniteUIBlazorCodeChangeStep_AddsWrappedCodeModificationStep()
    {
        var mockBuilder = CreateBuilder<WrappedCodeModificationStep>();

        IScaffoldBuilder result = mockBuilder.Object.WithIgniteUIBlazorCodeChangeStep();

        Assert.NotNull(result);
        VerifyStepAdded<WrappedCodeModificationStep>(mockBuilder);
    }

    [Fact]
    public void WithIgniteUIBlazorWasmCodeChangeStep_AddsWrappedCodeModificationStep()
    {
        var mockBuilder = CreateBuilder<WrappedCodeModificationStep>();

        IScaffoldBuilder result = mockBuilder.Object.WithIgniteUIBlazorWasmCodeChangeStep();

        Assert.NotNull(result);
        VerifyStepAdded<WrappedCodeModificationStep>(mockBuilder);
    }

    [Fact]
    public void WithIgniteUIBlazorImportsStep_AddsAddRazorImportsStep()
    {
        var mockBuilder = CreateBuilder<AddRazorImportsStep>();

        IScaffoldBuilder result = mockBuilder.Object.WithIgniteUIBlazorImportsStep();

        Assert.NotNull(result);
        VerifyStepAdded<AddRazorImportsStep>(mockBuilder);
    }

    [Fact]
    public void WithIgniteUIBlazorWasmImportsStep_AddsAddRazorImportsStep()
    {
        var mockBuilder = CreateBuilder<AddRazorImportsStep>();

        IScaffoldBuilder result = mockBuilder.Object.WithIgniteUIBlazorWasmImportsStep();

        Assert.NotNull(result);
        VerifyStepAdded<AddRazorImportsStep>(mockBuilder);
    }

    [Fact]
    public void WithIgniteUIBlazorThemeStylesheetStep_AddsAddIgniteUIThemeStylesheetStep()
    {
        var mockBuilder = CreateBuilder<AddIgniteUIThemeStylesheetStep>();

        IScaffoldBuilder result = mockBuilder.Object.WithIgniteUIBlazorThemeStylesheetStep();

        Assert.NotNull(result);
        VerifyStepAdded<AddIgniteUIThemeStylesheetStep>(mockBuilder);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithIgniteUIBlazorWasmAddPackagesStep_TargetsClientFromModel(bool hasClient)
    {
        var model = CreateModel(hasClient);
        var step = new WrappedAddPackagesStep(
            NullLogger<WrappedAddPackagesStep>.Instance,
            Mock.Of<ITelemetryService>(),
            new NuGetVersionService(Mock.Of<IEnvironmentService>(e => e.CurrentDirectory == Directory.GetCurrentDirectory())))
        {
            Packages = [],
            ProjectPath = string.Empty
        };

        ConfigureStep(step, CreateContext(model), builder => builder.WithIgniteUIBlazorWasmAddPackagesStep());

        Assert.Equal(!hasClient, step.SkipStep);
        if (hasClient)
        {
            Assert.Equal(model.ClientProjectPath, step.ProjectPath);
            Assert.Equal(2, step.Packages.Count);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithIgniteUIBlazorWasmImportsStep_TargetsClientImportsFromModel(bool hasClient)
    {
        var model = CreateModel(hasClient);
        var step = new AddRazorImportsStep(NullLogger<AddRazorImportsStep>.Instance, Mock.Of<IFileSystem>(), Mock.Of<ITelemetryService>())
        {
            ImportsFilePath = string.Empty,
            Namespaces = []
        };

        ConfigureStep(step, CreateContext(model), builder => builder.WithIgniteUIBlazorWasmImportsStep());

        Assert.Equal(!hasClient, step.SkipStep);
        if (hasClient)
        {
            Assert.Equal(model.ClientImportsFilePath, step.ImportsFilePath);
            Assert.Equal([IgniteUIBlazorHelper.ControlsNamespace], step.Namespaces);
        }
    }

    [Fact]
    public void WithIgniteUIBlazorWasmCodeChangeStep_SkipsWithoutClient()
    {
        var model = CreateModel(hasClient: false);
        var step = new WrappedCodeModificationStep(NullLogger<WrappedCodeModificationStep>.Instance, Mock.Of<ITelemetryService>())
        {
            CodeChangeOptions = [],
            ProjectPath = string.Empty
        };

        ConfigureStep(step, CreateContext(model), builder => builder.WithIgniteUIBlazorWasmCodeChangeStep());

        Assert.True(step.SkipStep);
    }

    [Fact]
    public void GetPackages_ReturnsBothPackages()
    {
        var packages = IgniteUIBlazorScaffolderBuilderExtensions.GetPackages();

        Assert.Equal(["IgniteUI.Blazor.Lite", "IgniteUI.Blazor.GridLite"], packages.ConvertAll(p => p.Name));
        Assert.All(packages, p => Assert.False(p.IsVersionRequired));
    }

    [Fact]
    public void CodeModificationConfigFileNames_AreStable()
    {
        Assert.Equal("igniteUIBlazorChanges.json", IgniteUIBlazorScaffolderBuilderExtensions.CodeModificationConfigFileName);
        Assert.Equal("igniteUIBlazorWasmChanges.json", IgniteUIBlazorScaffolderBuilderExtensions.WasmCodeModificationConfigFileName);
    }

    private static void ConfigureStep<TStep>(TStep step, ScaffolderContext context, Func<IScaffoldBuilder, IScaffoldBuilder> addStep)
        where TStep : Microsoft.DotNet.Scaffolding.Core.Steps.ScaffoldStep
    {
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<TStep>(It.IsAny<Action<ScaffoldStepConfigurator<TStep>>>(), It.IsAny<Action<ScaffoldStepConfigurator<TStep>>>()))
            .Callback<Action<ScaffoldStepConfigurator<TStep>>, Action<ScaffoldStepConfigurator<TStep>>?>((configure, _) =>
                configure(new ScaffoldStepConfigurator<TStep> { Step = step, Context = context }))
            .Returns(mockBuilder.Object);

        addStep(mockBuilder.Object);

        VerifyStepAdded<TStep>(mockBuilder);
    }

    private static ScaffolderContext CreateContext(IgniteUIBlazorModel model)
    {
        var context = new ScaffolderContext(Mock.Of<IScaffolder>());
        context.Properties[nameof(IgniteUIBlazorModel)] = model;
        context.Properties[nameof(IgniteUIBlazorSettings)] = new IgniteUIBlazorSettings
        {
            Project = model.ProjectPath,
            Theme = model.Theme,
            ThemeVariant = model.ThemeVariant,
            Prerelease = false
        };
        return context;
    }

    private static IgniteUIBlazorModel CreateModel(bool hasClient = false)
    {
        var projectDirectory = Path.Combine("C:", "src", "MyApp");
        var clientProjectDirectory = Path.Combine("C:", "src", "MyApp.Client");
        return new IgniteUIBlazorModel
        {
            ProjectInfo = new ProjectInfo(null),
            ProjectPath = Path.Combine(projectDirectory, "MyApp.csproj"),
            BaseOutputPath = projectDirectory,
            Theme = "bootstrap",
            ThemeVariant = "light",
            StylesheetPath = "_content/IgniteUI.Blazor/themes/light/bootstrap.css",
            IsWebAssemblyProject = false,
            HostPagePath = Path.Combine(projectDirectory, "Components", "App.razor"),
            ImportsFilePath = Path.Combine(projectDirectory, "Components", "_Imports.razor"),
            ClientProjectPath = hasClient ? Path.Combine(clientProjectDirectory, "MyApp.Client.csproj") : null,
            ClientImportsFilePath = hasClient ? Path.Combine(clientProjectDirectory, "_Imports.razor") : null
        };
    }
}
