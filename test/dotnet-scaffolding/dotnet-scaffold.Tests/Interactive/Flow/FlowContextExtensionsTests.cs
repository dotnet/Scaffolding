// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Tools.Scaffold.Interactive.Flow;
using Moq;
using Spectre.Console.Flow;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Interactive.Flow;

/// <summary>
/// Unit tests for FlowContextExtensions, particularly the GetDetectedTargetFramework method.
/// </summary>
public class FlowContextExtensionsTests
{
    [Fact]
    public void GetDetectedTargetFramework_WhenPropertyNotSet_ReturnsNull()
    {
        // Arrange
        var properties = new FlowProperties(new Dictionary<string, object>());
        var mockContext = new Mock<IFlowContext>();
        mockContext.Setup(c => c.Properties).Returns(properties);

        // Act
        TargetFramework? result = mockContext.Object.GetDetectedTargetFramework();

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(TargetFramework.Net8)]
    [InlineData(TargetFramework.Net9)]
    [InlineData(TargetFramework.Net10)]
    [InlineData(TargetFramework.Net11)]
    public void GetDetectedTargetFramework_WhenPropertySet_ReturnsValue(TargetFramework expectedTfm)
    {
        // Arrange
        var properties = new FlowProperties(new Dictionary<string, object>
        {
            [FlowContextProperties.DetectedTargetFramework] = new FlowProperty(
                FlowContextProperties.DetectedTargetFramework,
                expectedTfm,
                isVisible: false)
        });
        var mockContext = new Mock<IFlowContext>();
        mockContext.Setup(c => c.Properties).Returns(properties);

        // Act
        TargetFramework? result = mockContext.Object.GetDetectedTargetFramework();

        // Assert
        Assert.Equal(expectedTfm, result);
    }
}
