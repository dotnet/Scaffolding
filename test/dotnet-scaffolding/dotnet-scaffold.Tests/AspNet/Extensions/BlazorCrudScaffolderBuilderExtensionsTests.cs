// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Extensions;

public class BlazorCrudScaffolderBuilderExtensionsTests
{
    [Fact]
    public void WithBlazorCrudTextTemplatingStep_ReturnsBuilder()
    {
        // Arrange
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<WrappedTextTemplatingStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedTextTemplatingStep>>>()))
            .Returns(mockBuilder.Object);

        // Act
        IScaffoldBuilder result = mockBuilder.Object.WithBlazorCrudTextTemplatingStep();

        // Assert
        Assert.NotNull(result);
        mockBuilder.Verify(b => b.WithStep<WrappedTextTemplatingStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedTextTemplatingStep>>>()), Times.Once);
    }

    [Fact]
    public void WithBlazorCrudAddPackagesStep_ReturnsBuilder()
    {
        // Arrange
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<WrappedAddPackagesStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedAddPackagesStep>>>()))
            .Returns(mockBuilder.Object);

        // Act
        IScaffoldBuilder result = mockBuilder.Object.WithBlazorCrudAddPackagesStep();

        // Assert
        Assert.NotNull(result);
        mockBuilder.Verify(b => b.WithStep<WrappedAddPackagesStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedAddPackagesStep>>>()), Times.Once);
    }

    [Fact]
    public void WithBlazorCrudCodeChangeStep_ReturnsBuilder()
    {
        // Arrange
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<WrappedCodeModificationStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedCodeModificationStep>>>()))
            .Returns(mockBuilder.Object);

        // Act
        IScaffoldBuilder result = mockBuilder.Object.WithBlazorCrudCodeChangeStep();

        // Assert
        Assert.NotNull(result);
        // Verify WithStep is called twice (once for main changes, once for additional changes)
        mockBuilder.Verify(b => b.WithStep<WrappedCodeModificationStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedCodeModificationStep>>>()), Times.Exactly(2));
    }

    [Fact]
    public void WithBlazorCrudCodeChangeStep_ConfiguresTwoCodeModificationSteps()
    {
        // Arrange
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        int callCount = 0;
        
        mockBuilder.Setup(b => b.WithStep<WrappedCodeModificationStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedCodeModificationStep>>>()))
            .Callback(() => callCount++)
            .Returns(mockBuilder.Object);

        // Act
        IScaffoldBuilder result = mockBuilder.Object.WithBlazorCrudCodeChangeStep();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, callCount);
    }
}
