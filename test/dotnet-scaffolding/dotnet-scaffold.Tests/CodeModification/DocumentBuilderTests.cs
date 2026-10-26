// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.DotNet.Scaffolding.CodeModification;
using Microsoft.DotNet.Scaffolding.CodeModification.CodeChange;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.CodeModification;

public class DocumentBuilderTests
{
    [Fact]
    public void InvocationArguments_BindNamedAndPositionalOverloadsAndDeduplicateSchemes()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            class Registration
            {
                static void AddIdentity(object configuration, string scheme = "Default") { }
                static void AddIdentity(object configuration, object cookies, string scheme = "Default") { }
                static void Configure()
                {
                    AddIdentity(new object());
                    AddIdentity(new object(), "Positional");
                    AddIdentity(new object(), new object(), "OtherOverload");
                    AddIdentity(new object(), scheme: "Named");
                    AddIdentity(new object(), scheme: "Named");
                }
            }
            """);
        var compilation = CSharpCompilation.Create("Test", [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var snippet = CreateSnippet();
        snippet.Parent = "AddIdentity";
        snippet.InvocationArguments = new() { ["scheme"] = "\"Default\"" };
        snippet.Block = "Configure($scheme$);";

        var expanded = DocumentBuilder.ExpandInvocationArguments([snippet], tree.GetRoot(), compilation.GetSemanticModel(tree));

        Assert.Equal(["Configure(\"Default\");", "Configure(\"Positional\");", "Configure(\"OtherOverload\");", "Configure(\"Named\");"],
            expanded.Select(change => change.Block).ToArray());
        expanded[0].LeadingTrivia.NumberOfSpaces = 8;
        Assert.Equal(0, expanded[1].LeadingTrivia.NumberOfSpaces);
        Assert.Equal(0, snippet.LeadingTrivia.NumberOfSpaces);
    }

    [Fact]
    public void InvocationArguments_UseDefaultWhenNoInvocationExists()
    {
        var expanded = DocumentBuilder.ExpandInvocationArguments(
            [CreateSnippet()], CSharpSyntaxTree.ParseText("Console.WriteLine(\"AddIdentity\");").GetRoot(), null);

        Assert.Equal("Configure(\"Default\");", Assert.Single(expanded).Block);
    }

    [Fact]
    public void InvocationArguments_DoNotSilentlyUseDefaultForUnresolvedPositionalArguments()
    {
        Assert.Throws<InvalidOperationException>(() => DocumentBuilder.ExpandInvocationArguments(
            [CreateSnippet()], CSharpSyntaxTree.ParseText("AddIdentity(null, \"Custom\");").GetRoot(), null));
    }

    private static CodeSnippet CreateSnippet() => new()
    {
        Parent = "AddIdentity",
        InvocationArguments = new() { ["scheme"] = "\"Default\"" },
        Block = "Configure($scheme$);"
    };
}
