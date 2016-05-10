// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public class ModelType
    {
        public string Namespace { get; set; }

        public string Name { get; set; }

        public string FullName { get; set; }

        // Violating the principle that ModelType should be decoupled from Roslyn's API.
        // I had to do this for editing DbContext scenarios but I need to figure out if there
        // is a better way.
        public ITypeSymbol TypeSymbol { get; private set; }

        public static ModelType FromITypeSymbol(ITypeSymbol typeSymbol)
        {
            if (typeSymbol == null)
            {
                throw new ArgumentNullException(nameof(typeSymbol));
            }

            // Should we check for typeSymbol.IsType before returning here?
            return new ModelType()
            {
                Name = typeSymbol.Name,
                FullName = typeSymbol.ToDisplayString(),
                Namespace = typeSymbol.ContainingNamespace.IsGlobalNamespace
                    ? string.Empty
                    : typeSymbol.ContainingNamespace.ToDisplayString(),
                TypeSymbol = typeSymbol
            };
        }
    }
}