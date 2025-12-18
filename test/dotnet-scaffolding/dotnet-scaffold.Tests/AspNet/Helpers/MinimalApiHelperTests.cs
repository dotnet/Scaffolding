// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Helpers;

public class MinimalApiHelperTests
{
    [Fact]
    public void GetMinimalApiTemplatingProperty_WithEfScenario_ThrowsWhenTemplateNotFound()
    {
        // Arrange
        MinimalApiModel minimalApiModel = CreateTestMinimalApiModel();
        minimalApiModel.DbContextInfo.EfScenario = true;

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            MinimalApiHelper.GetMinimalApiTemplatingProperty(minimalApiModel));
        
        Assert.Contains("could not get minimal api template", exception.Message);
    }

    [Fact]
    public void GetMinimalApiTemplatingProperty_WithNonEfScenario_ThrowsWhenTemplateNotFound()
    {
        // Arrange
        MinimalApiModel minimalApiModel = CreateTestMinimalApiModel();
        minimalApiModel.DbContextInfo.EfScenario = false;

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            MinimalApiHelper.GetMinimalApiTemplatingProperty(minimalApiModel));
        
        Assert.Contains("could not get minimal api template", exception.Message);
    }

    [Fact]
    public void GetMinimalApiTemplatingProperty_WithNullEndpointsPath_ThrowsException()
    {
        // Arrange
        MinimalApiModel minimalApiModel = CreateTestMinimalApiModel();
        minimalApiModel.EndpointsPath = null;

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            MinimalApiHelper.GetMinimalApiTemplatingProperty(minimalApiModel));
        
        Assert.Contains("could not get minimal api template", exception.Message);
    }

    [Fact]
    public void GetMinimalApiTemplatingProperty_WithEmptyEndpointsPath_ThrowsException()
    {
        // Arrange
        MinimalApiModel minimalApiModel = CreateTestMinimalApiModel();
        minimalApiModel.EndpointsPath = string.Empty;

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            MinimalApiHelper.GetMinimalApiTemplatingProperty(minimalApiModel));
        
        Assert.Contains("could not get minimal api template", exception.Message);
    }

    private MinimalApiModel CreateTestMinimalApiModel()
    {
        return new MinimalApiModel
        {
            OpenAPI = true,
            UseTypedResults = true,
            EndpointsClassName = "ProductEndpoints",
            EndpointsFileName = "ProductEndpoints.cs",
            EndpointsPath = Path.Combine("test", "project", "ProductEndpoints.cs"),
            EndpointsNamespace = "TestProject",
            EndpointsMethodName = "MapProductEndpoints",
            DbContextInfo = new DbContextInfo
            {
                DbContextClassName = "ApplicationDbContext",
                EfScenario = true
            },
            ModelInfo = new ModelInfo
            {
                ModelTypeName = "Product"
            },
            ProjectInfo = new ProjectInfo(Path.Combine("test", "project", "TestProject.csproj"))
        };
    }
}
