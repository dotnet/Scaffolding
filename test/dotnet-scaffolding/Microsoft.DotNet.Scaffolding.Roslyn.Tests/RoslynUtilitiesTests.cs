// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.DotNet.Scaffolding.Roslyn.Tests;

public class RoslynUtilitiesTests
{
    [Theory]
    [InlineData("builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();", true)]
    [InlineData("AddInteractiveWebAssemblyComponents(builder);", true)]
    [InlineData("builder?.AddInteractiveWebAssemblyComponents();", true)]
    [InlineData("// builder.AddInteractiveWebAssemblyComponents();", false)]
    [InlineData("Console.WriteLine(\"AddInteractiveWebAssemblyComponents()\");", false)]
    [InlineData("builder.AddInteractiveWebAssemblyComponentsCustom();", false)]
    [InlineData("void AddInteractiveWebAssemblyComponents() { }", false)]
    public void CheckSyntaxNodeForMethodInvocation_MatchesOnlyExactInvokedNames(string source, bool expected)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        Assert.Equal(expected, RoslynUtilities.CheckSyntaxNodeForMethodInvocation(root, "AddInteractiveWebAssemblyComponents"));
    }

    [Fact]
    public async Task CheckSyntaxNodeForMethodInvocation_DetectsRegistrationsWithoutReferences()
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("SourceOnly", LanguageNames.CSharp);
        var document = workspace.AddDocument(project.Id, "Program.cs", SourceText.From("""
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddInteractiveWebAssemblyComponents();
            """));
        var root = await document.GetSyntaxRootAsync();

        Assert.Empty(document.Project.MetadataReferences);
        foreach (var methodName in new[] { "AddInteractiveServerComponents", "AddInteractiveWebAssemblyComponents" })
        {
            Assert.False(await RoslynUtilities.CheckDocumentForMethodInvocationAsync(
                document, methodName, "Microsoft.Extensions.DependencyInjection.IRazorComponentsBuilder"));
            Assert.True(RoslynUtilities.CheckSyntaxNodeForMethodInvocation(root, methodName));
        }
    }
}
