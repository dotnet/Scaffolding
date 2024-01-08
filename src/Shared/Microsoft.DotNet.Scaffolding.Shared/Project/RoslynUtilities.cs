// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.DotNet.Scaffolding.Shared.Project
{
    internal static class RoslynUtilities
    {
        public static IEnumerable<ITypeSymbol> GetDirectTypesInCompilation(CodeAnalysis.Compilation compilation)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }
            var types = new List<ITypeSymbol>();
            CollectTypes(compilation.Assembly.GlobalNamespace, types);
            return types;
        }

        /// <summary>
        /// Creates an escaped identifier if the identifier is a keyword (or contextual keyword) in C#.
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public static string CreateEscapedIdentifier(string identifier)
        {
            if (identifier == null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }
            return IsKeyWord(identifier) ? $"@{identifier}" : identifier;
        }

        public static bool IsValidNamespace(string namespaceName)
        {
            if (namespaceName == null)
            {
                throw new ArgumentNullException(nameof(namespaceName));
            }

            if (IsKeyWord(namespaceName))
            {
                return false;
            }

            var parts = namespaceName.Split('.');
            foreach (var part in parts)
            {
                if (!SyntaxFacts.IsValidIdentifier(part))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Given a document, checks if the document contains a method invocation with the given method name and containing type (string).
        /// </summary>
        /// <param name="document">CodeAnalysis.Document object</param>
        /// <param name="methodName">name of the method being invoked</param>
        /// <param name="methodContainingType">name of the type invoking the methodName</param>
        /// <returns></returns>
        internal static async Task<bool> CheckDocumentForMethodInvocationAsync(Document document, string methodName, string methodContainingType)
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

        internal static async Task<bool> CheckDocumentForTextAsync(Document document, string text)
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

            return fileText.ContainsIgnoreCase(text);
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
                if ((methodName is null || string.Equals(symbol.Name, methodName, StringComparison.Ordinal)))
                {
                    var methodContainingTypeIntersections = methodContainingTypes.Intersect(symbol.ReducedFrom?.Parameters.Select(x => x.Type.ToString()), StringComparer.Ordinal);
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
            if (SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None || 
                SyntaxFacts.GetContextualKeywordKind(identifier) != SyntaxKind.None)
            {
                return true;
            }

            return false;
        }
    }
}
