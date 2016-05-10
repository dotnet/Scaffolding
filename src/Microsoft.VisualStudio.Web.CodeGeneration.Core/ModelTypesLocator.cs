// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public class ModelTypesLocator : IModelTypesLocator
    {
        private ILibraryExporter _libraryExporter;
        private Workspace _projectWorkspace;

        public ModelTypesLocator(
            ILibraryExporter libraryExporter,
            Workspace projectWorkspace)
        {
            if (libraryExporter == null)
            {
                throw new ArgumentNullException(nameof(libraryExporter));
            }

            if (projectWorkspace == null)
            {
                throw new ArgumentNullException(nameof(projectWorkspace));
            }

            _libraryExporter = libraryExporter;
            _projectWorkspace = projectWorkspace;
        }

        public IEnumerable<ModelType> GetAllTypes()
        {

            return _projectWorkspace.CurrentSolution.Projects
                .Select(project => project.GetCompilationAsync().Result)
                .Select(comp => RoslynUtilities.GetDirectTypesInCompilation(comp))
                .Aggregate((col1, col2) => col1.Concat(col2).ToList())
                .Select(ts => ModelType.FromITypeSymbol(ts));

        }

        public IEnumerable<ModelType> GetType(string typeName)
        {
            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            var exactTypesInAllProjects = _projectWorkspace
                .CurrentSolution.Projects
                .Select(project => project.GetCompilationAsync().Result)
                .Select(comp => comp.Assembly.GetTypeByMetadataName(typeName) as ITypeSymbol)
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