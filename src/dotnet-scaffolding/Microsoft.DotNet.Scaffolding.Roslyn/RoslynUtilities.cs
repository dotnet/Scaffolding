// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.DotNet.Scaffolding.Roslyn;

public static class RoslynUtilities
{
    /// <summary>
    /// Given a document, checks if the document contains a method invocation with the given method name and containing type (string).
    /// </summary>
    /// <param name="document">CodeAnalysis.Document object</param>
    /// <param name="methodName">name of the method being invoked</param>
    /// <param name="methodContainingType">name of the type invoking the methodName</param>
    /// <returns></returns>
    public static async Task<bool> CheckDocumentForMethodInvocationAsync(Document document, string methodName, string methodContainingType)
    {
        if (document is null || string.IsNullOrEmpty(methodName) || string.IsNullOrEmpty(methodContainingType))
        {
            return false;
        }

        var programSyntaxRoot = await document.GetSyntaxRootAsync();
        var programSyntaxTree = programSyntaxRoot?.SyntaxTree;
        if (programSyntaxRoot is null || programSyntaxTree is null)
        {
            return false;
        }

        var matchingInvExpressions = programSyntaxRoot.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(x => x.ToFullString().Contains(methodName));
        if (matchingInvExpressions is null || !matchingInvExpressions.Any())
        {
            return false;
        }

        var semanticModel = await document.GetSemanticModelAsync();
        foreach (var expr in matchingInvExpressions)
        {
            var symbol = semanticModel?.GetSymbolInfo(expr.Expression).Symbol as IMethodSymbol;
            if (symbol is not null && SymbolMatchesType(symbol, methodName, methodContainingType))
            {
                return true;
            }
        }

        return false;
    }

    public static async Task<bool> CheckDocumentForTextAsync(Document document, string text)
    {
        if (document is null || string.IsNullOrEmpty(text))
        {
            return false;
        }

        var fileText = (await document.GetTextAsync()).ToString();
        if (string.IsNullOrEmpty(fileText))
        {
            return false;
        }

        return fileText.Contains(text, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool SymbolMatchesType(IMethodSymbol symbol, string methodName, string methodContainingType)
    {
        return SymbolMatchesType(symbol, methodName, new string[] { methodContainingType });
    }

    internal static bool SymbolMatchesType(IMethodSymbol symbol, string methodName, string[] methodContainingTypes)
    {
        if (symbol is null || string.IsNullOrEmpty(methodName) || methodContainingTypes is null || !methodContainingTypes.Any())
        {
            return false;
        }

        if (symbol.IsExtensionMethod)
        {
            if ((methodName is null || string.Equals(symbol.Name, methodName, StringComparison.Ordinal)) && symbol.ReducedFrom is not null)
            {
                var methodContainingTypeIntersections = methodContainingTypes.Intersect(symbol.ReducedFrom.Parameters.Select(x => x.Type.ToString()), StringComparer.Ordinal);
                return methodContainingTypeIntersections.Any();
            }
        }
        else
        {
            if ((methodName is null || string.Equals(symbol.Name, methodName, StringComparison.Ordinal)) &&
                methodContainingTypes.Contains(symbol.ContainingSymbol?.ToString(), StringComparer.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    internal static void CollectTypes(INamespaceSymbol ns, List<ITypeSymbol> types)
    {
        types.AddRange(ns.GetTypeMembers().Cast<ITypeSymbol>());

        foreach (var nestedNs in ns.GetNamespaceMembers())
        {
            CollectTypes(nestedNs, types);
        }
    }

    internal static bool IsKeyWord(string identifier)
    {
        if (SyntaxFacts.GetKeywordKind(identifier) is not SyntaxKind.None ||
            SyntaxFacts.GetContextualKeywordKind(identifier) is not SyntaxKind.None)
        {
            return true;
        }

        return false;
    }
}

