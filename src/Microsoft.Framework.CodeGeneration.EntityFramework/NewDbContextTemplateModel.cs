// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.Framework.CodeGeneration.EntityFramework
{
    public class NewDbContextTemplateModel
    {
        public NewDbContextTemplateModel([NotNull]string dbContextName, [NotNull]ITypeSymbol modelType)
        {
            var modelNamespace = modelType.ContainingNamespace.ToDisplayString();
            ModelTypeName = modelType.Name;
            RequiredNamespaces = new HashSet<string>();

            string typeName, namespaceName;
            TypeUtilities.GetTypeNameandNamespace(dbContextName, out typeName, out namespaceName);

            DbContextTypeName = typeName;
            DbContextNamespace = namespaceName;

            if (!string.Equals(modelNamespace, DbContextNamespace, StringComparison.Ordinal))
            {
                RequiredNamespaces.Add(modelNamespace);
            }
        }

        public string DbContextTypeName { get; private set; }

        public string DbContextNamespace { get; private set; }

        public string ModelTypeName { get; private set; }

        public HashSet<string> RequiredNamespaces { get; private set; }
    }
}