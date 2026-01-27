// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.MinimalApi;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Templates;

public class MinimalApiTemplateTests
{
    #region Non-EF Scenario Tests

    [Fact]
    public void MinimalApi_WithTypedResults_GeneratesTypedResultsCode()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: false, useTypedResults: true);
        var template = CreateMinimalApiTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.Contains("TypedResults.NoContent()", result);
        Assert.Contains("//return TypedResults.Created($\"/api/Product/{model.ID}\", model);", result);
        Assert.Contains("//return TypedResults.Ok(new Product { ID = id });", result);
        Assert.DoesNotContain(".Produces<", result);
        // Ensure we're using TypedResults, not just Results (check no standalone "Results." without "Typed" prefix)
        Assert.DoesNotContain(" Results.", result);
    }

    [Fact]
    public void MinimalApi_WithoutTypedResults_GeneratesResultsCode()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: false, useTypedResults: false);
        var template = CreateMinimalApiTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.Contains("Results.NoContent()", result);
        Assert.Contains("//return Results.Created($\"/{model.ID}\", model);", result);
        Assert.Contains("//return Results.Ok(new Product { ID = id });", result);
        Assert.Contains(".Produces<Product[]>(StatusCodes.Status200OK)", result);
        Assert.Contains(".Produces<Product>(StatusCodes.Status200OK)", result);
        Assert.Contains(".Produces(StatusCodes.Status204NoContent)", result);
        Assert.Contains(".Produces<Product>(StatusCodes.Status201Created)", result);
        Assert.DoesNotContain("TypedResults", result);
    }

    [Fact]
    public void MinimalApi_WithTypedResults_DoesNotGenerateProducesExtensions()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: false, useTypedResults: true);
        var template = CreateMinimalApiTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.DoesNotContain(".Produces<", result);
        Assert.DoesNotContain(".Produces(StatusCodes", result);
    }

    [Fact]
    public void MinimalApi_WithoutTypedResults_GeneratesProducesExtensions()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: false, useTypedResults: false);
        var template = CreateMinimalApiTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.Contains(".Produces<Product[]>(StatusCodes.Status200OK)", result);
        Assert.Contains(".Produces<Product>(StatusCodes.Status200OK)", result);
        Assert.Contains(".Produces(StatusCodes.Status204NoContent)", result);
        Assert.Contains(".Produces<Product>(StatusCodes.Status201Created)", result);
    }

    #endregion

    #region EF Scenario Tests

    [Fact]
    public void MinimalApiEf_WithTypedResults_GeneratesTypedResultsCode()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: true, useTypedResults: true);
        var template = CreateMinimalApiEfTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.Contains("TypedResults.NotFound()", result);
        Assert.Contains("TypedResults.Ok(model)", result);
        Assert.Contains("TypedResults.Ok()", result);
        Assert.Contains("TypedResults.Created(", result);
        Assert.Contains("Task<Results<Ok<Product>, NotFound>>", result);
        Assert.Contains("Task<Results<Ok, NotFound>>", result);
        // Ensure we're using TypedResults, not just Results (check no standalone "Results." without "Typed" prefix)
        Assert.DoesNotContain(" Results.", result);
    }

    [Fact]
    public void MinimalApiEf_WithoutTypedResults_GeneratesResultsCode()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: true, useTypedResults: false);
        var template = CreateMinimalApiEfTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.Contains("Results.NotFound()", result);
        Assert.Contains("Results.Ok(model)", result);
        Assert.Contains("Results.Ok()", result);
        Assert.Contains("Results.Created(", result);
        Assert.DoesNotContain("TypedResults.NotFound()", result);
        Assert.DoesNotContain("TypedResults.Ok(", result);
        Assert.DoesNotContain("Task<Results<Ok<Product>, NotFound>>", result);
    }

    [Fact]
    public void MinimalApiEf_WithTypedResults_GeneratesStronglyTypedReturnTypes()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: true, useTypedResults: true);
        var template = CreateMinimalApiEfTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.Contains("Task<Results<Ok<Product>, NotFound>>", result);
        Assert.Contains("Task<Results<Ok, NotFound>>", result);
    }

    [Fact]
    public void MinimalApiEf_WithoutTypedResults_DoesNotGenerateStronglyTypedReturnTypes()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: true, useTypedResults: false);
        var template = CreateMinimalApiEfTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.DoesNotContain("Task<Results<Ok<Product>, NotFound>>", result);
        Assert.DoesNotContain("Task<Results<Ok, NotFound>>", result);
        Assert.DoesNotContain("Task<Results<NotFound, NoContent>>", result);
    }

    [Fact]
    public void MinimalApiEf_WithTypedResults_ContainsHttpResultsUsing()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: true, useTypedResults: true);
        var template = CreateMinimalApiEfTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.Contains("using Microsoft.AspNetCore.Http.HttpResults;", result);
    }

    #endregion

    #region Helper Methods

    private MinimalApiModel CreateTestMinimalApiModel(bool efScenario, bool useTypedResults)
    {
        return new MinimalApiModel
        {
            OpenAPI = true,
            UseTypedResults = useTypedResults,
            EndpointsClassName = "ProductEndpoints",
            EndpointsFileName = "ProductEndpoints.cs",
            EndpointsPath = Path.Combine("test", "project", "ProductEndpoints.cs"),
            EndpointsNamespace = "TestProject",
            EndpointsMethodName = "MapProductEndpoints",
            DbContextInfo = new DbContextInfo
            {
                DbContextClassName = "ApplicationDbContext",
                DbContextNamespace = "TestProject.Data",
                DatabaseProvider = "sqlserver-efcore",
                EfScenario = efScenario,
                EntitySetVariableName = "Products"
            },
            ModelInfo = new ModelInfo
            {
                ModelTypeName = "Product",
                ModelNamespace = "TestProject.Models",
                PrimaryKeyName = "Id",
                PrimaryKeyShortTypeName = "int",
                PrimaryKeyTypeName = "System.Int32",
                ModelProperties = new List<Microsoft.CodeAnalysis.IPropertySymbol>()
            },
            ProjectInfo = new ProjectInfo(Path.Combine("test", "project", "TestProject.csproj"))
        };
    }

    private MinimalApi CreateMinimalApiTemplate(MinimalApiModel model)
    {
        var template = new MinimalApi();
        template.Session = new Dictionary<string, object>
        {
            { "Model", model }
        };
        template.Initialize();
        return template;
    }

    private MinimalApiEf CreateMinimalApiEfTemplate(MinimalApiModel model)
    {
        var template = new MinimalApiEf();
        template.Session = new Dictionary<string, object>
        {
            { "Model", model }
        };
        template.Initialize();
        return template;
    }

    #endregion
}
