// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Moq;
using Spectre.Console.Flow;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Helpers;

public class AzCliHelperTests
{
    [Fact]
    public void GetUsernameParameterValuesDynamically_WithExistingValue_ReturnsValue()
    {
        // Arrange
        List<string> expectedUsernames = ["user1@contoso.com", "user2@contoso.com"];
        FlowProperties properties = new FlowProperties(new Dictionary<string, object> { ["entraIdUsernames"] = expectedUsernames });
        Mock<IFlowContext> mockContext = new Mock<IFlowContext>();
        mockContext.Setup(c => c.Properties).Returns(properties);

        // Act
        List<string> result = AzCliHelper.GetUsernameParameterValuesDynamically(mockContext.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedUsernames, result);
    }

    [Fact]
    public void GetTenantParameterValuesDynamically_WithExistingValue_ReturnsValue()
    {
        // Arrange
        List<string> expectedTenants = ["tenant-id-1", "tenant-id-2"];
        FlowProperties properties = new FlowProperties(new Dictionary<string, object> { ["entraIdTenants"] = expectedTenants });
        Mock<IFlowContext> mockContext = new Mock<IFlowContext>();
        mockContext.Setup(c => c.Properties).Returns(properties);

        // Act
        List<string> result = AzCliHelper.GetTenantParameterValuesDynamically(mockContext.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTenants, result);
    }

    [Fact]
    public void GetAppIdParameterValuesDynamically_WithExistingValue_ReturnsValue()
    {
        // Arrange
        List<string> expectedAppIds = ["app-id-1", "app-id-2"];
        FlowProperties properties = new FlowProperties(new Dictionary<string, object> { ["entraIdAppIds"] = expectedAppIds });
        Mock<IFlowContext> mockContext = new Mock<IFlowContext>();
        mockContext.Setup(c => c.Properties).Returns(properties);

        // Act
        List<string> result = AzCliHelper.GetAppIdParameterValuesDynamically(mockContext.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedAppIds, result);
    }

    [Fact]
    public void GetAzCliErrors_WithExistingError_ReturnsError()
    {
        // Arrange
        string expectedError = "Azure CLI error message";
        FlowProperties properties = new FlowProperties(new Dictionary<string, object> { ["entraIdAzCliErrors"] = expectedError });
        Mock<IFlowContext> mockContext = new Mock<IFlowContext>();
        mockContext.Setup(c => c.Properties).Returns(properties);

        // Act
        string? result = AzCliHelper.GetAzCliErrors(mockContext.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedError, result);
    }

    [Fact]
    public void GetAzCliErrors_WithNoError_ReturnsNull()
    {
        // Arrange
        FlowProperties properties = new FlowProperties(new Dictionary<string, object>());
        Mock<IFlowContext> mockContext = new Mock<IFlowContext>();
        mockContext.Setup(c => c.Properties).Returns(properties);

        // Act
        string? result = AzCliHelper.GetAzCliErrors(mockContext.Object);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetUsernameParameterValuesDynamically_WithNullValue_ReturnsEmptyList()
    {
        // Arrange
        List<string> emptyList = new List<string>();
        FlowProperties properties = new FlowProperties(new Dictionary<string, object> { ["entraIdUsernames"] = emptyList });
        Mock<IFlowContext> mockContext = new Mock<IFlowContext>();
        mockContext.Setup(c => c.Properties).Returns(properties);

        // Act
        List<string> result = AzCliHelper.GetUsernameParameterValuesDynamically(mockContext.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetTenantParameterValuesDynamically_WithNullValue_ReturnsEmptyList()
    {
        // Arrange
        List<string> emptyList = new List<string>();
        FlowProperties properties = new FlowProperties(new Dictionary<string, object> { ["entraIdTenants"] = emptyList });
        Mock<IFlowContext> mockContext = new Mock<IFlowContext>();
        mockContext.Setup(c => c.Properties).Returns(properties);

        // Act
        List<string> result = AzCliHelper.GetTenantParameterValuesDynamically(mockContext.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetAppIdParameterValuesDynamically_WithNullValue_ReturnsEmptyList()
    {
        // Arrange
        List<string> emptyList = new List<string>();
        FlowProperties properties = new FlowProperties(new Dictionary<string, object> { ["entraIdAppIds"] = emptyList });
        Mock<IFlowContext> mockContext = new Mock<IFlowContext>();
        mockContext.Setup(c => c.Properties).Returns(properties);

        // Act
        List<string> result = AzCliHelper.GetAppIdParameterValuesDynamically(mockContext.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
