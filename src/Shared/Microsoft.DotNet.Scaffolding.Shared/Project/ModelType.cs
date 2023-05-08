// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Humanizer;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Scaffolding.Shared.Project
{
    public class ModelType
    {
        public string Namespace { get; set; }

        public string Name { get; set; }

        public string FullName { get; set; }

        public string PluralName => Name?.Pluralize(inputIsKnownToBeSingular: false);
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
