// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Extensions;

public class ScaffolderBuilderAspNetExtensionsTests
{
    [Theory]
    [InlineData("razorpages", true)]
    [InlineData("blazor", true)]
    [InlineData("controller", true)]
    [InlineData("minimalapi", true)]
    [InlineData("minimalapi", false)]
    public void AddPackagesStep_IncludesDesignPackageOnlyForEfScaffolding(string scaffolder, bool useEf)
    {
        var context = new ScaffolderContext(Mock.Of<IScaffolder>());
        var step = new WrappedAddPackagesStep(
            NullLogger<WrappedAddPackagesStep>.Instance,
            Mock.Of<ITelemetryService>(),
            new NuGetVersionService(Mock.Of<IEnvironmentService>()))
        {
            Packages = [],
            ProjectPath = string.Empty
        };
        var builder = new Mock<IScaffoldBuilder>();
        builder.Setup(b => b.WithStep<WrappedAddPackagesStep>(
                It.IsAny<Action<ScaffoldStepConfigurator<WrappedAddPackagesStep>>>()))
            .Callback<Action<ScaffoldStepConfigurator<WrappedAddPackagesStep>>, Action<ScaffoldStepConfigurator<WrappedAddPackagesStep>>>((configure, _) =>
                configure(new ScaffoldStepConfigurator<WrappedAddPackagesStep> { Step = step, Context = context }))
            .Returns(builder.Object);

        switch (scaffolder)
        {
            case "razorpages":
            case "blazor":
                context.Properties[nameof(CrudSettings)] = new CrudSettings
                {
                    Project = "TestProject.csproj",
                    Model = "Movie",
                    Page = "CRUD",
                    DatabaseProvider = PackageConstants.EfConstants.SQLite
                };
                if (scaffolder == "razorpages")
                {
                    builder.Object.WithRazorPagesAddPackagesStep();
                }
                else
                {
                    builder.Object.WithBlazorCrudAddPackagesStep();
                }
                break;
            case "controller":
                context.Properties[nameof(EfControllerSettings)] = new EfControllerSettings
                {
                    Project = "TestProject.csproj",
                    Model = "Movie",
                    ControllerName = "MoviesController",
                    ControllerType = "MVC",
                    DatabaseProvider = PackageConstants.EfConstants.SQLite
                };
                builder.Object.WithEfControllerAddPackagesStep();
                break;
            case "minimalapi":
                context.Properties[nameof(MinimalApiSettings)] = new MinimalApiSettings
                {
                    Project = "TestProject.csproj",
                    Model = "Movie",
                    DataContext = useEf ? "ApplicationDbContext" : null,
                    DatabaseProvider = PackageConstants.EfConstants.SQLite
                };
                builder.Object.WithMinimalApiAddPackagesStep();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scaffolder));
        }

        Assert.False(step.SkipStep);
        if (useEf)
        {
            Assert.Contains(PackageConstants.EfConstants.EfCoreDesignPackage, step.Packages);
        }
        else
        {
            Assert.DoesNotContain(PackageConstants.EfConstants.EfCoreDesignPackage, step.Packages);
        }
    }

    [Fact]
    public void WithAspNetConnectionStringStep_ReturnsBuilder()
    {
        // Arrange
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<AddAspNetConnectionStringStep>(It.IsAny<Action<ScaffoldStepConfigurator<AddAspNetConnectionStringStep>>>()))
            .Returns(mockBuilder.Object);

        // Act
        IScaffoldBuilder result = mockBuilder.Object.WithAspNetConnectionStringStep();

        // Assert
        Assert.NotNull(result);
        mockBuilder.Verify(b => b.WithStep<AddAspNetConnectionStringStep>(It.IsAny<Action<ScaffoldStepConfigurator<AddAspNetConnectionStringStep>>>()), Times.Once);
    }

    [Fact]
    public void WithIdentityDbContextStep_ReturnsBuilder()
    {
        // Arrange
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<WrappedTextTemplatingStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedTextTemplatingStep>>>()))
            .Returns(mockBuilder.Object);

        // Act
        IScaffoldBuilder result = mockBuilder.Object.WithIdentityDbContextStep();

        // Assert
        Assert.NotNull(result);
        mockBuilder.Verify(b => b.WithStep<WrappedTextTemplatingStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedTextTemplatingStep>>>()), Times.Once);
    }
}
