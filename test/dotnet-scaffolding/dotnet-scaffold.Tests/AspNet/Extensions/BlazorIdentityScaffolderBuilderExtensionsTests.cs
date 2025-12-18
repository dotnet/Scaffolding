// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
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
    public void WithBlazorIdentityStaticFilesStep_ReturnsBuilder()
    {
        // Arrange
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<AddFileStep>(It.IsAny<Action<ScaffoldStepConfigurator<AddFileStep>>>()))
            .Returns(mockBuilder.Object);

        // Act
        IScaffoldBuilder result = mockBuilder.Object.WithBlazorIdentityStaticFilesStep();

        // Assert
        Assert.NotNull(result);
        mockBuilder.Verify(b => b.WithStep<AddFileStep>(It.IsAny<Action<ScaffoldStepConfigurator<AddFileStep>>>()), Times.Once);
    }

    [Fact]
    public void WithBlazorIdentityAddPackagesStep_ReturnsBuilder()
    {
        // Arrange
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<WrappedAddPackagesStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedAddPackagesStep>>>()))
            .Returns(mockBuilder.Object);

        // Act
        IScaffoldBuilder result = mockBuilder.Object.WithBlazorIdentityAddPackagesStep();

        // Assert
        Assert.NotNull(result);
        mockBuilder.Verify(b => b.WithStep<WrappedAddPackagesStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedAddPackagesStep>>>()), Times.Once);
    }
}
