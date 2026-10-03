// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
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
    public void WithIgniteUIBlazorDetectBlazorWasmStep_AddsDetectBlazorWasmStep()
    {
        var mockBuilder = CreateBuilder<DetectBlazorWasmStep>();

        IScaffoldBuilder result = mockBuilder.Object.WithIgniteUIBlazorDetectBlazorWasmStep();

        Assert.NotNull(result);
        VerifyStepAdded<DetectBlazorWasmStep>(mockBuilder);
    }

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
    [InlineData(true, false, "IgniteUI.Blazor.Lite")]
    [InlineData(false, true, "IgniteUI.Blazor.GridLite")]
    [InlineData(true, true, "IgniteUI.Blazor.Lite", "IgniteUI.Blazor.GridLite")]
    public void GetPackages_ReturnsSelectedPackages(bool includeLite, bool includeGridLite, params string[] expectedPackageNames)
    {
        var model = CreateModel(includeLite, includeGridLite);

        var packages = IgniteUIBlazorScaffolderBuilderExtensions.GetPackages(model);

        Assert.Equal(expectedPackageNames, packages.ConvertAll(p => p.Name));
        Assert.All(packages, p => Assert.False(p.IsVersionRequired));
    }

    [Fact]
    public void Model_RequiresServiceRegistration_OnlyWhenLiteIncluded()
    {
        Assert.True(CreateModel(includeLite: true, includeGridLite: false).RequiresServiceRegistration);
        Assert.True(CreateModel(includeLite: true, includeGridLite: true).RequiresServiceRegistration);
        Assert.False(CreateModel(includeLite: false, includeGridLite: true).RequiresServiceRegistration);
    }

    [Fact]
    public void CodeModificationConfigFileNames_AreStable()
    {
        Assert.Equal("igniteUIBlazorChanges.json", IgniteUIBlazorScaffolderBuilderExtensions.CodeModificationConfigFileName);
        Assert.Equal("igniteUIBlazorWasmChanges.json", IgniteUIBlazorScaffolderBuilderExtensions.WasmCodeModificationConfigFileName);
    }

    private static IgniteUIBlazorModel CreateModel(bool includeLite, bool includeGridLite)
    {
        var projectDirectory = Path.Combine("C:", "src", "MyApp");
        return new IgniteUIBlazorModel
        {
            ProjectInfo = new ProjectInfo(null),
            ProjectPath = Path.Combine(projectDirectory, "MyApp.csproj"),
            BaseOutputPath = projectDirectory,
            IncludeLite = includeLite,
            IncludeGridLite = includeGridLite,
            Theme = "bootstrap",
            ThemeVariant = "light",
            StylesheetPath = "_content/IgniteUI.Blazor/themes/light/bootstrap.css",
            IsWebAssemblyProject = false,
            HostPagePath = Path.Combine(projectDirectory, "Components", "App.razor"),
            ImportsFilePath = Path.Combine(projectDirectory, "Components", "_Imports.razor")
        };
    }
}
