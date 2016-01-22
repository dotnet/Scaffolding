// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.CodeGeneration;
using Microsoft.Extensions.CodeGeneration.EntityFrameworkCore;

namespace Microsoft.Extensions.CodeGenerators.Mvc.Controller
{
    public class ControllerWithContextTemplateModel
    {
        public ControllerWithContextTemplateModel(
            ModelType modelType,
            string dbContextFullTypeName)
        {
            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            if (dbContextFullTypeName == null)
            {
                throw new ArgumentNullException(nameof(dbContextFullTypeName));
            }

            ModelType = modelType;

            var classNameModel = new ClassNameModel(dbContextFullTypeName);

            ContextTypeName = classNameModel.ClassName;
            DbContextNamespace = classNameModel.NamespaceName;
        }

        public string ControllerName { get; set; }

        public string AreaName { get; set; }

        public bool UseAsync { get; set; }

        public string ControllerNamespace { get; set; }

        public ModelMetadata ModelMetadata { get; set; }

        public ModelType ModelType { get; private set; }

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
                return RoslynUtilities.CreateEscapedIdentifier(ModelTypeName.ToLowerInvariantFirstChar());
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

                var modelTypeNamespace = ModelType.Namespace;

                if (!string.IsNullOrWhiteSpace(modelTypeNamespace))
                {
                    requiredNamespaces.Add(modelTypeNamespace);
                }

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