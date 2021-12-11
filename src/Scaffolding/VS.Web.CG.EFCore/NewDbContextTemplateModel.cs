// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Shared.Project;
    
namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore
{
    public class NewDbContextTemplateModel
    {
        public NewDbContextTemplateModel(string dbContextName, ModelType modelType, ModelType programType, bool nullableEnabled)
        {
            if (dbContextName == null)
            {
                throw new ArgumentNullException(nameof(dbContextName));
            }

            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            if (programType == null)
            {
                throw new ArgumentNullException(nameof(programType));
            }

            var modelNamespace = modelType.Namespace;

            ModelTypeName = modelType.Name;
            ModelTypeFullName = modelType.FullName;
            ProgramTypeName = programType.Name;
            ProgramNamespace = programType.Namespace;
            RequiredNamespaces = new HashSet<string>();
            NullableEnabled = nullableEnabled;
            var classNameModel = new ClassNameModel(dbContextName);

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
        public string ModelTypeFullName { get; private set; }

        public string ProgramTypeName { get; private set; }
        public string ProgramNamespace { get; private set; }

        public HashSet<string> RequiredNamespaces { get; private set; }
        public bool NullableEnabled { get; set; }
    }
}
