// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Scaffolding.CodeModification;
using Microsoft.DotNet.Scaffolding.CodeModification.CodeChange;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.CodeModification;

public class DocumentBuilderTests
{
    [Theory]
    [InlineData("namespace Data { class MyDbContextFactory { } class MyDbContext { } }", "MyDbContext.cs", "Data.MyDbContext")]
    [InlineData("namespace Other { class MyDbContext { } } namespace Data { class MyDbContext { } }", "MyDbContext.cs", "Data.MyDbContext")]
    [InlineData("namespace Data; class MyDbContext { }", "ContextTypes.cs", "Data.MyDbContext")]
    [InlineData("class MyDbContext { }", "ContextTypes.cs", "MyDbContext")]
    [InlineData("namespace Data; class Container { public class MyDbContext { } }", "ContextTypes.cs", "Data.Container.MyDbContext")]
    public async Task RunAsync_ModifiesOnlyExactConfiguredClass(string source, string fileName, string classFullName)
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = workspace.AddDocument(project.Id, fileName, SourceText.From(source));
        var config = new CodeFile
        {
            FileName = fileName,
            ClassFullName = classFullName,
            Usings = ["System"],
            ClassProperties = [new CodeBlock { Block = "public int Added { get; set; }" }]
        };

        var builder = new DocumentBuilder(document, config, [], NullLogger.Instance);
        var updatedDocument = await builder.RunAsync();
        var root = await updatedDocument.GetSyntaxRootAsync();
        var semanticModel = await updatedDocument.GetSemanticModelAsync();
        var declarations = root!.DescendantNodes().OfType<ClassDeclarationSyntax>();
        foreach (var declaration in declarations)
        {
            var properties = declaration.Members.OfType<PropertyDeclarationSyntax>();
            if (semanticModel!.GetDeclaredSymbol(declaration)!.ToDisplayString() == classFullName)
            {
                Assert.Equal("Added", Assert.Single(properties).Identifier.ValueText);
            }
            else
            {
                Assert.Empty(properties);
            }
        }
        Assert.Contains("using System;", root.ToFullString());
    }

    [Fact]
    public async Task RunAsync_RejectsMissingExactConfiguredClass()
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = workspace.AddDocument(project.Id, "MyDbContext.cs",
            SourceText.From("namespace Data; class MyDbContextFactory { }"));
        var config = new CodeFile
        {
            FileName = "MyDbContext.cs",
            ClassFullName = "Data.MyDbContext",
            ClassProperties = [new CodeBlock { Block = "public int Added { get; set; }" }]
        };

        var builder = new DocumentBuilder(document, config, [], NullLogger.Instance);
        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => builder.RunAsync());
        Assert.Contains("Data.MyDbContext", exception.Message);
    }
}
