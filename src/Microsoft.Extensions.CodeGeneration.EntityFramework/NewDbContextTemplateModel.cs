// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.CodeGeneration.EntityFramework
{
    public class NewDbContextTemplateModel
    {
        public NewDbContextTemplateModel([NotNull]string dbContextName, [NotNull]ModelType modelType)
        {
            var modelNamespace = modelType.Namespace;

            ModelTypeName = modelType.Name;
            RequiredNamespaces = new HashSet<string>();

            var classNameModel = TypeUtilities.GetTypeNameandNamespace(dbContextName);

            DbContextTypeName = classNameModel.ClassName;
            DbContextNamespace = classNameModel.NamespaceName;

            if (!string.IsNullOrEmpty(modelNamespace) &&
                !string.Equals(modelNamespace, DbContextNamespace, StringComparison.Ordinal))
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