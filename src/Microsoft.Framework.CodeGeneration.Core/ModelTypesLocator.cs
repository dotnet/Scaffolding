// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Roslyn;

namespace Microsoft.Framework.CodeGeneration
{
    public class ModelTypesLocator : IModelTypesLocator
    {
        private IApplicationEnvironment _application;
        private ILibraryManager _libraryManager;

        public ModelTypesLocator(
            [NotNull]ILibraryManager libraryManager,
            [NotNull]IApplicationEnvironment application)
        {
            _libraryManager = libraryManager;
            _application = application;
        }

        public IEnumerable<ITypeSymbol> GetAllTypes()
        {
            return _libraryManager
                .GetProjectsInApp(_application)
                .Select(compilation => GetDirectTypesInCompilation(compilation.Compilation))
                .Aggregate((coll1, coll2) => coll1.Concat(coll2).ToList());
        }

        public IEnumerable<ITypeSymbol> GetType([NotNull]string typeName)
        {
            var exactTypesInAllProjects = _libraryManager
                .GetProjectsInApp(_application)
                .Select(comp => comp.Compilation.Assembly.GetTypeByMetadataName(typeName) as ITypeSymbol)
                .Where(type => type != null);

            if (exactTypesInAllProjects.Any())
            {
                return exactTypesInAllProjects;
            }

            //For short type names, we don't give special preference to types in current app,
            //should we do that?
            return GetAllTypes()
                .Where(type => string.Equals(type.Name, typeName, StringComparison.Ordinal));
        }

        private IEnumerable<ITypeSymbol> GetDirectTypesInCompilation([NotNull]Compilation compilation)
        {
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