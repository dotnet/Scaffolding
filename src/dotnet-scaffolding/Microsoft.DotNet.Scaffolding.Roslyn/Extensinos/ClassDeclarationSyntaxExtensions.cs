// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Scaffolding.Roslyn.Extensions;

public static class ClassDeclarationSyntaxExtensions
{
    /// <summary>
    /// Returns true if class syntax node is based on type node with specified simple name
    /// (we cannot reliably compare full names for SyntaxNodes, we use symbols for full types.
    /// </summary>
    /// <param name="classNode"></param>
    /// <param name="baseTypeSimpleName"></param>
    /// <returns></returns>
    public static bool IsBasedOn(this ClassDeclarationSyntax classNode, string baseTypeSimpleName)
    {
        if (classNode is null)
        {
            return false;
        }

        return classNode.BaseList?.Types.Any(
            x => string.Equals(x.Type.ToString(), baseTypeSimpleName, StringComparison.Ordinal)) == true;
    }

    public static bool IsStaticClass(this ClassDeclarationSyntax? classDeclaration)
    {
        return classDeclaration is not null ?
            classDeclaration.Modifiers.Any(x => x.Text.Equals(SyntaxFactory.Token(SyntaxKind.StaticKeyword).Text)) :
            false;
    }

    public static bool IsInNamespace(this ClassDeclarationSyntax classSyntax, string namespaceName)
    {
        return classSyntax.Ancestors().OfType<NamespaceDeclarationSyntax>().Any(n => n.Name.ToString().Equals(namespaceName)) ||
               classSyntax.Ancestors().OfType<FileScopedNamespaceDeclarationSyntax>().Any(n => n.Name.ToString().Equals(namespaceName));
    }

    public static string? GetStringPropertyValue(this ClassDeclarationSyntax classSyntax, string propertyName)
    {
        var propertySyntax = classSyntax.Members
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault(prop => prop.Identifier.Text.Equals(propertyName));

        var propertyExpression = propertySyntax?.ExpressionBody?.Expression as LiteralExpressionSyntax;
        return propertyExpression?.Token.ValueText;
    }
}
