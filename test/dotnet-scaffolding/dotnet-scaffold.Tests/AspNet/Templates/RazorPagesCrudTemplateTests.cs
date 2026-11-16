// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Xunit;
using Net9 = Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net9.RazorPages;
using Net10 = Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.RazorPages;
using Net11 = Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net11.RazorPages;
using AspNetProjectInfo = Microsoft.DotNet.Tools.Scaffold.AspNet.Common.ProjectInfo;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Templates;

public class RazorPagesCrudTemplateTests
{
    public static TheoryData<int, string, string> KeyCases
    {
        get
        {
            var cases = new TheoryData<int, string, string>();
            foreach (int version in new[] { 8, 9, 10, 11 })
            {
                cases.Add(version, "BlogId", "int");
                cases.Add(version, "Id", "int");
                cases.Add(version, "RecordKey", "int");
                cases.Add(version, "BlogId", "string");
                cases.Add(version, "BlogId", "System.Guid");
            }
            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(KeyCases))]
    public void Handlers_UseIdParameter_AndPreserveEntityKey(int version, string keyName, string keyType)
    {
        RazorPageModel model = CreateModel(keyName, keyType);
        (ITextTransformation edit, ITextTransformation details, ITextTransformation delete) templates = version switch
        {
            8 => (SelectNet8Template<Net9.EditModel>(RazorPagesHelper.EditModelTemplate),
                SelectNet8Template<Net9.DetailsModel>(RazorPagesHelper.DetailsModelTemplate),
                SelectNet8Template<Net9.DeleteModel>(RazorPagesHelper.DeleteModelTemplate)),
            9 => (new Net9.EditModel(), new Net9.DetailsModel(), new Net9.DeleteModel()),
            10 => (new Net10.EditModel(), new Net10.DetailsModel(), new Net10.DeleteModel()),
            11 => (new Net11.EditModel(), new Net11.DetailsModel(), new Net11.DeleteModel()),
            _ => throw new System.ArgumentOutOfRangeException(nameof(version))
        };

        string editOutput = Transform(templates.edit, model);
        string detailsOutput = Transform(templates.details, model);
        string deleteOutput = Transform(templates.delete, model);

        foreach (string output in new[] { editOutput, detailsOutput, deleteOutput })
        {
            MethodDeclarationSyntax get = GetHandler(output, "OnGetAsync");
            AssertHandlerParameter(get, keyType);
            Assert.Contains("if (id is null)", get.Body!.ToString());
            Assert.Equal(2, get.Body.DescendantNodes().OfType<ReturnStatementSyntax>()
                .Count(statement => statement.Expression?.ToString() == "NotFound()"));
            Assert.Contains($"_context.Blogs.FirstOrDefaultAsync(m => m.{keyName} == id)", get.Body.ToString());
            Assert.Contains("return Page();", get.Body.ToString());
        }

        MethodDeclarationSyntax editPost = GetHandler(editOutput, "OnPostAsync");
        Assert.Empty(editPost.ParameterList.Parameters);
        Assert.Contains("_context.Attach(Blog).State = EntityState.Modified;", editPost.Body!.ToString());
        Assert.Contains($"BlogExists(Blog.{keyName})", editPost.Body.ToString());
        Assert.Contains($"private bool BlogExists({keyType} id)", editOutput);
        Assert.Contains($"_context.Blogs.Any(e => e.{keyName} == id)", editOutput);

        MethodDeclarationSyntax deletePost = GetHandler(deleteOutput, "OnPostAsync");
        AssertHandlerParameter(deletePost, keyType);
        Assert.Contains("if (id is null)", deletePost.Body!.ToString());
        Assert.Single(deletePost.Body.DescendantNodes().OfType<ReturnStatementSyntax>(),
            statement => statement.Expression?.ToString() == "NotFound()");
        Assert.Contains("_context.Blogs.FindAsync(id)", deletePost.Body.ToString());
        Assert.Contains("return RedirectToPage(\"./Index\");", deletePost.Body.ToString());
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    public void PageModels_ForIdModel_CompileWithoutIdentifierCollisions(int version)
    {
        const string modelName = "Id";
        const string keyName = "IdId";
        RazorPageModel model = CreateModel(keyName, "int", modelName);
        TargetFramework framework = version switch
        {
            8 => TargetFramework.Net8,
            9 => TargetFramework.Net9,
            10 => TargetFramework.Net10,
            11 => TargetFramework.Net11,
            _ => throw new ArgumentOutOfRangeException(nameof(version))
        };
        var trees = new List<SyntaxTree>
        {
            CSharpSyntaxTree.ParseText($$"""
                global using System.Linq;
                global using System.Threading.Tasks;
                namespace Models
                {
                    public class {{modelName}}
                    {
                        public int {{keyName}} { get; set; }
                        public string? Url { get; set; }
                    }
                    public class BloggingContext : Microsoft.EntityFrameworkCore.DbContext
                    {
                        public Microsoft.EntityFrameworkCore.DbSet<{{modelName}}> Blogs { get; set; } = default!;
                    }
                }
                """)
        };
        foreach (string templateName in new[] { "EditModel.tt", "DetailsModel.tt", "DeleteModel.tt" })
        {
            Type templateType = RazorPagesHelper.GetTemplateType(templateName, framework)!;
            var template = (ITextTransformation)Activator.CreateInstance(templateType)!;
            trees.Add(CSharpSyntaxTree.ParseText(Transform(template, model)));
        }

        string[] platformAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator);
        var references = platformAssemblies.Concat(
            [
                typeof(Microsoft.EntityFrameworkCore.DbContext).Assembly.Location,
                typeof(Microsoft.EntityFrameworkCore.DeleteBehavior).Assembly.Location
            ])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path));
        CSharpCompilation compilation = CSharpCompilation.Create(
            "GeneratedPageModels", trees, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var assembly = new MemoryStream();
        var result = compilation.Emit(assembly);

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
    }

    [Theory]
    [MemberData(nameof(KeyCases))]
    public void Links_UseRouteId_AndPreserveEntityKey(int version, string keyName, string keyType)
    {
        RazorPageModel model = CreateModel(keyName, keyType);
        (ITextTransformation index, ITextTransformation details) templates = version switch
        {
            8 => (SelectNet8Template<Net9.Index>(RazorPagesHelper.IndexTemplate),
                SelectNet8Template<Net9.Details>(RazorPagesHelper.DetailsTemplate)),
            9 => (new Net9.Index(), new Net9.Details()),
            10 => (new Net10.Index(), new Net10.Details()),
            11 => (new Net11.Index(), new Net11.Details()),
            _ => throw new System.ArgumentOutOfRangeException(nameof(version))
        };

        string indexOutput = Transform(templates.index, model);
        string detailsOutput = Transform(templates.details, model);

        foreach (string page in new[] { "Edit", "Details", "Delete" })
        {
            Assert.Contains($"asp-page=\"./{page}\" asp-route-id=\"@item.{keyName}\"", indexOutput);
        }
        Assert.Contains($"asp-page=\"./Edit\" asp-route-id=\"@Model.Blog.{keyName}\"", detailsOutput);
        Assert.DoesNotContain($"asp-route-{keyName}=", indexOutput);
        Assert.DoesNotContain($"asp-route-{keyName}=", detailsOutput);
    }

    [Theory]
    [MemberData(nameof(KeyCases))]
    public void Forms_PreserveEntityKey(int version, string keyName, string keyType)
    {
        RazorPageModel model = CreateModel(keyName, keyType);
        (ITextTransformation edit, ITextTransformation delete) templates = version switch
        {
            8 => (SelectNet8Template<Net9.Edit>(RazorPagesHelper.EditTemplate),
                SelectNet8Template<Net9.Delete>(RazorPagesHelper.DeleteTemplate)),
            9 => (new Net9.Edit(), new Net9.Delete()),
            10 => (new Net10.Edit(), new Net10.Delete()),
            11 => (new Net11.Edit(), new Net11.Delete()),
            _ => throw new System.ArgumentOutOfRangeException(nameof(version))
        };

        foreach (ITextTransformation template in new[] { templates.edit, templates.delete })
        {
            string output = Transform(template, model);
            Assert.Contains("<form method=\"post\">", output);
            Assert.Contains($"<input type=\"hidden\" asp-for=\"Blog.{keyName}\" />", output);
        }
    }

    private static ITextTransformation SelectNet8Template<T>(string templateName)
        where T : ITextTransformation
    {
        // The tool packages/copies net9 .tt files under net8.0/RazorPages;
        // net8's legacy Bootstrap4/5 .cshtml files are not used by this T4 path.
        string path = Path.Combine("AspNet", "Templates", "net8.0", "RazorPages", templateName);
        Type? selectedType = RazorPagesHelper.GetTemplateType(path, TargetFramework.Net8);
        Assert.Equal(typeof(T), selectedType);
        return (ITextTransformation)Activator.CreateInstance(selectedType!)!;
    }

    private static string Transform(ITextTransformation template, RazorPageModel model)
    {
        template.Session = new Dictionary<string, object> { { "Model", model } };
        template.Initialize();
        string output = template.TransformText();
        Assert.False(template.Errors.HasErrors);
        return output;
    }

    private static MethodDeclarationSyntax GetHandler(string output, string handlerName)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(output);
        Assert.DoesNotContain(tree.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        return Assert.Single(tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>(),
            method => method.Identifier.ValueText == handlerName);
    }

    private static void AssertHandlerParameter(MethodDeclarationSyntax handler, string keyType)
    {
        ParameterSyntax parameter = Assert.Single(handler.ParameterList.Parameters);
        Assert.Equal("id", parameter.Identifier.ValueText);
        Assert.Equal($"{keyType}?", parameter.Type?.ToString());
    }

    private static RazorPageModel CreateModel(string keyName, string keyType, string modelName = "Blog")
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText($$"""
            namespace Models
            {
                public class {{modelName}}
                {
                    public {{keyType}} {{keyName}} { get; set; }
                    public string? Url { get; set; }
                }
            }
            """);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        INamedTypeSymbol symbol = compilation.GetTypeByMetadataName($"Models.{modelName}")!;

        return new RazorPageModel
        {
            PageType = "Edit",
            DbContextInfo = new DbContextInfo
            {
                DbContextClassName = "BloggingContext",
                DbContextNamespace = "Models",
                EfScenario = true,
                EntitySetVariableName = "Blogs"
            },
            ModelInfo = new ModelInfo
            {
                ModelTypeName = modelName,
                ModelNamespace = "Models",
                ModelFullName = $"Models.{modelName}",
                PrimaryKeyName = keyName,
                PrimaryKeyTypeName = keyType,
                PrimaryKeyShortTypeName = keyType,
                ModelProperties = symbol.GetMembers().OfType<IPropertySymbol>().ToList()
            },
            RazorPageNamespace = "Pages.Blogs",
            ProjectInfo = new AspNetProjectInfo(null)
        };
    }
}
