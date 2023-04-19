// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

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

        private static void CollectTypes(INamespaceSymbol ns, List<ITypeSymbol> types)
        {
            types.AddRange(ns.GetTypeMembers().Cast<ITypeSymbol>());

            foreach (var nestedNs in ns.GetNamespaceMembers())
            {
                CollectTypes(nestedNs, types);
            }
        }
    }
}
