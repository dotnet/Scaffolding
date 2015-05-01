// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.Framework.CodeGeneration
{
    public class ModelType
    {
        public string Namespace { get; set; }

        public string Name { get; set; }

        public string FullName { get; set; }

        public static ModelType FromITypeSymbol([NotNull]ITypeSymbol typeSymbol)
        {
            // Should we check for typeSymbol.IsType before returning here?
            return new ModelType()
            {
                Name = typeSymbol.Name,
                FullName = typeSymbol.ToDisplayString(),
                Namespace = typeSymbol.ContainingNamespace.IsGlobalNamespace
                    ? string.Empty
                    : typeSymbol.ContainingNamespace.ToDisplayString()
            };
        }
    }
}