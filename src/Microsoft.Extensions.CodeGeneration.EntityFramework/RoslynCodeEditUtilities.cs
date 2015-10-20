// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Extensions.CodeGeneration.EntityFramework
{
    internal static class RoslynCodeEditUtilities
    {
        /// <summary>
        /// Adds a using directive if one doesn't already exist at the top of file
        /// after existing using directives.
        /// 
        /// Does not handle the scenarios where usings are defined within an inner node of
        /// given root node, ex, if the root node is CompilationUnit and usings are defined
        /// within a Namespace Declaration instead of top of the file, the new using is
        /// just added at the top of the file.
        /// </summary>
        /// <param name="namespaceName">The namespace to be added.</param>
        /// <param name="rootNode">Parent syntax node for which the childs are examined
        /// to see if a using with the given namespace already exists</param>
        /// <returns>A new syntax node containing the new using statement as an immediate
        /// child of given rootNode. If the using statement is already present, the rootNode
        /// is returned. Otherwise, a new statement is added at the end of existing
        /// usings and the new node is returned.</returns>
        public static SyntaxNode AddUsingDirectiveIfNeeded(string namespaceName, SyntaxNode rootNode)
        {
            Contract.Assert(rootNode != null);

            UsingDirectiveSyntax lastUsingNode = null;
            foreach (var usingNode in rootNode.ChildNodes().Where(n => n is UsingDirectiveSyntax))
            {
                if (((UsingDirectiveSyntax)usingNode).Name.ToString() == namespaceName)
                {
                    // Using already present, return this node
                    return rootNode;
                }
                lastUsingNode = (UsingDirectiveSyntax)usingNode;
            }

            var insertTree = CSharpSyntaxTree.ParseText("using " + namespaceName + ";" + Environment.NewLine);

            // Using not present, add to the last node
            if (lastUsingNode != null)
            {
                return rootNode.InsertNodesAfter(lastUsingNode, insertTree.GetRoot().ChildNodes());
            }
            else
            {
                return rootNode.InsertNodesBefore(rootNode.ChildNodes().First(), insertTree.GetRoot().ChildNodes());
            }
        }
    }
}
