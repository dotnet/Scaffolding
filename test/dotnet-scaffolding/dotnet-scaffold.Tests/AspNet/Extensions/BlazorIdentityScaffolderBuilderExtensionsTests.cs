// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Extensions;

public class BlazorIdentityScaffolderBuilderExtensionsTests
{
    [Fact]
    public void WithBlazorIdentityCodeChangeStep_ReturnsBuilder()
    {
        // Arrange
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<WrappedCodeModificationStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedCodeModificationStep>>>()))
            .Returns(mockBuilder.Object);

        // Act
        IScaffoldBuilder result = mockBuilder.Object.WithBlazorIdentityCodeChangeStep();

        // Assert
        Assert.NotNull(result);
        mockBuilder.Verify(b => b.WithStep<WrappedCodeModificationStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedCodeModificationStep>>>()), Times.Once);
    }

    [Fact]
    public void WithBlazorIdentityTextTemplatingStep_ReturnsBuilder()
    {
        // Arrange
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<WrappedTextTemplatingStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedTextTemplatingStep>>>()))
            .Returns(mockBuilder.Object);

        // Act
        IScaffoldBuilder result = mockBuilder.Object.WithBlazorIdentityTextTemplatingStep();

        // Assert
        Assert.NotNull(result);
        mockBuilder.Verify(b => b.WithStep<WrappedTextTemplatingStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedTextTemplatingStep>>>()), Times.Once);
    }

    [Fact]
    public void WithBlazorIdentityPasskeyJavaScriptStep_ReturnsBuilder()
    {
        // Arrange
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<AddFileStep>(It.IsAny<Action<ScaffoldStepConfigurator<AddFileStep>>>()))
            .Returns(mockBuilder.Object);

        // Act
        IScaffoldBuilder result = mockBuilder.Object.WithBlazorIdentityPasskeyJavaScriptStep();

        // Assert
        Assert.NotNull(result);
        mockBuilder.Verify(b => b.WithStep<AddFileStep>(It.IsAny<Action<ScaffoldStepConfigurator<AddFileStep>>>()), Times.Once);
    }

    [Theory]
    [InlineData(TargetFramework.Net10, PackageConstants.EfConstants.SqlServer, false)]
    [InlineData(TargetFramework.Net11, PackageConstants.EfConstants.SqlServer, true)]
    [InlineData(TargetFramework.Net11, PackageConstants.EfConstants.SQLite, false)]
    public void WithBlazorIdentityAddPackagesStep_ConfiguresPackages(TargetFramework targetFramework, string databaseProvider, bool includeAzurePackage)
    {
        var context = new ScaffolderContext(Mock.Of<IScaffolder>());
        context.SetSpecifiedTargetFramework(targetFramework);
        context.Properties[nameof(IdentitySettings)] = new IdentitySettings
        {
            Project = "TestProject.csproj",
            DataContext = "TestDbContext",
            DatabaseProvider = databaseProvider
        };
        var environmentService = Mock.Of<IEnvironmentService>(e => e.CurrentDirectory == Directory.GetCurrentDirectory());
        var step = new WrappedAddPackagesStep(
            NullLogger<WrappedAddPackagesStep>.Instance,
            Mock.Of<ITelemetryService>(),
            new NuGetVersionService(environmentService))
        {
            Packages = [],
            ProjectPath = string.Empty
        };
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<WrappedAddPackagesStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedAddPackagesStep>>>()))
            .Callback<Action<ScaffoldStepConfigurator<WrappedAddPackagesStep>>, Action<ScaffoldStepConfigurator<WrappedAddPackagesStep>>?>((configure, _) =>
                configure(new ScaffoldStepConfigurator<WrappedAddPackagesStep> { Step = step, Context = context }))
            .Returns(mockBuilder.Object);

        IScaffoldBuilder result = mockBuilder.Object.WithBlazorIdentityAddPackagesStep();

        Assert.Same(mockBuilder.Object, result);
        mockBuilder.Verify(b => b.WithStep<WrappedAddPackagesStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedAddPackagesStep>>>()), Times.Once);
        var azurePackage = step.Packages.SingleOrDefault(p => p.Name == "Microsoft.Data.SqlClient.Extensions.Azure");
        Assert.Equal(includeAzurePackage, azurePackage is not null);
        if (azurePackage is not null)
        {
            Assert.True(azurePackage.IsVersionRequired);
            Assert.True(azurePackage.UseLatestVersion);
        }
    }
}
