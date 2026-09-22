// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Templates;

public class BlazorCrudTemplateTests
{
    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    public void EditTemplate_RetainsLoadedModelWhenInteractiveRenderingStarts(int frameworkVersion)
    {
        BlazorCrudModel model = CreateModel("Edit");

        string result = TransformTemplate(frameworkVersion, "Edit", model).Replace("\r\n", "\n");

        Assert.Contains("[SupplyParameterFromForm]\n    private Employee? Employee { get; set; }", result);

        if (frameworkVersion >= 10)
        {
            Assert.Contains("[PersistentState]\n    public Employee? EmployeeState { get; set; }", result);
            Assert.Contains("Employee ??= EmployeeState ??= await context.Employees.FirstOrDefaultAsync", result);
            Assert.DoesNotContain("[SupplyParameterFromForm]\n    [PersistentState]", result);
        }
        else
        {
            Assert.Contains("@inject PersistentComponentState ApplicationState", result);
            Assert.Contains("persistingSubscription ??= ApplicationState.RegisterOnPersisting(PersistData)", result);
            Assert.Contains("ApplicationState.TryTakeFromJson<Employee>(nameof(Employee), out var restoredEmployee)", result);
            Assert.Contains("Employee = restoredEmployee", result);
            Assert.Contains("ApplicationState.PersistAsJson(nameof(Employee), Employee)", result);
            Assert.Contains("if (Employee is not null)", result);
            Assert.Contains("public void Dispose() => persistingSubscription?.Dispose();", result);

            int registerIndex = result.IndexOf("persistingSubscription ??= ApplicationState.RegisterOnPersisting(PersistData)", System.StringComparison.Ordinal);
            int queryIndex = result.IndexOf("await context.Employees.FirstOrDefaultAsync", System.StringComparison.Ordinal);
            Assert.True(registerIndex >= 0 && queryIndex >= 0 && registerIndex < queryIndex,
                "Persistent state callback registration should occur before awaiting the database query.");
        }
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    public void EditTemplate_GeneratesSingleDatabaseLookupStatement(int frameworkVersion)
    {
        BlazorCrudModel model = CreateModel("Edit");
        string result = TransformTemplate(frameworkVersion, "Edit", model);

        int first = result.IndexOf("FirstOrDefaultAsync", System.StringComparison.Ordinal);
        Assert.True(first >= 0, "Expected the generated edit template to query by primary key.");
        int second = result.IndexOf("FirstOrDefaultAsync", first + 1, System.StringComparison.Ordinal);
        Assert.True(second < 0, "Generated edit template should contain only one database lookup expression.");
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    public void EditTemplate_Net8AndNet9_ContainsRestoreFallbackAndEarlyNotFoundReturn(int frameworkVersion)
    {
        BlazorCrudModel model = CreateModel("Edit");
        string result = TransformTemplate(frameworkVersion, "Edit", model).Replace("\r\n", "\n");

        Assert.Contains("ApplicationState.TryTakeFromJson<Employee>(nameof(Employee), out var restoredEmployee)", result);
        Assert.Contains("Employee = restoredEmployee", result);
        Assert.Contains("NavigationManager.NavigateTo(\"notfound\");\n            return;", result);
    }

    private static BlazorCrudModel CreateModel(string pageType)
    {
        const string source = """
            namespace TestProject.Models;

            public class Employee
            {
                public int Id { get; set; }
                [System.ComponentModel.DataAnnotations.Required]
                public EmployeeType EmployeeType { get; set; }
                public EmployeeType? OptionalEmployeeType { get; set; }
                public string Name { get; set; } = string.Empty;
                public int Count { get; set; }
                public bool IsActive { get; set; }
                public System.DateTime StartDate { get; set; }
            }

            public enum EmployeeType
            {
                Permanent,
                Contract
            }
            """;

        return new BlazorCrudModel
        {
            PageType = pageType,
            DbContextInfo = new DbContextInfo
            {
                DbContextClassName = "AppDbContext",
                EntitySetVariableName = "Employees"
            },
            ModelInfo = new ModelInfo
            {
                ModelTypeName = "Employee",
                ModelNamespace = "TestProject.Models",
                PrimaryKeyName = "Id",
                PrimaryKeyShortTypeName = "int",
                PrimaryKeyTypeName = "System.Int32",
                ModelProperties = GetProperties(source)
            },
            ProjectInfo = new Microsoft.DotNet.Tools.Scaffold.AspNet.Common.ProjectInfo(null)
        };
    }

    private static string TransformTemplate(int frameworkVersion, string pageType, BlazorCrudModel model)
    {
        return frameworkVersion switch
        {
            8 when pageType == "Create" => Transform(new Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net8.BlazorCrud.Create(), model),
            8 => Transform(new Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net8.BlazorCrud.Edit(), model),
            9 when pageType == "Create" => Transform(new Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net9.BlazorCrud.Create(), model),
            9 => Transform(new Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net9.BlazorCrud.Edit(), model),
            10 when pageType == "Create" => Transform(new Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.BlazorCrud.Create(), model),
            10 => Transform(new Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.BlazorCrud.Edit(), model),
            11 when pageType == "Create" => Transform(new Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net11.BlazorCrud.Create(), model),
            11 => Transform(new Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net11.BlazorCrud.Edit(), model),
            _ => throw new System.ArgumentOutOfRangeException(nameof(frameworkVersion), frameworkVersion, "Unsupported framework version.")
        };
    }

    private static string Transform(Microsoft.DotNet.Scaffolding.TextTemplating.ITextTransformation template, BlazorCrudModel model)
    {
        template.Session = new Dictionary<string, object> { { "Model", model } };
        template.Initialize();
        return template.TransformText();
    }

    private static string Transform(Microsoft.DotNet.Scaffolding.Shared.T4Templating.ITextTransformation template, BlazorCrudModel model)
    {
        template.Session = new Dictionary<string, object> { { "Model", model } };
        template.Initialize();
        return template.TransformText();
    }

    private static List<IPropertySymbol> GetProperties(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute).Assembly.Location)
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        INamedTypeSymbol employeeType = compilation.GetTypeByMetadataName("TestProject.Models.Employee")
            ?? throw new System.InvalidOperationException("Employee type was not found.");

        return employeeType.GetMembers().OfType<IPropertySymbol>().ToList();
    }
}
