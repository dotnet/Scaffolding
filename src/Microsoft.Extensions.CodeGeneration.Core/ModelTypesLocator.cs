// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.ProjectModel.Workspaces;
using Microsoft.Extensions.CodeGeneration.DotNet;

namespace Microsoft.Extensions.CodeGeneration
{
    public class ModelTypesLocator : IModelTypesLocator
    {
        private IApplicationEnvironment _application;
        private ILibraryExporter _libraryExporter;
        private Workspace _projectWorkspace;

        public ModelTypesLocator(
            ILibraryExporter libraryExporter,
            IApplicationEnvironment application,
            Workspace projectWorkspace)
        {
            if (libraryExporter == null)
            {
                throw new ArgumentNullException(nameof(libraryExporter));
            }

            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            if (projectWorkspace == null)
            {
                throw new ArgumentNullException(nameof(projectWorkspace));
            }

            _libraryExporter = libraryExporter;
            _application = application;
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

            IEnumerable<ITypeSymbol> exactTypesInAllProjects = _projectWorkspace.CurrentSolution.Projects
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