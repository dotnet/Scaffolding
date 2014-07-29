// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.Framework.CodeGeneration;
using Microsoft.Framework.CodeGeneration.EntityFramework;

namespace Microsoft.Framework.CodeGenerators.WebFx
{
    public class ControllerGeneratorTemplateModel
    {
        public ControllerGeneratorTemplateModel(
            [NotNull]ITypeSymbol modelType,
            [NotNull]string dbContextFullTypeName)
        {
            ModelType = modelType;

            string typeName, namespaceName;
            TypeUtil.GetTypeNameandNamespace(dbContextFullTypeName, out typeName, out namespaceName);

            ContextTypeName = typeName;
            DbContextNamespace = namespaceName;
        }

        public string ControllerName { get; set; }

        public string AreaName { get; set; }

        public bool UseAsync { get; set; }

        public ModelMetadata ModelMetadata { get; set; }

        public ITypeSymbol ModelType { get; private set; }

        public string ControllerRootName
        {
            get
            {
                if (!string.IsNullOrEmpty(ControllerName))
                {
                    if (ControllerName.EndsWith(Constants.ControllerSuffix, StringComparison.Ordinal))
                    {
                        return ControllerName.Substring(0, ControllerName.Length - Constants.ControllerSuffix.Length);
                    }
                }
                return ControllerName;
            }
        }

        public string ControllerNamespace
        {
            get
            {
                // Review: MVC scaffolding used ActiveProject's MSBuild RootNamespace property
                // That's not possible in command line scaffolding - the closest we can get is
                // the name of assembly??
                return ModelType.ContainingAssembly.Name + "." + Constants.ControllersFolderName;
            }
        }

        public string ContextTypeName { get; private set; }

        public string ModelTypeName
        {
            get
            {
                return ModelType.Name;
            }
        }

        public string ModelVariable
        {
            get
            {
                // ToDo: This did CodeDomProvider.CreateEscapedIdentifier in MVC, do we need that?
                return ModelTypeName.ToLowerInvariantFirstChar();
            }
        }

        public string EntitySetVariable
        {
            get
            {
                return ContextTypeName.ToLowerInvariantFirstChar();
            }
        }

        public HashSet<string> RequiredNamespaces
        {
            get
            {
                var requiredNamespaces = new SortedSet<string>(StringComparer.Ordinal);
                // We add ControllerNamespace first to make other entries not added to the set if they match.
                requiredNamespaces.Add(ControllerNamespace);
                requiredNamespaces.Add(ModelType.ContainingNamespace.FullNameForSymbol());
                if (!string.IsNullOrWhiteSpace(DbContextNamespace))
                {
                    requiredNamespaces.Add(DbContextNamespace);
                }

                // Finally we remove the ControllerNamespace as it's not required.
                requiredNamespaces.Remove(ControllerNamespace);
                return new HashSet<string>(requiredNamespaces);
            }
        }

        public string DbContextNamespace { get; private set; }
    }
}