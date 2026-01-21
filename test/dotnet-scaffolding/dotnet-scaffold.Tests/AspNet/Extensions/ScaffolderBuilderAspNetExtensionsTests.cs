// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Extensions;

public class ScaffolderBuilderAspNetExtensionsTests
{
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
