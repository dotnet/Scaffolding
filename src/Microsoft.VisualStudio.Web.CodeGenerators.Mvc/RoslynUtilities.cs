// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    internal class RoslynUtilities
    {
        private static bool IsKeyWord(string identifier)
        {
            if (SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None
                || SyntaxFacts.GetContextualKeywordKind(identifier) != SyntaxKind.None)
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Creates an escaped identifier if the identifier is a keyword (or contextual keyword) in C#.
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public static string CreateEscapedIdentifier(string identifier)
        {
            if(identifier == null)
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
    }
}