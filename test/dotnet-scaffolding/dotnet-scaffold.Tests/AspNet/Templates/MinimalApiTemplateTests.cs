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

    #region OpenAPI Configuration Tests

    [Fact]
    public void MinimalApi_WithOpenAPI_GeneratesWithTagsExtension()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: false, useTypedResults: true);
        model.OpenAPI = true;
        var template = CreateMinimalApiTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.Contains(".WithTags(nameof(Product))", result);
    }

    [Fact]
    public void MinimalApi_WithoutOpenAPI_DoesNotGenerateWithTagsExtension()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: false, useTypedResults: true);
        model.OpenAPI = false;
        var template = CreateMinimalApiTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.DoesNotContain(".WithTags(", result);
        Assert.Contains("var group = routes.MapGroup(\"/api/Product\");", result);
    }

    [Fact]
    public void MinimalApiEf_WithOpenAPI_GeneratesWithTagsExtension()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: true, useTypedResults: true);
        model.OpenAPI = true;
        var template = CreateMinimalApiEfTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.Contains(".WithTags(nameof(Product))", result);
    }

    [Fact]
    public void MinimalApiEf_WithoutOpenAPI_DoesNotGenerateWithTagsExtension()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: true, useTypedResults: true);
        model.OpenAPI = false;
        var template = CreateMinimalApiEfTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.DoesNotContain(".WithTags(", result);
    }

    #endregion

    #region Model Namespace Tests

    [Fact]
    public void MinimalApi_WithModelNamespace_GeneratesUsingStatement()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: false, useTypedResults: true);
        model.ModelInfo.ModelNamespace = "TestProject.Models";
        var template = CreateMinimalApiTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.Contains("using TestProject.Models;", result);
    }

    [Fact]
    public void MinimalApi_WithoutModelNamespace_DoesNotGenerateUsingStatement()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: false, useTypedResults: true);
        model.ModelInfo.ModelNamespace = null;
        var template = CreateMinimalApiTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.DoesNotContain("using ;", result);
    }

    [Fact]
    public void MinimalApiEf_WithModelAndDbContextNamespaces_GeneratesBothUsingStatements()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: true, useTypedResults: true);
        model.ModelInfo.ModelNamespace = "TestProject.Models";
        model.DbContextInfo.DbContextNamespace = "TestProject.Data";
        var template = CreateMinimalApiEfTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.Contains("using TestProject.Models;", result);
        Assert.Contains("using TestProject.Data;", result);
    }

    #endregion

    #region Endpoint Naming Tests

    [Fact]
    public void MinimalApi_GeneratesCorrectEndpointMethodNames()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: false, useTypedResults: true);
        var template = CreateMinimalApiTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.Contains(".WithName(\"GetAllProducts\")", result);
        Assert.Contains(".WithName(\"GetProductById\")", result);
        Assert.Contains(".WithName(\"CreateProduct\")", result);
        Assert.Contains(".WithName(\"UpdateProduct\")", result);
        Assert.Contains(".WithName(\"DeleteProduct\")", result);
    }

    [Fact]
    public void MinimalApi_GeneratesCorrectClassAndMethodNames()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: false, useTypedResults: true);
        var template = CreateMinimalApiTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.Contains("public static class ProductEndpoints", result);
        Assert.Contains("public static void MapProductEndpoints(this IEndpointRouteBuilder routes)", result);
    }

    [Fact]
    public void MinimalApiEf_GeneratesCorrectEndpointMethodNames()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: true, useTypedResults: true);
        var template = CreateMinimalApiEfTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.Contains(".WithName(\"GetAllProducts\")", result);
        Assert.Contains(".WithName(\"GetProductById\")", result);
        Assert.Contains(".WithName(\"CreateProduct\")", result);
        Assert.Contains(".WithName(\"UpdateProduct\")", result);
        Assert.Contains(".WithName(\"DeleteProduct\")", result);
    }

    #endregion

    #region Route Prefix Tests

    [Fact]
    public void MinimalApi_GeneratesCorrectRoutePrefix()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: false, useTypedResults: true);
        var template = CreateMinimalApiTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.Contains("routes.MapGroup(\"/api/Product\")", result);
    }

    [Fact]
    public void MinimalApiEf_GeneratesCorrectRoutePrefix()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: true, useTypedResults: true);
        var template = CreateMinimalApiEfTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.Contains("routes.MapGroup(\"/api/Product\")", result);
    }

    #endregion

    #region EF DbContext Tests

    [Fact]
    public void MinimalApiEf_GeneratesDbContextParameter()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: true, useTypedResults: true);
        var template = CreateMinimalApiEfTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.Contains("ApplicationDbContext db", result);
    }

    [Fact]
    public void MinimalApiEf_GeneratesEntityFrameworkCoreUsing()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: true, useTypedResults: true);
        var template = CreateMinimalApiEfTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.Contains("using Microsoft.EntityFrameworkCore;", result);
    }

    [Fact]
    public void MinimalApiEf_GeneratesAsyncAwaitPattern()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: true, useTypedResults: true);
        var template = CreateMinimalApiEfTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.Contains("async", result);
        Assert.Contains("await", result);
        Assert.Contains("ToListAsync()", result);
    }

    [Fact]
    public void MinimalApiEf_GeneratesAsNoTrackingForGetById()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: true, useTypedResults: true);
        var template = CreateMinimalApiEfTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.Contains("AsNoTracking()", result);
        Assert.Contains("FirstOrDefaultAsync(", result);
    }

    [Fact]
    public void MinimalApiEf_GeneratesCorrectPrimaryKeyHandling()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: true, useTypedResults: true);
        var template = CreateMinimalApiEfTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.Contains("int id", result);
        Assert.Contains("model.Id == id", result);
    }

    #endregion

    #region Custom Model Name Tests

    [Fact]
    public void MinimalApi_WithCustomModelName_GeneratesCorrectOutput()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: false, useTypedResults: true);
        model.ModelInfo.ModelTypeName = "Customer";
        model.EndpointsClassName = "CustomerEndpoints";
        model.EndpointsMethodName = "MapCustomerEndpoints";
        var template = CreateMinimalApiTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.Contains("public static class CustomerEndpoints", result);
        Assert.Contains("public static void MapCustomerEndpoints(this IEndpointRouteBuilder routes)", result);
        Assert.Contains("routes.MapGroup(\"/api/Customer\")", result);
        Assert.Contains(".WithName(\"GetAllCustomers\")", result);
        Assert.Contains("new Customer()", result);
    }

    [Fact]
    public void MinimalApiEf_WithCustomEntitySetName_UsesCorrectEntitySet()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: true, useTypedResults: true);
        model.DbContextInfo.EntitySetVariableName = "MyProducts";
        var template = CreateMinimalApiEfTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.Contains("MyProducts.ToListAsync()", result);
        Assert.Contains("MyProducts.AsNoTracking()", result);
    }

    #endregion

    #region HTTP Methods Tests

    [Fact]
    public void MinimalApi_GeneratesAllHttpMethods()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: false, useTypedResults: true);
        var template = CreateMinimalApiTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.Contains("group.MapGet(\"/\"", result);
        Assert.Contains("group.MapGet(\"/{id}\"", result);
        Assert.Contains("group.MapPost(\"/\"", result);
        Assert.Contains("group.MapPut(\"/{id}\"", result);
        Assert.Contains("group.MapDelete(\"/{id}\"", result);
    }

    [Fact]
    public void MinimalApiEf_GeneratesAllHttpMethods()
    {
        // Arrange
        var model = CreateTestMinimalApiModel(efScenario: true, useTypedResults: true);
        var template = CreateMinimalApiEfTemplate(model);

        // Act
        var result = template.TransformText();

        // Assert
        Assert.Contains("group.MapGet(\"/\"", result);
        Assert.Contains("group.MapGet(\"/{id}\"", result);
        Assert.Contains("group.MapPost(\"/\"", result);
        Assert.Contains("group.MapPut(\"/{id}\"", result);
        Assert.Contains("group.MapDelete(\"/{id}\"", result);
    }

    #endregion
}
