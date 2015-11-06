// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.CompilationAbstractions;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.Extensions.CodeGeneration
{
    public class ModelTypesLocator : IModelTypesLocator
    {
        private IApplicationEnvironment _application;
        private ILibraryExporter _libraryExporter;

        public ModelTypesLocator(
            [NotNull]ILibraryExporter libraryExporter,
            [NotNull]IApplicationEnvironment application)
        {
            _libraryExporter = libraryExporter;
            _application = application;
        }

        public IEnumerable<ModelType> GetAllTypes()
        {
            return _libraryExporter
                .GetProjectsInApp(_application)
                .Select(compilation => RoslynUtilities.GetDirectTypesInCompilation(compilation.Compilation))
                .Aggregate((coll1, coll2) => coll1.Concat(coll2).ToList())
                .Select(ts => ModelType.FromITypeSymbol(ts));
        }

        public IEnumerable<ModelType> GetType([NotNull]string typeName)
        {
            var exactTypesInAllProjects = _libraryExporter
                .GetProjectsInApp(_application)
                .Select(comp => comp.Compilation.Assembly.GetTypeByMetadataName(typeName) as ITypeSymbol)
                .Where(type => type != null);

            if (exactTypesInAllProjects.Any())
            {
                return exactTypesInAllProjects.Select(ts => ModelType.FromITypeSymbol(ts));
            }

            //For short type names, we don't give special preference to types in current app,
            //should we do that?
            return GetAllTypes()
                .Where(type => string.Equals(type.Name, typeName, StringComparison.Ordinal));
        }
    }
}