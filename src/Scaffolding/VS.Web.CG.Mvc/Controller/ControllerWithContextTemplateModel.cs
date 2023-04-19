// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Controller
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
        public bool NullableEnabled { get; set; }

        public string ControllerNamespace { get; set; }

        public IModelMetadata ModelMetadata { get; set; }

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
