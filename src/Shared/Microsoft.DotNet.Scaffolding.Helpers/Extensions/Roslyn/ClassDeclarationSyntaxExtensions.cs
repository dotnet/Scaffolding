// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Scaffolding.Helpers.Extensions.Roslyn
{
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

        public static IEnumerable<MethodDeclarationSyntax> GetPublicMethods(this ClassDeclarationSyntax classNode)
        {
            return classNode
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(x =>
                    x.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword) &&
                    !x.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword))));
        }

        public static bool IsStaticClass(this ClassDeclarationSyntax? classDeclaration)
        {
            return classDeclaration is not null ?
                classDeclaration.Modifiers.Any(x => x.Text.Equals(SyntaxFactory.Token(SyntaxKind.StaticKeyword).Text)) :
                false;
        }
    }
}
