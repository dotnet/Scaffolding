// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Helpers;

public class EfControllerHelperTests
{
    [Fact]
    public void GetEfControllerTemplatingProperty_WithAPIControllerType_ThrowsWhenTemplateNotFound()
    {
        // Arrange
        EfControllerModel efControllerModel = CreateTestEfControllerModel();
        efControllerModel.ControllerType = "API";

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            EfControllerHelper.GetEfControllerTemplatingProperty(efControllerModel));
        
        Assert.Contains("API", exception.Message);
    }

    [Fact]
    public void GetEfControllerTemplatingProperty_WithMvcControllerType_ThrowsWhenTemplateNotFound()
    {
        // Arrange
        EfControllerModel efControllerModel = CreateTestEfControllerModel();
        efControllerModel.ControllerType = "MVC";

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            EfControllerHelper.GetEfControllerTemplatingProperty(efControllerModel));
        
        Assert.Contains("MVC", exception.Message);
    }

    [Fact]
    public void GetEfControllerTemplatingProperty_WithValidModel_RequiresControllerType()
    {
        // Arrange
        EfControllerModel efControllerModel = CreateTestEfControllerModel();

        // Act & Assert - Should throw because template files don't exist in test environment
        Assert.Throws<InvalidOperationException>(() =>
            EfControllerHelper.GetEfControllerTemplatingProperty(efControllerModel));
    }

    private EfControllerModel CreateTestEfControllerModel()
    {
        return new EfControllerModel
        {
            ControllerType = "API",
            ControllerName = "ProductsController",
            ControllerOutputPath = Path.Combine("Controllers"),
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
