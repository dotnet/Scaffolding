// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.DotNet.Tools.Scaffold.Interactive.Flow;
using Moq;
using Spectre.Console.Flow;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Interactive.Flow;

/// <summary>
/// Unit tests for FlowContextExtensions, particularly the GetIsAspireAvailable method.
/// </summary>
public class FlowContextExtensionsTests
{
    [Fact]
    public void GetIsAspireAvailable_WhenPropertyNotSet_ReturnsTrue()
    {
        // Arrange
        var properties = new FlowProperties(new Dictionary<string, object>());
        var mockContext = new Mock<IFlowContext>();
        mockContext.Setup(c => c.Properties).Returns(properties);

        // Act
        bool result = mockContext.Object.GetIsAspireAvailable();

        // Assert - default should be true when not set
        Assert.True(result);
    }

    [Fact]
    public void GetIsAspireAvailable_WhenPropertySetToTrue_ReturnsTrue()
    {
        // Arrange
        var properties = new FlowProperties(new Dictionary<string, object>
        {
            [FlowContextProperties.IsAspireAvailable] = new FlowProperty(
                FlowContextProperties.IsAspireAvailable,
                true,
                isVisible: false)
        });
        var mockContext = new Mock<IFlowContext>();
        mockContext.Setup(c => c.Properties).Returns(properties);

        // Act
        bool result = mockContext.Object.GetIsAspireAvailable();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetIsAspireAvailable_WhenPropertySetToFalse_ReturnsFalse()
    {
        // Arrange
        var properties = new FlowProperties(new Dictionary<string, object>
        {
            [FlowContextProperties.IsAspireAvailable] = new FlowProperty(
                FlowContextProperties.IsAspireAvailable,
                false,
                isVisible: false)
        });
        var mockContext = new Mock<IFlowContext>();
        mockContext.Setup(c => c.Properties).Returns(properties);

        // Act
        bool result = mockContext.Object.GetIsAspireAvailable();

        // Assert
        Assert.False(result);
    }
}
