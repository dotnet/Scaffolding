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

            var classNameModel = TypeUtilities.GetTypeNameandNamespace(dbContextName);

            DbContextTypeName = classNameModel.ClassName;
            DbContextNamespace = classNameModel.NamespaceName;

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