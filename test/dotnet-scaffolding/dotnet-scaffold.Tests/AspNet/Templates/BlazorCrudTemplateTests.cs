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
    [InlineData(9, "Create")]
    [InlineData(9, "Edit")]
    [InlineData(10, "Create")]
    [InlineData(10, "Edit")]
    [InlineData(11, "Create")]
    [InlineData(11, "Edit")]
    [InlineData(8, "Create")]
    [InlineData(8, "Edit")]
    public void FormTemplate_WithEnumProperty_GeneratesInputSelect(int frameworkVersion, string pageType)
    {
        // Arrange
        BlazorCrudModel model = CreateModel(pageType);

        // Act
        string result = TransformTemplate(frameworkVersion, pageType, model);

        // Assert
        string inputSelect = GetInputSelect(result, "employeetype");

        Assert.Contains("@bind-Value=\"Employee.EmployeeType\"", inputSelect);
        Assert.Contains("aria-required=\"true\"", inputSelect);
        Assert.Contains("Enum.GetValues<TestProject.Models.EmployeeType>()", inputSelect);
        Assert.Contains("<option value=\"@value\">@value</option>", inputSelect);
        Assert.Contains("</InputSelect>", inputSelect);
        Assert.DoesNotContain("<option value=\"\">", inputSelect);
        Assert.DoesNotContain("<InputText id=\"employeetype\"", result);
    }

    [Theory]
    [InlineData(9, "Create")]
    [InlineData(9, "Edit")]
    [InlineData(10, "Create")]
    [InlineData(10, "Edit")]
    [InlineData(11, "Create")]
    [InlineData(11, "Edit")]
    [InlineData(8, "Create")]
    [InlineData(8, "Edit")]
    public void FormTemplate_WithNullableEnumProperty_GeneratesInputSelectWithEmptyOption(int frameworkVersion, string pageType)
    {
        BlazorCrudModel model = CreateModel(pageType);

        string result = TransformTemplate(frameworkVersion, pageType, model);
        string inputSelect = GetInputSelect(result, "optionalemployeetype");

        Assert.Contains("@bind-Value=\"Employee.OptionalEmployeeType\"", inputSelect);
        Assert.Contains("<option value=\"\">-- select --</option>", inputSelect);
        Assert.Contains("Enum.GetValues<TestProject.Models.EmployeeType>()", inputSelect);
        Assert.Contains("</InputSelect>", inputSelect);
        Assert.DoesNotContain("<InputText id=\"optionalemployeetype\"", result);
    }

    [Theory]
    [InlineData(9, "Create")]
    [InlineData(9, "Edit")]
    [InlineData(10, "Create")]
    [InlineData(10, "Edit")]
    [InlineData(11, "Create")]
    [InlineData(11, "Edit")]
    [InlineData(8, "Create")]
    [InlineData(8, "Edit")]
    public void FormTemplate_WithStandardProperties_PreservesInputTypes(int frameworkVersion, string pageType)
    {
        BlazorCrudModel model = CreateModel(pageType);

        string result = TransformTemplate(frameworkVersion, pageType, model);

        Assert.Contains("<InputText id=\"name\" @bind-Value=\"Employee.Name\"", result);
        Assert.Contains("<InputNumber id=\"count\" @bind-Value=\"Employee.Count\"", result);
        Assert.Contains("<InputCheckbox id=\"isactive\" @bind-Value=\"Employee.IsActive\"", result);
        Assert.Contains("<InputDate id=\"startdate\" @bind-Value=\"Employee.StartDate\"", result);
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
        if (frameworkVersion == 8 && pageType == "Create")
        {
            return Transform(new Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net8.BlazorCrud.Create(), model);
        }

        if (frameworkVersion == 8)
        {
            return Transform(new Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net8.BlazorCrud.Edit(), model);
        }

        if (frameworkVersion == 9 && pageType == "Create")
        {
            return Transform(new Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net9.BlazorCrud.Create(), model);
        }

        if (frameworkVersion == 9)
        {
            return Transform(new Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net9.BlazorCrud.Edit(), model);
        }

        if (frameworkVersion == 10 && pageType == "Create")
        {
            return Transform(new Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.BlazorCrud.Create(), model);
        }

        if (frameworkVersion == 10)
        {
            return Transform(new Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.BlazorCrud.Edit(), model);
        }

        if (pageType == "Create")
        {
            return Transform(new Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net11.BlazorCrud.Create(), model);
        }

        return Transform(new Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net11.BlazorCrud.Edit(), model);
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

    private static string GetInputSelect(string result, string id)
    {
        int start = result.IndexOf($"<InputSelect id=\"{id}\"", System.StringComparison.Ordinal);
        Assert.True(start >= 0, $"InputSelect with id '{id}' was not generated.");

        const string closingTag = "</InputSelect>";
        int end = result.IndexOf(closingTag, start, System.StringComparison.Ordinal);
        Assert.True(end >= 0, $"InputSelect with id '{id}' was not closed.");

        return result.Substring(start, end - start + closingTag.Length);
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
