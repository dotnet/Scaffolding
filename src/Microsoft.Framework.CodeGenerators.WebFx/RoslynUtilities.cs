// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.Framework.CodeGeneration;

namespace Microsoft.Framework.CodeGenerators.WebFx
{
    internal static class RoslynUtilities
    {
        // ToDo: Perhaps find some existing utility in Roslyn or provide this as API?
        public static string FullNameForSymbol([NotNull]this ISymbol symbol)
        {
            if (symbol.ContainingNamespace != null & string.IsNullOrEmpty(symbol.ContainingNamespace.Name))
            {
                return symbol.Name;
            }
            return FullNameForSymbol(symbol.ContainingNamespace) + "." + symbol.Name;
        }
    }
}