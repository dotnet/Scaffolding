// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Xml;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.DotNet.Scaffolding.CodeModification;

internal static class ClassModifier
{
    public static CompilationUnitSyntax AddUsings(CompilationUnitSyntax docRoot, string[] usings)
    {
        var usingNodes = CreateUsings(usings);
        if (usingNodes.Length != 0 && docRoot.Usings.Count == 0)
        {
            return docRoot.WithUsings(SyntaxFactory.List(usingNodes));
        }
        else
        {
            var uniqueUsings = GetUniqueUsings(docRoot.Usings.ToArray(), usingNodes);
            return uniqueUsings.Any() ? docRoot.WithUsings(docRoot.Usings.AddRange(uniqueUsings)) : docRoot;
        }
    }

    public static UsingDirectiveSyntax[] CreateUsings(string[] usings)
    {
        var usingDirectiveList = new List<UsingDirectiveSyntax>();
        if (usings is null)
        {
            return usingDirectiveList.ToArray();
        }

        var nameLeadingTrivia = SyntaxFactory.TriviaList(SyntaxFactory.Space);
        var usingTrailingTrivia = SyntaxFactory.TriviaList(SyntaxFactory.CarriageReturnLineFeed);
        foreach (var usingDirectiveString in usings)
        {
            if (!string.IsNullOrEmpty(usingDirectiveString) && !usingDirectiveString.Equals("<global namespace>", StringComparison.OrdinalIgnoreCase))
            {
                //leading space on the value of the using eg. (using' 'Microsoft.Yadada)
                var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(usingDirectiveString)
                    .WithLeadingTrivia(nameLeadingTrivia))
                    .WithAdditionalAnnotations(Formatter.Annotation)
                    .WithTrailingTrivia(usingTrailingTrivia);

                usingDirectiveList.Add(usingDirective);
            }
        }

        return usingDirectiveList.ToArray();
    }

    public static SyntaxList<UsingDirectiveSyntax> GetUniqueUsings(UsingDirectiveSyntax[] existingUsings, UsingDirectiveSyntax[] newUsings)
    {
        return SyntaxFactory.List(
            newUsings.Where(u => u.Name != null && !existingUsings.Any(oldUsing => oldUsing.Name != null && oldUsing.Name.ToString().Equals(u.Name.ToString())))
                     .OrderBy(us => us.Name?.ToString()));
    }

    public static ClassDeclarationSyntax GetNewStaticClassDeclaration(string className)
    {
        return SyntaxFactory.ClassDeclaration(className)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
            .NormalizeWhitespace()
            .WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed, SyntaxFactory.CarriageReturnLineFeed);
    }

    public static CompilationUnitSyntax GetNewStaticCompilationUnit(string className)
    {
        var classDeclaration = GetNewStaticClassDeclaration(className);
        return SyntaxFactory.CompilationUnit()
              .AddMembers(classDeclaration);
    }

    public static void RemoveCompileIncludes(string? projectCsprojPath, List<string> excludeList)
    {
        if (string.IsNullOrEmpty(projectCsprojPath))
        {
            return;
        }

        // Load the project file
        var doc = new XmlDocument();
        doc.Load(projectCsprojPath);

        if (doc.DocumentElement is null)
        {
            return;
        }

        // Get the default namespace for the project file
        var nsMgr = new XmlNamespaceManager(doc.NameTable);
        nsMgr.AddNamespace("ns", doc.DocumentElement.NamespaceURI);

        // Find all <Compile> elements to remove
        foreach (var include in excludeList)
        {
            var compileNode = doc.SelectSingleNode(
                $"/ns:Project/ns:ItemGroup/ns:Compile[@Include='{include}']",
                nsMgr);

            // If the <Compile> element exists, remove it
            if (compileNode != null && compileNode.ParentNode != null)
            {
                compileNode.ParentNode.RemoveChild(compileNode);
            }
        }

        // Remove empty <ItemGroup> elements
        var itemGroups = doc.SelectNodes("/ns:Project/ns:ItemGroup", nsMgr);
        if (itemGroups != null && itemGroups.Count != 0)
        {
            foreach (XmlNode itemGroup in itemGroups)
            {
                if (itemGroup.ChildNodes.Count == 0 && itemGroup.ParentNode != null)
                {
                    itemGroup.ParentNode.RemoveChild(itemGroup);
                }
            }
        }

        // Save the modified project file
        doc.Save(projectCsprojPath);
    }
}
