// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public class ModelTypesLocator : IModelTypesLocator
    {
        private Workspace _projectWorkspace;

        public ModelTypesLocator(
            Workspace projectWorkspace)
        {
            _projectWorkspace = projectWorkspace ?? throw new ArgumentNullException(nameof(projectWorkspace));
        }
        
        public IEnumerable<ModelType> GetAllTypes()
        {
            return _projectWorkspace.CurrentSolution.Projects
                .Select(project => project.GetCompilationAsync().Result)
                .Select(comp => RoslynUtilities.GetDirectTypesInCompilation(comp))
                .Aggregate((col1, col2) => col1.Concat(col2).ToList())
                .Select(ts => ModelType.FromITypeSymbol(ts));
        }

        public IEnumerable<Document> GetAllDocuments()
        {
            var documents = new List<Document>();
            var allDocuments =  _projectWorkspace.CurrentSolution.Projects
                .Select(project => project.Documents);
            foreach (var documentList in allDocuments)
            {
                documents.AddRange(documentList);
            }
            return documents;
        }

        public IEnumerable<ModelType> GetType(string typeName)
        {
            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            var temp2 = _projectWorkspace
                .CurrentSolution.Projects
                .Select(project => project.GetCompilationAsync().Result).First().Assembly;

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
            var allTypes = GetAllTypes();
            return GetAllTypes()
                .Where(type => string.Equals(type.Name, typeName, StringComparison.Ordinal));
        }

        private class TypeSymbolEqualityComparer : IEqualityComparer<ITypeSymbol>
        {
            public bool Equals(ITypeSymbol x, ITypeSymbol y)
            {
                if (Object.ReferenceEquals(x, y))
                {
                    return true;
                }
                if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                {
                    return false;
                }
                
                //Check for namespace to be the same.
                var isNamespaceEqual = (Object.ReferenceEquals(x.ContainingNamespace, y.ContainingNamespace)
                        || ((x.ContainingNamespace != null && y.ContainingNamespace != null)
                            && (x.ContainingNamespace.Name == y.ContainingNamespace.Name)));
                //Check for assembly to be the same.
                var isAssemblyEqual = (object.ReferenceEquals(x.ContainingAssembly, y.ContainingAssembly)
                        || ((x.ContainingAssembly != null && y.ContainingAssembly != null)
                            && (x.ContainingAssembly.Name == y.ContainingAssembly.Name)));

                return x.Name == y.Name
                    && isNamespaceEqual
                    && isAssemblyEqual;

            }

            public int GetHashCode(ITypeSymbol obj)
            {
                if(Object.ReferenceEquals(obj, null))
                {
                    return 0;
                }
                var hashName = obj.Name == null ? 0 : obj.Name.GetHashCode();
                var hashNamespace = obj.ContainingNamespace?.Name == null ? 0 : obj.ContainingNamespace.Name.GetHashCode();
                var hashAssembly = obj.ContainingAssembly?.Name == null ? 0 : obj.ContainingAssembly.Name.GetHashCode();

                return hashName ^ hashNamespace ^ hashAssembly;
            }
        }
    }
}
