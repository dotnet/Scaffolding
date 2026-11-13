// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Extensions;

public class EfControllerScaffolderBuilderExtensionsTests
{
    [Theory]
    [InlineData("OrdersController")]
    [InlineData("Orderscontroller")]
    [InlineData("OrdersCONTROLLER")]
    public void WithMvcViewsStep_PropagatesControllerNameFromControllerModel(string controllerName)
    {
        var builder = new Mock<IScaffoldBuilder>();
        Action<ScaffoldStepConfigurator<WrappedTextTemplatingStep>>? configure = null;
        builder.Setup(b => b.WithStep<ValidateViewsStep>(It.IsAny<Action<ScaffoldStepConfigurator<ValidateViewsStep>>>()))
            .Returns(builder.Object);
        builder.Setup(b => b.WithStep<WrappedTextTemplatingStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedTextTemplatingStep>>>()))
            .Callback<Action<ScaffoldStepConfigurator<WrappedTextTemplatingStep>>, Action<ScaffoldStepConfigurator<WrappedTextTemplatingStep>>>((action, _) => configure = action)
            .Returns(builder.Object);
        builder.Setup(b => b.WithStep<AddFileStep>(It.IsAny<Action<ScaffoldStepConfigurator<AddFileStep>>>()))
            .Returns(builder.Object);
        var context = new ScaffolderContext(Mock.Of<IScaffolder>());
        context.OptionResults[new ScaffolderOption<bool> { DisplayName = "Views", CliOption = Constants.CliOptions.ViewsOption }] = true;
        var viewModel = new ViewModel
        {
            PageType = "CRUD",
            ModelInfo = new ModelInfo { ModelTypeName = "Order" },
            ProjectInfo = new ProjectInfo(string.Empty),
            DbContextInfo = new DbContextInfo()
        };
        context.Properties[nameof(ViewModel)] = viewModel;
        context.Properties[nameof(EfControllerModel)] = new EfControllerModel
        {
            ControllerName = controllerName,
            ControllerType = "MVC",
            ControllerOutputPath = "Controllers",
            ModelInfo = viewModel.ModelInfo,
            ProjectInfo = viewModel.ProjectInfo,
            DbContextInfo = viewModel.DbContextInfo
        };
        var step = new WrappedTextTemplatingStep(
            NullLogger<WrappedTextTemplatingStep>.Instance, Mock.Of<ITelemetryService>())
        {
            TextTemplatingProperties = []
        };

        builder.Object.WithMvcViewsStep();
        Assert.NotNull(configure);
        configure(new ScaffoldStepConfigurator<WrappedTextTemplatingStep> { Step = step, Context = context });

        Assert.Equal(controllerName, viewModel.ControllerName);
        Assert.Equal("Order", viewModel.ModelInfo.ModelTypeName);
    }

    [Fact]
    public void WithEfControllerTextTemplatingStep_ReturnsBuilder()
    {
        // Arrange
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<WrappedTextTemplatingStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedTextTemplatingStep>>>()))
            .Returns(mockBuilder.Object);

        // Act
        IScaffoldBuilder result = mockBuilder.Object.WithEfControllerTextTemplatingStep();

        // Assert
        Assert.NotNull(result);
        mockBuilder.Verify(b => b.WithStep<WrappedTextTemplatingStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedTextTemplatingStep>>>()), Times.Once);
    }

    [Fact]
    public void WithEfControllerAddPackagesStep_ReturnsBuilder()
    {
        // Arrange
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<WrappedAddPackagesStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedAddPackagesStep>>>()))
            .Returns(mockBuilder.Object);

        // Act
        IScaffoldBuilder result = mockBuilder.Object.WithEfControllerAddPackagesStep();

        // Assert
        Assert.NotNull(result);
        mockBuilder.Verify(b => b.WithStep<WrappedAddPackagesStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedAddPackagesStep>>>()), Times.Once);
    }

    [Fact]
    public void WithEfControllerCodeChangeStep_ReturnsBuilder()
    {
        // Arrange
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<WrappedCodeModificationStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedCodeModificationStep>>>()))
            .Returns(mockBuilder.Object);

        // Act
        IScaffoldBuilder result = mockBuilder.Object.WithEfControllerCodeChangeStep();

        // Assert
        Assert.NotNull(result);
        mockBuilder.Verify(b => b.WithStep<WrappedCodeModificationStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedCodeModificationStep>>>()), Times.Once);
    }

    [Fact]
    public void WithMvcViewsStep_ReturnsBuilder()
    {
        // Arrange
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<ValidateViewsStep>(It.IsAny<Action<ScaffoldStepConfigurator<ValidateViewsStep>>>()))
            .Returns(mockBuilder.Object);
        mockBuilder.Setup(b => b.WithStep<WrappedTextTemplatingStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedTextTemplatingStep>>>()))
            .Returns(mockBuilder.Object);
        mockBuilder.Setup(b => b.WithStep<AddFileStep>(It.IsAny<Action<ScaffoldStepConfigurator<AddFileStep>>>()))
            .Returns(mockBuilder.Object);

        // Act
        IScaffoldBuilder result = mockBuilder.Object.WithMvcViewsStep();

        // Assert
        Assert.NotNull(result);
        mockBuilder.Verify(b => b.WithStep<ValidateViewsStep>(It.IsAny<Action<ScaffoldStepConfigurator<ValidateViewsStep>>>()), Times.Once);
        mockBuilder.Verify(b => b.WithStep<WrappedTextTemplatingStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedTextTemplatingStep>>>()), Times.Once);
        mockBuilder.Verify(b => b.WithStep<AddFileStep>(It.IsAny<Action<ScaffoldStepConfigurator<AddFileStep>>>()), Times.Once);
    }
}
